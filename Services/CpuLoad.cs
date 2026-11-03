using System.Runtime.InteropServices;

namespace TweakHub.Services;

/// <summary>Load of one logical CPU over a short sample: busy share, interrupt and DPC share, interrupts per second.</summary>
internal sealed record CpuLoadSample(int Cpu, double BusyPercent, double InterruptDpcPercent, double InterruptsPerSecond);

/// <summary>Per-CPU load from NtQuerySystemInformation: language independent, unlike performance counter names.</summary>
internal static class CpuLoad
{
    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(int infoClass, byte[] buffer, int length, out int returned);

    private const int ProcessorPerformance = 8; // SystemProcessorPerformanceInformation
    private const int EntrySize = 48;            // Idle, Kernel, User, Dpc, Interrupt (100 ns) and InterruptCount

    public static async Task<IReadOnlyList<CpuLoadSample>> SampleAsync(TimeSpan duration)
    {
        if (!CpuTopology.Supported) return [];
        var before = Read();
        await Task.Delay(duration);
        var after = Read();
        return before is null || after is null ? [] : Compare(before, after, duration.TotalSeconds);
    }

    private static byte[]? Read()
    {
        var buffer = new byte[EntrySize * CpuTopology.Count];
        return NtQuerySystemInformation(ProcessorPerformance, buffer, buffer.Length, out var returned) == 0 && returned == buffer.Length ? buffer : null;
    }

    internal static IReadOnlyList<CpuLoadSample> Compare(byte[] before, byte[] after, double seconds)
    {
        long Delta(int cpu, int field) => BitConverter.ToInt64(after, cpu * EntrySize + field * 8) - BitConverter.ToInt64(before, cpu * EntrySize + field * 8);
        return Enumerable.Range(0, Math.Min(before.Length, after.Length) / EntrySize).Select(cpu =>
        {
            // Kernel time includes idle time.
            var total = Math.Max(1, Delta(cpu, 1) + Delta(cpu, 2));
            var interrupts = BitConverter.ToUInt32(after, cpu * EntrySize + 40) - BitConverter.ToUInt32(before, cpu * EntrySize + 40);
            return new CpuLoadSample(cpu, 100.0 * (total - Delta(cpu, 0)) / total, 100.0 * (Delta(cpu, 3) + Delta(cpu, 4)) / total,
                interrupts / Math.Max(0.001, seconds));
        }).ToList();
    }
}
