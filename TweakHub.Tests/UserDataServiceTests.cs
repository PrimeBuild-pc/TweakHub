using NUnit.Framework;
using System.IO;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Tests;

public class UserDataServiceTests
{
    [Test]
    public void ProfileV2RoundTripIncludesEveryTransferableCollection()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubProfile-{Guid.NewGuid():N}");
        var importedRoot = root + "-imported";
        var profilePath = root + ".tweakhub.json";
        try
        {
            var source = new UserDataService(root);
            var script = new CustomScript { Id = "script-1", Name = "Launch portable tool", Content = "& $env:TWEAKHUB_APPS\\Tool\\Tool.exe" };
            var tool = new ExternalTool
            {
                Id = "custom-tool",
                Name = "My setup command",
                Category = "Deployment",
                PowerShellCommand = "winget install --id Microsoft.PowerToys --exact",
                IsCustom = true
            };
            source.SaveCustomScripts([script]);
            source.SaveCustomTools([tool]);
            source.SaveCustomTweaks([new CustomRegistryTweak
            {
                Id = "registry-tool",
                Name = "Documented tweak",
                Description = "Why this custom value exists",
                RegistryPath = @"HKCU\Software\TweakHub",
                RegistryKey = "Example",
                Data = "1"
            }]);
            source.SavePlaybooks([new Playbook
            {
                Id = "setup",
                Name = "My setup",
                Steps =
                [
                    new() { Id = "app", Type = PlaybookStepType.Winget, Name = "PowerToys", WingetId = "Microsoft.PowerToys" },
                    new() { Id = "script", Type = PlaybookStepType.Script, Name = script.Name, ReferenceId = script.Id }
                ]
            }]);
            source.SaveFavoriteTools(["custom:custom-tool"]);
            source.SaveFavoriteTweaks(["builtin:disable_game_bar", "custom:registry-tool"]);
            source.SaveFavoriteScripts(["builtin:dism-sfc-chkdsk", "custom:script-1"]);
            source.SaveAppearance(new AppearanceSettings { Theme = "Dark", AccentColor = "#336699", Transparency = false, Language = "it" });
            source.ExportProfile(profilePath);

            var destination = new UserDataService(importedRoot);
            var result = destination.ImportProfile(profilePath);
            Assert.Multiple(() =>
            {
                Assert.That(destination.LoadCustomScripts().Single().Content, Does.Contain("TWEAKHUB_APPS"));
                Assert.That(destination.LoadCustomTools().Single().PowerShellCommand, Does.Contain("winget install"));
                Assert.That(destination.LoadCustomTweaks().Single().Description, Is.EqualTo("Why this custom value exists"));
                Assert.That(destination.LoadPlaybooks().Single().Steps.Single(step => step.Type == PlaybookStepType.Winget).WingetId,
                    Is.EqualTo("Microsoft.PowerToys"));
                Assert.That(destination.LoadFavoriteTools(), Does.Contain("custom:custom-tool"));
                Assert.That(destination.LoadFavoriteTweaks(), Is.EquivalentTo(new[] { "builtin:disable_game_bar", "custom:registry-tool" }));
                Assert.That(destination.LoadFavoriteScripts(), Is.EquivalentTo(new[] { "builtin:dism-sfc-chkdsk", "custom:script-1" }));
                Assert.That(result.Appearance.AccentColor, Is.EqualTo("#336699"));
                Assert.That(result.Appearance.Transparency, Is.False);
                Assert.That(result.Appearance.Language, Is.EqualTo("it"));
                Assert.That(File.Exists(result.RecoveryPath), Is.True);
            });
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
            if (Directory.Exists(importedRoot)) Directory.Delete(importedRoot, true);
            File.Delete(profilePath);
        }
    }

    [Test]
    public void VersionOneProfileMigratesWithEmptyPlaybooks()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubProfileV1-{Guid.NewGuid():N}");
        var profile = root + ".json";
        try
        {
            File.WriteAllText(profile, """
                {"Version":1,"CustomScripts":[{"Id":"legacy","Name":"Legacy","Language":0,"Content":"Write-Output ok","RequiresAdministrator":false}],"Appearance":{"Theme":"System","AccentColor":"","Transparency":true,"Language":"System"}}
                """);
            var service = new UserDataService(root);
            service.ImportProfile(profile);
            Assert.Multiple(() =>
            {
                Assert.That(service.LoadCustomScripts().Single().Id, Is.EqualTo("legacy"));
                Assert.That(service.LoadPlaybooks(), Is.Empty);
            });
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
            File.Delete(profile);
        }
    }

    [Test]
    public void InvalidProfileDoesNotReplaceExistingData()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubInvalidProfile-{Guid.NewGuid():N}");
        var profile = root + ".json";
        try
        {
            var service = new UserDataService(root);
            service.SaveCustomScripts([new CustomScript { Id = "keep", Name = "Keep", Content = "Write-Output keep" }]);
            File.WriteAllText(profile, """
                {"Version":2,"CustomScripts":[{"Id":"duplicate","Name":"One","Language":0,"Content":"1"},{"Id":"duplicate","Name":"Two","Language":0,"Content":"2"}],"Appearance":{"Theme":"System","AccentColor":"","Transparency":true,"Language":"System"}}
                """);
            Assert.Throws<InvalidDataException>(() => service.ImportProfile(profile));
            Assert.That(service.LoadCustomScripts().Single().Id, Is.EqualTo("keep"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
            File.Delete(profile);
        }
    }

    [Test]
    public void CustomToolRejectsMultipleActions()
    {
        var tool = new ExternalTool
        {
            Name = "Unsafe ambiguity",
            Category = "Custom",
            WingetId = "Microsoft.PowerToys",
            PowerShellCommand = "Write-Host test"
        };
        Assert.Throws<InvalidDataException>(() => UserDataService.ValidateCustomTool(tool));
    }
}
