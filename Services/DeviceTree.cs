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
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Get_DevNode_PropertyW(int devInst, in DevPropKey key, out int type, byte[]? buffer, ref int size, int flags);

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

    private const int FilterPresent = 0x100; // CM_GETIDLIST_FILTER_PRESENT

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
            var classBytes = Property(node, ClassGuidKey);
            var containerBytes = Property(node, ContainerKey);
            var nameBytes = Property(node, FriendlyNameKey) ?? Property(node, DescriptionKey);
            nodes.Add(new DeviceNode(id, parent,
                classBytes is { Length: 16 } ? new Guid(classBytes) : Guid.Empty,
                containerBytes is { Length: 16 } ? new Guid(containerBytes) : null,
                nameBytes is null ? string.Empty : System.Text.Encoding.Unicode.GetString(nameBytes).TrimEnd('\0')));
        }
        return nodes;
    }

    private static string[]? List(int flags)
    {
        if (CM_Get_Device_ID_List_SizeW(out var length, null, flags) != 0 || length <= 0) return null;
        var buffer = new char[length];
        if (CM_Get_Device_ID_ListW(null, buffer, length, flags) != 0) return null;
        return new string(buffer).Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    private static byte[]? Property(int node, DevPropKey key)
    {
        var size = 0;
        CM_Get_DevNode_PropertyW(node, key, out _, null, ref size, 0);
        if (size <= 0) return null;
        var buffer = new byte[size];
        return CM_Get_DevNode_PropertyW(node, key, out _, buffer, ref size, 0) == 0 ? buffer : null;
    }
}
