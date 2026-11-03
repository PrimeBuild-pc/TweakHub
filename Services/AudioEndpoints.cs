using System.Runtime.InteropServices;

namespace TweakHub.Services;

/// <summary>A default audio endpoint (playback or recording) and whether a front-panel jack of it is in use.</summary>
internal sealed record AudioEndpoint(string Id, bool Capture, bool FrontJack);

/// <summary>Default playback and recording devices from Core Audio; jack positions come from the driver (KSJACK_DESCRIPTION).</summary>
internal static class AudioEndpoints
{
    public static IReadOnlyList<AudioEndpoint> ReadDefaults()
    {
        var result = new List<AudioEndpoint>();
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            foreach (var capture in new[] { false, true })
            {
                // eRender = 0, eCapture = 1; eConsole = 0, the role games use.
                if (enumerator.GetDefaultAudioEndpoint(capture ? 1 : 0, 0, out var device) != 0 || device is null) continue;
                if (device.GetId(out var id) != 0 || string.IsNullOrEmpty(id)) continue;
                result.Add(new AudioEndpoint(id, capture, UsesFrontJack(device)));
            }
        }
        catch (COMException) { /* No audio service or no devices: nothing to suggest. */ }
        return result;
    }

    // eGeoLocFront = 2 and eGenLocPrimaryBox = 0: the jacks on the front of the case.
    internal static bool IsFrontPanel(KsJackDescription jack) => jack.IsConnected != 0 && jack.GeoLocation == 2 && jack.GenLocation == 0;

    private static bool UsesFrontJack(IMMDevice device)
    {
        try
        {
            var topologyId = typeof(IDeviceTopology).GUID;
            if (device.Activate(ref topologyId, 1, IntPtr.Zero, out var topologyObject) != 0) return false;
            var topology = (IDeviceTopology)topologyObject;
            if (topology.GetConnector(0, out var connector) != 0 || connector.GetConnectedTo(out var other) != 0) return false;
            var jackId = typeof(IKsJackDescription).GUID;
            if (((IPart)other).Activate(1, ref jackId, out var jackObject) != 0) return false;
            var jacks = (IKsJackDescription)jackObject;
            if (jacks.GetJackCount(out var count) != 0) return false;
            for (uint index = 0; index < count; index++)
                if (jacks.GetJackDescription(index, out var jack) == 0 && IsFrontPanel(jack)) return true;
        }
        catch (Exception error) when (error is COMException or InvalidCastException) { /* Many USB drivers describe no jacks. */ }
        return false;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KsJackDescription
    {
        public uint ChannelMapping;
        public uint Color;
        public int ConnectionType;
        public int GeoLocation;
        public int GenLocation;
        public int PortConnection;
        public int IsConnected;
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator;

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice? device);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
        [PreserveSig] int OpenPropertyStore(int access, out IntPtr store);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    }

    [ComImport, Guid("2A07407E-6497-4A18-9787-32F79BD0D98F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDeviceTopology
    {
        [PreserveSig] int GetConnectorCount(out uint count);
        [PreserveSig] int GetConnector(uint index, out IConnector connector);
    }

    [ComImport, Guid("9C2C4058-23F5-41DE-877A-DF3AF236A09E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IConnector
    {
        [PreserveSig] int GetType(out int type);
        [PreserveSig] int GetDataFlow(out int flow);
        [PreserveSig] int ConnectTo(IConnector other);
        [PreserveSig] int Disconnect();
        [PreserveSig] int IsConnected(out int connected);
        [PreserveSig] int GetConnectedTo(out IConnector other);
    }

    [ComImport, Guid("AE2DE0E4-5BCA-4F2D-AA46-5D13F8FDB3A9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPart
    {
        [PreserveSig] int GetName(out IntPtr name);
        [PreserveSig] int GetLocalId(out uint id);
        [PreserveSig] int GetGlobalId(out IntPtr id);
        [PreserveSig] int GetPartType(out int type);
        [PreserveSig] int GetSubType(out Guid subType);
        [PreserveSig] int GetControlInterfaceCount(out uint count);
        [PreserveSig] int GetControlInterface(uint index, out IntPtr control);
        [PreserveSig] int EnumPartsIncoming(out IntPtr parts);
        [PreserveSig] int EnumPartsOutgoing(out IntPtr parts);
        [PreserveSig] int GetTopologyObject(out IntPtr topology);
        [PreserveSig] int Activate(int clsContext, ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
    }

    [ComImport, Guid("4509F757-2D46-4637-8E62-CE7DB944F57B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IKsJackDescription
    {
        [PreserveSig] int GetJackCount(out uint count);
        [PreserveSig] int GetJackDescription(uint index, out KsJackDescription description);
    }
}
