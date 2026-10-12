using System.Text.RegularExpressions;
using TweakHub.Localization;

namespace TweakHub.Services;

public enum InputKind { Mouse, Keyboard, Controller }

public sealed record UsbInputChain(PnpDeviceInfo Input, InputKind Kind, IReadOnlyList<PnpDeviceInfo> Path, PnpDeviceInfo? Controller,
    PnpDeviceInfo? UsbDevice, int Hubs)
{
    public bool IsUsb => UsbDevice is not null;
    // Only the device's own nodes: a hub above it reports its own product name.
    public string Name => Path.TakeWhile(device => !UsbService.IsHub(device) && !device.Id.StartsWith(@"USB\ROOT_HUB", StringComparison.OrdinalIgnoreCase))
        .Select(device => device.BusName).FirstOrDefault(name => name.Length > 0) ?? Input.DisplayName;
    /// <summary>Every input type the physical device exposes, e.g. a keyboard with a mouse collection.</summary>
    public IReadOnlyList<InputKind> Kinds { get; init; } = [];
    public string KindText => string.Join(", ", (Kinds.Count > 0 ? Kinds : [Kind]).Select(kind => L.Get($"Devices:Input{kind}")));
    public string PathText => string.Join("  →  ", Path.Select(device => device.DisplayName));
    public string HubText => !IsUsb ? L.Get("Devices:NotUsb") : Hubs == 0 ? L.Get("Devices:HubsNone") : L.Format("Devices:Hubs", Hubs);
    public string ControllerText => Controller?.DisplayName ?? "–";
}

public sealed class UsbService
{
    public static UsbService Instance { get; } = new();
    private UsbService() { }

    private static InputKind? KindOf(PnpDeviceInfo device) => device.Class switch
    {
        "Mouse" => InputKind.Mouse,
        "Keyboard" => InputKind.Keyboard,
        "XnaComposite" or "XboxComposite" => InputKind.Controller,
        "HIDClass" when Regex.IsMatch(device.Name, @"(?i)game ?controller|gamepad|joystick") => InputKind.Controller,
        _ => null
    };

    /// <summary>Walks parent links from every present input device up to its PCI controller, counting the USB hubs in between.</summary>
    internal static List<UsbInputChain> BuildChains(IReadOnlyList<PnpDeviceInfo> devices)
    {
        var byId = devices.GroupBy(device => device.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var chains = new List<UsbInputChain>();
        foreach (var input in devices.Where(device => device.Present && device.Category != DeviceCategory.Disabled))
        {
            if (KindOf(input) is not { } kind) continue;
            var path = new List<PnpDeviceInfo> { input };
            var current = input;
            while (current.Parent.Length > 0 && byId.TryGetValue(current.Parent, out var parent) && path.Count < 16)
            {
                path.Add(parent);
                if (parent.Id.StartsWith(@"PCI\", StringComparison.OrdinalIgnoreCase)) break;
                current = parent;
            }

            var controller = path.LastOrDefault(device => device.Id.StartsWith(@"PCI\", StringComparison.OrdinalIgnoreCase));
            // Interface (MI_xx) and composite nodes belong to the same physical device; the topmost one below the first hub is the device itself.
            var usbDevice = path.TakeWhile(device => !IsHub(device) && !device.Id.StartsWith(@"USB\ROOT_HUB", StringComparison.OrdinalIgnoreCase))
                .LastOrDefault(device => device.Id.StartsWith(@"USB\VID_", StringComparison.OrdinalIgnoreCase));
            chains.Add(new UsbInputChain(input, kind, path, controller, usbDevice, path.Count(IsHub)));
        }

        // A mouse exposes several HID collections; keep one row per physical device.
        return chains.GroupBy(chain => chain.UsbDevice?.Id ?? chain.Input.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(chain => chain.Kind).First() with { Kinds = group.Select(chain => chain.Kind).Distinct().Order().ToList() })
            .OrderBy(chain => chain.Kind).ThenBy(chain => chain.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>External hubs run the hub driver; composite devices (usbccgp) and interfaces are not hubs even though they parent other nodes.</summary>
    internal static bool IsHub(PnpDeviceInfo device) =>
        device.Id.StartsWith(@"USB\VID_", StringComparison.OrdinalIgnoreCase)
        && (device.Service.Length > 0
            ? device.Service.StartsWith("usbhub", StringComparison.OrdinalIgnoreCase)
            : device.Name.Contains("hub", StringComparison.OrdinalIgnoreCase));

    private static string DeviceParametersKey(string id) => $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\{id}\Device Parameters";

    internal static RegistryValueChange[] SuspendChanges(string usbDeviceId) =>
    [
        new(DeviceParametersKey(usbDeviceId), "SelectiveSuspendEnabled", 0),
        new(DeviceParametersKey(usbDeviceId), "EnhancedPowerManagementEnabled", 0)
    ];

    /// <summary>True when the driver lets Windows suspend the device; missing values mean the driver default (usually on).</summary>
    public static bool IsDeviceSuspendAllowed(string usbDeviceId) => SuspendChanges(usbDeviceId).Any(change =>
        RegistryService.Instance.GetRegistryValue(change.KeyPath, change.ValueName) is not (int and 0));

    public static bool HasDeviceSuspendBackup(string usbDeviceId) =>
        SuspendChanges(usbDeviceId).Any(change => RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName));

    public Task<bool> DisableDeviceSuspendAsync(string usbDeviceId) =>
        RegistryService.Instance.ApplyValuesWithBackupAsync(SuspendChanges(usbDeviceId));

    public Task<bool> RestoreDeviceSuspendAsync(string usbDeviceId) =>
        RegistryService.Instance.RestoreValuesAsync(SuspendChanges(usbDeviceId)
            .Where(change => RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName)).ToList());

    private const string PlanSuspendId = "usb_selective_suspend";

    /// <summary>1 = enabled, 0 = disabled in the active plan's AC settings; null when the plan hides the setting.</summary>
    // GUIDs instead of the SUB_USB / USBSELECTIVESUSPEND aliases, which some plans do not define.
    private const string UsbSubgroup = "2a737441-1930-4402-8d77-b2bebba308a3";
    private const string SelectiveSuspendSetting = "48e6b7a6-50f5-4782-a5d4-53bb8f07e226";

    public static int? GetPlanSuspend() => PowerService.Instance.GetActiveSetting(UsbSubgroup, SelectiveSuspendSetting);

    public static Task<bool> DisablePlanSuspendAsync() => PowerService.Instance.ApplySettingAsync(PlanSuspendId, UsbSubgroup, SelectiveSuspendSetting, 0);

    public static Task<bool> RestorePlanSuspendAsync() => PowerService.Instance.RestoreSettingAsync(PlanSuspendId);

    public static bool HasPlanSuspendBackup => PowerService.Instance.HasBackup(PlanSuspendId);

    /// <summary>Polling-rate estimate from raw input arrival times: the median interval resists the gaps when the mouse slows down.</summary>
    internal static (double Hz, IReadOnlyList<int> Histogram) AnalyzeIntervals(IReadOnlyList<double> intervalsMs)
    {
        // Gaps over 50 ms are pauses in movement, not polling intervals.
        var valid = intervalsMs.Where(interval => interval is > 0 and < 50).OrderBy(interval => interval).ToList();
        if (valid.Count < 20) return (0, new int[PollingBuckets.Length]);
        var median = valid[valid.Count / 2];
        var histogram = new int[PollingBuckets.Length];
        foreach (var rate in valid.Select(interval => 1000 / interval))
            histogram[Array.FindLastIndex(PollingBuckets, bucket => rate >= bucket * 0.85)] += 1;
        return (1000 / median, histogram);
    }

    /// <summary>Lower bound of each histogram column in Hz; 15% tolerance absorbs scheduling jitter.</summary>
    internal static readonly int[] PollingBuckets = [0, 125, 250, 500, 1000, 2000, 4000, 8000];
}
