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

    // Windows PowerShell 5.1 splits native arguments with quotes or a trailing backslash: reg.exe failed on such text,
    // and with "C:\a b\" it lost /f and waited forever for an overwrite confirmation.
    [Test]
    public async Task GroupWritesKeepEveryTypeAndAwkwardTextAndRestoreIt()
    {
        var id = Guid.NewGuid().ToString("N");
        var subKey = $@"Software\TweakHubTests\{id}";
        var path = $@"HKCU\{subKey}";
        var dataPath = Path.Combine(Path.GetTempPath(), $"TweakHubTests-{id}");
        const string original = "\"C:\\Program Files\\App\\app.exe\" -tray";
        var changes = new[]
        {
            new RegistryValueChange(path, "Quoted", "\"C:\\Other App\\x.exe\" --flag \"a b\"", RegistryValueKind.String),
            new RegistryValueChange(path, "Trailing", @"C:\Path With Space\", RegistryValueKind.String),
            new RegistryValueChange(path, "Expand", @"%SystemRoot%\it's here", RegistryValueKind.ExpandString),
            new RegistryValueChange(path, "Multi", new[] { "one two", "th\"ree" }, RegistryValueKind.MultiString),
            new RegistryValueChange(path, "Blob", new byte[] { 0, 1, 254, 255 }, RegistryValueKind.Binary),
            new RegistryValueChange(path, "Big", 5_000_000_000L, RegistryValueKind.QWord),
            new RegistryValueChange(path, "AllBits", 0xFFFFFFFFu),
            new RegistryValueChange(path, "None", null) // REG_NONE, deleted like a policy reset
        };
        try
        {
            using (var key = Registry.CurrentUser.CreateSubKey(subKey))
            {
                key!.SetValue("Quoted", original, RegistryValueKind.String);
                key.SetValue("None", new byte[] { 7, 0, 9 }, RegistryValueKind.None);
            }
            var service = new RegistryService(dataPath);
            service.Initialize();

            var apply = service.ApplyValuesWithBackupAsync(changes);
            Assert.That(await Task.WhenAny(apply, Task.Delay(TimeSpan.FromSeconds(60))), Is.SameAs(apply), "the group write hung");
            Assert.That(await apply, Is.True);
            using (var written = Registry.CurrentUser.OpenSubKey(subKey))
            {
                Assert.That(written!.GetValue("Quoted"), Is.EqualTo("\"C:\\Other App\\x.exe\" --flag \"a b\""));
                Assert.That(written.GetValue("Trailing"), Is.EqualTo(@"C:\Path With Space\"));
                Assert.That(written.GetValue("Expand", null, RegistryValueOptions.DoNotExpandEnvironmentNames), Is.EqualTo(@"%SystemRoot%\it's here"));
                Assert.That(written.GetValueKind("Expand"), Is.EqualTo(RegistryValueKind.ExpandString));
                Assert.That(written.GetValue("Multi"), Is.EqualTo(new[] { "one two", "th\"ree" }));
                Assert.That(written.GetValue("Blob"), Is.EqualTo(new byte[] { 0, 1, 254, 255 }));
                Assert.That(written.GetValue("Big"), Is.EqualTo(5_000_000_000L));
                Assert.That(written.GetValue("AllBits"), Is.EqualTo(-1));
            }

            Assert.That(await service.RestoreValuesAsync(changes), Is.True);
            using var restored = Registry.CurrentUser.OpenSubKey(subKey);
            Assert.That(restored!.GetValue("Quoted"), Is.EqualTo(original));
            Assert.That(restored.GetValue("None"), Is.EqualTo(new byte[] { 7, 0, 9 }));
            Assert.That(restored.GetValueNames(), Is.EquivalentTo(new[] { "Quoted", "None" }));
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
    public void UserPoliciesNeedElevationAndScriptsWriteThisUsersHive()
    {
        var sid = System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value;
        Assert.Multiple(() =>
        {
            Assert.That(RegistryService.RequiresElevation(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Search"), Is.False);
            Assert.That(RegistryService.RequiresElevation(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer"), Is.True,
                "Users can only read their own policy key");
            Assert.That(RegistryService.RequiresElevation(@"HKLM\SOFTWARE\Policies\Microsoft"), Is.True);
            Assert.That(RegistryService.ForScript(@"HKCU\Software\Policies\Microsoft"), Is.EqualTo($@"HKEY_USERS\{sid}\Software\Policies\Microsoft"));
            Assert.That(RegistryService.ForScript(@"HKEY_CURRENT_USER\Control Panel\Mouse"), Is.EqualTo($@"HKEY_USERS\{sid}\Control Panel\Mouse"));
            Assert.That(RegistryService.ForScript(@"HKLM\SOFTWARE\Microsoft"), Is.EqualTo(@"HKLM\SOFTWARE\Microsoft"));
        });
    }

    [Test]
    public void InvalidRootIsRejectedInsteadOfFallingBackToHkcu()
    {
        var service = new RegistryService(Path.Combine(Path.GetTempPath(), $"TweakHubTests-{Guid.NewGuid():N}"));
        Assert.That(service.SetRegistryValue(@"HKTYPO\Software\TweakHub", "Value", 1), Is.False);
        Assert.Throws<ArgumentException>(() => RegistryService.ValidateLocation(@"HKTYPO\Software\TweakHub", "Value"));
    }
}
