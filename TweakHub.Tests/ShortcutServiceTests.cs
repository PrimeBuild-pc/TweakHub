using NUnit.Framework;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Tests;

public class ShortcutServiceTests
{
    [Test]
    public void ExternalToolCatalogueUsesOnlyCuratedWingetIdsOrHttpsPages()
    {
        var service = ShortcutService.Instance;
        service.Initialize();
        var tools = service.ExternalTools.Where(tool => !tool.IsCustom).ToList();
        var categories = new[]
        {
            "System Utilities", "CPU & Memory", "Firmware & Power", "Monitoring & Diagnostics",
            "GPU & Display", "Gaming & Input", "Storage & USB", "Network", "Audio",
            "Benchmarks & Stability", "AI Tools"
        };

        Assert.That(tools, Has.Count.InRange(90, 125));
        Assert.That(tools.Select(tool => tool.Name), Is.Unique);
        Assert.That(tools.Select(tool => tool.Category).Distinct(), Is.SubsetOf(categories));
        Assert.That(tools.Where(tool => tool.WingetId.Length > 0).Select(tool => tool.WingetId), Is.Unique);
        Assert.That(tools, Has.All.Matches<ExternalTool>(tool =>
        {
            var hasWingetId = tool.WingetId.Length > 0;
            var hasHttpsUrl = Uri.TryCreate(tool.DownloadUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
            var safePage = !tool.DownloadUrl.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)
                           && !tool.DownloadUrl.Contains("get.activated.win", StringComparison.OrdinalIgnoreCase);
            return hasWingetId ^ hasHttpsUrl && safePage;
        }));
    }
}
