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
            "limit_background_updates",
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
    public void FlipModelLeavesMultiplaneOverlayToTweakCheckAndKeepsTheCurrentDirectXString()
    {
        var changes = TweakService.GetCompositeRegistryChanges("optimize_flip_model");
        Assert.Multiple(() =>
        {
            Assert.That(changes.Select(change => change.ValueName), Does.Not.Contain("OverlayTestMode"));
            Assert.That(changes.Single(change => change.ValueName == "HwSchMode").Value, Is.EqualTo(2));
            Assert.That((string)changes.Single(change => change.ValueName == "DirectXUserGlobalSettings").Value!,
                Does.Contain("SwapEffectUpgradeEnable=1;"));
        });
    }

    [TestCase("8086", false, "*InterruptModeration")]
    [TestCase("8086", true, "MIMOPowerSaveMode")]
    [TestCase("10EC", false, "EnableGreenEthernet")]
    [TestCase("0BDA", false, "EnableGreenEthernet")]
    public void NetworkPresetHasDocumentedSettingsForIntelAndRealtek(string vendor, bool wireless, string keyword)
    {
        Assert.That(NetworkAdapterService.SettingsFor(vendor, wireless).Select(setting => setting.Keyword), Does.Contain(keyword));
    }

    [TestCase("10EC", true)]
    [TestCase("0BDA", true)]
    [TestCase("1969", false)]
    [TestCase("1D6A", false)]
    [TestCase("14E4", false)]
    [TestCase("", false)]
    public void NetworkPresetLeavesUndocumentedAdaptersAlone(string vendor, bool wireless)
    {
        Assert.That(NetworkAdapterService.SettingsFor(vendor, wireless), Is.Empty);
    }

    [TestCase(@"PCI\VEN_8086&DEV_125C", "8086")]
    [TestCase(@"PCI\VEN_10ec&DEV_8125&SUBSYS_E0001458", "10EC")]
    [TestCase(@"USB\VID_0BDA&PID_8156", "0BDA")]
    [TestCase(@"ROOT\NET\0000", "")]
    [TestCase(null, "")]
    public void NetworkAdapterVendorComesFromTheHardwareId(string? hardwareId, string expected)
    {
        Assert.That(NetworkAdapterService.VendorId(hardwareId), Is.EqualTo(expected));
    }

    [Test]
    public void NetworkPresetDefaultsToTheConnectedSupportedAdapters()
    {
        RegistryValueChange[] change = [new(@"HKEY_LOCAL_MACHINE\x", "*EEE", "0")];
        var wired = new NetworkAdapter("{1}", "Ethernet", "I226-V", "Intel", false, true, change);
        var wifi = new NetworkAdapter("{2}", "Wi-Fi", "AC 3168", "Intel", true, false, change);
        var unknown = new NetworkAdapter("{3}", "Ethernet 2", "Other", "14E4", false, true, []);
        Assert.Multiple(() =>
        {
            Assert.That(NetworkAdapterService.DefaultSelection([wired, wifi, unknown]), Is.EqualTo(new[] { wired }));
            Assert.That(NetworkAdapterService.DefaultSelection([wifi, unknown]), Is.EqualTo(new[] { wifi }));
            Assert.That(NetworkAdapterService.DefaultSelection([unknown]), Is.Empty);
        });
    }

    [Test]
    public void NetworkPresetOnlyChangesManagedKeywordsOfInstalledAdapters()
    {
        var adapters = NetworkAdapterService.GetAdapters();
        Assert.That(adapters.SelectMany(adapter => adapter.Changes), Has.All.Matches<RegistryValueChange>(change =>
            change.KeyPath.StartsWith(NetworkAdapterService.ClassKey + @"\") && NetworkAdapterService.ManagedKeywords.Contains(change.ValueName)
            && RegistryService.Instance.GetRegistryValue(change.KeyPath, change.ValueName) is string));
        Assert.That(TweakService.GetCompositeRegistryChanges("optimize_network_for_gaming"), Is.Empty);
    }

    [Test]
    public async Task MissingServiceIsNotDetectedAsStopped()
    {
        Assert.That(await TweakService.IsServiceStoppedAsync("TweakHub-Service-That-Does-Not-Exist"), Is.False);
    }
}
