using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace TweakHub.Services;

/// <summary>Negotiated USB speed of one hub port, as the hub driver reports it.</summary>
/// <param name="Speed">0 = low (1.5 Mb/s), 1 = full (12 Mb/s), 2 = high (480 Mb/s), 3 = SuperSpeed (5 Gb/s or more).</param>
public sealed record UsbPortSpeed(int Speed, bool SuperSpeedCapable, bool SuperSpeedPlus)
{
    public string Manufacturer { get; init; } = string.Empty;
    public string Product { get; init; } = string.Empty;
}

/// <summary>Reads negotiated speeds with the hub IOCTLs USBView uses; hubs open without administrator rights.</summary>
internal static class UsbSpeedService
{
    private static readonly Guid HubInterface = new("f18a0e88-c30c-11d0-8815-00a0c906bed8");
    private const uint IoctlConnectionInfoEx = 0x220448, IoctlConnectionInfoExV2 = 0x22045C, IoctlDescriptor = 0x220410;

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Get_Device_Interface_List_SizeW(out int length, in Guid interfaceGuid, string deviceId, int flags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Get_Device_Interface_ListW(in Guid interfaceGuid, string deviceId, char[] buffer, int length, int flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle device, uint code, byte[] input, int inputLength, byte[] output, int outputLength,
        out int returned, IntPtr overlapped);

    [StructLayout(LayoutKind.Sequential)]
    private struct DevPropKey { public Guid Category; public uint Id; }

    // DEVPKEY_Device_PowerData: a CM_POWER_DATA whose second field is the most recent device power state.
    private static readonly DevPropKey PowerData = new() { Category = new("a45c254e-df1c-4efd-8020-67d146a850e0"), Id = 32 };

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Locate_DevNodeW(out uint devInst, string deviceId, int flags);

    [DllImport("CfgMgr32.dll")]
    private static extern int CM_Get_DevNode_PropertyW(uint devInst, in DevPropKey key, out uint type, byte[] buffer, ref int size, int flags);

    /// <summary>
    /// A string descriptor is a control transfer: the hub resumes a suspended device to answer it. Only an awake (D0)
    /// device is asked; an unknown power state keeps the old behaviour and asks.
    /// </summary>
    private static bool IsAwake(string deviceId)
    {
        var buffer = new byte[64];
        var size = buffer.Length;
        return CM_Locate_DevNodeW(out var node, deviceId, 0) != 0
            || CM_Get_DevNode_PropertyW(node, PowerData, out _, buffer, ref size, 0) != 0
            || IsAwake(buffer, size);
    }

    internal static bool IsAwake(byte[] powerData, int size) => size < 8 || BitConverter.ToInt32(powerData, 4) is 0 or 1; // 0 unspecified, 1 D0

    private static string? HubPath(string hubInstanceId)
    {
        if (CM_Get_Device_Interface_List_SizeW(out var length, HubInterface, hubInstanceId, 0) != 0 || length <= 1) return null;
        var buffer = new char[length];
        if (CM_Get_Device_Interface_ListW(HubInterface, hubInstanceId, buffer, length, 0) != 0) return null;
        var path = new string(buffer).Split('\0')[0];
        return path.Length > 0 ? path : null;
    }

    // USB_DESCRIPTOR_REQUEST: 4-byte port index, 8-byte setup packet, then the USB string descriptor.
    private static byte[] ReadDescriptor(SafeFileHandle handle, int port, byte index, ushort language)
    {
        var request = new byte[12 + 255];
        BitConverter.GetBytes(port).CopyTo(request, 0);
        request[4] = 0x80;
        request[5] = 6; // GET_DESCRIPTOR
        request[6] = index;
        request[7] = 3; // STRING
        BitConverter.GetBytes(language).CopyTo(request, 8);
        BitConverter.GetBytes((ushort)255).CopyTo(request, 10);
        return DeviceIoControl(handle, IoctlDescriptor, request, request.Length, request, request.Length, out var returned, IntPtr.Zero)
            ? DescriptorData(request, returned) : [];
    }

    internal static byte[] DescriptorData(byte[] response, int returned)
    {
        if (returned < 14 || returned > response.Length || response[13] != 3) return [];
        var length = response[12];
        if (length < 2 || length % 2 != 0 || length > returned - 12) return [];
        return response.AsSpan(14, length - 2).ToArray();
    }

    /// <summary>(hub instance id, port) → speed and USB identity; ports that cannot be read are left out.</summary>
    public static Dictionary<(string Hub, int Port), UsbPortSpeed> Read(IEnumerable<(string Hub, int Port, string Device)> ports)
    {
        var speeds = new Dictionary<(string Hub, int Port), UsbPortSpeed>();
        foreach (var hub in ports.GroupBy(port => port.Hub, StringComparer.OrdinalIgnoreCase))
        {
            if (HubPath(hub.Key) is not { } path) continue;
            // GENERIC_WRITE, FILE_SHARE_WRITE, OPEN_EXISTING: what the hub driver expects for these IOCTLs.
            using var handle = CreateFileW(path, 0x40000000, 2, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (handle.IsInvalid) continue;
            foreach (var (_, port, device) in hub)
            {
                // USB_NODE_CONNECTION_INFORMATION_EX (packed): index at 0, Speed at 23 after the 18-byte device descriptor.
                var info = new byte[64];
                BitConverter.GetBytes(port).CopyTo(info, 0);
                if (!DeviceIoControl(handle, IoctlConnectionInfoEx, info, info.Length, info, info.Length, out var infoLength, IntPtr.Zero) || infoLength < 24) continue;
                // USB_NODE_CONNECTION_INFORMATION_EX_V2: index, length, protocols to ask about (USB 3), flags.
                var v2 = new byte[16];
                BitConverter.GetBytes(port).CopyTo(v2, 0);
                BitConverter.GetBytes(16).CopyTo(v2, 4);
                BitConverter.GetBytes(4).CopyTo(v2, 8);
                var flags = DeviceIoControl(handle, IoctlConnectionInfoExV2, v2, v2.Length, v2, v2.Length, out var v2Length, IntPtr.Zero) && v2Length >= 16
                    ? BitConverter.ToInt32(v2, 12) : 0;
                // Flags: 1 operating at SuperSpeed, 2 SuperSpeed capable, 4 operating at SuperSpeedPlus.
                // iManufacturer/iProduct belong to the device descriptor at offset 4, not the Windows driver provider.
                var languages = (info[18] != 0 || info[19] != 0) && IsAwake(device) ? ReadDescriptor(handle, port, 0, 0) : [];
                var language = languages.Length >= 2 ? BitConverter.ToUInt16(languages, 0) : (ushort)0;
                string Text(byte index) => index == 0 || language == 0 ? string.Empty
                    : Encoding.Unicode.GetString(ReadDescriptor(handle, port, index, language)).Trim('\0', ' ');
                speeds[(hub.Key, port)] = new UsbPortSpeed(info[23], (flags & 2) != 0, (flags & 4) != 0)
                {
                    Manufacturer = Text(info[18]), Product = Text(info[19])
                };
            }
        }
        return speeds;
    }
}
