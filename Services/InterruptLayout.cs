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
    LayoutSkip? Skip = null, string? AudioDevice = null);

/// <summary>What is plugged into one USB controller: input devices, a camera and the default audio devices.</summary>
internal sealed record UsbControllerUse(bool Mouse, bool Keyboard, bool Camera, IReadOnlyList<string> Audio)
{
    public bool HasInput => Mouse || Keyboard;
}

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

    /// <summary>
    /// Rows of the layout. A USB controller that carries the default playback or recording device and no mouse or keyboard
    /// gets the audio core, so the audio interface or DAC is not interrupted by input polling.
    /// </summary>
    public static IReadOnlyList<InterruptLayoutRow> Rows(InterruptLayout layout, IEnumerable<InterruptDevice> devices,
        IReadOnlyDictionary<string, UsbControllerUse>? usb = null)
    {
        var all = devices.ToList();
        return all.Select(device => (Device: device, Role: RoleOf(device)))
            .Where(item => item.Role is { } role && layout.CpuByRole.ContainsKey(role))
            .Select(item =>
            {
                var role = item.Role!.Value;
                string? audioDevice = null;
                if (role == InterruptRole.Usb && usb?.GetValueOrDefault(item.Device.Id) is { Audio.Count: > 0, HasInput: false } use)
                {
                    role = InterruptRole.Audio;
                    audioDevice = string.Join(", ", use.Audio);
                }
                var cpu = layout.CpuByRole[role];
                var mask = 1UL << cpu;
                var skip = role == InterruptRole.Audio && audioDevice is null ? AudioSkip(item.Device, all) : null;
                return new InterruptLayoutRow(item.Device, role, cpu, mask,
                    skip is null && item.Device.HasAffinity && item.Device.AffinityMask == mask, skip, audioDevice);
            })
            .ToList();
    }

    private static readonly Guid RootContainer = new("00000000-0000-0000-ffff-ffffffffffff");
    private static readonly Guid MouseClass = new("4d36e96f-e325-11ce-bfc1-08002be10318");
    private static readonly Guid KeyboardClass = new("4d36e96b-e325-11ce-bfc1-08002be10318");
    private static readonly Guid ImageClass = new("6bdd1fc6-810f-11d0-bec7-08002be2092f");
    private static readonly Guid CameraClass = new("ca3e7ab9-b4c3-4ae6-8251-579ef933890f");

    /// <summary>
    /// For each PCI USB controller (by instance ID), what hangs below it. Default audio endpoints are matched to their USB
    /// device through the container ID; a headset's own media-key "keyboard" belongs to the audio device, not to input.
    /// </summary>
    public static IReadOnlyDictionary<string, UsbControllerUse> UsbUse(IReadOnlyList<DeviceNode> nodes, IEnumerable<string> defaultAudioIds)
    {
        var byId = new Dictionary<string, DeviceNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes) byId.TryAdd(node.Id, node);
        var audio = new Dictionary<Guid, List<string>>();
        foreach (var id in defaultAudioIds)
            if (byId.GetValueOrDefault(@"SWD\MMDEVAPI\" + id) is { Container: { } container } endpoint && container != RootContainer && container != Guid.Empty)
            {
                var names = audio.TryGetValue(container, out var list) ? list : audio[container] = [];
                if (!names.Contains(endpoint.Name)) names.Add(endpoint.Name);
            }

        string? Controller(DeviceNode node)
        {
            for (DeviceNode? current = node; current is not null; current = current.Parent is { } parent ? byId.GetValueOrDefault(parent) : null)
                if (current.Id.StartsWith(@"PCI\", StringComparison.OrdinalIgnoreCase)) return current == node ? null : current.Id;
            return null;
        }

        var uses = new Dictionary<string, UseBuilder>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes.Where(node => node.Id.StartsWith(@"USB\", StringComparison.OrdinalIgnoreCase) || node.Id.StartsWith(@"HID\", StringComparison.OrdinalIgnoreCase)))
        {
            if (Controller(node) is not { } controller) continue;
            if (!uses.TryGetValue(controller, out var use)) uses[controller] = use = new UseBuilder();
            if (node.Container is { } container && audio.TryGetValue(container, out var names))
                use.Audio.AddRange(names.Where(name => !use.Audio.Contains(name)));
            else
            {
                use.Mouse |= node.ClassGuid == MouseClass;
                use.Keyboard |= node.ClassGuid == KeyboardClass;
                use.Camera |= node.ClassGuid == ImageClass || node.ClassGuid == CameraClass;
            }
        }
        return uses.ToDictionary(entry => entry.Key, entry => new UsbControllerUse(entry.Value.Mouse, entry.Value.Keyboard, entry.Value.Camera, entry.Value.Audio),
            StringComparer.OrdinalIgnoreCase);
    }

    private sealed class UseBuilder
    {
        public bool Mouse, Keyboard, Camera;
        public List<string> Audio { get; } = [];
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
