using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using NUnit.Framework;
using TweakHub.Models;
using TweakHub.Services;
using TweakHub.Views;

namespace TweakHub.Tests;

/// <summary>Driver restore, Group Policy reset and risk badges.</summary>
public class PolicyRiskDriverTests
{
    [Test]
    public async Task DriverRestoreRefusesAFolderWithoutPackagesBeforeCallingPnputil()
    {
        var folder = Directory.CreateTempSubdirectory("TweakHub Drivers 'test'");
        try
        {
            var result = await PowerShellService.Instance.ExecuteScriptAsync(BuiltInScripts.DriverRestoreScript(folder.FullName));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error + result.Output, Does.Contain("No driver package"));
            Assert.That(result.Output, Does.Not.Contain("Installing"));
        }
        finally { folder.Delete(true); }
    }

    [TestCase(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", @"HKLM · Microsoft\Windows\WindowsUpdate")]
    [TestCase(@"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services", @"HKLM · Microsoft\Windows NT\Terminal Services")]
    [TestCase(@"HKEY_CURRENT_USER\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist", @"HKCU · Google\Chrome")]
    [TestCase(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge", @"HKLM · Microsoft\Edge")]
    public void PolicyAreasGroupByVendorAndWindowsComponent(string key, string category) =>
        Assert.That(PolicyService.CategoryOf(key), Is.EqualTo(category));

    [Test]
    public void OnlyValuesOutsideTweakHubAreResetAndBackupsCanBeUndone()
    {
        const string update = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU";
        const string chrome = @"HKEY_CURRENT_USER\SOFTWARE\Policies\Google\Chrome";
        var owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { RegistryService.BackupKey(update, "NoAutoUpdate") };
        var categories = PolicyService.Group(
            [(update, "NoAutoUpdate", "1"), (update, "AUOptions", "2"), (chrome, "HomepageLocation", "x")],
            owned, [(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry")]);

        var wu = categories.Single(category => category.Name.EndsWith("WindowsUpdate"));
        var telemetry = categories.Single(category => category.Name.EndsWith("DataCollection"));
        Assert.Multiple(() =>
        {
            Assert.That(wu.Values, Has.Count.EqualTo(2));
            Assert.That(wu.Resettable.Select(value => value.Name), Is.EqualTo(new[] { "AUOptions" }), "TweakHub's own value stays");
            Assert.That(wu.Preview, Does.Contain("[TweakHub]"));
            Assert.That(telemetry.Values, Is.Empty, "Deleted earlier: the area stays listed so it can be undone");
            Assert.That(telemetry.CanUndo && !telemetry.CanReset, Is.True);
            Assert.That(categories.Select(category => category.Name), Has.Exactly(3).Items);
        });
    }

    [Test]
    public void PolicyValuesAreReadFromTheWholeTree()
    {
        var path = $@"Software\TweakHubTests\{Guid.NewGuid():N}\Policies";
        using var root = Registry.CurrentUser.CreateSubKey(path);
        try
        {
            using (var vendor = root.CreateSubKey(@"Vendor\App\Deep"))
            {
                vendor.SetValue("Number", 5, RegistryValueKind.DWord);
                vendor.SetValue("List", new[] { "a", "b" }, RegistryValueKind.MultiString);
                vendor.SetValue("Blob", new byte[] { 1, 2 }, RegistryValueKind.Binary);
            }
            var values = new List<(string KeyPath, string Name, string Data)>();
            PolicyService.Read(root, "HKEY_CURRENT_USER\\" + path, values);

            Assert.That(values.Select(value => value.Data), Is.EquivalentTo(new[] { "5 (0x5)", "a; b", "0102" }));
            Assert.That(values.All(value => value.KeyPath.EndsWith(@"Vendor\App\Deep")), Is.True);
        }
        finally { Registry.CurrentUser.DeleteSubKeyTree(path.Replace(@"\Policies", string.Empty)); }
    }

    [Test]
    public void OnlyRealOrganizationEnrollmentsCountAsManaged()
    {
        Assert.Multiple(() =>
        {
            foreach (var builtIn in new[] { "Local Authority", "Deploy Authority", "Cloud Authority" })
                Assert.That(PolicyService.IsOrganizationEnrollment(builtIn, 1), Is.False, builtIn);
            Assert.That(PolicyService.IsOrganizationEnrollment("MS DM Server", 1), Is.True);
            Assert.That(PolicyService.IsOrganizationEnrollment("MS DM Server", 0), Is.False, "not completed");
            Assert.That(PolicyService.IsOrganizationEnrollment(null, 1), Is.False);
        });
    }

    [Test]
    public void RiskBadgesAndFiltersFollowTheRiskScale()
    {
        PerformanceTweak Tweak(string id, int risk) => new() { Id = id, RiskLevel = risk };
        var categories = new[]
        {
            new TweakCategory { Name = "A", Tweaks = [Tweak("disable_animations", 1), Tweak("one", 2), Tweak("three", 3)] },
            new TweakCategory { Name = "B", Tweaks = [Tweak("five", 5), Tweak("disable_sysmain", 3)] }
        };
        Assert.Multiple(() =>
        {
            Assert.That(categories.SelectMany(category => category.Tweaks).Select(tweak => tweak.RiskClass),
                Is.EqualTo(new[] { "Cosmetic", "Safe", "Moderate", "Caution", "Moderate" }));
            Assert.That(RegistryTweaksPage.FilterByRisk(categories, "Caution").Single().Name, Is.EqualTo("B"), "Empty categories are hidden");
            Assert.That(RegistryTweaksPage.FilterByRisk(categories, "Moderate").SelectMany(category => category.Tweaks).Count(), Is.EqualTo(2));
            Assert.That(RegistryTweaksPage.FilterByRisk(categories, "Recommended").SelectMany(category => category.Tweaks)
                .Select(tweak => tweak.Id), Does.Not.Contain("disable_sysmain"));
            Assert.That(RegistryTweaksPage.FilterByRisk(categories, "All").SelectMany(category => category.Tweaks).Count(), Is.EqualTo(5));
            Assert.That(RegistryTweaksPage.FilterByRisk(categories, "Safe").Single().Tweaks.Single(), Is.SameAs(categories[0].Tweaks[1]),
                "The filtered list shows the same objects, so toggles still work");
        });
    }

    [Test]
    public void CustomTweaksKeepTheUserRiskBadge()
    {
        CustomRegistryTweak Tweak(string? risk) => new()
        {
            Name = "Mine", RegistryPath = @"HKCU\Software\TweakHubTest", RegistryKey = "Value", ValueType = "REG_SZ", RiskClass = risk!
        };
        var caution = Tweak(" caution ");
        var legacy = JsonSerializer.Deserialize<CustomRegistryTweak>(
            """{"Id":"old-tweak","Name":"Old","RegistryPath":"HKCU\\Software\\TweakHubTest","RegistryKey":"Value","ValueType":"REG_SZ","Data":""}""")!;

        UserDataService.ValidateCustomTweak(caution);
        UserDataService.ValidateCustomTweak(legacy);
        var nulled = Tweak(null);
        UserDataService.ValidateCustomTweak(nulled);
        Assert.Multiple(() =>
        {
            Assert.That(caution.RiskClass, Is.EqualTo("Caution"));
            Assert.That(legacy.RiskClass, Is.EqualTo("Moderate"), "Files saved before the badge existed");
            Assert.That(nulled.RiskClass, Is.EqualTo("Moderate"));
            Assert.Throws<InvalidDataException>(() => UserDataService.ValidateCustomTweak(Tweak("Harmless")));
            Assert.That(JsonSerializer.Serialize(caution), Does.Contain("\"RiskClass\":\"Caution\"").And.Not.Contain("RiskText"));
        });
    }
}
