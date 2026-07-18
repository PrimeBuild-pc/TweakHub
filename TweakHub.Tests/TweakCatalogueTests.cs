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
                "disable_windows_search",
                "disable_automatic_driver_updates",
                "windows_update_security_preset"
            }.Contains(tweak.Id)),
            Has.All.Property("RiskLevel").GreaterThanOrEqualTo(3));
    }
}
