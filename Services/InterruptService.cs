using Microsoft.Win32;
using TweakHub.Localization;

namespace TweakHub.Services;

public enum InterruptPriority { Undefined = 0, Low = 1, Normal = 2, High = 3 }

public sealed record InterruptDevice(string Id, string Name, string Class, bool MsiCapable, bool MsiEnabled, int? MessageLimit,
    InterruptPriority Priority, int Policy, ulong AffinityMask, bool IsHdAudio = false,
    int? Bus = null, int? Address = null, bool AudioInUse = true, bool Working = true, bool? NetConnected = null)
{
    // IrqPolicySpecifiedProcessors: the device only interrupts the CPUs in AssignmentSetOverride.
    public const int SpecifiedProcessorsPolicy = 4;
    public bool HasAffinity => Policy == SpecifiedProcessorsPolicy && AffinityMask != 0;
    public string ModeText => L.Get(!MsiCapable ? "Devices:InterruptLineBased" : MsiEnabled ? "Devices:InterruptMsi" : "Devices:InterruptLineBased");
    public string PriorityText => L.Get($"Devices:Priority{Priority}");
    public string AffinityText => HasAffinity ? InterruptService.FormatMask(AffinityMask) : L.Get("Devices:AffinityAuto");
    public string LimitText => MessageLimit?.ToString(L.Culture) ?? "–";
    public bool HasTweakHubChanges => InterruptService.ManagedValues(Id).Take(2).Any(change => RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName));
    public bool HasAffinityChanges => InterruptService.ManagedValues(Id).Skip(2).Any(change => RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName));
}

public sealed class InterruptService
{
    public static InterruptService Instance { get; } = new();
    private InterruptService() { }

    public async Task<IReadOnlyList<InterruptDevice>> GetDevicesAsync() => Visible(await Task.Run(ReadDevices));

    /// <summary>
    /// Present PCI devices with their interrupt settings, read from cfgmgr32 and the registry directly
    /// (the same scan through PowerShell took about 3 s).
    /// </summary>
    internal static List<InterruptDevice> ReadDevices()
    {
        var present = DeviceTree.ReadInventory().Where(device => device.Present).ToList();
        // Controllers with at least one working HD Audio function behind them.
        var working = present.Where(device => device.Id.StartsWith(@"HDAUDIO\", StringComparison.OrdinalIgnoreCase) && device.Status == "OK")
            .Select(device => device.Parent).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var up = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
            .Where(adapter => adapter.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
            .Select(adapter => adapter.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return present.Where(device => device.Id.StartsWith(@"PCI\", StringComparison.OrdinalIgnoreCase)).Select(device =>
        {
            var root = $@"SYSTEM\CurrentControlSet\Enum\{device.Id}\Device Parameters\Interrupt Management";
            using var msi = Registry.LocalMachine.OpenSubKey(root + @"\MessageSignaledInterruptProperties");
            using var affinity = Registry.LocalMachine.OpenSubKey(root + @"\Affinity Policy");
            var hd = DeviceTree.CompatibleIds(device.Id).Any(id => id.Contains("&CC_0403", StringComparison.OrdinalIgnoreCase));
            var slot = hd || device.Class.Equals("Display", StringComparison.OrdinalIgnoreCase);
            // Other tools sometimes write the mask as a QWORD instead of binary.
            var mask = affinity?.GetValue("AssignmentSetOverride") switch
            {
                byte[] bytes => bytes,
                long number => BitConverter.GetBytes(number),
                int number => BitConverter.GetBytes(number),
                _ => []
            };
            Array.Resize(ref mask, 8);
            return new InterruptDevice(device.Id, device.Name, device.Class,
                msi is not null, msi?.GetValue("MSISupported") is 1, msi?.GetValue("MessageNumberLimit") as int?,
                (InterruptPriority)Math.Clamp(affinity?.GetValue("DevicePriority") as int? ?? 0, 0, 3), affinity?.GetValue("DevicePolicy") as int? ?? 0,
                BitConverter.ToUInt64(mask), hd,
                slot && device.BusNumber >= 0 ? device.BusNumber : null, slot && device.Address >= 0 ? device.Address : null,
                !hd || working.Contains(device.Id), device.Status == "OK", NetConnected(device.Id, up));
        }).ToList();
    }

    /// <summary>Whether the network adapter on this device has a link; null when the device is not a network adapter.</summary>
    private static bool? NetConnected(string id, HashSet<string> up)
    {
        using var device = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{id}");
        if (device?.GetValue("Driver") is not string driver) return null;
        using var adapter = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Class\{driver}");
        return adapter?.GetValue("NetCfgInstanceId") is string guid ? up.Contains(guid) : null;
    }

    /// <summary>Bridges and root ports have no driver-level interrupt settings worth changing.</summary>
    internal static List<InterruptDevice> Visible(IEnumerable<InterruptDevice> devices) => devices
        .Where(device => device.Id.Length > 0 && (device.MsiCapable || device.Class is not "System"))
        .OrderByDescending(device => device.MsiCapable).ThenBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    private static string MsiKey(string id) =>
        $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\{id}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";

    private static string AffinityKey(string id) =>
        $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\{id}\Device Parameters\Interrupt Management\Affinity Policy";

    /// <summary>Every value this service may write for a device, used to find and restore its backups.</summary>
    internal static RegistryValueChange[] ManagedValues(string id) =>
    [
        new(MsiKey(id), "MSISupported", 1),
        new(AffinityKey(id), "DevicePriority", 0),
        new(AffinityKey(id), "DevicePolicy", 0),
        new(AffinityKey(id), "AssignmentSetOverride", Array.Empty<byte>(), RegistryValueKind.Binary)
    ];

    internal static IReadOnlyCollection<RegistryValueChange> BuildChanges(InterruptDevice device, bool? msi, InterruptPriority? priority, ulong? affinityMask)
    {
        var changes = new List<RegistryValueChange>();
        if (msi is { } enableMsi) changes.Add(new(MsiKey(device.Id), "MSISupported", enableMsi ? 1 : 0));
        if (priority is { } level) changes.Add(new(AffinityKey(device.Id), "DevicePriority", (int)level));
        if (affinityMask is { } mask)
        {
            // An explicit zero resets only the affinity policy, not MSI or interrupt priority.
            changes.Add(new(AffinityKey(device.Id), "DevicePolicy", mask == 0 ? null : InterruptDevice.SpecifiedProcessorsPolicy));
            changes.Add(new(AffinityKey(device.Id), "AssignmentSetOverride", mask == 0 ? null : BitConverter.GetBytes(mask), RegistryValueKind.Binary));
        }
        return changes;
    }

    internal static bool CanAssign(ulong mask) => mask != 0 && CpuTopology.Supported && (mask & ~CpuTopology.AllowedMask) == 0;

    public Task<bool> ApplyAsync(InterruptDevice device, bool? msi, InterruptPriority? priority, ulong? affinityMask) =>
        affinityMask is > 0 && !CanAssign(affinityMask.Value)
            ? Task.FromResult(false)
            : RegistryService.Instance.ApplyValuesWithBackupAsync(BuildChanges(device, msi, priority, affinityMask));

    /// <summary>Restores only the editor's own values; MSI and affinity do not undo each other's changes.</summary>
    public Task<bool> RestoreAsync(InterruptDevice device, bool affinityOnly = false) =>
        RegistryService.Instance.RestoreValuesAsync(ManagedValues(device.Id).Skip(affinityOnly ? 2 : 0).Take(2)
            .Where(change => RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName)).ToList());

    /// <summary>Writes several affinities, and MSI on the devices given, with one backup and one elevation; priority is not touched.</summary>
    public Task<bool> ApplyAffinitiesAsync(IReadOnlyCollection<(InterruptDevice Device, ulong Mask)> items,
        IReadOnlyCollection<InterruptDevice>? enableMsi = null) =>
        items.Count + (enableMsi?.Count ?? 0) == 0 || items.Any(item => !CanAssign(item.Mask))
            ? Task.FromResult(false)
            : RegistryService.Instance.ApplyValuesWithBackupAsync(
                items.SelectMany(item => BuildChanges(item.Device, null, null, item.Mask))
                    .Concat((enableMsi ?? []).Where(device => device.MsiCapable).SelectMany(device => BuildChanges(device, true, null, null))).ToList());

    /// <summary>
    /// Devices whose layout affinity is back to the original value while TweakHub's backup is still there: a driver update
    /// (often the GPU one) recreated the device key. Only present PCI devices, by display name.
    /// </summary>
    internal static IReadOnlyList<string> ResetAffinityDevices(IReadOnlyList<DeviceNode> nodes)
    {
        var present = nodes.DistinctBy(node => node.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(node => node.Id, node => node.Name, StringComparer.OrdinalIgnoreCase);
        return RegistryService.Instance.BackedUpValues
            .Where(value => value.ValueName == "DevicePolicy")
            .Select(value => (value.KeyPath, Id: AffinityKeyDevice(value.KeyPath)))
            .Where(item => item.Id is not null && present.ContainsKey(item.Id) && RegistryService.Instance.IsAtOriginal(item.KeyPath, "DevicePolicy") == true)
            .Select(item => present[item.Id!] is { Length: > 0 } name ? name : item.Id!)
            .ToList();
    }

    internal static string? AffinityKeyDevice(string keyPath)
    {
        var match = System.Text.RegularExpressions.Regex.Match(keyPath,
            @"^HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Enum\\(PCI\\[^\\]+\\[^\\]+)\\Device Parameters\\Interrupt Management\\Affinity Policy$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    internal static string FormatMask(ulong mask) => string.Join(", ",
        Enumerable.Range(0, 64).Where(cpu => (mask >> cpu & 1) == 1).Select(cpu => $"CPU {cpu}"));
}
