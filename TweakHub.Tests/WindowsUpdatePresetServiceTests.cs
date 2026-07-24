using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class WindowsUpdatePresetServiceTests
{
    [Test]
    public void SecurityDefersFeaturesButNotQualityOrSecurityUpdates()
    {
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Scripts", "WindowsUpdatePreset.ps1"));
        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("Set-Policy $wu 'DeferFeatureUpdatesPeriodInDays' 365"));
            Assert.That(script, Does.Not.Contain("Set-Policy $wu 'DeferQualityUpdates'"));
            Assert.That(script, Does.Not.Contain("Set-Policy $wu 'DeferQualityUpdatesPeriodInDays'"));
            Assert.That(script, Does.Match("Save-Backup\\s+Clear-ManagedPolicies"));
            Assert.That(script, Does.Contain("Restore-Backup"));
            Assert.That(script, Does.Not.Contain("Invoke-WebRequest").And.Not.Contain("Invoke-RestMethod"));
        });
    }

    [Test]
    public void BundledPresetScriptParses()
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "WindowsUpdatePreset.ps1");
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -Command \"$tokens=$null; $errors=$null; [System.Management.Automation.Language.Parser]::ParseFile('{scriptPath.Replace("'", "''")}', [ref]$tokens, [ref]$errors) | Out-Null; if ($errors.Count) {{ $errors | Out-String | Write-Error; exit 1 }}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        })!;
        process.WaitForExit();
        Assert.That(process.ExitCode, Is.Zero);
    }
}
