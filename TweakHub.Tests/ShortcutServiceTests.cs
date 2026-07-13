using NUnit.Framework;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Tests;

public class ShortcutServiceTests
{
    [Test]
    public void ExternalToolCatalogueIsSmallAndUsesOnlyExplicitWingetIdsOrHttpsUrls()
    {
        var service = ShortcutService.Instance;
        service.Initialize();
        var tools = service.ExternalTools;

        Assert.That(tools, Has.Count.LessThanOrEqualTo(25));
        Assert.That(tools.Select(tool => tool.Name), Is.Unique);
        Assert.That(tools.Where(tool => tool.WingetId.Length > 0).Select(tool => tool.WingetId), Is.Unique);
        Assert.That(tools, Has.All.Matches<ExternalTool>(tool =>
        {
            var hasWingetId = tool.WingetId.Length > 0;
            var hasHttpsUrl = Uri.TryCreate(tool.DownloadUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
            return hasWingetId ^ hasHttpsUrl;
        }));
    }
}
