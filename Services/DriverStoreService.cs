using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using TweakHub.Localization;

namespace TweakHub.Services;

/// <summary>A third-party package in the Windows driver store (oemNN.inf). Bound: some device, present or not, uses it.</summary>
public sealed record DriverPackage(string Inf, string Name, string Provider, string Class, string Version, string Date, bool Bound)
{
    /// <summary>The newer version of the same driver that makes this one obsolete.</summary>
    public string NewerVersion { get; init; } = string.Empty;
}

/// <summary>A removed package kept in the backup folder; Folder is what restoring adds back to the store.</summary>
public sealed record DriverBackup(string Folder, string Inf, string Name, string Provider, string Version, DateTime Removed)
{
    public string Title => $"{Name} {Version} · {Provider}";
    public string RemovedText => Removed.ToString("g", L.Culture);
}

/// <summary>
/// Driver Store Explorer's "Select old drivers": older versions of a driver that a newer version replaces and no device uses.
/// Every package is exported before it is deleted, into a folder only administrators can change, and can be added back.
/// </summary>
public sealed class DriverStoreService
{
    public static DriverStoreService Instance { get; } = new();
    private DriverStoreService() { }

    internal static string BackupRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TweakHub-DriverBackups");
    private static readonly Regex OemInf = new(@"^oem\d{1,5}\.inf$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // Get-WindowsDriver needs administrator rights. Get-PnpDevice also returns devices that are not connected now.
    internal const string InventoryScript = """
        $ErrorActionPreference = 'Stop'
        $bound = @(Get-PnpDevice | Get-PnpDeviceProperty -KeyName DEVPKEY_Device_DriverInfPath -ErrorAction SilentlyContinue | ForEach-Object { [string]$_.Data })
        $packages = @(Get-WindowsDriver -Online | ForEach-Object {
            [pscustomobject]@{ Inf = [string]$_.Driver; Name = [IO.Path]::GetFileName([string]$_.OriginalFileName); Provider = [string]$_.ProviderName
                Class = [string]$_.ClassName; Version = [string]$_.Version; Date = if ($_.Date) { $_.Date.ToString('yyyy-MM-dd') } else { '' } } })
        [pscustomobject]@{ Packages = $packages; Bound = $bound } | ConvertTo-Json -Compress -Depth 3
        """ + PowerShellService.AsciiJson;

    public async Task<IReadOnlyList<DriverPackage>> GetPackagesAsync()
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(InventoryScript, requireAdministrator: true, TimeSpan.FromMinutes(3));
        if (!result.Success) throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.Error) ? result.ExitCode.ToString() : result.Error);
        return ParsePackages(result.Output);
    }

    internal static List<DriverPackage> ParsePackages(string json)
    {
        using var document = JsonDocument.Parse(json);
        var bound = JsonRead.Texts(document.RootElement, "Bound").ToHashSet(StringComparer.OrdinalIgnoreCase);
        return JsonRead.Items(document.RootElement, "Packages")
            .Select(item => new DriverPackage(JsonRead.Text(item, "Inf"), JsonRead.Text(item, "Name"), JsonRead.Text(item, "Provider"),
                JsonRead.Text(item, "Class"), JsonRead.Text(item, "Version"), JsonRead.Text(item, "Date"), false))
            .Where(package => OemInf.IsMatch(package.Inf))
            .Select(package => package with { Bound = bound.Contains(package.Inf) })
            .ToList();
    }

    private static Version VersionOf(DriverPackage package) => Version.TryParse(package.Version, out var version) ? version : new Version(0, 0);

    /// <summary>Per driver (file name, provider, class) every version but the newest, unless a device uses it.</summary>
    internal static List<DriverPackage> OldPackages(IEnumerable<DriverPackage> packages) =>
        packages.Where(package => package.Name.Length > 0)
            .GroupBy(package => (package.Name.ToLowerInvariant(), package.Provider.ToLowerInvariant(), package.Class.ToLowerInvariant()))
            .SelectMany(group =>
            {
                var ordered = group.OrderByDescending(VersionOf).ThenByDescending(package => package.Date, StringComparer.Ordinal).ToList();
                var newest = ordered[0];
                // Same version and date as the newest is a duplicate, not an older driver: which one Windows prefers is unknown.
                return ordered.Skip(1)
                    .Where(package => !package.Bound && (VersionOf(package) < VersionOf(newest) || string.CompareOrdinal(package.Date, newest.Date) < 0))
                    .Select(package => package with { NewerVersion = newest.Version });
            })
            .OrderBy(package => package.Name, StringComparer.OrdinalIgnoreCase).ThenBy(VersionOf)
            .ToList();

    // Shared by delete and restore: the root must be a real folder owned by Administrators (or SYSTEM), never a link,
    // with no inherited rules and no write, delete or permission rights for anyone else.
    private const string RootCheck = """
        $admins = [Security.Principal.SecurityIdentifier]'S-1-5-32-544'
        $root = Join-Path $env:ProgramData 'TweakHub-DriverBackups'
        function Assert-Root {
            $item = Get-Item -LiteralPath $root -Force
            $acl = Get-Acl -LiteralPath $root
            $trusted = 'S-1-5-32-544', 'S-1-5-18'
            # WriteData, AppendData, WriteEA, DeleteChild, WriteAttributes, Delete, WriteDAC, WriteOwner, GENERIC_ALL, GENERIC_WRITE
            $write = 0x2 -bor 0x4 -bor 0x10 -bor 0x40 -bor 0x100 -bor 0x10000 -bor 0x40000 -bor 0x80000 -bor 0x10000000 -bor 0x40000000
            $open = @($acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]) | Where-Object {
                $_.AccessControlType -eq 'Allow' -and $_.IdentityReference.Value -notin $trusted -and ([int64]$_.FileSystemRights -band $write) })
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -or $acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -notin $trusted `
                    -or -not $acl.AreAccessRulesProtected -or $open.Count -gt 0) {
                throw 'The driver backup folder is not protected for Administrators only; nothing was changed.'
            }
        }
        """;

    /// <summary>
    /// Exports each package, then deletes it without /force: Windows refuses a package a device still uses, and that backup
    /// is dropped. One line per package: OK|oemNN.inf or FAIL|oemNN.inf|reason.
    /// </summary>
    internal static string BuildRemoveScript(IEnumerable<DriverPackage> packages)
    {
        var list = packages.ToList();
        if (list.Any(package => !OemInf.IsMatch(package.Inf))) throw new ArgumentException("Only third-party oemNN.inf packages can be removed.");
        var entries = string.Join(",\n    ", list.Select(package =>
            $"@({PowerShellService.Literal(package.Inf)}, {PowerShellService.Literal(string.Join("\t", new[] { package.Name, package.Provider, package.Version }.Select(text => text.Replace('\t', ' '))))})"));
        return RootCheck + $$"""

            $ErrorActionPreference = 'Stop'
            $pnputil = Join-Path $env:windir 'System32\pnputil.exe'
            if (-not (Test-Path -LiteralPath $root)) {
                $acl = New-Object Security.AccessControl.DirectorySecurity
                $acl.SetOwner($admins)
                $acl.SetAccessRuleProtection($true, $false)
                foreach ($sid in 'S-1-5-32-544', 'S-1-5-18') {
                    $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule([Security.Principal.SecurityIdentifier]$sid, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
                }
                # Users may read the backups so TweakHub can list them without elevation.
                $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule([Security.Principal.SecurityIdentifier]'S-1-5-32-545', 'ReadAndExecute', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
                [void][IO.Directory]::CreateDirectory($root, $acl)
            }
            # Also after creating it: a folder that appeared in between is returned as is, without this ACL.
            Assert-Root
            $batch = Join-Path $root (Get-Date -Format 'yyyyMMdd-HHmmss')
            foreach ($entry in @(
                {{entries}}
            )) {
                $inf = $entry[0]
                $target = Join-Path $batch $inf
                try {
                    [void](New-Item -ItemType Directory -Path $target -Force)
                    $null = & $pnputil /export-driver $inf $target
                    if ($LASTEXITCODE -ne 0 -or -not (Get-ChildItem -LiteralPath $target -Filter *.inf -Recurse -File)) { throw "export failed ($LASTEXITCODE)" }
                    [IO.File]::WriteAllText((Join-Path $target 'tweakhub.txt'), $entry[1])
                    $null = & $pnputil /delete-driver $inf
                    if ($LASTEXITCODE -ne 0) { throw "Windows kept the package (pnputil $LASTEXITCODE): a device may still use it." }
                    "OK|$inf"
                } catch {
                    Remove-Item -LiteralPath $target -Recurse -Force -ErrorAction SilentlyContinue
                    "FAIL|$inf|$($_.Exception.Message)"
                }
            }
            if ((Test-Path -LiteralPath $batch) -and -not (Get-ChildItem -LiteralPath $batch -Force)) { Remove-Item -LiteralPath $batch -Force }
            """;
    }

    /// <summary>Removes the packages; returns the ones Windows kept, with the reason.</summary>
    public async Task<IReadOnlyList<string>> RemoveAsync(IReadOnlyList<DriverPackage> packages)
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(BuildRemoveScript(packages), requireAdministrator: true, TimeSpan.FromMinutes(10));
        // Declined UAC or a refused backup folder: no package line at all.
        if (!result.Output.Contains('|')) throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.Error) ? result.ExitCode.ToString() : result.Error.Trim());
        var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var done = lines.Where(line => line.StartsWith("OK|", StringComparison.Ordinal)).Select(line => line[3..]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var reasons = lines.Where(line => line.StartsWith("FAIL|", StringComparison.Ordinal)).Select(line => line.Split('|', 3))
            .Where(parts => parts.Length == 3).ToDictionary(parts => parts[1], parts => parts[2], StringComparer.OrdinalIgnoreCase);
        return packages.Where(package => !done.Contains(package.Inf))
            .Select(package => $"{package.Name} {package.Version}: {reasons.GetValueOrDefault(package.Inf, result.Error.Trim())}").ToList();
    }

    /// <summary>Backups listed from the folder itself (readable without elevation); newest first.</summary>
    public static IReadOnlyList<DriverBackup> LoadBackups(string? root = null)
    {
        root ??= BackupRoot;
        try { return ReadBackups(root); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return []; }
    }

    private static IReadOnlyList<DriverBackup> ReadBackups(string root)
    {
        if (!Directory.Exists(root)) return [];
        var backups = new List<DriverBackup>();
        foreach (var batch in Directory.EnumerateDirectories(root))
        {
            if (!DateTime.TryParseExact(Path.GetFileName(batch), "yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var removed)) continue;
            foreach (var folder in Directory.EnumerateDirectories(batch))
            {
                var info = Path.Combine(folder, "tweakhub.txt");
                if (!OemInf.IsMatch(Path.GetFileName(folder)) || !File.Exists(info)) continue;
                var parts = File.ReadAllText(info).Split('\t');
                if (parts.Length == 3) backups.Add(new(folder, Path.GetFileName(folder), parts[0], parts[1], parts[2], removed));
            }
        }
        return backups.OrderByDescending(backup => backup.Removed).ToList();
    }

    /// <summary>Adds the package back to the store (no /install: it was not in use), then drops the backup.</summary>
    internal static string BuildRestoreScript(string folder) => RootCheck + $$"""

        $ErrorActionPreference = 'Stop'
        Assert-Root
        $folder = [IO.Path]::GetFullPath({{PowerShellService.Literal(folder)}})
        if (-not $folder.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Not a TweakHub driver backup.' }
        for ($path = $folder; $path.Length -gt $root.Length; $path = Split-Path -Parent $path) {
            if ((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Not a TweakHub driver backup.' }
        }
        $null = & (Join-Path $env:windir 'System32\pnputil.exe') /add-driver (Join-Path $folder '*.inf') /subdirs
        # 259: added to the store, no device needed it now (the expected case for an unused package).
        if ($LASTEXITCODE -notin 0, 259, 3010) { throw "pnputil failed with exit code $LASTEXITCODE" }
        Remove-Item -LiteralPath $folder -Recurse -Force
        $batch = Split-Path -Parent $folder
        if (-not (Get-ChildItem -LiteralPath $batch -Force)) { Remove-Item -LiteralPath $batch -Force }
        """;

    public Task<PowerShellResult> RestoreAsync(DriverBackup backup) =>
        PowerShellService.Instance.ExecuteScriptAsync(BuildRestoreScript(backup.Folder), requireAdministrator: true, TimeSpan.FromMinutes(5));
}
