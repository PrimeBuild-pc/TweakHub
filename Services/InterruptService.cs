using System.Text.Json;
using Microsoft.Win32;
using TweakHub.Localization;

namespace TweakHub.Services;

public enum InterruptPriority { Undefined = 0, Low = 1, Normal = 2, High = 3 }

public sealed record InterruptDevice(string Id, string Name, string Class, bool MsiCapable, bool MsiEnabled, int? MessageLimit,
    InterruptPriority Priority, int Policy, ulong AffinityMask)
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

    private const string ScanScript = """
        $ErrorActionPreference = 'SilentlyContinue'
        @(Get-PnpDevice -PresentOnly | Where-Object { $_.InstanceId -like 'PCI\*' } | ForEach-Object {
            $base = "HKLM:\SYSTEM\CurrentControlSet\Enum\$($_.InstanceId)\Device Parameters\Interrupt Management"
            $msi = Get-ItemProperty -LiteralPath "$base\MessageSignaledInterruptProperties"
            $affinity = Get-ItemProperty -LiteralPath "$base\Affinity Policy"
            [pscustomobject]@{ Id = [string]$_.InstanceId; Name = [string]$_.FriendlyName; Class = [string]$_.Class
                MsiCapable = [bool]$msi; Msi = $msi.MSISupported; Limit = $msi.MessageNumberLimit
                Priority = $affinity.DevicePriority; Policy = $affinity.DevicePolicy
                Mask = $(if ($affinity.AssignmentSetOverride) { [Convert]::ToBase64String([byte[]]$affinity.AssignmentSetOverride) }) }
        }) | ConvertTo-Json -Compress
        """ + PowerShellService.AsciiJson;

    public async Task<IReadOnlyList<InterruptDevice>> GetDevicesAsync()
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(ScanScript, timeout: TimeSpan.FromMinutes(1));
        if (!result.Success) throw new InvalidOperationException(result.Error);
        return ParseDevices(result.Output);
    }

    internal static List<InterruptDevice> ParseDevices(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        using var document = JsonDocument.Parse(json);
        return JsonRead.Items(document.RootElement).Select(item =>
        {
            var mask = JsonRead.Text(item, "Mask");
            var bytes = mask.Length > 0 ? Convert.FromBase64String(mask) : [];
            Array.Resize(ref bytes, 8);
            return new InterruptDevice(
                JsonRead.Text(item, "Id"), JsonRead.Text(item, "Name"), JsonRead.Text(item, "Class"),
                JsonRead.Bool(item, "MsiCapable"), JsonRead.Int(item, "Msi") == 1, JsonRead.Int(item, "Limit"),
                (InterruptPriority)Math.Clamp(JsonRead.Int(item, "Priority") ?? 0, 0, 3), JsonRead.Int(item, "Policy") ?? 0,
                BitConverter.ToUInt64(bytes));
        })
        // Bridges and root ports have no driver-level interrupt settings worth changing.
        .Where(device => device.Id.Length > 0 && (device.MsiCapable || device.Class is not "System"))
        .OrderByDescending(device => device.MsiCapable).ThenBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
        .ToList();
    }

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

    internal static string FormatMask(ulong mask) => string.Join(", ",
        Enumerable.Range(0, 64).Where(cpu => (mask >> cpu & 1) == 1).Select(cpu => $"CPU {cpu}"));
}
