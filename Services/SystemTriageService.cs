using System.IO;
using System.Text;
using System.Text.Json;
using TweakHub.Localization;

namespace TweakHub.Services;

public enum TrustState { Signed, Unsigned, ScriptHost, Missing, Unknown }

public sealed record NetworkConnection(
    string Protocol, string Local, int LocalPort, string Remote, int RemotePort, string State,
    int Pid, string Process, string Path)
{
    public TrustState Trust { get; init; } = TrustState.Unknown;
    public string Signer { get; init; } = string.Empty;
    public bool IsListening => State == "Listen";
    public bool IsLoopback => Remote is "127.0.0.1" or "::1" || Local is "127.0.0.1" or "::1" && IsListening;
    public string LocalText => $"{Local}:{LocalPort}";
    public string RemoteText => IsListening ? string.Empty : $"{Remote}:{RemotePort}";
    public bool HasPublicRemote => !IsListening && SystemTriageService.IsPublicAddress(Remote);
    public string StateText => L.Get(IsListening ? "Security:StateListening" : "Security:StateEstablished");
    public string TrustText => SystemTriageService.TrustText(Trust, Signer);
}

public sealed record StartupEntry(string Kind, string Name, string Command, string Location, string Path)
{
    public TrustState Trust { get; init; } = TrustState.Unknown;
    public string Signer { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    /// <summary>StartupApproved key Task Manager uses to switch Run and Startup folder entries off; empty for RunOnce.</summary>
    public string ApprovedKey { get; init; } = string.Empty;
    public bool Delayed { get; init; }
    public bool CanToggle => Kind is "Task" or "Service" || ApprovedKey.Length > 0;
    public bool ToggleNeedsAdministrator => Kind is "Task" or "Service" || ApprovedKey.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase);
    public string StatusText => L.Get(Enabled ? "Security:StartupEnabled" : "Security:StartupDisabled");
    public string ToggleText => L.Get(Enabled ? "Security:StartupDisable" : "Security:StartupEnable");
    public string KindText => L.Get($"Security:Startup{Kind}");
    public string TrustText => SystemTriageService.TrustText(Trust, Signer);
}

public sealed record SystemTriage(IReadOnlyList<NetworkConnection> Connections, IReadOnlyList<StartupEntry> Startup, bool Elevated);

public sealed class SystemTriageService
{
    public static SystemTriageService Instance { get; } = new();
    private SystemTriageService() { }

    private const string InventoryScript = """
        $ErrorActionPreference = 'SilentlyContinue'
        $procs = @{}; Get-Process | ForEach-Object { $procs[$_.Id] = $_ }
        function Conn($protocol, $c, $remote, $remotePort, $state) {
            $p = $procs[[int]$c.OwningProcess]
            [pscustomobject]@{ Protocol = $protocol; Local = [string]$c.LocalAddress; LocalPort = [int]$c.LocalPort; Remote = [string]$remote
                RemotePort = [int]$remotePort; State = $state; Pid = [int]$c.OwningProcess; Process = [string]$p.ProcessName; Path = [string]$p.Path }
        }
        $connections = @(
            Get-NetTCPConnection | Where-Object { $_.State -in 'Established', 'Listen' } | ForEach-Object { Conn 'TCP' $_ $_.RemoteAddress $_.RemotePort ([string]$_.State) }
            Get-NetUDPEndpoint | ForEach-Object { Conn 'UDP' $_ '' 0 'Listen' })
        $approvedBase = 'SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved'
        # Task Manager marks disabled entries with an odd first byte in StartupApproved.
        function IsApproved($key, $name) {
            $value = (Get-ItemProperty -LiteralPath $key -Name $name).$name
            -not ($value -and ($value[0] -band 1))
        }
        $startup = @(
            foreach ($run in @('HKLM', 'SOFTWARE\Microsoft\Windows\CurrentVersion\Run', 'Run'), @('HKLM', 'SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce', ''),
                @('HKLM', 'SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run', 'Run32'), @('HKCU', 'SOFTWARE\Microsoft\Windows\CurrentVersion\Run', 'Run'),
                @('HKCU', 'SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce', '')) {
                $key = $run[0] + ':\' + $run[1]
                $approved = if ($run[2]) { $run[0] + ':\' + $approvedBase + '\' + $run[2] } else { '' }
                $item = Get-Item -LiteralPath $key
                foreach ($name in $item.GetValueNames()) { if ($name) {
                    [pscustomobject]@{ Kind = 'Run'; Name = $name; Command = [string]$item.GetValue($name); Location = $key
                        Approved = $approved; Enabled = (-not $approved) -or (IsApproved $approved $name) } } }
            }
            $shell = New-Object -ComObject WScript.Shell
            foreach ($folder in @('Startup', 'HKCU'), @('CommonStartup', 'HKLM')) {
                $dir = [Environment]::GetFolderPath($folder[0])
                $approved = $folder[1] + ':\' + $approvedBase + '\StartupFolder'
                Get-ChildItem -LiteralPath $dir -File | Where-Object Name -ne 'desktop.ini' | ForEach-Object {
                    $command = '"' + $_.FullName + '"'
                    if ($_.Extension -eq '.lnk') { $link = $shell.CreateShortcut($_.FullName); $command = ('"{0}" {1}' -f $link.TargetPath, $link.Arguments).Trim() }
                    [pscustomobject]@{ Kind = 'Folder'; Name = $_.Name; Command = $command; Location = $dir; Approved = $approved; Enabled = IsApproved $approved $_.Name } }
            }
            Get-ScheduledTask | Where-Object { $_.TaskPath -notlike '\Microsoft\*' } | ForEach-Object {
                $task = $_
                $task.Actions | Where-Object Execute | Select-Object -First 1 | ForEach-Object {
                    [pscustomobject]@{ Kind = 'Task'; Name = $task.TaskName; Command = ('"{0}" {1}' -f $_.Execute, $_.Arguments).Trim(); Location = $task.TaskPath
                        Enabled = $task.State -ne 'Disabled' } } }
            # Services that live inside the Windows directory are OS components and are skipped to keep the list readable.
            $tracked = @(__TRACKED_SERVICES__)
            Get-CimInstance Win32_Service | Where-Object { $_.Name -in $tracked -or ($_.StartMode -eq 'Auto' -and $_.PathName -and $_.PathName -notmatch '(?i)^"?[a-z]:\\windows\\') } |
                ForEach-Object { [pscustomobject]@{ Kind = 'Service'; Name = $_.DisplayName; Command = $_.PathName; Location = $_.Name
                    Enabled = $_.StartMode -eq 'Auto'; Delayed = [bool]$_.DelayedAutoStart } }
            $winlogon = Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
            if ($winlogon.Shell -and $winlogon.Shell -ne 'explorer.exe') {
                [pscustomobject]@{ Kind = 'Winlogon'; Name = 'Shell'; Command = [string]$winlogon.Shell; Location = 'Winlogon' } }
            if ($winlogon.Userinit -and $winlogon.Userinit.TrimEnd(',') -notmatch '(?i)^[a-z]:\\windows\\system32\\userinit\.exe$') {
                [pscustomobject]@{ Kind = 'Winlogon'; Name = 'Userinit'; Command = [string]$winlogon.Userinit; Location = 'Winlogon' } })
        [pscustomobject]@{ Connections = $connections; Startup = $startup } | ConvertTo-Json -Depth 4 -Compress
        """ + PowerShellService.AsciiJson;

    public async Task<SystemTriage> CollectAsync(bool elevated, CancellationToken cancellationToken = default)
    {
        var tracked = string.Join(",", LoadDisabledServices().Keys.Select(PowerShellService.Literal));
        var script = InventoryScript.Replace("__TRACKED_SERVICES__", tracked);
        var inventory = await PowerShellService.Instance.ExecuteScriptAsync(script, elevated, TimeSpan.FromMinutes(2), cancellationToken);
        if (!inventory.Success) throw new InvalidOperationException(string.IsNullOrWhiteSpace(inventory.Error) ? inventory.ExitCode.ToString() : inventory.Error);
        var (connections, startup) = ParseInventory(inventory.Output);

        var paths = connections.Select(item => item.Path).Concat(startup.Select(item => item.Path))
            .Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var signatures = await GetSignaturesAsync(paths, cancellationToken);

        return new SystemTriage(
            connections.Select(item => item with { Trust = Classify(item.Path, signatures, out var signer), Signer = signer }).ToList(),
            startup.Select(item => item with { Trust = Classify(item.Path, signatures, out var signer), Signer = signer }).ToList(),
            elevated || Elevation.IsAdministrator);
    }

    internal static (List<NetworkConnection> Connections, List<StartupEntry> Startup) ParseInventory(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        static string Text(JsonElement element, string name) => JsonRead.Text(element, name);
        static int Number(JsonElement element, string name) => JsonRead.Int(element, name) ?? 0;

        var connections = JsonRead.Items(root, "Connections").Select(item => new NetworkConnection(
            Text(item, "Protocol"), Text(item, "Local"), Number(item, "LocalPort"), Text(item, "Remote"), Number(item, "RemotePort"),
            Text(item, "State"), Number(item, "Pid"), Text(item, "Process") is { Length: > 0 } name ? name : "System", Text(item, "Path")))
            .ToList();
        var startup = JsonRead.Items(root, "Startup").Select(item => new StartupEntry(
            Text(item, "Kind"), Text(item, "Name"), Text(item, "Command"), Text(item, "Location"), ExtractExecutable(Text(item, "Command")))
            {
                Enabled = JsonRead.Bool(item, "Enabled", fallback: true),
                ApprovedKey = Text(item, "Approved"),
                Delayed = JsonRead.Bool(item, "Delayed")
            })
            .ToList();
        return (connections, startup);
    }

    private static readonly string[] ScriptHosts =
        ["rundll32.exe", "regsvr32.exe", "mshta.exe", "wscript.exe", "cscript.exe", "powershell.exe", "pwsh.exe", "cmd.exe", "conhost.exe"];

    private static readonly string[] ScriptExtensions = [".bat", ".cmd", ".vbs", ".vbe", ".js", ".jse", ".ps1", ".wsf", ".hta"];

    /// <summary>Resolves the program a command line starts, the way CreateProcess would read it.</summary>
    internal static string ExtractExecutable(string command)
    {
        var text = Environment.ExpandEnvironmentVariables(command).Trim();
        if (text.Length == 0) return string.Empty;

        string candidate;
        if (text[0] == '"')
        {
            text = text.TrimStart('"');
            var end = text.IndexOf('"');
            candidate = end < 0 ? text : text[..end];
        }
        else
        {
            var exe = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            candidate = exe >= 0 ? text[..(exe + 4)] : text.Split(' ', 2)[0];
        }

        candidate = candidate.Trim().TrimEnd(',');
        if (candidate.StartsWith(@"\??\", StringComparison.Ordinal)) candidate = candidate[4..];
        if (candidate.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase))
            candidate = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), candidate[12..]);
        if (!Path.IsPathRooted(candidate) && candidate.Length > 0)
        {
            var name = Path.HasExtension(candidate) ? candidate : candidate + ".exe";
            candidate = new[] { Environment.SystemDirectory, Environment.GetFolderPath(Environment.SpecialFolder.Windows) }
                .Select(directory => Path.Combine(directory, name)).FirstOrDefault(File.Exists) ?? candidate;
        }
        return candidate;
    }

    private static TrustState Classify(string path, IReadOnlyDictionary<string, (string Status, string Signer)> signatures, out string signer)
    {
        signer = string.Empty;
        if (string.IsNullOrEmpty(path)) return TrustState.Unknown;
        var fileName = Path.GetFileName(path);
        if (ScriptHosts.Contains(fileName, StringComparer.OrdinalIgnoreCase)
            || ScriptExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            return TrustState.ScriptHost;
        if (!File.Exists(path)) return TrustState.Missing;
        if (!signatures.TryGetValue(path, out var signature)) return TrustState.Unknown;
        signer = signature.Signer;
        return signature.Status == "Valid" ? TrustState.Signed : TrustState.Unsigned;
    }

    internal static string TrustText(TrustState trust, string signer) => trust switch
    {
        TrustState.Signed => signer,
        TrustState.Unsigned => L.Get("Security:TrustUnsigned"),
        TrustState.ScriptHost => L.Get("Security:TrustScriptHost"),
        TrustState.Missing => L.Get("Security:TrustMissing"),
        _ => L.Get("Security:TrustUnknown")
    };

    /// <summary>Internet addresses worth a reputation lookup; private, loopback, link-local, CGNAT and multicast ones are not.</summary>
    internal static bool IsPublicAddress(string address)
    {
        if (!System.Net.IPAddress.TryParse(address, out var ip)) return false;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (System.Net.IPAddress.IsLoopback(ip)) return false;
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            return !(ip.Equals(System.Net.IPAddress.IPv6Any) || ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal
                || ip.IsIPv6UniqueLocal || ip.IsIPv6Multicast);
        var b = ip.GetAddressBytes();
        return !(b[0] is 0 or 10 or >= 224 || b[0] == 172 && b[1] is >= 16 and <= 31 || b[0] == 192 && b[1] == 168
            || b[0] == 169 && b[1] == 254 || b[0] == 100 && b[1] is >= 64 and <= 127);
    }

    public static async Task<IReadOnlyDictionary<string, (string Status, string Signer)>> GetSignaturesAsync(
        IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
        if (paths.Count == 0) return result;
        var list = string.Join(",\n", paths.Select(PowerShellService.Literal));
        var output = await PowerShellService.Instance.ExecuteScriptAsync($$"""
            $ErrorActionPreference = 'SilentlyContinue'
            @(@({{list}}) | ForEach-Object {
                $s = Get-AuthenticodeSignature -LiteralPath $_
                [pscustomobject]@{ Path = $_; Status = [string]$s.Status; Signer = $(if ($s.SignerCertificate) { $s.SignerCertificate.GetNameInfo('SimpleName', $false) }) }
            }) | ConvertTo-Json -Compress
            """ + PowerShellService.AsciiJson, timeout: TimeSpan.FromMinutes(3), cancellationToken: cancellationToken);
        if (!output.Success || string.IsNullOrWhiteSpace(output.Output)) return result;

        using var document = JsonDocument.Parse(output.Output);
        foreach (var item in JsonRead.Items(document.RootElement))
            result[JsonRead.Text(item, "Path")] = (JsonRead.Text(item, "Status"), JsonRead.Text(item, "Signer"));
        return result;
    }

    private static string DisabledServicesPath => System.IO.Path.Combine(AppDataPath.BasePath, "disabled-services.json");

    /// <summary>Services TweakHub switched to Manual, with the start mode to restore ("auto" or "delayed-auto").</summary>
    private static Dictionary<string, string> LoadDisabledServices() => JsonStore.Load<Dictionary<string, string>>(DisabledServicesPath);

    private static void SaveDisabledServices(Dictionary<string, string> services) => JsonStore.Save(DisabledServicesPath, services);

    internal static string BuildToggleScript(StartupEntry entry, bool enable, string? serviceStartMode = null)
    {
        static string Q(string value) => PowerShellService.Literal(value);
        return entry.Kind switch
        {
            "Task" => $"{(enable ? "Enable" : "Disable")}-ScheduledTask -TaskPath {Q(entry.Location)} -TaskName {Q(entry.Name)} | Out-Null",
            // Manual instead of Disabled: the service can still start when an app needs it.
            "Service" when !enable => $"Set-Service -Name {Q(entry.Location)} -StartupType Manual",
            "Service" => $"& sc.exe config {Q(entry.Location)} start= {(serviceStartMode == "delayed-auto" ? "delayed-auto" : "auto")} | Out-Null; if ($LASTEXITCODE) {{ exit $LASTEXITCODE }}",
            // Same 12-byte value Task Manager writes; never recreate the key, New-Item -Force would wipe its values.
            _ => $"if (-not (Test-Path -LiteralPath {Q(entry.ApprovedKey)})) {{ New-Item -Path {Q(entry.ApprovedKey)} | Out-Null }}\n"
                 + $"Set-ItemProperty -LiteralPath {Q(entry.ApprovedKey)} -Name {Q(entry.Name)} -Type Binary -Value ([byte[]]@({(enable ? 2 : 3)},0,0,0,0,0,0,0,0,0,0,0))"
        };
    }

    public async Task<PowerShellResult> SetStartupEnabledAsync(StartupEntry entry, bool enable)
    {
        if (!entry.CanToggle) throw new InvalidOperationException(entry.Kind);
        var services = LoadDisabledServices();
        services.TryGetValue(entry.Location, out var startMode);
        var result = await PowerShellService.Instance.ExecuteScriptAsync(BuildToggleScript(entry, enable, startMode),
            entry.ToggleNeedsAdministrator, TimeSpan.FromMinutes(1));
        if (result.Success && entry.Kind == "Service")
        {
            if (enable) services.Remove(entry.Location);
            else services[entry.Location] = entry.Delayed ? "delayed-auto" : "auto";
            SaveDisabledServices(services);
        }
        return result;
    }

    public static string ToCsv(SystemTriage triage)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Section,Type,Name,PID,Local,Remote,State,Command,Path,Trust,Signer,Enabled");
        foreach (var c in triage.Connections)
            builder.AppendLine(Csv.Row("Connection", c.Protocol, c.Process, c.Pid, c.LocalText, c.RemoteText, c.State, "", c.Path, c.Trust, c.Signer, ""));
        foreach (var s in triage.Startup)
            builder.AppendLine(Csv.Row("Startup", s.Kind, s.Name, "", "", "", s.Location, s.Command, s.Path, s.Trust, s.Signer, s.Enabled));
        return builder.ToString();
    }
}
