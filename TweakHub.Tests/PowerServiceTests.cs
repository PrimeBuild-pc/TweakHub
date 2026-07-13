using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class PowerServiceTests
{
    [Test]
    public void ParseAcSettingReadsTheTargetBlockNotItsLimitsOrDcValue()
    {
        const string output = """
            GUID Alias: CPMINCORES
            Minimum: 0x00000000
            Maximum: 0x00000064
            Increment: 0x00000001
            Current AC: 0x00000032
            Current DC: 0x0000000a
            GUID Alias: PROCTHROTTLEMIN
            Current AC: 0x00000064
            Current DC: 0x00000005
            """;

        Assert.That(PowerService.ParseAcSetting(output, "CPMINCORES"), Is.EqualTo(50));
        Assert.That(PowerService.ParseAcSetting(output, "PROCTHROTTLEMIN"), Is.EqualTo(100));
        Assert.That(PowerService.ParseAcSetting(output, "MISSING"), Is.Null);
    }
}
