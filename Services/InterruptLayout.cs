namespace TweakHub.Services;

public enum InterruptRole { Gpu, Usb, Network, Audio }

/// <summary>One logical CPU (the first thread of a physical core) per role, or why none can be suggested.</summary>
internal sealed record InterruptLayout(
    IReadOnlyDictionary<InterruptRole, int> CpuByRole,
    bool HybridPerformanceOnly = false,
    bool CacheGroupChosen = false,
    bool SharedNetworkAudio = false,
    bool Unavailable = false);

/// <summary>A device of the layout and whether its affinity is already the suggested one.</summary>
internal sealed record InterruptLayoutRow(InterruptDevice Device, InterruptRole Role, int Cpu, ulong Mask, bool AlreadySet);

internal static class InterruptLayoutPlanner
{
    private static readonly InterruptLayout None = new(new Dictionary<InterruptRole, int>(), Unavailable: true);

    public static InterruptRole? RoleOf(InterruptDevice device) => device.IsHdAudio ? InterruptRole.Audio : device.Class switch
    {
        "Display" => InterruptRole.Gpu,
        "USB" => InterruptRole.Usb,
        "Net" => InterruptRole.Network,
        "MEDIA" => InterruptRole.Audio,
        _ => null
    };

    /// <summary>
    /// Cores come from the live topology, never from fixed numbers: outside core 0, P-cores only on hybrid CPUs,
    /// and with several L3 caches (dual CCD) the smaller one (no 3D V-Cache), or the one without CPU 0 when equal.
    /// Up to 6 cores, network and audio share a core. Highest cores first: games favour the lowest ones.
    /// </summary>
    public static InterruptLayout Plan(IReadOnlyList<LogicalCpu> cpus, IReadOnlyList<CacheGroup> caches)
    {
        if (cpus.Count == 0 || cpus.All(cpu => cpu.Index != 0)) return None;
        var cores = cpus.GroupBy(cpu => cpu.Core).Select(core => core.OrderBy(cpu => cpu.Index).First()).ToList();
        var coreZero = cpus.First(cpu => cpu.Index == 0).Core;
        var candidates = cores.Where(cpu => cpu.Core != coreZero).ToList();
        var classes = cpus.Select(cpu => cpu.EfficiencyClass).Distinct().ToList();
        var hybrid = classes.Count > 1;
        if (hybrid) candidates = candidates.Where(cpu => cpu.EfficiencyClass == classes.Max()).ToList();
        var cacheChosen = false;
        if (caches.Count > 1)
        {
            var smallest = caches.Min(cache => cache.Bytes);
            var chosen = caches.All(cache => cache.Bytes == smallest)
                ? caches.FirstOrDefault(cache => (cache.Mask & 1) == 0)
                : caches.First(cache => cache.Bytes == smallest);
            if (chosen is not null)
            {
                candidates = candidates.Where(cpu => (chosen.Mask >> cpu.Index & 1) == 1).ToList();
                cacheChosen = true;
            }
        }
        candidates = candidates.OrderByDescending(cpu => cpu.Index).ToList();
        if (candidates.Count < 3) return None;
        var shared = cores.Count <= 6 || candidates.Count < 4;
        return new InterruptLayout(new Dictionary<InterruptRole, int>
        {
            [InterruptRole.Gpu] = candidates[0].Index,
            [InterruptRole.Usb] = candidates[1].Index,
            [InterruptRole.Network] = candidates[2].Index,
            [InterruptRole.Audio] = candidates[shared ? 2 : 3].Index
        }, hybrid, cacheChosen, shared);
    }

    public static IReadOnlyList<InterruptLayoutRow> Rows(InterruptLayout layout, IEnumerable<InterruptDevice> devices) =>
        devices.Select(device => (Device: device, Role: RoleOf(device)))
            .Where(item => item.Role is { } role && layout.CpuByRole.ContainsKey(role))
            .Select(item =>
            {
                var cpu = layout.CpuByRole[item.Role!.Value];
                var mask = 1UL << cpu;
                return new InterruptLayoutRow(item.Device, item.Role.Value, cpu, mask,
                    item.Device.HasAffinity && item.Device.AffinityMask == mask);
            })
            .ToList();
}
