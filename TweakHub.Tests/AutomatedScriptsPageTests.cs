using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using TweakHub.Views;

namespace TweakHub.Tests;

public class AutomatedScriptsPageTests
{
    [Test]
    public void BuiltInScriptsContainRequestedCommands()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("ctt-winutil"),
                Is.EqualTo("irm christitus.com/win | iex"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("dism-sfc-chkdsk"),
                Does.Contain("RestoreHealth").And.Contain("sfc.exe").And.Contain("chkdsk.exe"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("component-cleanup"),
                Does.Contain("StartComponentCleanup"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("network-reset"),
                Does.Contain("winsock reset").And.Contain("int ip reset"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("windows-update-reset"),
                Does.Contain("SoftwareDistribution").And.Contain("catroot2"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("prevent-device-metadata"),
                Does.Contain("PreventDeviceMetadataFromNetwork").And.Contain("gpupdate.exe"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("exclude-wu-drivers"),
                Does.Contain("ExcludeWUDriversInQualityUpdate").And.Contain("gpupdate.exe"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("empty-standby-list"),
                Does.Contain("RAMMap").And.Contain("-Et"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("adobe-hosts-block"),
                Does.Contain("TweakHub Adobe block START"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("adobe-hosts-unblock"),
                Does.Contain("TweakHub Adobe block START"));
        });
    }

    [Test]
    public void BuiltInPowerShellParses()
    {
        foreach (var id in new[]
        {
            "ctt-winutil", "dism-sfc-chkdsk", "component-cleanup", "network-reset", "windows-update-reset",
            "prevent-device-metadata", "exclude-wu-drivers", "empty-standby-list", "remove-windows-ai",
            "adobe-hosts-block", "adobe-hosts-unblock"
        })
        {
            var path = Path.Combine(Path.GetTempPath(), $"TweakHub-{Guid.NewGuid():N}.ps1");
            try
            {
                File.WriteAllText(path, AutomatedScriptsPage.GetBuiltInScript(id));
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -Command \"$tokens=$null; $errors=$null; [System.Management.Automation.Language.Parser]::ParseFile('{path.Replace("'", "''")}', [ref]$tokens, [ref]$errors) | Out-Null; if ($errors.Count) {{ $errors | Out-String | Write-Error; exit 1 }}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                })!;
                process.WaitForExit();
                Assert.That(process.ExitCode, Is.Zero, $"PowerShell syntax error in {id}");
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
