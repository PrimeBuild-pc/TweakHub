using System.Diagnostics;

namespace TweakHub.Services;

/// <summary>The cfgmgr32 calls ghost removal needs; tests swap in a fake. Each int result is a CONFIGRET code.</summary>
internal interface IDevNodeApi
{
    IReadOnlyList<string> DeviceIds();
    /// <summary>CM_Locate_DevNode with CM_LOCATE_DEVNODE_PHANTOM; <paramref name="node"/> is valid only on success.</summary>
    int Locate(string id, out int node);
    /// <summary>CM_Get_DevNode_Status; CR_NO_SUCH_DEVNODE means the device is installed but not connected.</summary>
    int Status(int node);
    /// <summary>
    /// A FILETIME device property in DEVPKEY_Device_InstallDate's set (100 install, 101 first install, 102 arrival,
    /// 103 removal); null when missing.
    /// </summary>
    DateTime? FileTime(int node, uint pid);
    int Uninstall(int node);
    /// <summary>The device's setup class as Device Manager shows it; empty when unknown.</summary>
    string ClassDescription(int node);
    /// <summary>The manufacturer its driver names; empty when unknown.</summary>
    string Manufacturer(int node);
}

/// <summary>What cfgmgr32 knows about a ghost: last use, type as Device Manager names it, manufacturer and first install.</summary>
internal sealed record GhostInfo(DateTime? LastUsed, string Type, string Manufacturer = "", DateTime? Installed = null);

/// <summary>Non-present devices ("ghosts"): which they are, when they were last used, and removing them safely.</summary>
internal static class GhostDevices
{
    internal const int CrSuccess = 0;
    internal const int CrNoSuchDevNode = 0x0D;
    internal const uint InstallPid = 100, FirstInstallPid = 101, LastArrivalPid = 102, LastRemovalPid = 103;

    /// <summary>
    /// Ghost instance ids with their last use. Only CR_NO_SUCH_DEVNODE marks a ghost: any other failure is treated as present.
    /// </summary>
    public static Dictionary<string, GhostInfo> Scan(IDevNodeApi api)
    {
        var ghosts = new Dictionary<string, GhostInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in api.DeviceIds())
            if (api.Locate(id, out var node) == CrSuccess && api.Status(node) == CrNoSuchDevNode)
                ghosts[id] = new(LastUsed(api.FileTime(node, LastArrivalPid), api.FileTime(node, LastRemovalPid)), api.ClassDescription(node),
                    api.Manufacturer(node), api.FileTime(node, FirstInstallPid) ?? api.FileTime(node, InstallPid));
        return ghosts;
    }

    private static readonly long MaxFileTime = DateTime.MaxValue.ToFileTimeUtc();

    /// <summary>A FILETIME as UTC; null when zero, negative or past DateTime.MaxValue (corrupt values would throw).</summary>
    internal static DateTime? FileTimeToUtc(long value) => value > 0 && value <= MaxFileTime ? DateTime.FromFileTimeUtc(value) : null;

    /// <summary>The later of last arrival and last removal; either may be missing.</summary>
    internal static DateTime? LastUsed(DateTime? arrival, DateTime? removal) => arrival > removal ? arrival : removal ?? arrival;

    /// <summary>Hidden by the "used in the last N days" filter. A device with no date stays visible.</summary>
    public static bool UsedWithin(DateTime? lastUsed, int days, DateTime now) => lastUsed is { } used && (now - used).TotalDays < days; // no AddDays: a huge N would overflow DateTime

    /// <summary>
    /// Locates and checks the device again right before uninstalling, so one reconnected meanwhile is never removed.
    /// Returns OK|id, SKIP|id (present again) or FAIL|reason|id, the same lines the elevated script prints.
    /// The id comes last because instance ids may contain '|'.
    /// </summary>
    public static string Uninstall(IDevNodeApi api, string id)
    {
        var line = UninstallCore(api, id);
        if (!line.StartsWith("OK|", StringComparison.Ordinal)) Debug.WriteLine($"Ghost removal: {line}");
        return line;
    }

    private static string UninstallCore(IDevNodeApi api, string id)
    {
        var result = api.Locate(id, out var node);
        if (result != CrSuccess) return $"FAIL|CM_Locate_DevNode {result}|{id}";
        if (api.Status(node) != CrNoSuchDevNode) return $"SKIP|{id}";
        result = api.Uninstall(node);
        return result == CrSuccess ? $"OK|{id}" : $"FAIL|CM_Uninstall_DevNode {result}|{id}";
    }
}
