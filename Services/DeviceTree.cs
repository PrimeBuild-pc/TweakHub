using System.Runtime.InteropServices;

namespace TweakHub.Services;

/// <summary>A present device: its parent, setup class, container (one physical device) and display name.</summary>
internal sealed record DeviceNode(string Id, string? Parent, Guid ClassGuid, Guid? Container, string Name);

/// <summary>Reads the present device tree through cfgmgr32: fast and without PowerShell.</summary>
internal static class DeviceTree
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DevPropKey(Guid fmtid, uint pid)
    {
        public Guid Fmtid = fmtid;
        public uint Pid = pid;
    }

    private static readonly DevPropKey ClassGuidKey = new(new("a45c254e-df1c-4efd-8020-67d146a850e0"), 10);
    private static readonly DevPropKey ContainerKey = new(new("8c7ed206-3f8a-4827-b3ab-ae9e1faefc6c"), 2);
    private static readonly DevPropKey FriendlyNameKey = new(new("a45c254e-df1c-4efd-8020-67d146a850e0"), 14);
    private static readonly DevPropKey DescriptionKey = new(new("a45c254e-df1c-4efd-8020-67d146a850e0"), 2);
    private static readonly DevPropKey ServiceKey = new(new("a45c254e-df1c-4efd-8020-67d146a850e0"), 6);
    private static readonly DevPropKey ClassKey = new(new("a45c254e-df1c-4efd-8020-67d146a850e0"), 9);
    private static readonly DevPropKey BusNumberKey = new(new("a45c254e-df1c-4efd-8020-67d146a850e0"), 23);
    private static readonly DevPropKey AddressKey = new(new("a45c254e-df1c-4efd-8020-67d146a850e0"), 30);
    private static readonly DevPropKey ProblemCodeKey = new(new("4340a6c5-93fa-4706-972c-7b648008a5a7"), 3);
    private static readonly DevPropKey ParentKey = new(new("4340a6c5-93fa-4706-972c-7b648008a5a7"), 8);
    private static readonly DevPropKey BusReportedDescKey = new(new("540b947e-8b40-45bc-a8a2-6a0b894cbda2"), 4);
    private static readonly DevPropKey IsPresentKey = new(new("540b947e-8b40-45bc-a8a2-6a0b894cbda2"), 5);
    private static readonly DevPropKey InfPathKey = new(new("a8b865dd-2e3d-4094-ad97-e593a70c75d6"), 5);
    private static readonly Guid PciKeys = new("3ab22e31-8264-4b4e-9af5-a8d2d8e33e62");
    private static readonly DevPropKey LinkSpeedKey = new(PciKeys, 9), LinkWidthKey = new(PciKeys, 10),
        MaxLinkSpeedKey = new(PciKeys, 11), MaxLinkWidthKey = new(PciKeys, 12);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Get_Device_ID_List_SizeW(out int length, string? filter, int flags);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Get_Device_ID_ListW(string? filter, char[] buffer, int length, int flags);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Locate_DevNodeW(out int devInst, string id, int flags);
    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Get_Parent(out int parent, int devInst, int flags);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Get_Device_IDW(int devInst, char[] buffer, int length, int flags);
    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Get_DevNode_Status(out int status, out int problem, int devInst, int flags);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern unsafe int CM_Get_DevNode_PropertyW(int devInst, in DevPropKey key, out int type, byte* buffer, ref int size, int flags);

    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Get_First_Log_Conf(out IntPtr logConf, int devInst, int flags);
    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Get_Next_Res_Des(out IntPtr resDes, IntPtr current, int forResource, out int resourceId, int flags);
    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Get_Res_Des_Data_Size(out int size, IntPtr resDes, int flags);
    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Get_Res_Des_Data(IntPtr resDes, byte[] buffer, int size, int flags);
    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Free_Res_Des_Handle(IntPtr resDes);
    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Free_Log_Conf_Handle(IntPtr logConf);

    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Uninstall_DevNode(int devInst, int flags);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetupDiGetClassDescriptionW(in Guid classGuid, char[] description, int size, out int required);

    private const int FilterPresent = 0x100; // CM_GETIDLIST_FILTER_PRESENT
    private const int BufferSmall = 0x1A; // CR_BUFFER_SMALL
    private const int LocatePhantom = 1; // CM_LOCATE_DEVNODE_PHANTOM

    /// <summary>The real cfgmgr32 behind <see cref="GhostDevices"/>; uninstalling needs administrator rights.</summary>
    internal sealed class Api : IDevNodeApi
    {
        public static Api Instance { get; } = new();
        private static readonly Guid DateSet = new("83da6326-97a6-4088-9453-a1923f573b29");
        private static readonly DevPropKey ManufacturerKey = new(new("a45c254e-df1c-4efd-8020-67d146a850e0"), 13);

        // An empty list would read as "no ghosts", so a failed read is an error instead.
        public IReadOnlyList<string> DeviceIds() => List(0) ?? throw new InvalidOperationException("CM_Get_Device_ID_List failed.");
        public int Locate(string id, out int node) => CM_Locate_DevNodeW(out node, id, LocatePhantom);
        public int Status(int node) => CM_Get_DevNode_Status(out _, out _, node, 0);
        public int Uninstall(int node) => CM_Uninstall_DevNode(node, 0);

        /// <summary>The setup class as Device Manager names it, in the Windows language ("Keyboards", "Monitors").</summary>
        public string ClassDescription(int node)
        {
            Span<byte> guid = stackalloc byte[16];
            var bytes = Property(node, ClassGuidKey, guid);
            if (bytes.Length != 16) return string.Empty;
            var buffer = new char[256];
            return SetupDiGetClassDescriptionW(new Guid(bytes), buffer, buffer.Length, out _) ? new string(buffer, 0, Math.Max(0, Array.IndexOf(buffer, '\0'))) : string.Empty; // SetupAPI leaves text after the terminator
        }

        /// <summary>DEVPKEY_Device_Manufacturer, from the driver ("Logitech", "(Standard system devices)").</summary>
        public string Manufacturer(int node) => Text(node, ManufacturerKey)?.Split('\0')[0] ?? string.Empty;

        public DateTime? FileTime(int node, uint pid)
        {
            Span<byte> buffer = stackalloc byte[8];
            var bytes = Property(node, new DevPropKey(DateSet, pid), buffer);
            return GhostDevices.FileTimeToUtc(bytes.Length == 8 ? BitConverter.ToInt64(bytes) : 0);
        }
    }

    /// <summary>
    /// Processor masks of the interrupts Windows allocated to a device in this boot (one per message; 0 means no specific
    /// processor). They show the affinity Windows really uses, which changes only after a restart.
    /// </summary>
    public static IReadOnlyList<ulong> AllocatedInterruptAffinities(string id)
    {
        var masks = new List<ulong>();
        // ALLOC_LOG_CONF = 2, ResType_IRQ = 4; IRQ_DES_64 keeps IRQD_Affinity at offset 16.
        if (CM_Locate_DevNodeW(out var node, id, 0) != 0 || CM_Get_First_Log_Conf(out var conf, node, 2) != 0) return masks;
        var current = conf;
        while (CM_Get_Next_Res_Des(out var descriptor, current, 4, out _, 0) == 0)
        {
            if (CM_Get_Res_Des_Data_Size(out var size, descriptor, 0) == 0 && size >= 24)
            {
                var buffer = new byte[size];
                if (CM_Get_Res_Des_Data(descriptor, buffer, size, 0) == 0) masks.Add(BitConverter.ToUInt64(buffer, 16));
            }
            if (current != conf) CM_Free_Res_Des_Handle(current);
            current = descriptor;
        }
        if (current != conf) CM_Free_Res_Des_Handle(current);
        CM_Free_Log_Conf_Handle(conf);
        return masks;
    }

    public static IReadOnlyList<DeviceNode> Read()
    {
        var ids = List(FilterPresent) ?? List(0) ?? [];
        var nodes = new List<DeviceNode>(ids.Length);
        Span<byte> guidBuffer = stackalloc byte[16];
        foreach (var id in ids)
        {
            // Locating with the normal flag fails for devices that are not present.
            if (CM_Locate_DevNodeW(out var node, id, 0) != 0) continue;
            string? parent = null;
            if (CM_Get_Parent(out var parentNode, node, 0) == 0)
            {
                var buffer = new char[512];
                if (CM_Get_Device_IDW(parentNode, buffer, buffer.Length, 0) == 0) parent = new string(buffer).TrimEnd('\0');
            }
            var classBytes = Property(node, ClassGuidKey, guidBuffer);
            var classGuid = classBytes.Length == 16 ? new Guid(classBytes) : Guid.Empty;
            var containerBytes = Property(node, ContainerKey, guidBuffer);
            nodes.Add(new DeviceNode(id, parent, classGuid,
                containerBytes.Length == 16 ? new Guid(containerBytes) : null,
                Text(node, FriendlyNameKey) ?? Text(node, DescriptionKey) ?? string.Empty));
        }
        return nodes;
    }

    /// <summary>
    /// Every device Windows knows, plugged in or not, with what Device Manager shows. Reads cfgmgr32 directly:
    /// the same data through Get-PnpDeviceProperty took about 20 s on a desktop with 400 devices.
    /// </summary>
    public static List<PnpDeviceInfo> ReadInventory()
    {
        var ids = List(0) ?? [];
        var devices = new List<PnpDeviceInfo>(ids.Length);
        Span<byte> presentFlag = stackalloc byte[1];
        foreach (var id in ids)
        {
            // CM_LOCATE_DEVNODE_PHANTOM: devices that are not plugged in are found too.
            if (CM_Locate_DevNodeW(out var node, id, 1) != 0) continue;
            var present = Property(node, IsPresentKey, presentFlag) is not [0, ..];
            // The live status is the only source for disabled devices (22); unplugged ones have none and Windows reports 45 (CM_PROB_PHANTOM).
            var problem = CM_Get_DevNode_Status(out _, out var live, node, 0) == 0 ? live : Number(node, ProblemCodeKey) ?? (present ? 0 : 45);
            // Upper case like Get-PnpDevice: these ids are saved in backups, playbooks and the disabled-devices list.
            devices.Add(new PnpDeviceInfo(id.ToUpperInvariant(), Text(node, FriendlyNameKey) ?? Text(node, DescriptionKey) ?? string.Empty,
                Text(node, ClassKey) ?? string.Empty,
                // The values Win32_PnPEntity.Status reports for these cases.
                !present ? "Unknown" : problem != 0 ? "Error" : "OK", present, problem,
                Text(node, ParentKey) ?? string.Empty, Text(node, ServiceKey) ?? string.Empty, Text(node, InfPathKey) ?? string.Empty)
            {
                BusName = Text(node, BusReportedDescKey) ?? string.Empty,
                BusNumber = Number(node, BusNumberKey) ?? -1, Address = Number(node, AddressKey) ?? -1,
                LinkGen = Number(node, LinkSpeedKey) ?? 0, LinkWidth = Number(node, LinkWidthKey) ?? 0,
                MaxLinkGen = Number(node, MaxLinkSpeedKey) ?? 0, MaxLinkWidth = Number(node, MaxLinkWidthKey) ?? 0
            });
        }
        return devices;
    }

    private static readonly DevPropKey CompatibleIdsKey = new(new("a45c254e-df1c-4efd-8020-67d146a850e0"), 4);

    /// <summary>The device's compatible ids (for PCI they carry the class code, e.g. CC_0403 for HD Audio); empty when unknown.</summary>
    public static string[] CompatibleIds(string id) =>
        CM_Locate_DevNodeW(out var node, id, 0) == 0 && Text(node, CompatibleIdsKey) is { } list
            ? list.Split('\0', StringSplitOptions.RemoveEmptyEntries) : [];

    private static string? Text(int node, DevPropKey key)
    {
        Span<byte> buffer = stackalloc byte[512];
        var bytes = Property(node, key, buffer);
        return bytes.IsEmpty ? null : System.Text.Encoding.Unicode.GetString(bytes).TrimEnd('\0');
    }

    private static int? Number(int node, DevPropKey key)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        var bytes = Property(node, key, buffer);
        return bytes.Length == sizeof(int) ? BitConverter.ToInt32(bytes) : null;
    }

    private static string[]? List(int flags)
    {
        // A device can appear between the size query and the read (CR_BUFFER_SMALL): ask again a few times.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (CM_Get_Device_ID_List_SizeW(out var length, null, flags) != 0 || length <= 0) return null;
            var buffer = new char[length];
            var result = CM_Get_Device_ID_ListW(null, buffer, length, flags);
            if (result == 0) return new string(buffer).Split('\0', StringSplitOptions.RemoveEmptyEntries);
            if (result != BufferSmall) return null;
        }
        return null;
    }

    private static unsafe ReadOnlySpan<byte> Property(int node, DevPropKey key, Span<byte> buffer)
    {
        // Most values fit on the first call. Larger driver strings still get their full buffer.
        var size = buffer.Length;
        int result;
        fixed (byte* data = buffer)
            result = CM_Get_DevNode_PropertyW(node, key, out _, data, ref size, 0);
        if (result == 0) return buffer[..size];
        if (result != BufferSmall || size <= buffer.Length) return [];

        var expanded = new byte[size];
        fixed (byte* data = expanded)
            return CM_Get_DevNode_PropertyW(node, key, out _, data, ref size, 0) == 0 ? expanded.AsSpan(0, size) : [];
    }
}
