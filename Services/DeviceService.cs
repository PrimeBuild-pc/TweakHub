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
    /// <summary>Ghosts only: the later of the last arrival and last removal; null when Windows recorded neither.</summary>
    public DateTime? LastUsed { get; init; }
    /// <summary>Ghosts only: the setup class as Device Manager names it ("Keyboards"); empty when unknown.</summary>
    public string TypeName { get; init; } = string.Empty;
    /// <summary>Ghosts only: the manufacturer the driver names, and when Windows first installed the device.</summary>
    public string Manufacturer { get; init; } = string.Empty;
    public DateTime? InstalledOn { get; init; }

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
        public IReadOnlyList<NicLink> Nics { get; init; } = [];
        /// <summary>HD Audio functions of a graphics card (HDMI/DP) that no default audio device uses.</summary>
        public IReadOnlySet<string> SpareGpuAudio { get; init; } = new HashSet<string>();
        /// <summary>Other HD Audio functions (the built-in sound card) that no default audio device uses.</summary>
        public IReadOnlySet<string> SpareOnboardAudio { get; init; } = new HashSet<string>();

        /// <summary>The connected card to keep: wired before wireless, then the fastest link.</summary>
        public NicLink? BestNic => Nics.Where(nic => nic.Connected).OrderBy(nic => nic.Wireless).ThenByDescending(nic => nic.Speed).FirstOrDefault();
    }

    public sealed record NicLink(string Id, string Name, bool Connected, bool Wireless, long Speed);

    /// <summary>
    /// Adds the audio functions nobody listens to: an HD Audio function is in use when a default playback or recording
    /// endpoint hangs below it. A GPU function is the one in the slot of a display device from the same vendor.
    /// </summary>
    public static UsageContext WithAudio(UsageContext context, IReadOnlyList<PnpDeviceInfo> devices, IReadOnlyCollection<string> defaultEndpointIds)
    {
        if (defaultEndpointIds.Count == 0) return context;
        var byId = devices.DistinctBy(device => device.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(device => device.Id, StringComparer.OrdinalIgnoreCase);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in defaultEndpointIds)
            for (var current = byId.GetValueOrDefault(@"SWD\MMDEVAPI\" + id); current is not null && used.Add(current.Id);
                 current = byId.GetValueOrDefault(current.Parent)) { }
        var displays = devices.Where(device => device.Class == "Display" && device.BusNumber >= 0 && device.Address >= 0).ToList();
        bool OnGpu(PnpDeviceInfo controller) => displays.Any(display => display.BusNumber == controller.BusNumber
            && display.Address >> 16 == controller.Address >> 16 && InterruptLayoutPlanner.PciVendor(display.Id) == InterruptLayoutPlanner.PciVendor(controller.Id));
        var spare = devices.Where(device => device.Id.StartsWith(@"HDAUDIO\", StringComparison.OrdinalIgnoreCase) && device.Present
                && !device.IsDisabled && !used.Contains(device.Id))
            .Select(device => (device.Id, Gpu: byId.GetValueOrDefault(device.Parent) is { } controller && OnGpu(controller))).ToList();
        return context with
        {
            SpareGpuAudio = spare.Where(item => item.Gpu).Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase),
            SpareOnboardAudio = spare.Where(item => !item.Gpu).Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase)
        };
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
        # NdisPhysicalMedium 9 is native 802.11 (Wi-Fi); Speed is the link in bits per second.
        $nics = @(Get-NetAdapter -Physical | ForEach-Object { [pscustomobject]@{ Id = [string]$_.PnPDeviceID; Name = [string]$_.InterfaceDescription
            Connected = [int]$_.MediaConnectionState -eq 1; Wireless = [int]$_.NdisPhysicalMedium -eq 9; Speed = [long]$_.Speed } })
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
            UncheckedUsers = Users(entries => entries < 0),
            Nics = nics.Select(nic => new NicLink(JsonRead.Text(nic, "Id"), JsonRead.Text(nic, "Name"), JsonRead.Bool(nic, "Connected"),
                JsonRead.Bool(nic, "Wireless"), nic.TryGetProperty("Speed", out var speed) && speed.TryGetInt64(out var bits) ? bits : 0))
                .Where(nic => nic.Id.Length > 0).ToList()
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
        // Two live connections: Windows may route games over the worse one.
        if (context.Nics.Any(nic => nic.Connected && nic.Id.Equals(device.Id, StringComparison.OrdinalIgnoreCase))
            && context.BestNic is { } best && !best.Id.Equals(device.Id, StringComparison.OrdinalIgnoreCase)) return "Devices:UnusedNicSlower";
        if (context.SpareGpuAudio.Contains(device.Id)) return "Devices:UnusedGpuAudio";
        if (context.SpareOnboardAudio.Contains(device.Id)) return "Devices:UnusedOnboardAudio";
        return null;
    }

    /// <summary>The reason shown in the list; for other accounts it names them.</summary>
    public static string UnusedText(string reason, UsageContext context) => reason switch
    {
        "Devices:UnusedVpnOtherUser" => L.Format(reason, string.Join(", ", context.OtherVpnUsers)),
        "Devices:UnusedVpnUnchecked" => L.Format(reason, string.Join(", ", context.UncheckedUsers)),
        "Devices:UnusedNicSlower" => L.Format(reason, context.BestNic?.Name ?? string.Empty),
        _ => L.Get(reason)
    };

    /// <summary>Ticked in the list unless it may still be needed: a card without a link today, another account's VPN, a second connection, audio.</summary>
    public static bool TickedByDefault(string reason) =>
        reason is not ("Devices:UnusedNic" or "Devices:UnusedVpnOtherUser" or "Devices:UnusedVpnUnchecked"
            or "Devices:UnusedNicSlower" or "Devices:UnusedGpuAudio" or "Devices:UnusedOnboardAudio");

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

    // Devices created by software rather than found on a bus: some are hidden on purpose and still needed (virtual adapters,
    // app-created devices), so they are listed but never ticked by default.
    private static readonly string[] SoftwareEnumeratorPrefixes = [@"ROOT\", @"SWD\", @"HTREE\", @"SW\", @"UMB\", "{"];

    /// <summary>Ghosts that may be removed: not present, and not one TweakHub disabled (it stays tracked for "Re-enable all").</summary>
    public static IReadOnlyList<PnpDeviceInfo> GhostDevices(IEnumerable<PnpDeviceInfo> devices) =>
        devices.Where(device => !device.Present && !device.DisabledByTweakHub).ToList();

    public static bool GhostTickedByDefault(PnpDeviceInfo device) =>
        !SoftwareEnumeratorPrefixes.Any(prefix => device.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The ghosts from <see cref="GhostDevices(IEnumerable{PnpDeviceInfo})"/> that cfgmgr32 also reports as not present
    /// (<paramref name="scan"/> from <see cref="Services.GhostDevices.Scan"/>), with their last use.
    /// </summary>
    internal static IReadOnlyList<PnpDeviceInfo> ConfirmedGhosts(IEnumerable<PnpDeviceInfo> devices, IReadOnlyDictionary<string, GhostInfo> scan) =>
        GhostDevices(devices).Where(device => scan.ContainsKey(device.Id))
            .Select(device => scan[device.Id] is var info
                ? device with { LastUsed = info.LastUsed, TypeName = info.Type, Manufacturer = info.Manufacturer, InstalledOn = info.Installed } : device)
            .ToList();

    private static readonly Regex VidPid = new(@"VID_[0-9A-F]{4}&PID_[0-9A-F]{4}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// The product name the hardware reported ("Wooting 60HE+"): the device's own, else the one of its highest
    /// ancestor with the same USB VID/PID (a "HID Keyboard Device" has none; its USB interface may name only a part,
    /// "Mouse", and the composite device above names the product). Only the device's own parent chain is used, so two
    /// units of the same model never borrow each other's name. Empty when none reported one.
    /// </summary>
    internal static string ProductName(PnpDeviceInfo device, IReadOnlyDictionary<string, PnpDeviceInfo> byId)
    {
        if (device.BusName.Length > 0) return device.BusName;
        if (VidPid.Match(device.Id) is not { Success: true } match) return string.Empty;
        var name = string.Empty;
        // Bounded: a corrupt parent loop must not hang the dialog.
        for (var (current, depth) = (device, 0); depth < 8 && byId.TryGetValue(current.Parent, out var parent)
            && VidPid.Match(parent.Id) is { Success: true } m && m.Value.Equals(match.Value, StringComparison.OrdinalIgnoreCase); (current, depth) = (parent, depth + 1))
            if (parent.BusName.Length > 0) name = parent.BusName;
        return name;
    }

    internal static Dictionary<string, PnpDeviceInfo> ById(IEnumerable<PnpDeviceInfo> devices) =>
        devices.GroupBy(device => device.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The elevated fallback: the same checks as <see cref="Services.GhostDevices.Uninstall"/>, run when TweakHub is not
    /// administrator. Each line reports OK|id, SKIP|id or FAIL|reason|id (id last: it may contain '|').
    /// </summary>
    internal static string BuildRemoveGhostsScript(IEnumerable<string> instanceIds) => """
        Add-Type -Namespace TweakHub -Name CfgMgr -MemberDefinition @'
        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)] public static extern int CM_Locate_DevNodeW(out int devInst, string id, int flags);
        [DllImport("cfgmgr32.dll")] public static extern int CM_Get_DevNode_Status(out int status, out int problem, int devInst, int flags);
        [DllImport("cfgmgr32.dll")] public static extern int CM_Uninstall_DevNode(int devInst, int flags);
        '@

        """ + $"foreach ($id in @({string.Join(", ", instanceIds.Select(PowerShellService.Literal))})) {{\n" + """
            $node = 0; $status = 0; $problem = 0
            # 1 = CM_LOCATE_DEVNODE_PHANTOM; only 13 (CR_NO_SUCH_DEVNODE) means not connected, anything else counts as present.
            $cr = [TweakHub.CfgMgr]::CM_Locate_DevNodeW([ref]$node, $id, 1)
            if ($cr -ne 0) { "FAIL|CM_Locate_DevNode $cr|$id"; continue }
            if ([TweakHub.CfgMgr]::CM_Get_DevNode_Status([ref]$status, [ref]$problem, $node, 0) -ne 13) { "SKIP|$id"; continue }
            $cr = [TweakHub.CfgMgr]::CM_Uninstall_DevNode($node, 0)
            if ($cr -eq 0) { "OK|$id" } else { "FAIL|CM_Uninstall_DevNode $cr|$id" }
        }
        """;

    /// <summary>Removes the ghosts given; returns how many were removed and the ones that were skipped or failed, with the reason.</summary>
    public async Task<(int Removed, IReadOnlyList<string> NotRemoved)> RemoveGhostsAsync(IReadOnlyList<PnpDeviceInfo> devices)
    {
        string output;
        if (Elevation.IsAdministrator)
            output = await Task.Run(() => string.Join("\n", devices.Select(device => Services.GhostDevices.Uninstall(DeviceTree.Api.Instance, device.Id))));
        else
        {
            var result = await PowerShellService.Instance.ExecuteScriptAsync(BuildRemoveGhostsScript(devices.Select(device => device.Id)),
                requireAdministrator: true, TimeSpan.FromMinutes(10));
            // Declined UAC: no device line at all, and the reason is in the error.
            if (!result.Output.Contains('|')) throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.Error) ? result.ExitCode.ToString() : result.Error.Trim());
            output = result.Output;
        }
        return ParseRemoveGhosts(output, devices);
    }

    internal static (int Removed, IReadOnlyList<string> NotRemoved) ParseRemoveGhosts(string output, IReadOnlyList<PnpDeviceInfo> devices)
    {
        // OK|id, SKIP|id or FAIL|reason|id: the id is everything after the fields, so a '|' inside it is kept.
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('|', 2)).Where(parts => parts.Length == 2)
            .Select(parts => parts[0] == "FAIL" && parts[1].Split('|', 2) is [var reason, var id]
                ? (Status: "FAIL", Id: id, Reason: reason) : (Status: parts[0], Id: parts[1], Reason: ""))
            .GroupBy(line => line.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
        var removed = devices.Count(device => lines.TryGetValue(device.Id, out var line) && line.Status == "OK");
        var notRemoved = devices.Where(device => !(lines.TryGetValue(device.Id, out var line) && line.Status == "OK"))
            .Select(device => lines.TryGetValue(device.Id, out var line) && line.Reason.Length > 0 ? $"{device.DisplayName} ({line.Reason})" : device.DisplayName)
            .ToList();
        return (removed, notRemoved);
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
