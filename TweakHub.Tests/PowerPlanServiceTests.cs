using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class PowerPlanServiceTests
{
    // Reads the real power settings of this machine: read-only, nothing is written.
    [Test, CancelAfter(20000)]
    public void DefinitionsIncludeNamedRangeAndOptionSettingsOfTheActivePlan()
    {
        var definitions = PowerPlanService.LoadDefinitions();
        var minimumProcessorState = definitions.SingleOrDefault(definition => definition.Id == new Guid("893dee8e-2bef-41e0-89c6-b55d0929964c"));
        var active = PowerPlanService.ActiveScheme();

        Assert.Multiple(() =>
        {
            Assert.That(definitions, Has.Count.GreaterThan(20));
            Assert.That(definitions.All(definition => definition.Name.Length > 0), Is.True, "Every setting has a localized name");
            // Only the kind is checked: tweaking tools sometimes rewrite a setting's limits (here ValueMax = 0 was seen).
            Assert.That(minimumProcessorState?.IsRange, Is.True);
            Assert.That(definitions.Any(definition => definition.IsRange && definition.Max > 0 && definition.Units.Length > 0), Is.True);
            Assert.That(definitions.Any(definition => definition.Options.Count >= 2), Is.True);
            Assert.That(active, Is.Not.Null);
            Assert.That(PowerPlanService.GetPlans().Count(plan => plan.Active), Is.EqualTo(1));
            Assert.That(PowerPlanService.ReadValues(active!.Value), Has.Count.EqualTo(definitions.Count));
        });
    }

    [Test]
    public void ElevatedFallbackWritesBothValuesAndReappliesOnlyTheActivePlan()
    {
        var scheme = new Guid("11111111-2222-3333-4444-555555555555");
        var script = PowerPlanService.BuildElevatedWriteScript(scheme,
            [new PowerSettingValue(new Guid("54533251-82be-4824-96c1-47b60b740d00"), new Guid("893dee8e-2bef-41e0-89c6-b55d0929964c"), 100, 5)]);

        Assert.That(script, Does.Contain($"powercfg /setacvalueindex {scheme} 54533251-82be-4824-96c1-47b60b740d00 893dee8e-2bef-41e0-89c6-b55d0929964c 100"));
        Assert.That(script, Does.Contain("/setdcvalueindex").And.Contain(" 5\n"));
        Assert.That(script, Does.Contain($"-match '{scheme}'"));
    }
}
