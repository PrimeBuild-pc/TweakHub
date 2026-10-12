using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class TweakCatalogueTests
{
    [Test]
    public void CatalogueContainsOnlyApprovedTweaks()
    {
        var service = TweakService.Instance;
        service.LoadTweaks();
        var tweaks = service.TweakCategories.SelectMany(category => category.Tweaks).ToList();

        Assert.That(tweaks.Select(tweak => tweak.Id), Is.EquivalentTo(new[]
        {
            "cpu_priority_separation",
            "disable_cpu_throttling",
            "disable_core_parking",
            "high_performance_power_plan",
            "optimize_network_throttling",
            "optimize_network_for_gaming",
            "disable_mouse_acceleration",
            "disable_fullscreen_optimizations",
            "optimize_flip_model",
            "disable_game_bar",
            "reduce_menu_delay",
            "disable_startup_delay",
            "enable_taskbar_end_task",
            "disable_sysmain",
            "disable_prefetch",
            "disable_windows_ai_policies",
            "disable_wpbt",
            "prevent_device_metadata",
            "disable_device_coinstallers",
            "disable_notifications_calendar",
            "disable_advertising_id",
            "disable_tailored_experiences",
            "disable_activity_history",
            "disable_telemetry",
            "disable_consumer_content",
            "disable_bing_search",
            "disable_windows_search",
            "disable_fast_startup",
            "detailed_bsod",
            "disable_automatic_driver_updates",
            "disable_animations",
            "disable_transparency",
            "optimize_visual_effects"
        }));
        Assert.That(
            tweaks.Where(tweak => new[]
            {
                "cpu_priority_separation",
                "disable_cpu_throttling",
                "disable_core_parking",
                "high_performance_power_plan",
                "optimize_network_throttling",
                "optimize_network_for_gaming",
                "disable_sysmain",
                "disable_prefetch",
                "disable_windows_ai_policies",
                "disable_wpbt",
                "prevent_device_metadata",
                "disable_device_coinstallers",
                "disable_notifications_calendar",
                "disable_activity_history",
                "disable_telemetry",
                "disable_windows_search",
                "disable_automatic_driver_updates"
            }.Contains(tweak.Id)),
            Has.All.Property("RiskLevel").GreaterThanOrEqualTo(3));
    }

    [Test]
    public void CompositePrivacyTweaksContainEveryRequiredRegistryValue()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TweakService.GetCompositeRegistryChanges("disable_windows_ai_policies").Select(change => change.ValueName),
                Is.EquivalentTo(new[] { "SettingsPageVisibility", "DisableAIFeatures", "TurnOffWindowsCopilot", "DisableAIDataAnalysis", "AllowRecallEnablement", "DisableClickToDo" }));
            Assert.That(TweakService.GetCompositeRegistryChanges("disable_telemetry").Select(change => change.ValueName),
                Is.EquivalentTo(new[] { "AllowTelemetry", "DoNotShowFeedbackNotifications", "NumberOfSIUFInPeriod", "Start" }));
            Assert.That(TweakService.GetCompositeRegistryChanges("disable_bing_search").Select(change => change.ValueName),
                Is.EquivalentTo(new[] { "DisableSearchBoxSuggestions", "BingSearchEnabled", "IsDynamicSearchBoxEnabled" }));
            Assert.That(TweakService.GetCompositeRegistryChanges("disable_consumer_content").Select(change => change.ValueName),
                Does.Contain("DisableWindowsConsumerFeatures").And.Contain("SilentInstalledAppsEnabled"));
            Assert.That(TweakService.GetCompositeRegistryChanges("disable_device_coinstallers").Select(change => change.ValueName),
                Is.EquivalentTo(new[] { "SearchOrderConfig", "DisableCoInstallers" }));
            Assert.That(TweakService.GetCompositeRegistryChanges("disable_notifications_calendar").Select(change => change.ValueName),
                Is.EquivalentTo(new[] { "DisableNotificationCenter", "ToastEnabled" }));
            Assert.That(TweakService.GetCompositeRegistryChanges("disable_activity_history").Select(change => change.ValueName),
                Is.EquivalentTo(new[] { "EnableActivityFeed", "PublishUserActivities", "UploadUserActivities" }));
        });
    }

    [Test]
    public void RecommendedSetUsesSecurityUpdatesAndAllTweaksExceptTheDocumentedExceptions()
    {
        var service = TweakService.Instance;
        service.LoadTweaks();
        var tweaks = service.TweakCategories.SelectMany(category => category.Tweaks).ToList();

        Assert.That(TweakService.RecommendedWindowsUpdatePreset, Is.EqualTo(WindowsUpdatePreset.Security));
        var exceptions = new[] { "disable_sysmain", "disable_prefetch", "disable_fullscreen_optimizations", "optimize_network_for_gaming" };
        Assert.That(tweaks.Where(tweak => !exceptions.Contains(tweak.Id)),
            Has.All.Matches<TweakHub.Models.PerformanceTweak>(TweakService.IsRecommended));
        Assert.That(tweaks.Where(tweak => exceptions.Contains(tweak.Id)),
            Has.None.Matches<TweakHub.Models.PerformanceTweak>(TweakService.IsRecommended));
    }

    [TestCase(null, "SwapEffectUpgradeEnable=1;")]
    [TestCase("", "SwapEffectUpgradeEnable=1;")]
    [TestCase("VRROptimizeEnable=0;AutoHDREnable=1;", "VRROptimizeEnable=0;AutoHDREnable=1;SwapEffectUpgradeEnable=1;")]
    [TestCase("SwapEffectUpgradeEnable=0;VRROptimizeEnable=0;", "SwapEffectUpgradeEnable=1;VRROptimizeEnable=0;")]
    [TestCase("SwapEffectUpgradeEnable=1;AutoHDREnable=0;", "SwapEffectUpgradeEnable=1;AutoHDREnable=0;")]
    public void FlipModelKeepsTheOtherDirectXSettingsInPlace(string? current, string expected)
    {
        Assert.That(TweakService.WithDirectXSetting(current, "SwapEffectUpgradeEnable", "1"), Is.EqualTo(expected));
    }

    [Test]
    public void FlipModelTurnsMultiplaneOverlayBackOnAndKeepsTheCurrentDirectXString()
    {
        var changes = TweakService.GetCompositeRegistryChanges("optimize_flip_model");
        Assert.Multiple(() =>
        {
            Assert.That(changes.Single(change => change.ValueName == "OverlayTestMode").Value, Is.Null);
            Assert.That(changes.Single(change => change.ValueName == "HwSchMode").Value, Is.EqualTo(2));
            Assert.That((string)changes.Single(change => change.ValueName == "DirectXUserGlobalSettings").Value!,
                Does.Contain("SwapEffectUpgradeEnable=1;"));
        });
    }

    [Test]
    public void NetworkPresetOnlyTouchesKeywordsThePhysicalAdaptersAlreadyExpose()
    {
        var changes = TweakService.GetCompositeRegistryChanges("optimize_network_for_gaming");
        var adapters = changes.Where(change => change.ValueName != "DOPercentageMaxBackgroundBandwidth").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(changes.Single(change => change.ValueName == "DOPercentageMaxBackgroundBandwidth").Value, Is.EqualTo(30));
            Assert.That(adapters, Has.All.Matches<RegistryValueChange>(change =>
                change.KeyPath.Contains(@"Class\{4d36e972-e325-11ce-bfc1-08002be10318}\") && Equals(change.Value, "0")
                && TweakService.AdapterLatencyKeywords.Contains(change.ValueName)
                && RegistryService.Instance.GetRegistryValue(change.KeyPath, change.ValueName) is string));
        });
    }

    [Test]
    public async Task MissingServiceIsNotDetectedAsStopped()
    {
        Assert.That(await TweakService.IsServiceStoppedAsync("TweakHub-Service-That-Does-Not-Exist"), Is.False);
    }
}
