using NUnit.Framework;
using System.IO;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Tests;

public class UserDataServiceTests
{
    [Test]
    public void ProfileRoundTripIncludesCustomToolsFavoritesAndAppearance()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubProfile-{Guid.NewGuid():N}");
        var importedRoot = root + "-imported";
        var profilePath = root + ".tweakhub.json";
        try
        {
            var source = new UserDataService(root);
            var tool = new ExternalTool
            {
                Id = "custom-tool",
                Name = "My setup command",
                Category = "Deployment",
                PowerShellCommand = "winget install --id Microsoft.PowerToys --exact",
                IsCustom = true
            };
            source.SaveCustomTools([tool]);
            source.SaveFavoriteTools(["custom:custom-tool"]);
            source.SaveAppearance(new AppearanceSettings { Theme = "Dark", AccentColor = "#336699", Transparency = false });
            source.MarkRestartPending("restart-required");
            Assert.That(source.LoadPendingRestartIds(), Does.Contain("restart-required"));
            source.ExportProfile(profilePath);

            var destination = new UserDataService(importedRoot);
            var appearance = destination.ImportProfile(profilePath);
            Assert.Multiple(() =>
            {
                Assert.That(destination.LoadCustomTools().Single().PowerShellCommand, Does.Contain("winget install"));
                Assert.That(destination.LoadFavoriteTools(), Does.Contain("custom:custom-tool"));
                Assert.That(appearance.AccentColor, Is.EqualTo("#336699"));
                Assert.That(appearance.Transparency, Is.False);
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
