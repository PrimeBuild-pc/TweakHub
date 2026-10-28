using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace TweakHub.Services;

/// <summary>Explicit download, extraction and launch. No code runs during a security scan.</summary>
public sealed class TronService
{
    public const string ProjectUrl = "https://github.com/bmrf/tron";
    private const string Repository = "https://bmrf.org/repos/tron/";
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromMinutes(20) };
    public static TronService Instance { get; } = new(Path.Combine(AppDataPath.BasePath, "Tools", "Tron"), Http);
    private readonly string _directory;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _gate = new(1, 1);
    internal TronService(string directory, HttpClient http) { _directory = directory; _http = http; }
    internal sealed record Package(string File, string Hash, long Size, string Version);
    private sealed class State
    {
        public Package? Package { get; set; }
        public string? ManifestHash { get; set; }
        public int? ProcessId { get; set; }
        public long StartedUtcTicks { get; set; }
    }
    private string StatePath => Path.Combine(_directory, "package.json");
    private string ExtractedPath => Path.Combine(_directory, "Extracted");

    internal static IEnumerable<Package> ListedPackages(string manifest) =>
        Regex.Matches(manifest, @"(?m)^(\d+),([a-fA-F0-9]{64}),(Tron v(\d+\.\d+\.\d+) \(\d{4}-\d{2}-\d{2}\)\.exe)\s*$")
            .Select(match => new Package(match.Groups[3].Value, match.Groups[2].Value.ToLowerInvariant(),
                long.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), match.Groups[4].Value))
            .Where(package => package.Size is > 0 and <= 600_000_000);

    internal static Package LatestPackage(string manifest) =>
        ListedPackages(manifest).OrderByDescending(package => Version.Parse(package.Version)).FirstOrDefault()
            ?? throw new InvalidDataException("No supported Tron package in the publisher's SHA-256 manifest.");

    private async Task<string> ManifestAsync()
    {
        var manifest = await _http.GetStringAsync(Repository + "sha256sums.txt");
        return manifest.Length > 64_000 ? throw new InvalidDataException("Publisher manifest is too large.") : manifest;
    }

    private static async Task<string> HashAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream)).ToLowerInvariant();
    }

    public async Task<string> DownloadAsync()
    {
        if (!await _gate.WaitAsync(0)) throw new InvalidOperationException("Another Tron operation is in progress.");
        try { return await DownloadCoreAsync(); }
        catch (IOException ex) when (IsAntivirusBlock(ex)) { throw new InvalidOperationException(Localization.L.Get("Maintenance:TronBlockedByAntivirus"), ex); }
        finally { _gate.Release(); }
    }

    private async Task<string> DownloadCoreAsync()
    {
        EnsureNotRunning(JsonStore.Load<State>(StatePath));
        var package = LatestPackage(await ManifestAsync());
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, package.File);
        var partial = path + ".partial";
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(20));
            using var response = await _http.GetAsync(Repository + Uri.EscapeDataString(package.File), HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            response.EnsureSuccessStatusCode();
            await using (var source = await response.Content.ReadAsStreamAsync())
            await using (var target = File.Create(partial))
            {
                var buffer = new byte[81920];
                long total = 0;
                int count;
                while ((count = await source.ReadAsync(buffer, deadline.Token)) > 0)
                {
                    total += count;
                    if (total > package.Size) throw new InvalidDataException("Tron package size mismatch.");
                    await target.WriteAsync(buffer.AsMemory(0, count), deadline.Token);
                }
                if (total != package.Size) throw new InvalidDataException("Incomplete Tron package.");
            }
            if (await HashAsync(partial) != package.Hash) throw new InvalidDataException("Tron SHA-256 mismatch; package rejected.");
            File.Move(partial, path, true);
            JsonStore.Save(StatePath, new State { Package = package });
            return $"Tron {package.Version} · SHA-256 {package.Hash}";
        }
        finally { if (File.Exists(partial)) File.Delete(partial); }
    }

    public async Task ExtractAsync()
    {
        if (!await _gate.WaitAsync(0)) throw new InvalidOperationException("Another Tron operation is in progress.");
        try { await ExtractCoreAsync(); }
        catch (IOException ex) when (IsAntivirusBlock(ex)) { throw new InvalidOperationException(Localization.L.Get("Maintenance:TronBlockedByAntivirus"), ex); }
        finally { _gate.Release(); }
    }

    // ERROR_VIRUS_INFECTED / ERROR_VIRUS_DELETED: Defender flags Tron as potentially unwanted software and removes it (seen in a VM).
    internal static bool IsAntivirusBlock(IOException ex) => (ex.HResult & 0xFFFF) is 225 or 226;

    private async Task ExtractCoreAsync()
    {
        var state = JsonStore.Load<State>(StatePath);
        EnsureNotRunning(state);
        var package = state.Package ?? throw new InvalidOperationException("Download Tron first.");
        if (LatestPackage($"{package.Size},{package.Hash},{package.File}") != package) throw new InvalidDataException("Invalid package metadata.");
        var path = Path.Combine(_directory, package.File);
        if (await HashAsync(path) != package.Hash) throw new InvalidDataException("The downloaded Tron package has changed.");
        // A fresh directory prevents unverified leftovers from becoming part of the next run.
        if (Directory.Exists(ExtractedPath)) Directory.Delete(ExtractedPath, true);
        Directory.CreateDirectory(ExtractedPath);
        using var process = Process.Start(new ProcessStartInfo(path)
        {
            WorkingDirectory = ExtractedPath, UseShellExecute = false, Arguments = "-y -o\"" + ExtractedPath + "\""
        }) ?? throw new InvalidOperationException("Cannot start the verified self-extractor.");
        state.ManifestHash = null;
        state.ProcessId = process.Id;
        state.StartedUtcTicks = process.StartTime.ToUniversalTime().Ticks;
        JsonStore.Save(StatePath, state);
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException("Tron extraction failed.");
        var root = FindRoot();
        state.ManifestHash = await HashAsync(CheckedPath(ExtractedPath, Manifest));
        await VerifyFilesAsync(ExtractedPath, root);
        JsonStore.Save(StatePath, state);
    }

    private string FindRoot() => new[] { ExtractedPath, Path.Combine(ExtractedPath, "tron") }
        .FirstOrDefault(root => File.Exists(Path.Combine(root, "tron.bat")) && Directory.Exists(Path.Combine(root, "resources", "functions")))
        ?? throw new InvalidDataException("The complete Tron package (tron.bat and resources) was not found.");

    internal static string CheckedPath(string root, string relative)
    {
        if (Path.IsPathRooted(relative) || relative.Contains(':') || relative.Replace('\\', '/').Split('/')
                .Any(part => part != "." && (part == ".." || part.EndsWith(' ') || part.EndsWith('.'))))
            throw new InvalidDataException("Unsafe Tron manifest path.");
        var full = Path.GetFullPath(Path.Combine(root, relative.Replace('\\', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Tron manifest leaves its package directory.");
        for (var candidate = full; candidate is not null && candidate.Length >= Path.GetFullPath(root).Length; candidate = Path.GetDirectoryName(candidate))
            if ((File.Exists(candidate) || Directory.Exists(candidate)) && (File.GetAttributes(candidate) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Tron manifest refers to a reparse point.");
        return full;
    }

    private const string Manifest = @"integrity_verification\checksums.txt";

    /// <summary>
    /// The real package extracts integrity_verification\ beside tron\ (the folder with tron.bat): manifest paths are
    /// relative to the extraction folder, not to Tron's root.
    /// </summary>
    internal static async Task VerifyFilesAsync(string package, string root)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in await File.ReadAllLinesAsync(CheckedPath(package, Manifest)))
        {
            var match = Regex.Match(line, @"^\d+,([a-fA-F0-9]{64}),(.+)$");
            if (!match.Success) continue; // hashdeep headers and comments are not file entries.
            var path = CheckedPath(package, match.Groups[2].Value);
            // hashdeep also lists the manifest and its signature, hashed before they were final: they are not code.
            if (IsIntegrityMetadata(package, path)) continue;
            if (!seen.Add(path) || !File.Exists(path) || await HashAsync(path) != match.Groups[1].Value.ToLowerInvariant())
                throw new InvalidDataException("An extracted Tron file failed SHA-256 verification.");
        }
        if (!seen.Contains(Path.Combine(root, "tron.bat")) || !seen.Contains(Path.Combine(root, "resources", "functions", "tron_settings.bat")))
            throw new InvalidDataException("Unrecognized or incomplete Tron integrity manifest.");
        var folders = new Stack<string>();
        folders.Push(package);
        while (folders.TryPop(out var folder))
        foreach (var entry in Directory.EnumerateFileSystemEntries(folder))
        {
            var path = CheckedPath(package, Path.GetRelativePath(package, entry)); // Reject links before entering a subdirectory.
            if (Directory.Exists(path)) { folders.Push(path); continue; }
            if (!seen.Contains(path) && !IsIntegrityMetadata(package, path))
                throw new InvalidDataException("Tron package contains unverified extra files. Extract a fresh copy.");
        }
    }

    private static bool IsIntegrityMetadata(string package, string path) =>
        Path.GetRelativePath(package, path).StartsWith("integrity_verification" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        && Path.GetExtension(path).ToLowerInvariant() is ".txt" or ".asc" or ".gpg" or ".md" or ".sig";

    internal const string RunArguments = "/d /k tron.bat -p -spr -m -sor -se -scs";

    /// <summary>
    /// The extracted copy in the user's folder can be swapped while Tron runs elevated for hours. The elevated script, which
    /// travels on the command line, copies the package into a new folder only administrators can write, checks the copy's
    /// hash, extracts it there and starts Tron from there. Older copies of it are removed first.
    /// </summary>
    internal static string BuildRunScript(string packagePath, string hash) => $$"""
        $ErrorActionPreference = 'Stop'
        try {
            $admins = [Security.Principal.SecurityIdentifier]'S-1-5-32-544'
            Get-ChildItem -LiteralPath $env:ProgramData -Directory -Filter 'TweakHub-Tron-*' -Attributes !ReparsePoint |
                Where-Object { (Get-Acl -LiteralPath $_.FullName).GetOwner([Security.Principal.SecurityIdentifier]) -eq $admins } |
                ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }
            $acl = New-Object Security.AccessControl.DirectorySecurity
            $acl.SetOwner($admins)
            $acl.SetAccessRuleProtection($true, $false)
            foreach ($sid in 'S-1-5-32-544', 'S-1-5-18') {
                $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule([Security.Principal.SecurityIdentifier]$sid, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
            }
            $folder = Join-Path $env:ProgramData ('TweakHub-Tron-' + [guid]::NewGuid().ToString('N'))
            [void][IO.Directory]::CreateDirectory($folder, $acl)
            $package = Join-Path $folder 'tron.exe'
            Copy-Item -LiteralPath {{PowerShellService.Literal(packagePath)}} -Destination $package
            if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne '{{hash.ToUpperInvariant()}}') { throw 'Tron package SHA-256 mismatch; nothing was run.' }
            $extracted = Join-Path $folder 'x'
            $extractor = Start-Process -FilePath $package -ArgumentList '-y', ('-o"' + $extracted + '"') -WorkingDirectory $folder -Wait -PassThru
            if ($extractor.ExitCode -ne 0) { throw 'Tron extraction failed.' }
            $root = @($extracted, (Join-Path $extracted 'tron')) | Where-Object { Test-Path -LiteralPath (Join-Path $_ 'tron.bat') -PathType Leaf } | Select-Object -First 1
            if (-not $root) { throw 'tron.bat was not found in the verified package.' }
            Start-Process -FilePath (Join-Path $env:windir 'System32\cmd.exe') -ArgumentList '{{RunArguments}}' -WorkingDirectory $root -NoNewWindow -Wait
        } catch {
            Write-Host $_ -ForegroundColor Red
            Read-Host 'Press Enter to close'
        }
        """;

    private static void EnsureNotRunning(State state)
    {
        if (state.ProcessId is not { } id) return;
        try
        {
            using var process = Process.GetProcessById(id);
            if (!process.HasExited && process.StartTime.ToUniversalTime().Ticks == state.StartedUtcTicks)
                throw new InvalidOperationException("Close the existing Tron console before downloading, extracting or launching another copy.");
        }
        catch (ArgumentException) { } // Process no longer exists; PID reuse is checked by its start time.
    }

    public async Task RunAsync()
    {
        if (!await _gate.WaitAsync(0)) throw new InvalidOperationException("Another Tron operation is in progress.");
        try { await RunCoreAsync(); }
        finally { _gate.Release(); }
    }

    private async Task RunCoreAsync()
    {
        var state = JsonStore.Load<State>(StatePath);
        EnsureNotRunning(state);
        var root = FindRoot();
        if (state.ManifestHash is null || await HashAsync(CheckedPath(ExtractedPath, Manifest)) != state.ManifestHash)
            throw new InvalidDataException("Extract the verified package again before running Tron.");
        await VerifyFilesAsync(ExtractedPath, root);
        var settings = await File.ReadAllTextAsync(Path.Combine(root, "resources", "functions", "tron_settings.bat"));
        foreach (var flag in new[] { "AUTORUN", "AUTORUN_IN_SAFE_MODE", "EULA_ACCEPTED", "EMAIL_REPORT", "UPLOAD_DEBUG_LOGS", "DEV_MODE", "AUTO_SHUTDOWN" })
            if (!Regex.IsMatch(settings, $@"(?im)^set {flag}=no\s*$")) throw new InvalidDataException("Unsafe or unknown Tron settings; run rejected.");
        if (!Regex.IsMatch(settings, @"(?im)^set AUTO_REBOOT_DELAY=0\s*$")) throw new InvalidDataException("Automatic reboot is not allowed.");
        // package.json is user-writable: the hash the elevated run trusts comes from the publisher again, not from disk.
        var package = state.Package ?? throw new InvalidOperationException("Download Tron first.");
        if (!ListedPackages(await ManifestAsync()).Contains(package)) throw new InvalidDataException("The package is not in the publisher's SHA-256 manifest.");
        var startInfo = new ProcessStartInfo(SystemTools.PowerShell) { UseShellExecute = true, Verb = "runas" };
        foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-EncodedCommand" }) startInfo.ArgumentList.Add(argument);
        startInfo.ArgumentList.Add(Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(
            BuildRunScript(Path.Combine(_directory, package.File), package.Hash))));
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Tron was not launched.");
        state.ProcessId = process.Id;
        state.StartedUtcTicks = process.StartTime.ToUniversalTime().Ticks;
        JsonStore.Save(StatePath, state);
    }
}
