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
    public string DisplayName => Name.Length > 0 ? Name : Id;
    public string CategoryText => L.Get($"Devices:Category{Category}");
    public string AdviceText => Rule is null ? string.Empty : L.Get($"Devices:Advice{Rule.Advice}");
    public string ReasonText => Rule is null ? string.Empty : L.Get($"Devices:Rule{Rule.Id}");
    public string BreaksText => Rule is null ? string.Empty : L.Get($"Devices:Rule{Rule.Id}Breaks");
    public string ToggleText => L.Get(IsDisabled ? "Devices:Enable" : "Devices:Disable");
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

    internal static DeviceRule? MatchRule(string id, string name) => Rules.FirstOrDefault(rule => rule.Match.IsMatch($"{id}|{name}"));

    private const string InventoryScript = """
        $ErrorActionPreference = 'SilentlyContinue'
        $devices = @(Get-PnpDevice)
        $props = @{}
        # One pipeline for every device: about 25 ms per device instead of one process each.
        $devices | Get-PnpDeviceProperty -KeyName DEVPKEY_Device_Parent, DEVPKEY_Device_Service, DEVPKEY_Device_DriverInfPath, DEVPKEY_Device_BusReportedDeviceDesc | ForEach-Object {
            if (-not $props.ContainsKey($_.InstanceId)) { $props[$_.InstanceId] = @{} }
            $props[$_.InstanceId][$_.KeyName] = $_.Data
        }
        @($devices | ForEach-Object {
            $p = $props[$_.InstanceId]
            if (-not $p) { $p = @{} }
            [pscustomobject]@{ Id = [string]$_.InstanceId; Name = [string]$_.FriendlyName; Class = [string]$_.Class; Status = [string]$_.Status
                Present = [bool]$_.Present; Problem = [int]$_.ConfigManagerErrorCode; Parent = [string]$p['DEVPKEY_Device_Parent']
                Service = [string]$p['DEVPKEY_Device_Service']; Inf = [string]$p['DEVPKEY_Device_DriverInfPath']
                BusName = [string]$p['DEVPKEY_Device_BusReportedDeviceDesc'] }
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
            JsonRead.Text(item, "Parent"), JsonRead.Text(item, "Service"), JsonRead.Text(item, "Inf")) { BusName = JsonRead.Text(item, "BusName") })
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
                Rule = MatchRule(device.Id, device.Name),
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

    public static string ToCsv(IEnumerable<PnpDeviceInfo> devices)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Csv.Row("Name", "Class", "Status", "Present", "Category", "HighRisk", "Advice", "Children", "Problem", "Service", "Inf", "Parent", "InstanceId"));
        foreach (var d in devices)
            builder.AppendLine(Csv.Row(d.DisplayName, d.Class, d.Status, d.Present, d.Category, d.HighRisk, d.Rule?.Advice.ToString() ?? "", d.Children, d.Problem, d.Service, d.Inf, d.Parent, d.Id));
        return builder.ToString();
    }
}
