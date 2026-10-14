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

    // A value the script sets without a backup stays changed after Restore (PreventDeviceMetadataFromNetwork did).
    [Test]
    public void EveryValueThePresetsSetIsBackedUpForRestore()
    {
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Scripts", "WindowsUpdatePreset.ps1"));
        var set = System.Text.RegularExpressions.Regex.Matches(script, @"Set-Policy \$\w+ '(\w+)'").Select(match => match.Groups[1].Value).Distinct().ToList();
        Assert.That(set, Is.Not.Empty);
        Assert.That(WindowsUpdatePresetService.PresetRegistryValues.Select(value => value.ValueName), Is.SupersetOf(set));
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

    // New-Item -Force recreates an existing key: every preset kept only the last value it set in each key.
    [Test]
    public void SetPolicyKeepsTheValuesAlreadyInTheKey()
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "WindowsUpdatePreset.ps1");
        var key = $@"HKCU:\Software\TweakHubTests\{Guid.NewGuid():N}";
        var command = $$"""
            $ast = [System.Management.Automation.Language.Parser]::ParseFile('{{scriptPath.Replace("'", "''")}}', [ref]$null, [ref]$null)
            Invoke-Expression $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Set-Policy' }, $true).Extent.Text
            Set-Policy '{{key}}' 'First' 1
            Set-Policy '{{key}}' 'Second' 2
            (Get-Item '{{key}}').GetValueNames() -join ','
            Remove-Item '{{key}}' -Recurse -Force
            """;
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(command)),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        })!;
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        Assert.That(output, Is.EqualTo("First,Second"));
    }
}
