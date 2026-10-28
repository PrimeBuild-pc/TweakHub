using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TweakHub.Localization;

namespace TweakHub.Services;

public enum DeviceCategory { Healthy, NotPresent, Disabled, Problem, IdleBus }

/// <summary>Common: usually safe to disable. Situational: depends on features in use. Avoid: often recommended but harmful.</summary>
public enum DeviceAdvice { Common, Situational, Avoid }

public sealed record DeviceRule(string Id, DeviceAdvice Advice, Regex Match);

public sealed record PnpDeviceInfo(string Id, string Name, string Class, string Status, bool Present, int Problem, string Parent, string Service, string Inf)
{
    public int Children { get; init; }
    /// <summary>Product name the device reports over USB (for example "G502 HERO"); empty for most other buses.</summary>
    public string BusName { get; init; } = string.Empty;
    /// <summary>PCI: bus number and (device &lt;&lt; 16 | function); USB: Address is the hub port. -1 when the bus reports none.</summary>
    public int BusNumber { get; init; } = -1;
    public int Address { get; init; } = -1;
    /// <summary>PCIe link as Windows reads it from the device: generation (1 = 2.5 GT/s) and lanes; 0 when the device has none.</summary>
    public int LinkGen { get; init; }
    public int LinkWidth { get; init; }
    public int MaxLinkGen { get; init; }
    public int MaxLinkWidth { get; init; }
    public DeviceRule? Rule { get; init; }
    public bool DisabledByTweakHub { get; init; }

    public DeviceCategory Category => !Present ? DeviceCategory.NotPresent
        : Problem == 22 ? DeviceCategory.Disabled
        : Status != "OK" || Problem != 0 ? DeviceCategory.Problem
        : Children == 0 && Regex.IsMatch(Name, @"(?i)enumerat|\bbus\b") ? DeviceCategory.IdleBus
        : DeviceCategory.Healthy;

    /// <summary>Core classes and ACPI/PCI/ROOT devices: disabling one can stop the PC from booting or remove input.</summary>
    public bool HighRisk => Rule?.Advice != DeviceAdvice.Common
        && (DeviceService.CriticalClasses.Contains(Class) || Regex.IsMatch(Id, @"^(?i:ACPI|PCI|ROOT)\\"));

    public bool IsDisabled => Category == DeviceCategory.Disabled;
    /// <summary>Enable is the action: disabled now, or disabled by TweakHub and unplugged (Enable-PnpDevice works on it too).</summary>
    public bool NeedsEnable => IsDisabled || !Present && DisabledByTweakHub;
    public bool CanToggle => Present || DisabledByTweakHub;
    public bool IsSuggestion => Present && Rule is { Advice: not DeviceAdvice.Avoid } && !IsDisabled;
    public string DisplayName => Name.Length > 0 ? Name : Id;
    public string CategoryText => L.Get($"Devices:Category{Category}");
    public string AdviceText => !Present ? CategoryText : L.Get(Rule is null ? "Devices:AdviceUnknown" : $"Devices:Advice{Rule.Advice}");
    public string ReasonText => L.Get(Rule is null ? "Devices:RuleUnknown" : $"Devices:Rule{Rule.Id}");
    public string BreaksText => L.Get(Rule is null ? "Devices:RuleUnknownBreaks" : $"Devices:Rule{Rule.Id}Breaks");
    public string ToggleText => L.Get(NeedsEnable ? "Devices:Enable" : "Devices:Disable");
    public string CategoryHintText => L.Get($"Devices:Category{Category}Hint");
}

public sealed class DeviceService
{
    public static DeviceService Instance { get; } = new();
    private DeviceService() { }

    internal static readonly HashSet<string> CriticalClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Computer", "Processor", "Display", "DiskDrive", "SCSIAdapter", "HDC", "Net", "USB", "HIDClass",
        "Keyboard", "Mouse", "MEDIA", "Firmware", "SecurityDevices", "Battery", "Volume", "VolumeSnapshot", "Storage",
        "Bluetooth", "Monitor", "SoftwareComponent", "SoftwareDevice", "Biometric", "SmartCardReader", "NetService"
    };

    // Matched against "<instance id>|<name>". Order matters: the first match wins.
    internal static readonly DeviceRule[] Rules =
    [
        new("Hpet", DeviceAdvice.Avoid, new(@"(?i)^ACPI\\PNP0103\\|High precision event timer")),
        new("SmBus", DeviceAdvice.Avoid, new(@"(?i)\bSM ?Bus\b")),
        new("KernelDebug", DeviceAdvice.Common, new(@"(?i)^ROOT\\KDNIC\\|Kernel Debug Network Adapter")),
        new("SystemSpeaker", DeviceAdvice.Common, new(@"(?i)^ACPI\\PNP0800\\|System speaker")),
        new("WanMiniport", DeviceAdvice.Situational, new(@"(?i)^SWD\\MSRRAS\\|WAN Miniport")),
        new("RdpBus", DeviceAdvice.Situational, new(@"(?i)^ROOT\\RDPBUS\\|Remote Desktop Device Redirector")),
        new("VirtualDrive", DeviceAdvice.Situational, new(@"(?i)^ROOT\\VDRVROOT\\|Virtual Drive Enumerator")),
        new("NdisVirtual", DeviceAdvice.Situational, new(@"(?i)^ROOT\\NDISVIRTUALBUS\\|NDIS Virtual Network Adapter Enumerator")),
        new("UmBus", DeviceAdvice.Situational, new(@"(?i)^ROOT\\UMBUS\\|UMBus Root Bus Enumerator")),
        new("CompositeBus", DeviceAdvice.Situational, new(@"(?i)^ROOT\\COMPOSITEBUS\\|Composite Bus Enumerator")),
        new("HyperV", DeviceAdvice.Situational, new(@"(?i)^ROOT\\VID\\|Hyper-V Virtualization Infrastructure")),
        new("WifiDirect", DeviceAdvice.Situational, new(@"(?i)Wi-Fi Direct Virtual Adapter")),
        new("Teredo", DeviceAdvice.Situational, new(@"(?i)Teredo Tunneling|ISATAP")),
        new("VirtualPrinter", DeviceAdvice.Situational, new(@"(?i)^SWD\\PRINTENUM\\.*\|(Microsoft Print to PDF|Microsoft XPS Document Writer|Fax|OneNote)"))
    ];

    private static readonly DeviceRule HubRule = new("UsbHub", DeviceAdvice.Avoid, new("^USB\\\\", RegexOptions.IgnoreCase));

    internal static DeviceRule? MatchRule(string id, string name, string service = "") =>
        Rules.FirstOrDefault(rule => rule.Match.IsMatch($"{id}|{name}"))
        ?? (service.Equals("USBHUB3", StringComparison.OrdinalIgnoreCase) || service.Equals("USBHUB", StringComparison.OrdinalIgnoreCase) ? HubRule : null);

    internal const string InventoryScript = """
        $ErrorActionPreference = 'SilentlyContinue'
        $devices = @(Get-PnpDevice)
        $props = @{}
        # One pipeline for every device: about 25 ms per device instead of one process each.
        $devices | Get-PnpDeviceProperty -KeyName DEVPKEY_Device_Parent, DEVPKEY_Device_Service, DEVPKEY_Device_DriverInfPath, DEVPKEY_Device_BusReportedDeviceDesc,
            DEVPKEY_Device_IsPresent, DEVPKEY_Device_ProblemCode,
            DEVPKEY_Device_BusNumber, DEVPKEY_Device_Address, DEVPKEY_PciDevice_CurrentLinkSpeed, DEVPKEY_PciDevice_CurrentLinkWidth,
            DEVPKEY_PciDevice_MaxLinkSpeed, DEVPKEY_PciDevice_MaxLinkWidth | ForEach-Object {
            if (-not $props.ContainsKey($_.InstanceId)) { $props[$_.InstanceId] = @{} }
            $props[$_.InstanceId][$_.KeyName] = $_.Data
        }
        @($devices | ForEach-Object {
            $p = $props[$_.InstanceId]
            if (-not $p) { $p = @{} }
            $present = $p['DEVPKEY_Device_IsPresent']
            if ($null -eq $present) { $present = $_.Present }
            $problem = $p['DEVPKEY_Device_ProblemCode']
            if ($null -eq $problem) { $problem = $_.ConfigManagerErrorCode }
            [pscustomobject]@{ Id = [string]$_.InstanceId; Name = [string]$_.FriendlyName; Class = [string]$_.Class; Status = [string]$_.Status
                Present = [bool]$present; Problem = [int]$problem; Parent = [string]$p['DEVPKEY_Device_Parent']
                Service = [string]$p['DEVPKEY_Device_Service']; Inf = [string]$p['DEVPKEY_Device_DriverInfPath']
                BusName = [string]$p['DEVPKEY_Device_BusReportedDeviceDesc']
                BusNumber = $p['DEVPKEY_Device_BusNumber']; Address = $p['DEVPKEY_Device_Address']
                LinkGen = $p['DEVPKEY_PciDevice_CurrentLinkSpeed']; LinkWidth = $p['DEVPKEY_PciDevice_CurrentLinkWidth']
                MaxLinkGen = $p['DEVPKEY_PciDevice_MaxLinkSpeed']; MaxLinkWidth = $p['DEVPKEY_PciDevice_MaxLinkWidth'] }
        }) | ConvertTo-Json -Compress
        """ + PowerShellService.AsciiJson;

    public async Task<IReadOnlyList<PnpDeviceInfo>> GetDevicesAsync()
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(InventoryScript, timeout: TimeSpan.FromMinutes(3));
        if (!result.Success) throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.Error) ? result.ExitCode.ToString() : result.Error);
        return ParseDevices(result.Output, LoadDisabled().Keys);
    }

    internal static List<PnpDeviceInfo> ParseDevices(string json, IEnumerable<string> disabledByTweakHub)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        using var document = JsonDocument.Parse(json);
        var raw = JsonRead.Items(document.RootElement).Select(item => new PnpDeviceInfo(
            JsonRead.Text(item, "Id"), JsonRead.Text(item, "Name"), JsonRead.Text(item, "Class"), JsonRead.Text(item, "Status"),
            JsonRead.Bool(item, "Present", fallback: true), JsonRead.Int(item, "Problem") ?? 0,
            JsonRead.Text(item, "Parent"), JsonRead.Text(item, "Service"), JsonRead.Text(item, "Inf"))
            {
                BusName = JsonRead.Text(item, "BusName"),
                BusNumber = JsonRead.Int(item, "BusNumber") ?? -1, Address = JsonRead.Int(item, "Address") ?? -1,
                LinkGen = JsonRead.Int(item, "LinkGen") ?? 0, LinkWidth = JsonRead.Int(item, "LinkWidth") ?? 0,
                MaxLinkGen = JsonRead.Int(item, "MaxLinkGen") ?? 0, MaxLinkWidth = JsonRead.Int(item, "MaxLinkWidth") ?? 0
            })
            .Where(device => device.Id.Length > 0)
            .ToList();

        // Children come from the parent links, so one property per device is enough.
        var children = raw.Where(device => device.Present && device.Parent.Length > 0)
            .GroupBy(device => device.Parent, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var tracked = disabledByTweakHub.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return raw.Select(device => device with
            {
                Children = children.GetValueOrDefault(device.Id),
                Rule = MatchRule(device.Id, device.Name, device.Service),
                DisabledByTweakHub = tracked.Contains(device.Id)
            })
            .OrderBy(device => device.Category).ThenBy(device => device.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    // Devices TweakHub disabled, by instance id, with their name for the restore list.
    private static string DisabledPath => Path.Combine(AppDataPath.BasePath, "disabled-devices.json");
    private static Dictionary<string, string> LoadDisabled() => JsonStore.Load<Dictionary<string, string>>(DisabledPath);

    public static bool HasDisabledDevices => LoadDisabled().Count > 0;

    internal static string BuildToggleScript(IEnumerable<string> instanceIds, bool enable) => string.Join("\n", instanceIds.Select(id =>
        $"{(enable ? "Enable" : "Disable")}-PnpDevice -InstanceId {PowerShellService.Literal(id)} -Confirm:$false -ErrorAction Stop"));

    public async Task<PowerShellResult> SetEnabledAsync(PnpDeviceInfo device, bool enable)
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(BuildToggleScript([device.Id], enable), requireAdministrator: true, TimeSpan.FromMinutes(2));
        if (result.Success)
        {
            var disabled = LoadDisabled();
            if (enable) disabled.Remove(device.Id); else disabled[device.Id] = device.DisplayName;
            JsonStore.Save(DisabledPath, disabled);
        }
        return result;
    }

    /// <summary>
    /// Playbook toggle: the saved instance id first, then the only device of the same class and name (instance ids change
    /// between PCs). Prints SKIPPED when the device is not on this PC, otherwise the instance id it changed.
    /// </summary>
    internal static string BuildMatchingToggleScript(string id, string name, string deviceClass, bool enable) => $$"""
        $device = Get-PnpDevice -InstanceId {{PowerShellService.Literal(id)}} -ErrorAction SilentlyContinue
        if (-not $device) {
            $device = @(Get-PnpDevice -Class {{PowerShellService.Literal(deviceClass.Length > 0 ? deviceClass : "-")}} -ErrorAction SilentlyContinue |
                Where-Object FriendlyName -eq {{PowerShellService.Literal(name)}})
            if ($device.Count -ne 1) { 'SKIPPED'; exit 0 }
            $device = $device[0]
        }
        {{(enable ? "Enable" : "Disable")}}-PnpDevice -InstanceId $device.InstanceId -Confirm:$false -ErrorAction Stop
        $device.InstanceId
        """;

    public async Task<(bool Success, bool Skipped, string Error)> SetEnabledMatchingAsync(string id, string name, string deviceClass, bool enable)
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(BuildMatchingToggleScript(id, name, deviceClass, enable),
            requireAdministrator: true, TimeSpan.FromMinutes(2));
        if (!result.Success) return (false, false, result.Error);
        var changed = result.Output.Trim().Split('\n').LastOrDefault()?.Trim() ?? string.Empty;
        if (changed is "SKIPPED" or "") return (true, true, string.Empty);
        var disabled = LoadDisabled();
        if (enable) disabled.Remove(changed); else disabled[changed] = name;
        JsonStore.Save(DisabledPath, disabled);
        return (true, false, string.Empty);
    }

    public async Task<PowerShellResult> EnableAllTrackedAsync()
    {
        var disabled = LoadDisabled();
        var result = await PowerShellService.Instance.ExecuteScriptAsync(BuildToggleScript(disabled.Keys, enable: true), requireAdministrator: true, TimeSpan.FromMinutes(5));
        if (result.Success) JsonStore.Save(DisabledPath, new Dictionary<string, string>());
        return result;
    }

    /// <summary>
    /// What this PC shows it does not use: Remote Desktop off, no VPN/PPPoE entries for this user or all users, physical
    /// network cards without a link. Other accounts keep their own VPN list: the ones that have entries, and the ones whose
    /// profile this process cannot read, are named.
    /// </summary>
    public sealed record UsageContext(bool RemoteDesktopOff, bool NoDialUpEntries, IReadOnlySet<string> DisconnectedNics, bool AnyNicConnected)
    {
        public IReadOnlyList<string> OtherVpnUsers { get; init; } = [];
        public IReadOnlyList<string> UncheckedUsers { get; init; } = [];
    }

    internal const string UsageScript = """
        $ErrorActionPreference = 'SilentlyContinue'
        $rdp = (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server').fDenyTSConnections
        # Windows VPN and PPPoE (dial-up) connections live in these phone books, one [section] per entry.
        $entries = @(@("$env:APPDATA\Microsoft\Network\Connections\Pbk\rasphone.pbk", "$env:ProgramData\Microsoft\Network\Connections\Pbk\rasphone.pbk") |
            Where-Object { Test-Path -LiteralPath $_ } | ForEach-Object { Select-String -LiteralPath $_ -Pattern '^\[' }).Count
        # Other people's accounts (not service profiles): -1 when this process may not read their profile.
        $me = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
        $others = @(Get-CimInstance Win32_UserProfile | Where-Object { $_.SID -ne $me -and $_.SID -match '^S-1-(5-21|12-1)-' } | ForEach-Object {
            $name = try { ([Security.Principal.SecurityIdentifier]$_.SID).Translate([Security.Principal.NTAccount]).Value } catch { Split-Path -Leaf $_.LocalPath }
            $roaming = Join-Path $_.LocalPath 'AppData\Roaming'
            $count = try {
                [void][IO.Directory]::GetDirectories($roaming)
                $pbk = Join-Path $roaming 'Microsoft\Network\Connections\Pbk\rasphone.pbk'
                if (Test-Path -LiteralPath $pbk) { @(Select-String -LiteralPath $pbk -Pattern '^\[').Count } else { 0 }
            } catch [UnauthorizedAccessException] { -1 } catch { 0 }
            [pscustomobject]@{ User = [string]$name; Entries = [int]$count } })
        $nics = @(Get-NetAdapter -Physical | ForEach-Object { [pscustomobject]@{ Id = [string]$_.PnPDeviceID; Connected = [int]$_.MediaConnectionState -eq 1 } })
        [pscustomobject]@{ RemoteDesktopOff = $rdp -eq 1; DialUpEntries = $entries; Others = $others; Nics = $nics } | ConvertTo-Json -Compress -Depth 3
        """ + PowerShellService.AsciiJson;

    public async Task<UsageContext> GetUsageContextAsync()
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(UsageScript, timeout: TimeSpan.FromMinutes(1));
        if (!result.Success) throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.Error) ? result.ExitCode.ToString() : result.Error);
        return ParseUsageContext(result.Output);
    }

    internal static UsageContext ParseUsageContext(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var nics = root.TryGetProperty("Nics", out var list) ? JsonRead.Items(list).ToList() : [];
        var others = root.TryGetProperty("Others", out var users) ? JsonRead.Items(users).ToList() : [];
        List<string> Users(Func<int, bool> entries) => others.Where(user => entries(JsonRead.Int(user, "Entries") ?? -1))
            .Select(user => JsonRead.Text(user, "User")).Where(name => name.Length > 0).ToList();
        return new(JsonRead.Bool(root, "RemoteDesktopOff"), (JsonRead.Int(root, "DialUpEntries") ?? 1) == 0,
            nics.Where(nic => !JsonRead.Bool(nic, "Connected")).Select(nic => JsonRead.Text(nic, "Id")).Where(id => id.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase),
            nics.Any(nic => JsonRead.Bool(nic, "Connected")))
        {
            OtherVpnUsers = Users(entries => entries > 0),
            UncheckedUsers = Users(entries => entries < 0)
        };
    }

    // The VPN and PPPoE miniports; IP, IPv6 and Network Monitor serve other RAS features and are left alone.
    private static readonly Regex DialUpMiniport = new(@"(?i)^SWD\\MSRRAS\\MS_(SSTP|PPTP|L2TP|AGILEVPN|PPPOE)MINIPORT$");

    /// <summary>Why this device counts as unused here (a Devices:Unused* key), or null. Never a device already off or not present.</summary>
    internal static string? UnusedReason(PnpDeviceInfo device, UsageContext context)
    {
        if (!device.Present || device.IsDisabled) return null;
        if (device.Rule?.Advice == DeviceAdvice.Common) return "Devices:UnusedAlways";
        if (device.Rule?.Id == "Teredo") return "Devices:UnusedTunnel";
        if (device.Rule?.Id == "RdpBus" && context.RemoteDesktopOff) return "Devices:UnusedRemoteDesktop";
        if (DialUpMiniport.IsMatch(device.Id) && context.NoDialUpEntries)
            return context.OtherVpnUsers.Count > 0 ? "Devices:UnusedVpnOtherUser"
                : context.UncheckedUsers.Count > 0 ? "Devices:UnusedVpnUnchecked" : "Devices:UnusedVpn";
        // Never the last way online: only when another physical card has a link.
        if (context.DisconnectedNics.Contains(device.Id) && context.AnyNicConnected) return "Devices:UnusedNic";
        return null;
    }

    /// <summary>The reason shown in the list; for other accounts it names them.</summary>
    public static string UnusedText(string reason, UsageContext context) => reason switch
    {
        "Devices:UnusedVpnOtherUser" => L.Format(reason, string.Join(", ", context.OtherVpnUsers)),
        "Devices:UnusedVpnUnchecked" => L.Format(reason, string.Join(", ", context.UncheckedUsers)),
        _ => L.Get(reason)
    };

    /// <summary>Ticked in the list unless it may still be needed: a card without a link today, another account's VPN.</summary>
    public static bool TickedByDefault(string reason) =>
        reason is not ("Devices:UnusedNic" or "Devices:UnusedVpnOtherUser" or "Devices:UnusedVpnUnchecked");

    /// <summary>One elevated script for the whole list; each line reports OK|id or FAIL|id|reason so partial runs stay tracked.</summary>
    internal static string BuildDisableManyScript(IEnumerable<string> instanceIds) =>
        $"foreach ($id in @({string.Join(", ", instanceIds.Select(PowerShellService.Literal))})) {{\n" +
        "    try { Disable-PnpDevice -InstanceId $id -Confirm:$false -ErrorAction Stop; \"OK|$id\" } catch { \"FAIL|$id|$($_.Exception.Message)\" }\n}";

    /// <summary>Disables every device given; returns the names that failed. Disabled ones are tracked for "Enable all".</summary>
    public async Task<IReadOnlyList<string>> DisableManyAsync(IReadOnlyList<PnpDeviceInfo> devices)
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(BuildDisableManyScript(devices.Select(device => device.Id)),
            requireAdministrator: true, TimeSpan.FromMinutes(5));
        // Declined UAC: no device line at all, and the reason is in the error.
        if (!result.Output.Contains('|')) throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.Error) ? result.ExitCode.ToString() : result.Error.Trim());
        var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var done = lines.Where(line => line.StartsWith("OK|", StringComparison.Ordinal)).Select(line => line[3..]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var disabled = LoadDisabled();
        foreach (var device in devices.Where(device => done.Contains(device.Id))) disabled[device.Id] = device.DisplayName;
        JsonStore.Save(DisabledPath, disabled);
        return devices.Where(device => !done.Contains(device.Id)).Select(device => device.DisplayName).ToList();
    }

    public static string ToCsv(IEnumerable<PnpDeviceInfo> devices)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Csv.Row("Name", "Class", "Status", "Present", "Category", "HighRisk", "Advice", "Children", "Problem", "Service", "Inf", "Parent", "InstanceId"));
        foreach (var d in devices)
            builder.AppendLine(Csv.Row(d.DisplayName, d.Class, d.Status, d.Present, d.Category, d.HighRisk, d.Rule?.Advice.ToString() ?? "", d.Children, d.Problem, d.Service, d.Inf, d.Parent, d.Id));
        return builder.ToString();
    }
}
