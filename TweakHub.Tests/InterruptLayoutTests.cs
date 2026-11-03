using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class InterruptLayoutTests
{
    /// <summary>CPU numbers in core order, like GetLogicalProcessorInformationEx reports them.</summary>
    private static List<LogicalCpu> Cpus(int cores, int smt = 2, Func<int, byte>? efficiency = null) =>
        Enumerable.Range(0, cores).SelectMany(core => Enumerable.Range(0, smt)
            .Select(thread => new LogicalCpu(core * smt + thread, core, efficiency?.Invoke(core) ?? 0))).ToList();

    private static ulong Mask(int from, int count) => Enumerable.Range(from, count).Aggregate(0UL, (mask, cpu) => mask | 1UL << cpu);

    [Test]
    public void EightCoresGetOneFirstThreadPerRoleOutsideCoreZero()
    {
        var layout = InterruptLayoutPlanner.Plan(Cpus(8), []);
        Assert.That(layout.Unavailable, Is.False);
        Assert.That(layout.CpuByRole[InterruptRole.Gpu], Is.EqualTo(14));
        Assert.That(layout.CpuByRole[InterruptRole.Usb], Is.EqualTo(12));
        Assert.That(layout.CpuByRole[InterruptRole.Network], Is.EqualTo(10));
        Assert.That(layout.CpuByRole[InterruptRole.Audio], Is.EqualTo(8));
        Assert.That(layout.SharedNetworkAudio, Is.False);
    }

    [Test]
    public void SixCoresOrFewerShareNetworkAndAudio()
    {
        var layout = InterruptLayoutPlanner.Plan(Cpus(6), []);
        Assert.That(layout.SharedNetworkAudio, Is.True);
        Assert.That(layout.CpuByRole[InterruptRole.Audio], Is.EqualTo(layout.CpuByRole[InterruptRole.Network]));
        var four = InterruptLayoutPlanner.Plan(Cpus(4, smt: 1), []);
        Assert.That(four.CpuByRole.Values, Has.None.EqualTo(0));
        Assert.That(InterruptLayoutPlanner.Plan(Cpus(2), []).Unavailable, Is.True);
        Assert.That(InterruptLayoutPlanner.Plan([], []).Unavailable, Is.True);
    }

    [Test]
    public void HybridUsesOnlyPerformanceCores()
    {
        // 8 P-cores with SMT (CPU 0-15, class 1), then 16 E-cores (class 0).
        var cpus = Cpus(8, efficiency: _ => 1)
            .Concat(Enumerable.Range(16, 16).Select(cpu => new LogicalCpu(cpu, cpu - 8, 0))).ToList();
        var layout = InterruptLayoutPlanner.Plan(cpus, []);
        Assert.That(layout.HybridPerformanceOnly, Is.True);
        Assert.That(layout.CpuByRole.Values, Has.All.InRange(2, 15));
    }

    [Test]
    public void DualCcdPicksTheCcdWithoutVCacheOrWithoutCpuZero()
    {
        var x3d = InterruptLayoutPlanner.Plan(Cpus(16), [new(Mask(0, 16), 96 << 20), new(Mask(16, 16), 32 << 20)]);
        Assert.That(x3d.CacheGroupChosen, Is.True);
        Assert.That(x3d.CpuByRole[InterruptRole.Gpu], Is.EqualTo(30));
        Assert.That(x3d.CpuByRole.Values, Has.All.GreaterThanOrEqualTo(16));

        var swapped = InterruptLayoutPlanner.Plan(Cpus(16), [new(Mask(0, 16), 32 << 20), new(Mask(16, 16), 96 << 20)]);
        Assert.That(swapped.CpuByRole.Values, Has.All.InRange(2, 15));

        var equal = InterruptLayoutPlanner.Plan(Cpus(16), [new(Mask(0, 16), 32 << 20), new(Mask(16, 16), 32 << 20)]);
        Assert.That(equal.CpuByRole.Values, Has.All.GreaterThanOrEqualTo(16));
    }

    [Test]
    public void RolesComeFromTheClassAndHdAudioFromTheClassCode()
    {
        static InterruptDevice Device(string cls, bool hdAudio = false, int policy = 0, ulong mask = 0) =>
            new($@"PCI\{cls}", cls, cls, true, false, null, InterruptPriority.Undefined, policy, mask, hdAudio);
        Assert.That(InterruptLayoutPlanner.RoleOf(Device("Display")), Is.EqualTo(InterruptRole.Gpu));
        Assert.That(InterruptLayoutPlanner.RoleOf(Device("USB")), Is.EqualTo(InterruptRole.Usb));
        Assert.That(InterruptLayoutPlanner.RoleOf(Device("Net")), Is.EqualTo(InterruptRole.Network));
        Assert.That(InterruptLayoutPlanner.RoleOf(Device("System", hdAudio: true)), Is.EqualTo(InterruptRole.Audio));
        Assert.That(InterruptLayoutPlanner.RoleOf(Device("System")), Is.Null);

        var layout = InterruptLayoutPlanner.Plan(Cpus(8), []);
        var rows = InterruptLayoutPlanner.Rows(layout, [Device("Display", policy: 4, mask: 1UL << 14), Device("Net"), Device("System")]);
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].AlreadySet, Is.True);
        Assert.That(rows[1].AlreadySet, Is.False);
        Assert.That(rows[1].Mask, Is.EqualTo(1UL << 10));
    }

    [Test]
    public void LayoutWritesOnlyAffinityNeverMsiOrPriority()
    {
        var device = new InterruptDevice(@"PCI\GPU", "GPU", "Display", true, true, 1, InterruptPriority.High, 0, 0);
        var changes = InterruptService.BuildChanges(device, null, null, 1UL << 14);
        Assert.That(changes.Select(change => change.ValueName), Is.EquivalentTo(new[] { "DevicePolicy", "AssignmentSetOverride" }));
    }

    [Test]
    public void ScanReadsTheHdAudioClassCode()
    {
        var device = InterruptService.ParseDevices("""
            [{"Id":"PCI\\VEN_1002&DEV_AB30\\4&1","Name":"HD Audio","Class":"System","HdAudio":true,"MsiCapable":true,"Msi":1}]
            """).Single();
        Assert.That(device.IsHdAudio, Is.True);
        Assert.That(device.AudioInUse, Is.True, "older scans without the field keep the device");
    }

    [Test]
    public void AudioCoreSkipsGpuAudioAndUnusedControllers()
    {
        static InterruptDevice Audio(string id, int address, bool inUse = true) =>
            new(id, id, "System", true, true, null, InterruptPriority.Undefined, 0, 0, true, 48, address, inUse);
        var gpu = new InterruptDevice(@"PCI\VEN_1002&DEV_73A5\1", "GPU", "Display", true, true, 1, InterruptPriority.Undefined, 0, 0,
            Bus: 48, Address: 0);
        var layout = InterruptLayoutPlanner.Plan(Cpus(8), []);
        IReadOnlyList<InterruptLayoutRow> Rows(InterruptDevice audio) => InterruptLayoutPlanner.Rows(layout, [gpu, audio]);

        // Radeon HDMI/DP audio: function 1 of the GPU slot, same vendor.
        var hdmi = Rows(Audio(@"PCI\VEN_1002&DEV_AB28\2", 1));
        Assert.That(hdmi[1].Skip, Is.EqualTo(LayoutSkip.GpuAudio));
        Assert.That(InterruptLayoutPlanner.HasAudio(hdmi), Is.False);

        // APU: onboard HD Audio shares the slot but not the vendor.
        var onboard = Rows(Audio(@"PCI\VEN_1022&DEV_15E3\3", 6));
        Assert.That(onboard[1].Skip, Is.Null);
        Assert.That(InterruptLayoutPlanner.HasAudio(onboard), Is.True);

        // Realtek disabled by a user who plays with a USB DAC.
        var unused = Rows(Audio(@"PCI\VEN_1022&DEV_1487\4", 0x10003, inUse: false));
        Assert.That(unused[1].Skip, Is.EqualTo(LayoutSkip.AudioUnused));
        Assert.That(unused[1].AlreadySet, Is.False);
    }

    [Test]
    public void ParsesUnifiedLevelThreeCaches()
    {
        static byte[] Record(byte level, uint bytes, ulong mask)
        {
            var record = new byte[56];
            BitConverter.GetBytes(2).CopyTo(record, 0); // RelationCache
            BitConverter.GetBytes(56).CopyTo(record, 4);
            record[8] = level;
            BitConverter.GetBytes(bytes).CopyTo(record, 12);
            BitConverter.GetBytes((ushort)1).CopyTo(record, 38);
            BitConverter.GetBytes(mask).CopyTo(record, 40);
            return record;
        }
        var buffer = Record(2, 1 << 20, 0x3).Concat(Record(3, 32u << 20, 0xffff)).Concat(Record(3, 96u << 20, 0xffff0000)).ToArray();
        var caches = CpuTopology.ParseL3(buffer);
        Assert.That(caches, Has.Count.EqualTo(2));
        Assert.That(caches[0], Is.EqualTo(new CacheGroup(0xffff, 32 << 20)));
        Assert.That(caches[1].Bytes, Is.EqualTo(96 << 20));
    }
}
