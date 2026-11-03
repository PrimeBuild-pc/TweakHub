namespace TweakHub.Services;

public enum InterruptRole { Gpu, Usb, Network, Audio }

/// <summary>One logical CPU (the first thread of a physical core) per role, or why none can be suggested.</summary>
internal sealed record InterruptLayout(
    IReadOnlyDictionary<InterruptRole, int> CpuByRole,
    bool HybridPerformanceOnly = false,
    bool CacheGroupChosen = false,
    bool SharedNetworkAudio = false,
    bool Unavailable = false);

/// <summary>Why an audio controller is left alone: it is not where gaming audio plays.</summary>
public enum LayoutSkip { GpuAudio, AudioUnused }

/// <summary>A device of the layout and whether its affinity is already the suggested one.</summary>
internal sealed record InterruptLayoutRow(InterruptDevice Device, InterruptRole Role, int Cpu, ulong Mask, bool AlreadySet,
    LayoutSkip? Skip = null);

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

    public static IReadOnlyList<InterruptLayoutRow> Rows(InterruptLayout layout, IEnumerable<InterruptDevice> devices)
    {
        var all = devices.ToList();
        return all.Select(device => (Device: device, Role: RoleOf(device)))
            .Where(item => item.Role is { } role && layout.CpuByRole.ContainsKey(role))
            .Select(item =>
            {
                var cpu = layout.CpuByRole[item.Role!.Value];
                var mask = 1UL << cpu;
                var skip = item.Role == InterruptRole.Audio ? AudioSkip(item.Device, all) : null;
                return new InterruptLayoutRow(item.Device, item.Role.Value, cpu, mask,
                    skip is null && item.Device.HasAffinity && item.Device.AffinityMask == mask, skip);
            })
            .ToList();
    }

    /// <summary>True when some audio device will actually get the audio core.</summary>
    public static bool HasAudio(IEnumerable<InterruptLayoutRow> rows) => rows.Any(row => row.Role == InterruptRole.Audio && row.Skip is null);

    /// <summary>
    /// The HDMI/DP audio of a graphics card (same PCI slot and vendor as a display device) or a controller with no
    /// working audio device behind it is not the gaming audio, which is then USB or elsewhere.
    /// </summary>
    private static LayoutSkip? AudioSkip(InterruptDevice audio, IReadOnlyList<InterruptDevice> devices)
    {
        static string Vendor(InterruptDevice device) =>
            System.Text.RegularExpressions.Regex.Match(device.Id, "VEN_([0-9A-F]{4})", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value.ToUpperInvariant();
        if (audio is { Bus: { } bus, Address: { } address } && devices.Any(other => other.Class == "Display"
                && other.Bus == bus && other.Address is { } otherAddress && otherAddress >> 16 == address >> 16
                && Vendor(other) == Vendor(audio)))
            return LayoutSkip.GpuAudio;
        return audio.AudioInUse ? null : LayoutSkip.AudioUnused;
    }
}
