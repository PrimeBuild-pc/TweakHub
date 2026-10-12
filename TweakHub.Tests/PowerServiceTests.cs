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

    [Test]
    public void ParseSchemesKeepsCustomNamesActiveFlagAndSkipsDuplicates()
    {
        const string output = "Existing Power Schemes (* Active)\r\n-----------------------------------\r\n" +
            "Power Scheme GUID: 57696e68-616e-6365-506f-776572000000  (Winhance Power Plan)\r\n" +
            "Power Scheme GUID: C0AE4F83-B24F-4216-AB0B-B94722F1E8DD  (zoic's ultimate performance(amd)) *\r\n" +
            "Power Scheme GUID: c0ae4f83-b24f-4216-ab0b-b94722f1e8dd  (zoic's ultimate performance(amd))\r\n";

        var schemes = PowerService.ParseSchemes(output);
        Assert.Multiple(() =>
        {
            Assert.That(schemes.Select(scheme => scheme.Name), Is.EqualTo(new[] { "Winhance Power Plan", "zoic's ultimate performance(amd)" }));
            Assert.That(schemes[1].Guid, Is.EqualTo("c0ae4f83-b24f-4216-ab0b-b94722f1e8dd"));
            Assert.That(schemes.Select(scheme => scheme.Active), Is.EqualTo(new[] { false, true }));
        });
    }
}
