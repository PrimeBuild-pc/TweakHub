using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class ToolDownloadServiceTests
{
    [TestCase("Downloading 42%", 42)]
    [TestCase("100% complete", 100)]
    [TestCase("250%", 100)]
    [TestCase("No progress", -1)]
    public void ParsesWingetProgress(string output, int expected) =>
        Assert.That(ToolDownloadService.TryParsePercent(output), Is.EqualTo(expected));

    [Test]
    public void ParseExportCollectsPackageIdsFromEverySource()
    {
        const string json = """
            {"Sources":[
              {"Packages":[{"PackageIdentifier":"CPUID.CPU-Z"},{"PackageIdentifier":"Microsoft.PowerToys"}]},
              {"Packages":[{"PackageIdentifier":"9NBLGGH4NNS1"}]},
              {"SourceDetails":{}}]}
            """;
        var ids = ToolDownloadService.ParseExport(json);
        Assert.That(ids, Is.EquivalentTo(new[] { "CPUID.CPU-Z", "Microsoft.PowerToys", "9NBLGGH4NNS1" }));
        Assert.That(ids.Contains("cpuid.cpu-z"), Is.True);
        Assert.That(ToolDownloadService.ParseExport("{}"), Is.Empty);
    }

    [Test]
    public void ToolPresetsOnlyNameCataloguedWingetTools()
    {
        var catalogue = ShortcutService.Instance.ExternalTools.Select(tool => tool.WingetId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (nameKey, ids) in TweakHub.Views.ExternalToolsPage.Presets)
        {
            Assert.That(ids, Is.Not.Empty, nameKey);
            Assert.That(ids.Where(id => !catalogue.Contains(id)), Is.Empty, $"{nameKey} names ids missing from the catalogue");
        }
    }
}
