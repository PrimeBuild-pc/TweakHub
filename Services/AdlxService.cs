using System.Runtime.InteropServices;

namespace TweakHub.Services;

/// <summary>
/// Real PCIe link of AMD GPUs from ADLX (amdadlx64.dll, installed with the Radeon driver). Radeon cards have a PCIe switch
/// on board, so the link Windows reads from the GPU is the internal one and always looks like x16.
/// </summary>
internal static unsafe class AdlxService
{
    [DllImport("amdadlx64.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ADLXQueryFullVersion(out ulong version);

    [DllImport("amdadlx64.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ADLXInitialize(ulong version, out IntPtr system);

    [DllImport("amdadlx64.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ADLXTerminate();

    // Vtable slots from the ADLX SDK headers (ISystem.h: IADLXSystem, IADLXGPUList, IADLXGPU; ISystem1.h: IADLXGPU1).
    private const int Release = 1, QueryInterface = 2, SystemGetGpus = 1, ListSize = 3, ListAtGpu = 11, GpuPnpString = 9,
        Gpu1BusType = 19, Gpu1LaneWidth = 20;

    private static IntPtr Slot(IntPtr instance, int slot) => Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size);

    private static void ReleaseInstance(IntPtr instance)
    {
        if (instance != IntPtr.Zero) ((delegate* unmanaged[Stdcall]<IntPtr, long>)Slot(instance, Release))(instance);
    }

    /// <summary>Instance id → (generation, lanes) of every AMD GPU; empty without the Radeon driver.</summary>
    public static Dictionary<string, (int Gen, int Width)> ReadLinks()
    {
        var links = new Dictionary<string, (int Gen, int Width)>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (ADLXQueryFullVersion(out var version) != 0 || ADLXInitialize(version, out var system) != 0) return links;
            try
            {
                IntPtr list;
                if (((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Slot(system, SystemGetGpus))(system, &list) != 0) return links;
                var count = ((delegate* unmanaged[Stdcall]<IntPtr, uint>)Slot(list, ListSize))(list);
                for (uint index = 0; index < count; index++)
                {
                    IntPtr gpu = IntPtr.Zero, gpu1 = IntPtr.Zero;
                    try
                    {
                        if (((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>)Slot(list, ListAtGpu))(list, index, &gpu) != 0) continue;
                        sbyte* pnp;
                        int busType;
                        uint lanes;
                        fixed (char* iid = "IADLXGPU1")
                            if (((delegate* unmanaged[Stdcall]<IntPtr, sbyte**, int>)Slot(gpu, GpuPnpString))(gpu, &pnp) != 0
                                || ((delegate* unmanaged[Stdcall]<IntPtr, char*, IntPtr*, int>)Slot(gpu, QueryInterface))(gpu, iid, &gpu1) != 0
                                || ((delegate* unmanaged[Stdcall]<IntPtr, int*, int>)Slot(gpu1, Gpu1BusType))(gpu1, &busType) != 0
                                || ((delegate* unmanaged[Stdcall]<IntPtr, uint*, int>)Slot(gpu1, Gpu1LaneWidth))(gpu1, &lanes) != 0)
                                continue;
                        // ADLX_PCI_BUS_TYPE: 3 = PCIe 1.x, 4 = 2.0 … 7 = 5.0.
                        links[new string(pnp)] = (busType >= 3 ? busType - 2 : 0, (int)lanes);
                    }
                    finally
                    {
                        ReleaseInstance(gpu1);
                        ReleaseInstance(gpu);
                    }
                }
                ReleaseInstance(list);
            }
            finally
            {
                ADLXTerminate();
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException) { }
        return links;
    }
}
