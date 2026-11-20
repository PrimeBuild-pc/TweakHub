using System.IO;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

// Run explicitly with -p:StorePackage=true --filter FullyQualifiedName~StorePackageTests.
public class StorePackageTests
{
    [Test]
    public void StoreDataAndDownloadedToolsAreOutsideTheImmutablePayload()
    {
        Assert.That(StoreDistribution.IsStoreBuild, Is.True);
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TweakHub", "Store");
        Assert.Multiple(() =>
        {
            Assert.That(AppDataPath.RootPath, Is.EqualTo(expected));
            Assert.That(AppDataPath.BasePath, Is.EqualTo(Path.Combine(expected, "Data")));
            Assert.That(AppDataPath.AppsPath, Is.EqualTo(Path.Combine(expected, "Apps")));
            Assert.That(AppDataPath.MachinePath, Is.EqualTo(Path.Combine(expected, "Machine")));
            Assert.That(AppDataPath.IsPortable, Is.False);
            Assert.That(AppDataPath.RootPath, Is.Not.EqualTo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)));
        });
    }

    [Test]
    public async Task AutomaticStoreUpdateCheckReturnsWithoutOpeningUiOrDownloadingAnExe()
    {
        Assert.That(StoreDistribution.IsStoreBuild, Is.True);
        // Null owner and cancelled token are safe: Store exits before either UI or HTTP is used.
        await UpdateService.Instance.CheckAndPromptAsync(null!, false, new CancellationToken(true));
    }
}
