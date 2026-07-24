using NUnit.Framework;
using System.IO;
using System.Text.Json;
using TweakHub.Services;

namespace TweakHub.Tests;

public class RestartVerificationServiceTests
{
    [Test]
    public async Task PendingOperationIsVerifiedOnlyAfterBootChangesAndResultCanBeAcknowledged()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubRestart-{Guid.NewGuid():N}");
        var statePath = Path.Combine(root, "pending.json");
        var boot = DateTimeOffset.UtcNow.AddHours(-1);
        try
        {
            var service = new RestartVerificationService(statePath, () => boot);
            TweakService.Instance.LoadTweaks();
            var tweak = TweakService.Instance.TweakCategories.SelectMany(category => category.Tweaks)
                .First(item => item.Id == "disable_game_bar");
            tweak.IsEnabled = false;
            service.TrackTweak(tweak.Id, true);

            Assert.That(service.CurrentBootPendingTweakIds(), Does.Contain(tweak.Id));
            Assert.That(await service.VerifyAfterRebootAsync(), Is.Empty);

            boot = boot.AddHours(2);
            var results = await service.VerifyAfterRebootAsync();
            Assert.Multiple(() =>
            {
                Assert.That(results.Single().Status, Is.EqualTo(RestartVerificationStatus.Failed));
                Assert.That(service.CurrentBootPendingTweakIds(), Is.Empty);
            });

            service.AcknowledgeResults();
            Assert.That(service.Results, Is.Empty);

            service.TrackTweak(tweak.Id, false);
            boot = boot.AddHours(2);
            Assert.That((await service.VerifyAfterRebootAsync()).Single().Status,
                Is.EqualTo(RestartVerificationStatus.Verified));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task CompositePartialStateIsReported()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubRestartPartial-{Guid.NewGuid():N}");
        var boot = DateTimeOffset.UtcNow.AddHours(-1);
        try
        {
            var service = new RestartVerificationService(Path.Combine(root, "pending.json"), () => boot);
            TweakService.Instance.LoadTweaks();
            var tweak = TweakService.Instance.TweakCategories.SelectMany(category => category.Tweaks)
                .First(item => item.Id == "disable_activity_history");
            service.TrackTweak(tweak.Id, true);
            tweak.IsPartiallyApplied = true;
            boot = boot.AddHours(2);

            Assert.That((await service.VerifyAfterRebootAsync()).Single().Status,
                Is.EqualTo(RestartVerificationStatus.Partial));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task RemovedCatalogueItemIsReportedUnavailable()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubRestartMissing-{Guid.NewGuid():N}");
        var boot = DateTimeOffset.UtcNow.AddHours(-1);
        try
        {
            var service = new RestartVerificationService(Path.Combine(root, "pending.json"), () => boot);
            TweakService.Instance.LoadTweaks();
            service.TrackTweak("removed-tweak", true);
            boot = boot.AddHours(2);
            Assert.That((await service.VerifyAfterRebootAsync()).Single().Status,
                Is.EqualTo(RestartVerificationStatus.Unavailable));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Test]
    public void LegacyPortableStateMigratesOnlyOnSameBoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubRestartLegacy-{Guid.NewGuid():N}");
        var legacy = Path.Combine(root, "legacy.json");
        var boot = DateTimeOffset.UtcNow.AddHours(-1);
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(legacy, JsonSerializer.Serialize(new
            {
                BootTimeUtc = boot,
                TweakIds = new[] { "disable_game_bar" }
            }));
            TweakService.Instance.LoadTweaks();
            var tweaks = TweakService.Instance.TweakCategories.SelectMany(category => category.Tweaks).ToList();
            tweaks.First(item => item.Id == "disable_game_bar").IsEnabled = true;
            var service = new RestartVerificationService(Path.Combine(root, "machine.json"), () => boot, legacy);

            service.MigrateLegacyCurrentBoot(tweaks);
            Assert.Multiple(() =>
            {
                Assert.That(service.CurrentBootPendingTweakIds(), Does.Contain("disable_game_bar"));
                Assert.That(File.Exists(legacy), Is.False);
            });
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
