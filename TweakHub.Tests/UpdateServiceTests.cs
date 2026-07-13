using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class UpdateServiceTests
{
    [Test]
    public void VersionComparisonHandlesStableAndNumericPrereleases()
    {
        Assert.Multiple(() =>
        {
            Assert.That(UpdateService.IsNewerVersion("0.2.0-beta2", "0.2.0-beta10"), Is.True);
            Assert.That(UpdateService.IsNewerVersion("0.2.0-beta", "0.2.0"), Is.True);
            Assert.That(UpdateService.IsNewerVersion("0.2.0", "0.2.0-beta1"), Is.False);
            Assert.That(UpdateService.IsNewerVersion("0.2.0", "0.3.0"), Is.True);
            Assert.That(UpdateService.IsNewerVersion("1.0.0", "1.0.0"), Is.False);
        });
    }
}
