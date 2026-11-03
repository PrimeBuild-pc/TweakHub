using System.Runtime.InteropServices;

namespace TweakHub.Services;

internal sealed record LogicalCpu(int Index, int Core, byte EfficiencyClass);

/// <summary>One level-3 cache: the group-0 CPUs that share it and its size.</summary>
internal sealed record CacheGroup(ulong Mask, long Bytes);

internal static class CpuTopology
{
    [DllImport("kernel32.dll")]
    private static extern ushort GetActiveProcessorGroupCount();
    [DllImport("kernel32.dll")]
    private static extern uint GetActiveProcessorCount(ushort group);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformationEx(int relationship, IntPtr buffer, ref int length);

    // AssignmentSetOverride has no processor-group selector in this editor. Never truncate a multi-group machine.
    public static bool Supported => Environment.Is64BitProcess && GetActiveProcessorGroupCount() == 1;
    public static int Count => Supported ? checked((int)GetActiveProcessorCount(0)) : 0;
    public static ulong AllowedMask => Read().Aggregate(0UL, (mask, cpu) => mask | 1UL << cpu.Index);

    public static IReadOnlyList<LogicalCpu> Read()
    {
        if (!Supported) return [];
        var length = 0;
        GetLogicalProcessorInformationEx(0, IntPtr.Zero, ref length); // RelationProcessorCore
        if (length <= 0) return [];
        var pointer = Marshal.AllocHGlobal(length);
        try
        {
            if (!GetLogicalProcessorInformationEx(0, pointer, ref length)) return [];
            var bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, length);
            return Parse(bytes);
        }
        finally { Marshal.FreeHGlobal(pointer); }
    }

    /// <summary>Level-3 caches of group 0; several on dual-CCD CPUs, with the 3D V-Cache one larger.</summary>
    public static IReadOnlyList<CacheGroup> ReadL3()
    {
        if (!Supported) return [];
        var length = 0;
        GetLogicalProcessorInformationEx(2, IntPtr.Zero, ref length); // RelationCache
        if (length <= 0) return [];
        var pointer = Marshal.AllocHGlobal(length);
        try
        {
            if (!GetLogicalProcessorInformationEx(2, pointer, ref length)) return [];
            var bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, length);
            return ParseL3(bytes);
        }
        finally { Marshal.FreeHGlobal(pointer); }
    }

    internal static IReadOnlyList<CacheGroup> ParseL3(byte[] buffer)
    {
        var groups = new List<CacheGroup>();
        for (var offset = 0; offset + 8 <= buffer.Length;)
        {
            var size = BitConverter.ToInt32(buffer, offset + 4);
            if (size < 8 || size > buffer.Length - offset) return [];
            // CACHE_RELATIONSHIP: Level at 8, CacheSize at 12, Type at 16 (0 = unified), GroupMask at 40.
            if (BitConverter.ToInt32(buffer, offset) == 2 && size >= 56 && buffer[offset + 8] == 3
                && BitConverter.ToInt32(buffer, offset + 16) == 0 && BitConverter.ToUInt16(buffer, offset + 48) == 0)
                groups.Add(new CacheGroup(BitConverter.ToUInt64(buffer, offset + 40), BitConverter.ToUInt32(buffer, offset + 12)));
            offset += size;
        }
        return groups;
    }

    internal static IReadOnlyList<LogicalCpu> Parse(byte[] buffer)
    {
        var cpus = new List<LogicalCpu>();
        var core = 0;
        for (var offset = 0; offset + 8 <= buffer.Length;)
        {
            var size = BitConverter.ToInt32(buffer, offset + 4);
            if (size < 8 || size > buffer.Length - offset) return [];
            if (BitConverter.ToInt32(buffer, offset) == 0)
            {
                if (size < 48 || BitConverter.ToUInt16(buffer, offset + 30) != 1 || BitConverter.ToUInt16(buffer, offset + 40) != 0) return [];
                var mask = BitConverter.ToUInt64(buffer, offset + 32);
                cpus.AddRange(Enumerable.Range(0, 64).Where(index => (mask & 1UL << index) != 0)
                    .Select(index => new LogicalCpu(index, core, buffer[offset + 9])));
                core++;
            }
            offset += size;
            if (offset == buffer.Length) return cpus.OrderBy(cpu => cpu.Index).ToList();
        }
        return [];
    }
}
