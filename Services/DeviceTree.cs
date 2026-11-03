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

    private const int FilterPresent = 0x100; // CM_GETIDLIST_FILTER_PRESENT

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
