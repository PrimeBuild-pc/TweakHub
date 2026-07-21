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
            "disable_mouse_acceleration",
            "disable_fullscreen_optimizations",
            "disable_game_bar",
            "reduce_menu_delay",
            "disable_startup_delay",
            "disable_sysmain",
            "disable_prefetch",
            "disable_windows_ai_policies",
            "disable_wpbt",
            "prevent_device_metadata",
            "disable_device_coinstallers",
            "disable_notifications_calendar",
            "disable_windows_search",
            "disable_automatic_driver_updates",
            "windows_update_security_preset",
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
                "disable_sysmain",
                "disable_prefetch",
                "disable_windows_ai_policies",
                "disable_wpbt",
                "prevent_device_metadata",
                "disable_device_coinstallers",
                "disable_notifications_calendar",
                "disable_windows_search",
                "disable_automatic_driver_updates",
                "windows_update_security_preset"
            }.Contains(tweak.Id)),
            Has.All.Property("RiskLevel").GreaterThanOrEqualTo(3));
    }

    [Test]
    public void CompositePrivacyTweaksContainEveryRequiredRegistryValue()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TweakService.GetCompositeRegistryChanges("disable_windows_ai_policies").Select(change => change.ValueName),
                Is.EquivalentTo(new[] { "SettingsPageVisibility", "DisableAIFeatures" }));
            Assert.That(TweakService.GetCompositeRegistryChanges("disable_device_coinstallers").Select(change => change.ValueName),
                Is.EquivalentTo(new[] { "SearchOrderConfig", "DisableCoInstallers" }));
            Assert.That(TweakService.GetCompositeRegistryChanges("disable_notifications_calendar").Select(change => change.ValueName),
                Is.EquivalentTo(new[] { "DisableNotificationCenter", "ToastEnabled" }));
        });
    }

    [Test]
    public async Task MissingServiceIsNotDetectedAsStopped()
    {
        Assert.That(await TweakService.IsServiceStoppedAsync("TweakHub-Service-That-Does-Not-Exist"), Is.False);
    }

    [Test]
    public void MemoryAndPrivacyTweaksAreExcludedFromRecommendedPreset()
    {
        var service = TweakService.Instance;
        service.LoadTweaks();
        var protectedCategories = service.TweakCategories
            .Where(category => category.Name is "Memory Management" or "Privacy & Device Control")
            .SelectMany(category => category.Tweaks);

        Assert.That(protectedCategories, Has.All.Property("RiskLevel").GreaterThanOrEqualTo(3));
    }
}
