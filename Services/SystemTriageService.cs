using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    /// <summary>Task trigger kinds ("Logon, Boot") or the event query of a WMI subscription.</summary>
    public string Trigger { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public DateTimeOffset? LastRun { get; init; }
    public DateTimeOffset? NextRun { get; init; }
    /// <summary>The program the entry starts no longer exists: an orphan, safe to delete.</summary>
    public bool IsMissing { get; init; }
    public DateTime? FileDate { get; init; }
    public string Category => Kind switch
    {
        "Task" => "Tasks",
        "Service" => "Services",
        "Driver" => "Drivers",
        "ShellEx" => "Explorer",
        "Ifeo" or "AppInit" => "Hijacks",
        "Wmi" => "Wmi",
        _ => "Logon"
    };
    private static readonly string WindowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\') + "\\";

    // Unknown counts as Windows: when signatures cannot be read, OS files must stay protected.
    private bool WindowsImage => Path.StartsWith(WindowsDirectory, StringComparison.OrdinalIgnoreCase)
        && (Trust == TrustState.Unknown || Trust == TrustState.Signed && Signer.Contains("Microsoft", StringComparison.OrdinalIgnoreCase));

    /// <summary>Part of Windows itself: the Microsoft task folders need the Windows image too, so a planted task is not hidden.</summary>
    public bool IsWindowsComponent => Kind switch
    {
        // The default Windows subscription only writes to the event log.
        "Wmi" => Name == "SCM Event Log Consumer" && Command == "NTEventLogEventConsumer",
        // Windows' own tasks often start rundll32 or cmd: then what they run decides, not the launcher.
        "Task" => Location.StartsWith(@"\Microsoft\", StringComparison.OrdinalIgnoreCase) && (Path.Length == 0 || WindowsImage
            || Trust == TrustState.ScriptHost && Path.StartsWith(WindowsDirectory, StringComparison.OrdinalIgnoreCase) && RunsOnlyWindowsFiles),
        "Winlogon" => false,
        _ => WindowsImage
    };

    private static readonly Regex FileArgument = new(@"(?:[a-z]:\\[^"",]*?|(?<![\\/\w.-])[\w.-]+)\.(?:exe|dll|cpl|cmd|bat|ps1|vbs|js|msc)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // Folders under C:\Windows where standard users can create files.
    private static readonly string[] UserWritableWindowsFolders = ["Temp", "Tasks", "tracing", @"System32\Tasks", @"System32\spool", @"SysWOW64\Tasks"];

    // The launcher plus at least one file, each under C:\Windows or a bare name Windows finds in System32.
    private bool RunsOnlyWindowsFiles
    {
        get
        {
            var files = FileArgument.Matches(Environment.ExpandEnvironmentVariables(Command)).Select(match => match.Value).ToList();
            return files.Count > 1 && files.All(file => !System.IO.Path.IsPathRooted(file)
                || System.IO.Path.GetFullPath(file) is var full && full.StartsWith(WindowsDirectory, StringComparison.OrdinalIgnoreCase)
                    && !UserWritableWindowsFolders.Any(folder => full.StartsWith(System.IO.Path.Combine(WindowsDirectory, folder) + "\\", StringComparison.OrdinalIgnoreCase)));
        }
    }

    /// <summary>Hidden by the default filter, like Autoruns' "Hide Microsoft entries".</summary>
    public bool IsMicrosoft => IsWindowsComponent || Trust == TrustState.Signed && Signer.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);

    /// <summary>Why the entry can never be deleted (Security:StartupProtected*), empty when it can.</summary>
    public string Protection => Kind switch
    {
        "Driver" => "Driver",
        // Only non-default machine Shell/Userinit values are listed; removing one leaves Windows without a shell.
        "Winlogon" when Location.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase) => "Winlogon",
        "Service" when IsWindowsComponent => "WindowsService",
        _ when IsWindowsComponent => "Windows",
        _ => string.Empty
    };

    public bool CanDelete => Protection.Length == 0;
    // HKCU stays unelevated: an elevated process of another administrator account would write that account's hive.
    public bool DeleteNeedsAdministrator => Kind is "Task" or "Service" or "Wmi" || Location.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase)
        || Kind == "Folder" && Location.Equals(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), StringComparison.OrdinalIgnoreCase);
    public bool CanToggle => Protection is not ("Driver" or "Winlogon" or "WindowsService") && (Kind is "Task" or "Service" || ApprovedKey.Length > 0);
    public bool ToggleNeedsAdministrator => Kind is "Task" or "Service" || ApprovedKey.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase);
    public string StatusText => L.Get(Enabled ? "Security:StartupEnabled" : "Security:StartupDisabled");
    public string ToggleText => L.Get(Enabled ? "Security:StartupDisable" : "Security:StartupEnable");
    public string KindText => L.Get($"Security:Startup{Kind}");
    public string TrustText => SystemTriageService.TrustText(Trust, Signer);
    public string TriggerText => Kind == "Task"
        ? string.Join(", ", Trigger.Split(", ", StringSplitOptions.RemoveEmptyEntries).Select(name => L.Get($"Security:Trigger{name}")))
        : Trigger;
    public string LastRunText => LastRun?.LocalDateTime.ToString("g", L.Culture) ?? string.Empty;
    public string NextRunText => NextRun?.LocalDateTime.ToString("g", L.Culture) ?? string.Empty;
    public string FileDateText => FileDate?.ToString("d", L.Culture) ?? string.Empty;
    public string ProtectionText => Protection.Length == 0 ? string.Empty : L.Get($"Security:StartupProtected{Protection}");
}

/// <summary>A deleted startup entry. Payload is the JSON the delete script captured before deleting.</summary>
public sealed record StartupBackup(string Id, DateTime DeletedAt, string Kind, string Name, string Command, string Location, string Path,
    string ApprovedKey, string Payload)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public StartupEntry Entry => new(Kind, Name, Command, Location, Path) { ApprovedKey = ApprovedKey };
    [System.Text.Json.Serialization.JsonIgnore]
    public string DeletedText => DeletedAt.ToString("g", L.Culture);
    /// <summary>Read from the administrator store in HKLM, the only source trusted for an elevated restore.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Machine { get; init; }
}

public sealed record SystemTriage(IReadOnlyList<NetworkConnection> Connections, IReadOnlyList<StartupEntry> Startup, bool Elevated);

public sealed class SystemTriageService
{
    public static SystemTriageService Instance { get; } = new();
    private SystemTriageService() { }

    private const string ConnectionsScript = """
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
        [pscustomobject]@{ Connections = $connections } | ConvertTo-Json -Depth 4 -Compress
        """ + PowerShellService.AsciiJson;

    // Autoruns' locations, from the persistence-hunting references: logon, tasks, services, drivers, Explorer, hijacks, WMI.
    // Bindings can hold paths relative to root\subscription; [wmi] alone would look for them in root\cimv2.
    private const string WmiFunction = """
        function Wmi($path) {
            if ([string]$path -notmatch '^(\\\\|root\\)') { $path = 'root\subscription:' + $path }
            [wmi][string]$path
        }

        """;

    internal const string StartupScript = WmiFunction + """
        $ErrorActionPreference = 'SilentlyContinue'
        $approvedBase = 'SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved'
        $caller = '__CALLER_SID__'
        $userStartup = [Environment]::GetFolderPath('Startup')
        if ($caller -match '^S-1-[0-9-]+$' -and $caller -ne [Security.Principal.WindowsIdentity]::GetCurrent().User.Value) {
            # Elevated with another administrator account: list the signed-in user's entries, not that account's.
            Remove-PSDrive -Name HKCU -Scope Global -Force
            $null = New-PSDrive -Name HKCU -PSProvider Registry -Root ('HKEY_USERS\' + $caller) -Scope Global
            $callerProfile = (Get-ItemProperty -LiteralPath ('HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\' + $caller)).ProfileImagePath
            $shellFolder = (Get-Item -LiteralPath 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders').GetValue('Startup', $null, 'DoNotExpandEnvironmentNames')
            $userStartup = if ($shellFolder -match '^%USERPROFILE%\\') { $callerProfile + $shellFolder.Substring(13) }
                elseif ($shellFolder -and $shellFolder -notmatch '%') { $shellFolder }
                else { Join-Path $callerProfile 'AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup' }
        }
        # Task Manager marks disabled entries with an odd first byte in StartupApproved.
        function IsApproved($key, $name) {
            $value = (Get-ItemProperty -LiteralPath $key -Name $name).$name
            -not ($value -and ($value[0] -band 1))
        }
        function Clsid($id) {
            foreach ($root in 'HKCU:\SOFTWARE\Classes\CLSID\', 'HKLM:\SOFTWARE\Classes\CLSID\') {
                $dll = (Get-ItemProperty -LiteralPath ($root + $id + '\InprocServer32')).'(default)'
                if ($dll) { return [string]$dll } }
            ''
        }
        # Task Scheduler reports "never" as a date in 1899 or 1999.
        function RunTime($value) { if ($value -and $value.Year -gt 2000) { $value.ToString('o') } else { '' } }
        function Tasks($folder) {
            $location = $folder.Path.TrimEnd('\') + '\'
            # One unreadable task or folder must not end the whole list: the elevated wrapper turns errors into stops.
            foreach ($task in @(try { $folder.GetTasks(1) } catch { })) { try {
                $definition = $task.Definition
                $action = @($definition.Actions) | Select-Object -First 1
                $image = ''; $command = ''
                if ($action.Type -eq 0) { $command = ('"{0}" {1}' -f $action.Path, $action.Arguments).Trim(); $image = $null }
                elseif ($action.Type -eq 5) { $command = [string]$action.ClassId; $image = Clsid $action.ClassId }
                $user = if ($definition.Principal.UserId) { [string]$definition.Principal.UserId } else { [string]$definition.Principal.GroupId }
                [pscustomobject]@{ Kind = 'Task'; Name = [string]$task.Name; Command = $command; Image = $image; Location = $location
                    Enabled = [bool]$task.Enabled; Triggers = @(foreach ($trigger in $definition.Triggers) { [int]$trigger.Type }); User = $user
                    LastRun = RunTime $task.LastRunTime; NextRun = RunTime $task.NextRunTime }
            } catch { } }
            foreach ($sub in @(try { $folder.GetFolders(0) } catch { })) { Tasks $sub }
        }
        $startup = @(
            foreach ($run in @('HKLM', 'SOFTWARE\Microsoft\Windows\CurrentVersion\Run', 'Run', 'Run'), @('HKLM', 'SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce', '', 'Run'),
                @('HKLM', 'SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run', 'Run32', 'Run'), @('HKLM', 'SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce', '', 'Run'),
                @('HKCU', 'SOFTWARE\Microsoft\Windows\CurrentVersion\Run', 'Run', 'Run'), @('HKCU', 'SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce', '', 'Run'),
                @('HKLM', 'SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run', '', 'PolicyRun'),
                @('HKCU', 'SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run', '', 'PolicyRun')) {
                $key = $run[0] + ':\' + $run[1]
                $approved = if ($run[2]) { $run[0] + ':\' + $approvedBase + '\' + $run[2] } else { '' }
                # Most of these keys do not exist; under the elevated wrapper a call on $null stops the whole script.
                $item = Get-Item -LiteralPath $key
                if ($item) { foreach ($name in $item.GetValueNames()) { if ($name) {
                    [pscustomobject]@{ Kind = $run[3]; Name = $name; Command = [string]$item.GetValue($name); Location = $key
                        Approved = $approved; Enabled = (-not $approved) -or (IsApproved $approved $name) } } } }
            }
            $shell = New-Object -ComObject WScript.Shell
            foreach ($folder in @($userStartup, 'HKCU'), @([Environment]::GetFolderPath('CommonStartup'), 'HKLM')) {
                $dir = $folder[0]
                $approved = $folder[1] + ':\' + $approvedBase + '\StartupFolder'
                Get-ChildItem -LiteralPath $dir -File | Where-Object Name -ne 'desktop.ini' | ForEach-Object {
                    $command = '"' + $_.FullName + '"'
                    if ($_.Extension -eq '.lnk') { try { $link = $shell.CreateShortcut($_.FullName); $command = ('"{0}" {1}' -f $link.TargetPath, $link.Arguments).Trim() } catch { } }
                    [pscustomobject]@{ Kind = 'Folder'; Name = $_.Name; Command = $command; Location = $dir; Approved = $approved; Enabled = IsApproved $approved $_.Name } }
            }
            $winlogonKey = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
            $winlogon = Get-ItemProperty -LiteralPath $winlogonKey
            if ($winlogon.Shell -and $winlogon.Shell -ne 'explorer.exe') {
                [pscustomobject]@{ Kind = 'Winlogon'; Name = 'Shell'; Command = [string]$winlogon.Shell; Location = $winlogonKey } }
            if ($winlogon.Userinit -and $winlogon.Userinit.TrimEnd(',') -notmatch '(?i)^[a-z]:\\windows\\system32\\userinit\.exe$') {
                [pscustomobject]@{ Kind = 'Winlogon'; Name = 'Userinit'; Command = [string]$winlogon.Userinit; Location = $winlogonKey } }
            $userWinlogon = 'HKCU:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
            $userShell = (Get-ItemProperty -LiteralPath $userWinlogon).Shell
            if ($userShell) { [pscustomobject]@{ Kind = 'Winlogon'; Name = 'Shell'; Command = [string]$userShell; Location = $userWinlogon } }
            foreach ($root in 'HKLM:\SOFTWARE\Microsoft\Active Setup\Installed Components', 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Active Setup\Installed Components') {
                Get-ChildItem -LiteralPath $root | ForEach-Object {
                    $stub = [string]$_.GetValue('StubPath')
                    if ($stub) {
                        $title = [string]$_.GetValue('')
                        [pscustomobject]@{ Kind = 'ActiveSetup'; Name = $(if ($title) { $title } else { $_.PSChildName }); Command = $stub
                            Location = $root + '\' + $_.PSChildName; Enabled = $_.GetValue('IsInstalled') -ne 0 } } }
            }
            $scheduler = New-Object -ComObject Schedule.Service
            $scheduler.Connect()
            Tasks $scheduler.GetFolder('\')
            $tracked = @(__TRACKED_SERVICES__)
            Get-CimInstance Win32_Service | Where-Object { $_.Name -in $tracked -or $_.StartMode -eq 'Auto' } |
                ForEach-Object {
                    # svchost services run their ServiceDll: that is the file to verify, not svchost.exe.
                    $dll = if ($_.PathName -match '(?i)\\svchost\.exe') { [string](Get-ItemProperty -LiteralPath ('HKLM:\SYSTEM\CurrentControlSet\Services\' + $_.Name + '\Parameters')).ServiceDll }
                    [pscustomobject]@{ Kind = 'Service'; Name = $_.DisplayName; Command = $_.PathName; Image = $(if ($dll) { $dll } else { $null }); Location = $_.Name
                        Enabled = $_.StartMode -eq 'Auto'; Delayed = [bool]$_.DelayedAutoStart } }
            Get-CimInstance Win32_SystemDriver | Where-Object { $_.StartMode -in 'Boot', 'System', 'Auto' } |
                ForEach-Object { [pscustomobject]@{ Kind = 'Driver'; Name = $_.DisplayName; Command = $_.PathName; Location = $_.Name } }
            foreach ($root in 'HKLM:\SOFTWARE\Classes', 'HKCU:\SOFTWARE\Classes') {
                foreach ($type in '*', 'AllFilesystemObjects', 'Directory', 'Directory\Background', 'Drive', 'Folder') {
                    $base = $root + '\' + $type + '\ShellEx\ContextMenuHandlers'
                    Get-ChildItem -LiteralPath $base | ForEach-Object {
                        $id = [string]$_.GetValue(''); if ($id -notmatch '^\{') { $id = $_.PSChildName }
                        [pscustomobject]@{ Kind = 'ShellEx'; Name = $_.PSChildName; Command = $id; Image = Clsid $id; Location = $base + '\' + $_.PSChildName } } }
            }
            $overlays = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers'
            Get-ChildItem -LiteralPath $overlays | ForEach-Object {
                $id = [string]$_.GetValue('')
                [pscustomobject]@{ Kind = 'ShellEx'; Name = $_.PSChildName.Trim(); Command = $id; Image = Clsid $id; Location = $overlays + '\' + $_.PSChildName } }
            $ifeo = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options'
            Get-ChildItem -LiteralPath $ifeo | ForEach-Object {
                $debugger = [string]$_.GetValue('Debugger')
                if ($debugger) { [pscustomobject]@{ Kind = 'Ifeo'; Name = $_.PSChildName; Command = $debugger; Location = $ifeo + '\' + $_.PSChildName } } }
            foreach ($key in 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows', 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Windows') {
                $dlls = [string](Get-ItemProperty -LiteralPath $key).AppInit_DLLs
                if ($dlls.Trim()) { [pscustomobject]@{ Kind = 'AppInit'; Name = 'AppInit_DLLs'; Command = $dlls; Location = $key } }
            }
            Get-WmiObject -Namespace root\subscription -Class __FilterToConsumerBinding | ForEach-Object {
                $consumer = try { Wmi $_.Consumer } catch { $null }; $filter = try { Wmi $_.Filter } catch { $null }
                # Without administrator rights the consumer may be unreadable: take class and name from the binding path.
                $class = [string]$_.Consumer -replace '^.*:', '' -replace '\..*$', ''
                $name = if ($consumer) { [string]$consumer.Name } elseif ([string]$_.Consumer -match 'Name="(.*)"$') { $Matches[1] -replace '\\"', '"' }
                $command = $class; $image = ''
                if ($consumer.__CLASS -eq 'CommandLineEventConsumer') {
                    $command = if ($consumer.CommandLineTemplate) { [string]$consumer.CommandLineTemplate } else { [string]$consumer.ExecutablePath }; $image = $null }
                elseif ($consumer.__CLASS -eq 'ActiveScriptEventConsumer') {
                    $command = if ($consumer.ScriptFileName) { [string]$consumer.ScriptFileName } else { [string]$consumer.ScriptText }; $image = [string]$consumer.ScriptFileName }
                [pscustomobject]@{ Kind = 'Wmi'; Name = $name; Command = $command; Image = $image; Location = [string]$_.__RELPATH
                    Trigger = [string]$filter.Query } })
        [pscustomobject]@{ Startup = $startup } | ConvertTo-Json -Depth 5 -Compress
        """ + PowerShellService.AsciiJson;

    /// <summary>Connections for Security → System, or the startup inventory for the Startup page.</summary>
    public async Task<SystemTriage> CollectAsync(bool elevated, bool startupOnly = false, CancellationToken cancellationToken = default)
    {
        var tracked = string.Join(",", LoadDisabledServices().Keys.Select(PowerShellService.Literal));
        var script = startupOnly
            ? StartupScript.Replace("__TRACKED_SERVICES__", tracked).Replace("__CALLER_SID__", PowerShellService.CurrentUserSid)
            : ConnectionsScript;
        var inventory = await PowerShellService.Instance.ExecuteScriptAsync(script, elevated, TimeSpan.FromMinutes(3), cancellationToken);
        if (!inventory.Success) throw new InvalidOperationException(string.IsNullOrWhiteSpace(inventory.Error) ? inventory.ExitCode.ToString() : inventory.Error);
        var (connections, startup) = ParseInventory(inventory.Output);

        var paths = connections.Select(item => item.Path).Concat(startup.Select(item => item.Path))
            .Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var signatures = await GetSignaturesAsync(paths, cancellationToken);
        // Without signatures every file under C:\Windows would count as Windows: hidden and protected. Fail instead.
        if (startupOnly && paths.Count > 0 && signatures.Count == 0) throw new InvalidOperationException("The digital signatures could not be read.");

        return new SystemTriage(
            connections.Select(item => item with { Trust = Classify(item.Path, signatures, out var signer), Signer = signer }).ToList(),
            startup.Select(item => item with
            {
                Trust = Classify(item.Path, signatures, out var signer), Signer = signer,
                FileDate = File.Exists(item.Path) ? File.GetLastWriteTime(item.Path) : null
            }).ToList(),
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
        var startup = JsonRead.Items(root, "Startup").Select(item =>
        {
            // A string Image (even empty) is the file the entry loads; null or absent means read it from the command line.
            var path = item.TryGetProperty("Image", out var image) && image.ValueKind == JsonValueKind.String
                ? ExtractExecutable('"' + image.GetString()!.Trim('"') + '"')
                : ExtractExecutable(Text(item, "Command"));
            return new StartupEntry(Text(item, "Kind"), Text(item, "Name"), Text(item, "Command"), Text(item, "Location"), path)
            {
                Enabled = JsonRead.Bool(item, "Enabled", fallback: true),
                ApprovedKey = Text(item, "Approved"),
                Delayed = JsonRead.Bool(item, "Delayed"),
                Trigger = JsonRead.Items(item, "Triggers").Any()
                    ? string.Join(", ", JsonRead.Items(item, "Triggers").Select(type => TriggerName(type.TryGetInt32(out var number) ? number : -1)).Distinct())
                    : Text(item, "Trigger"),
                User = Text(item, "User"),
                LastRun = DateTimeOffset.TryParse(Text(item, "LastRun"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var last) ? last : null,
                NextRun = DateTimeOffset.TryParse(Text(item, "NextRun"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var next) ? next : null,
                // Only full paths: "/UserInstall" or a bare "powershell.exe" are not files that went missing.
                IsMissing = FileMissing(path)
            };
        }).ToList();
        return (connections, startup);
    }

    // Task Scheduler TASK_TRIGGER_TYPE2 values.
    private static string TriggerName(int type) => type switch
    {
        0 => "Event",
        1 => "Time",
        2 => "Daily",
        3 => "Weekly",
        4 or 5 => "Monthly",
        6 => "Idle",
        7 => "Registration",
        8 => "Boot",
        9 => "Logon",
        11 => "SessionState",
        _ => "Other"
    };

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

    /// <summary>
    /// Only a full path Windows reports as not found: an argument such as "/UserInstall" or a file this account may not
    /// read (another user's profile) is not an orphan.
    /// </summary>
    internal static bool FileMissing(string path)
    {
        if (!System.IO.Path.IsPathFullyQualified(path)) return false;
        try { File.GetAttributes(path); return false; }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException) { return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { return false; }
    }

    private static TrustState Classify(string path, IReadOnlyDictionary<string, (string Status, string Signer)> signatures, out string signer)
    {
        signer = string.Empty;
        if (string.IsNullOrEmpty(path)) return TrustState.Unknown;
        var fileName = Path.GetFileName(path);
        if (ScriptHosts.Contains(fileName, StringComparer.OrdinalIgnoreCase)
            || ScriptExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            return TrustState.ScriptHost;
        if (!File.Exists(path)) return FileMissing(path) ? TrustState.Missing : TrustState.Unknown;
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
            "Task" => TaskScheduler(entry) + $"$folder.GetTask({Q(entry.Name)}).Enabled = ${(enable ? "true" : "false")}",
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
        if (entry.Kind == "Service" && OptionalServicesService.Instance.HasBackup(entry.Location)
            && (!enable || !services.ContainsKey(entry.Location)))
            return new PowerShellResult { Success = false, Error = L.Get("Maintenance:ServiceManagedElsewhere") };
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

    private static Regex Key(string pattern) => new($"^{pattern}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // The only keys each kind may touch: restores read their target from a backup file the user can edit.
    private static readonly Dictionary<string, Regex> AllowedKeys = new()
    {
        ["Run"] = Key(@"HK(LM|CU):\\SOFTWARE\\(WOW6432Node\\)?Microsoft\\Windows\\CurrentVersion\\Run(Once)?"),
        ["PolicyRun"] = Key(@"HK(LM|CU):\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run"),
        ["Winlogon"] = Key(@"HKCU:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon"),
        ["AppInit"] = Key(@"HKLM:\\SOFTWARE\\(WOW6432Node\\)?Microsoft\\Windows NT\\CurrentVersion\\Windows"),
        ["Ifeo"] = Key(@"HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\[^\\]+"),
        ["ActiveSetup"] = Key(@"HKLM:\\SOFTWARE\\(WOW6432Node\\)?Microsoft\\Active Setup\\Installed Components\\[^\\]+"),
        ["ShellEx"] = Key(@"HK(LM|CU):\\SOFTWARE\\(Classes\\(\*|AllFilesystemObjects|Directory|Directory\\Background|Drive|Folder)\\ShellEx\\ContextMenuHandlers|Microsoft\\Windows\\CurrentVersion\\Explorer\\ShellIconOverlayIdentifiers)\\[^\\]+"),
    };

    /// <summary>The registry key of a registry entry, and its value name; null for kinds that remove the whole key.</summary>
    private static (string Hive, string SubKey, string? Value) RegistryTarget(StartupEntry entry)
    {
        var key = entry.Kind == "Service" ? @"HKLM:\SYSTEM\CurrentControlSet\Services\" + entry.Location : entry.Location;
        if (entry.Kind == "Service" ? entry.Location.Length == 0 || entry.Location.IndexOfAny(['\\', '/']) >= 0
                : !AllowedKeys.TryGetValue(entry.Kind, out var allowed) || !allowed.IsMatch(key))
            throw new InvalidOperationException($"Not a startup location: {key}");
        var value = entry.Kind switch
        {
            "Ifeo" => "Debugger",
            "Run" or "PolicyRun" or "Winlogon" or "AppInit" => entry.Name,
            _ => null
        };
        return (key.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase) ? "LocalMachine" : "CurrentUser", key[(key.IndexOf('\\') + 1)..], value);
    }

    private static string StartupFile(StartupEntry entry)
    {
        var folders = new[] { Environment.SpecialFolder.Startup, Environment.SpecialFolder.CommonStartup }.Select(Environment.GetFolderPath);
        if (!folders.Contains(entry.Location, StringComparer.OrdinalIgnoreCase) || entry.Name.Length == 0
            || entry.Name != System.IO.Path.GetFileName(entry.Name) || entry.Name is "." or ".." || entry.Name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidOperationException($"Not a startup file: {entry.Location}\\{entry.Name}");
        return System.IO.Path.Combine(entry.Location, entry.Name);
    }

    private static void ValidateTask(StartupEntry entry)
    {
        if (!entry.Location.StartsWith('\\') || !entry.Location.EndsWith('\\') || entry.Name.Length == 0 || entry.Name.Contains('\\'))
            throw new InvalidOperationException($"Not a scheduled task: {entry.Location}{entry.Name}");
    }

    private const string RegistryCapture = """
        function Value($key, $name) {
            $data = $key.GetValue($name, $null, 'DoNotExpandEnvironmentNames')
            if ($data -is [byte[]]) { $data = [Convert]::ToBase64String($data) }
            [pscustomobject]@{ Name = $name; Kind = [string]$key.GetValueKind($name); Data = $data }
        }
        function Tree($key, $name) {
            [pscustomobject]@{ Name = $name; Values = @(foreach ($value in $key.GetValueNames()) { Value $key $value })
                Keys = @(foreach ($sub in $key.GetSubKeyNames()) { Tree $key.OpenSubKey($sub) $sub }) }
        }
        """;

    private const string Json = " | ConvertTo-Json -Depth 64 -Compress";

    // Backups of machine-wide entries live under HKLM, where only administrators write: an elevated restore can only
    // replay what an elevated delete captured, never what an unelevated process put in a file.
    private const string MachineStore = @"SOFTWARE\TweakHub\StartupBackups\";

    private static readonly Regex BackupIdPattern = new(@"^\d{8}-\d{6}-[0-9a-f]{32}$", RegexOptions.CultureInvariant);

    private static string ValidId(string id) =>
        BackupIdPattern.IsMatch(id) ? id : throw new InvalidOperationException($"Not a backup id: {id}");

    private static StartupBackup NewBackup(StartupEntry entry) => new($"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}", DateTime.Now,
        entry.Kind, entry.Name, entry.Command, entry.Location, entry.Path, entry.ApprovedKey, string.Empty);

    // The task folder for the COM API: "\Vendor\" becomes "\Vendor", the root stays "\".
    private static string TaskFolder(StartupEntry entry) => entry.Location.Length > 1 ? entry.Location.TrimEnd('\\') : entry.Location;

    // Names go through COM, which matches them exactly; the ScheduledTasks cmdlets treat [ ] in a name as a wildcard.
    private static string TaskScheduler(StartupEntry entry) =>
        $"$scheduler = New-Object -ComObject Schedule.Service; $scheduler.Connect(); $folder = $scheduler.GetFolder({PowerShellService.Literal(TaskFolder(entry))})\n";

    /// <summary>
    /// Captures the backup into $payload, stores it (HKLM for machine-wide entries), writes it to the output, then deletes.
    /// With $ErrorActionPreference = 'Stop' a failed capture stops the script before anything is removed.
    /// Names come from the system and are only ever passed as quoted literals.
    /// </summary>
    internal static string BuildDeleteScript(StartupEntry entry, StartupBackup? record = null)
    {
        if (!entry.CanDelete) throw new InvalidOperationException($"Protected: {entry.Protection}");
        static string Q(string value) => PowerShellService.Literal(value);
        string capture, remove;
        switch (entry.Kind)
        {
            case "Task":
                ValidateTask(entry);
                capture = TaskScheduler(entry) + $"$payload = [pscustomobject]@{{ Xml = $folder.GetTask({Q(entry.Name)}).Xml }}{Json}";
                remove = $"$folder.DeleteTask({Q(entry.Name)}, 0)";
                break;
            case "Folder":
                capture = $$"""
                    $file = Get-Item -LiteralPath {{Q(StartupFile(entry))}} -Force
                    if ($file.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'The startup file is a link; nothing was deleted.' }
                    if ($file.Length -gt 1MB) { throw 'The startup file is larger than 1 MB; nothing was deleted.' }
                    $payload = [pscustomobject]@{ Data = [Convert]::ToBase64String([IO.File]::ReadAllBytes($file.FullName)) }{{Json}}
                    """;
                remove = "Remove-Item -LiteralPath $file.FullName -Force";
                break;
            case "Wmi":
                if (!entry.Location.StartsWith("__FilterToConsumerBinding.", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Not a WMI binding: {entry.Location}");
                capture = WmiFunction + $$"""
                    $all = @(Get-WmiObject -Namespace root\subscription -Class __FilterToConsumerBinding)
                    $binding = $all | Where-Object { $_.__RELPATH -eq {{Q(entry.Location)}} } | Select-Object -First 1
                    if (-not $binding) { throw 'The WMI subscription no longer exists.' }
                    $filter = Wmi $binding.Filter; $consumer = Wmi $binding.Consumer
                    $payload = [pscustomobject]@{ Mof = (@($filter, $consumer, $binding) | ForEach-Object { $_.PSBase.GetText('Mof') }) -join "`n" }{{Json}}
                    """;
                // Filter and consumer go too, unless another binding still uses them.
                remove = """
                    $others = @($all | Where-Object { $_.__RELPATH -ne $binding.__RELPATH })
                    $binding.Delete()
                    if (-not ($others | Where-Object { $_.Filter -eq $binding.Filter })) { $filter.Delete() }
                    if (-not ($others | Where-Object { $_.Consumer -eq $binding.Consumer })) { $consumer.Delete() }
                    """;
                break;
            default:
                var (hive, subKey, value) = RegistryTarget(entry);
                var open = $"$hive = [Microsoft.Win32.RegistryKey]::OpenBaseKey({Q(hive)}, 'Registry64')\n" + RegistryCapture + "\n";
                if (value is not null)
                {
                    capture = open + $$"""
                        $key = $hive.OpenSubKey({{Q(subKey)}}, $true)
                        if ($null -eq $key -or $null -eq $key.GetValue({{Q(value)}})) { throw 'The startup value no longer exists.' }
                        $payload = [pscustomobject]@{ Name = ''; Values = @(Value $key {{Q(value)}}); Keys = @() }{{Json}}
                        """;
                    remove = $"$key.DeleteValue({Q(value)})";
                    break;
                }
                capture = open + $$"""
                    $key = $hive.OpenSubKey({{Q(subKey)}})
                    if ($null -eq $key) { throw 'The startup key no longer exists.' }
                    $payload = Tree $key ''{{Json}}
                    $key.Close()
                    """;
                // sc.exe delete lets the service control manager drop the service; removing its key under it would not.
                remove = entry.Kind == "Service"
                    ? $"& \"$env:SystemRoot\\System32\\sc.exe\" delete {Q(entry.Location)} | Out-Null; if ($LASTEXITCODE) {{ throw \"sc.exe delete failed: $LASTEXITCODE\" }}"
                    : $"$hive.DeleteSubKeyTree({Q(subKey)})";
                break;
        }

        // A running service would only be marked for deletion: it keeps running and its key stays until a restart.
        // Stop it first, never its running dependents: if it cannot stop, nothing is stored or deleted.
        var prepare = entry.Kind == "Service"
            ? $$"""
                Add-Type -AssemblyName System.ServiceProcess
                $service = New-Object System.ServiceProcess.ServiceController({{Q(entry.Location)}})
                if ($service.Status -ne 'Stopped') {
                    if (@($service.DependentServices | Where-Object { $_.Status -ne 'Stopped' }).Count) { throw 'Other running services depend on this service; nothing was deleted.' }
                    $service.Stop()
                    $service.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
                }
                """
            : string.Empty;

        record ??= NewBackup(entry);
        var json = Q(JsonSerializer.Serialize(record with { Payload = string.Empty }));
        // Saved before the delete. Machine-wide entries go to HKLM, which only administrators can write; user entries
        // to the user's own folder, written unelevated.
        var store = entry.DeleteNeedsAdministrator
            ? $$"""
                $store = [Microsoft.Win32.RegistryKey]::OpenBaseKey('LocalMachine', 'Registry64').CreateSubKey({{Q(MachineStore + ValidId(record.Id))}})
                $store.SetValue('Record', {{json}})
                $store.SetValue('Payload', $payload)
                $store.Close()
                """
            : $$"""
                [void][IO.Directory]::CreateDirectory({{Q(BackupFolder)}})
                $record = ConvertFrom-Json {{json}}
                $record.Payload = $payload
                [IO.File]::WriteAllText({{Q(System.IO.Path.Combine(BackupFolder, ValidId(record.Id) + ".json"))}}, ($record | ConvertTo-Json -Compress), [Text.UTF8Encoding]::new($false))
                """;
        return $"{capture}\n{prepare}\n{store}\n{remove}";
    }

    internal static string BuildRestoreScript(StartupBackup backup)
    {
        static string Q(string value) => PowerShellService.Literal(value);
        var entry = backup.Entry;
        var machine = entry.DeleteNeedsAdministrator;
        if (machine && !backup.Machine)
            throw new InvalidOperationException("This backup is not in the administrator store; it is not restored with administrator rights.");
        // The machine record is removed by the same elevated script that restores it.
        var tail = machine
            ? $"\n[Microsoft.Win32.RegistryKey]::OpenBaseKey('LocalMachine', 'Registry64').DeleteSubKeyTree({Q(MachineStore + ValidId(backup.Id))}, $false)"
            : string.Empty;
        using var document = JsonDocument.Parse(backup.Payload);
        var payload = document.RootElement;
        switch (entry.Kind)
        {
            case "Task":
                ValidateTask(entry);
                return $"Register-ScheduledTask -Xml {Q(JsonRead.Text(payload, "Xml"))} -TaskPath {Q(entry.Location)} -TaskName {Q(entry.Name)} | Out-Null" + tail;
            case "Folder":
                return $$"""
                    $path = {{Q(StartupFile(entry))}}
                    if (Test-Path -LiteralPath $path) { throw 'A file with this name is already in the startup folder.' }
                    [IO.File]::WriteAllBytes($path, [Convert]::FromBase64String({{Q(JsonRead.Text(payload, "Data"))}}))
                    """ + tail;
            case "Wmi":
                // Windows records the creator itself and refuses a restored CreatorSID.
                var mof = Regex.Replace(JsonRead.Text(payload, "Mof"), @"^\s*CreatorSID\s*=\s*\{[^}]*\};\s*$\n?", "", RegexOptions.Multiline);
                return $$"""
                    $file = Join-Path $env:SystemRoot ('Temp\TweakHub-' + [guid]::NewGuid().ToString('N') + '.mof')
                    [IO.File]::WriteAllText($file, "#pragma namespace(`"\\\\.\\root\\subscription`")`n" + {{Q(mof)}}, [Text.Encoding]::Unicode)
                    try { & "$env:SystemRoot\System32\wbem\mofcomp.exe" $file | Out-Null; if ($LASTEXITCODE) { throw "mofcomp failed: $LASTEXITCODE" } }
                    finally { Remove-Item -LiteralPath $file -Force -ErrorAction SilentlyContinue }
                    """ + tail;
        }

        var (hive, subKey, value) = RegistryTarget(entry);
        var script = new StringBuilder($"$hive = [Microsoft.Win32.RegistryKey]::OpenBaseKey({Q(hive)}, 'Registry64')\n");
        if (value is not null)
        {
            // Only the value that was deleted, whatever else the payload holds.
            script.AppendLine($"$key = $hive.CreateSubKey({Q(subKey)})")
                .AppendLine($"if ($null -ne $key.GetValue({Q(value)})) {{ throw 'The startup value already exists.' }}");
            foreach (var item in JsonRead.Items(payload, "Values").Where(item => JsonRead.Text(item, "Name") == value))
                script.AppendLine(SetValue(item));
            script.Append("$key.Close()");
        }
        else
        {
            script.AppendLine($"if ($hive.OpenSubKey({Q(subKey)})) {{ throw 'The startup key already exists.' }}");
            AppendTree(script, payload, subKey);
        }
        return script.ToString().TrimEnd() + tail;

        static void AppendTree(StringBuilder script, JsonElement tree, string subKey)
        {
            script.AppendLine($"$key = $hive.CreateSubKey({Q(subKey)})");
            foreach (var item in JsonRead.Items(tree, "Values")) script.AppendLine(SetValue(item));
            script.AppendLine("$key.Close()");
            foreach (var child in JsonRead.Items(tree, "Keys"))
                AppendTree(script, child, subKey + "\\" + JsonRead.Text(child, "Name"));
        }

        static string SetValue(JsonElement item)
        {
            var data = item.TryGetProperty("Data", out var element) ? element : default;
            var (literal, kind) = JsonRead.Text(item, "Kind") switch
            {
                "String" or "ExpandString" => (Q(data.ValueKind == JsonValueKind.String ? data.GetString()! : ""), JsonRead.Text(item, "Kind")),
                "DWord" => ($"[int]{data.GetInt32().ToString(CultureInfo.InvariantCulture)}", "DWord"),
                "QWord" => ($"[long]{data.GetInt64().ToString(CultureInfo.InvariantCulture)}", "QWord"),
                "MultiString" => ($"[string[]]@({string.Join(", ", JsonRead.Items(data).Select(text => Q(text.GetString() ?? "")))})", "MultiString"),
                // shortcut: REG_NONE and other rare types come back as REG_BINARY with the same bytes; keep the type if one ever matters.
                _ => ($"[byte[]][Convert]::FromBase64String({Q(data.ValueKind == JsonValueKind.String ? data.GetString()! : "")})", "Binary")
            };
            return $"$key.SetValue({Q(JsonRead.Text(item, "Name"))}, {literal}, {Q(kind)})";
        }
    }

    private static string BackupFolder => System.IO.Path.Combine(AppDataPath.BasePath, "startup-backups");

    /// <summary>User entries from the user's backup files, machine-wide ones from the administrator store in HKLM.</summary>
    public static IReadOnlyList<StartupBackup> LoadBackups()
    {
        var backups = new List<StartupBackup>();
        if (Directory.Exists(BackupFolder))
            foreach (var file in Directory.EnumerateFiles(BackupFolder, "*.json"))
                try { if (JsonSerializer.Deserialize<StartupBackup>(File.ReadAllText(file)) is { } backup && Complete(backup)) backups.Add(backup); }
                catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException) { }
        using var store = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64)
            .OpenSubKey(MachineStore);
        foreach (var id in store?.GetSubKeyNames().Where(id => BackupIdPattern.IsMatch(id)) ?? [])
        {
            using var key = store!.OpenSubKey(id);
            try
            {
                if (key?.GetValue("Record") is string record && JsonSerializer.Deserialize<StartupBackup>(record) is { } backup
                    && Complete(backup = backup with { Id = id, Payload = key.GetValue("Payload") as string ?? string.Empty, Machine = true }))
                    backups.Add(backup);
            }
            catch (JsonException) { }
        }
        return backups.OrderByDescending(backup => backup.DeletedAt).ToList();

        // A truncated or hand-edited file can miss fields; System.Text.Json leaves them null.
        static bool Complete(StartupBackup b) => b is { Id: not null, Kind: not null, Name: not null, Command: not null, Location: not null,
            Path: not null, ApprovedKey: not null, Payload: not null };
    }

    /// <summary>Deletes the entry; the same script saves its backup first.</summary>
    public async Task DeleteAsync(StartupEntry entry)
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(BuildDeleteScript(entry, NewBackup(entry)), entry.DeleteNeedsAdministrator, TimeSpan.FromMinutes(2));
        if (!result.Success) throw new InvalidOperationException(ErrorText(result));
    }

    public async Task RestoreAsync(StartupBackup backup)
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(BuildRestoreScript(backup), backup.Entry.DeleteNeedsAdministrator, TimeSpan.FromMinutes(2));
        if (!result.Success) throw new InvalidOperationException(ErrorText(result));
        if (!backup.Machine) File.Delete(System.IO.Path.Combine(BackupFolder, ValidId(backup.Id) + ".json"));
    }

    private static string ErrorText(PowerShellResult result) =>
        string.IsNullOrWhiteSpace(result.Error) ? result.ExitCode.ToString(CultureInfo.InvariantCulture) : result.Error.Trim();

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
