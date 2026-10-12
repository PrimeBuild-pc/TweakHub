using Microsoft.Win32;
using NUnit.Framework;
using System.IO;
using TweakHub.Services;

namespace TweakHub.Tests;

public class RegistryServiceTests
{
    [Test]
    public void BackupSurvivesRestartAndRestoresExistingAndMissingValues()
    {
        var id = Guid.NewGuid().ToString("N");
        var subKey = $@"Software\TweakHubTests\{id}";
        var path = $@"HKCU\{subKey}";
        var dataPath = Path.Combine(Path.GetTempPath(), $"TweakHubTests-{id}");

        try
        {
            using (var key = Registry.CurrentUser.CreateSubKey(subKey))
                key!.SetValue("Existing", 7, RegistryValueKind.DWord);

            var service = new RegistryService(dataPath);
            service.Initialize();
            Assert.That(service.ApplyValueWithBackup(path, "Existing", 9, RegistryValueKind.DWord), Is.True);
            Assert.That(service.ApplyValueWithBackup(path, "Missing", "temporary", RegistryValueKind.String), Is.True);

            var restarted = new RegistryService(dataPath);
            restarted.Initialize();
            Assert.That(restarted.RestoreRegistryValue(path, "Existing"), Is.True);
            Assert.That(restarted.RestoreRegistryValue(path, "Missing"), Is.True);

            using var restored = Registry.CurrentUser.OpenSubKey(subKey);
            Assert.That(restored!.GetValue("Existing"), Is.EqualTo(7));
            Assert.That(restored.GetValueNames(), Does.Not.Contain("Missing"));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(subKey, false);
            if (Directory.Exists(dataPath)) Directory.Delete(dataPath, true);
        }
    }

    [Test]
    public async Task GroupedValuesUseOneOperationAndRestoreTogether()
    {
        var id = Guid.NewGuid().ToString("N");
        var subKey = $@"Software\TweakHubTests\{id}";
        var path = $@"HKCU\{subKey}";
        var dataPath = Path.Combine(Path.GetTempPath(), $"TweakHubTests-{id}");
        var changes = new[]
        {
            new RegistryValueChange(path, "First", 10),
            new RegistryValueChange(path, "Second", 20)
        };
        try
        {
            using (var key = Registry.CurrentUser.CreateSubKey(subKey))
                key!.SetValue("First", 1, RegistryValueKind.DWord);
            var service = new RegistryService(dataPath);
            service.Initialize();
            Assert.That(await service.ApplyValuesWithBackupAsync(changes), Is.True);
            Assert.That(await service.RestoreValuesAsync(changes), Is.True);
            using var restored = Registry.CurrentUser.OpenSubKey(subKey);
            Assert.That(restored!.GetValue("First"), Is.EqualTo(1));
            Assert.That(restored.GetValueNames(), Does.Not.Contain("Second"));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(subKey, false);
            if (Directory.Exists(dataPath)) Directory.Delete(dataPath, true);
        }
    }

    // HKCU\ and HKEY_CURRENT_USER\ are one value: two backups would remember two different originals.
    // Restoring values that never changed (UAC declined on apply) must succeed and clear the backups.
    [Test]
    public async Task ShortAndLongRootsShareOneBackupAndUnchangedValuesRestoreCleanly()
    {
        var id = Guid.NewGuid().ToString("N");
        var subKey = $@"Software\TweakHubTests\{id}";
        var dataPath = Path.Combine(Path.GetTempPath(), $"TweakHubTests-{id}");
        try
        {
            using (var key = Registry.CurrentUser.CreateSubKey(subKey))
                key!.SetValue("Existing", 7, RegistryValueKind.DWord);
            var service = new RegistryService(dataPath);
            service.Initialize();
            service.CreateBackupValues([new($@"HKCU\{subKey}", "Existing", 9), new($@"HKCU\{subKey}", "Missing", 1)]);
            Assert.That(service.CreateBackupValues([new($@"HKEY_CURRENT_USER\{subKey}", "Existing", 9)]), Is.Zero);
            Assert.That(service.HasBackup($@"HKEY_CURRENT_USER\{subKey}", "Missing"), Is.True);

            Assert.That(await service.RestoreValuesAsync([new($@"HKEY_CURRENT_USER\{subKey}", "Existing", null), new($@"HKCU\{subKey}", "Missing", null)]), Is.True);
            Assert.That(service.BackupCount, Is.Zero);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(subKey, false);
            if (Directory.Exists(dataPath)) Directory.Delete(dataPath, true);
        }
    }

    [Test]
    public void InvalidRootIsRejectedInsteadOfFallingBackToHkcu()
    {
        var service = new RegistryService(Path.Combine(Path.GetTempPath(), $"TweakHubTests-{Guid.NewGuid():N}"));
        Assert.That(service.SetRegistryValue(@"HKTYPO\Software\TweakHub", "Value", 1), Is.False);
        Assert.Throws<ArgumentException>(() => RegistryService.ValidateLocation(@"HKTYPO\Software\TweakHub", "Value"));
    }
}
