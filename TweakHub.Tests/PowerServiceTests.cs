using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class PowerServiceTests
{
    // Reads the active plan of this machine: read-only. Old backups name settings by alias, newer callers by GUID.
    [Test]
    public void GetAcValueReadsTheSameSettingByAliasOrGuid()
    {
        var scheme = PowerPlanService.ActiveScheme()!.Value;
        var expected = PowerPlanService.ReadValue(scheme, new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("893dee8e-2bef-41e0-89c6-b55d0929964c"))!.Value.Ac;

        Assert.Multiple(() =>
        {
            Assert.That(PowerService.GetAcValue(scheme.ToString(), "SUB_PROCESSOR", "PROCTHROTTLEMIN"), Is.EqualTo((int)expected));
            Assert.That(PowerService.GetAcValue(scheme.ToString(), "54533251-82be-4824-96c1-47b60b740d00", "893dee8e-2bef-41e0-89c6-b55d0929964c"), Is.EqualTo((int)expected));
            Assert.That(PowerService.GetAcValue(scheme.ToString(), "SUB_PROCESSOR", "MISSING"), Is.Null);
        });
    }

    // Saved choices (PreferredScheme, HighPerformanceScheme) are lowercase GUID strings and compared as text.
    [Test]
    public void GetSchemesUsesLowercaseGuidsAndMarksTheActivePlan()
    {
        var schemes = PowerService.GetSchemes();
        Assert.Multiple(() =>
        {
            Assert.That(schemes.All(scheme => scheme.Guid == scheme.Guid.ToLowerInvariant() && Guid.TryParse(scheme.Guid, out _)), Is.True);
            Assert.That(schemes.Single(scheme => scheme.Active).Guid, Is.EqualTo(PowerPlanService.ActiveScheme().ToString()));
        });
    }
}
