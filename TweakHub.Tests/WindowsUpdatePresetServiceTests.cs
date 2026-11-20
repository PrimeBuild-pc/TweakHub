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
            Assert.That(script, Does.Contain("Set-Policy $au 'AUOptions' 2"), "security updates wait for the user");
            Assert.That(script, Does.Contain("Set-Policy $au 'NoAutoRebootWithLoggedOnUsers' 1"));
            Assert.That(script, Does.Not.Contain("Get-UpdateTasks | Enable-ScheduledTask"), "tasks Windows ships disabled stay disabled");
            Assert.That(script, Does.Contain("'SetUpdateNotificationLevel', 'UpdateNotificationLevel'"), "notification blocks are cleared");
            Assert.That(script, Does.Not.Contain("Set-Policy $wu 'DeferQualityUpdates'"));
            Assert.That(script, Does.Not.Contain("Set-Policy $wu 'DeferQualityUpdatesPeriodInDays'"));
            Assert.That(script, Does.Match("Save-Backup\\s+Clear-ManagedPolicies"));
            Assert.That(script, Does.Contain("Restore-Backup"));
            Assert.That(script, Does.Not.Contain("Invoke-WebRequest").And.Not.Contain("Invoke-RestMethod"));
        });
    }

    [TestCase("Core", true)]
    [TestCase("CoreN", true)]
    [TestCase("CoreSingleLanguage", true)]
    [TestCase("CoreCountrySpecific", true)]
    [TestCase("Professional", false)]
    [TestCase("ProfessionalWorkstation", false)]
    [TestCase("Enterprise", false)]
    [TestCase("ServerStandardCore", false)]
    [TestCase(null, false)]
    public void HomeEditionIsRecognizedFromEditionId(string? editionId, bool isHome) =>
        Assert.That(WindowsUpdatePresetService.IsHomeEditionId(editionId), Is.EqualTo(isHome));

    [Test]
    public async Task LegacyBackupIsKeptWithoutRunningThePreset()
    {
        var directory = Directory.CreateTempSubdirectory("TweakHubUpdateTests-");
        try
        {
            var legacyPath = Path.Combine(directory.FullName, "windows-update-backup.json");
            const string oldBackup = """{"Services":[],"Tasks":[]}""";
            File.WriteAllText(legacyPath, oldBackup);
            var service = new WindowsUpdatePresetService(Path.Combine(directory.FullName, "missing.ps1"), legacyPath, () => false);

            var apply = await service.ApplyAsync(WindowsUpdatePreset.Default);
            var restore = await service.RestoreAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.HasBackup, Is.True);
                Assert.That(service.HasCompleteBackup, Is.False);
                Assert.That(apply.Success, Is.False);
                Assert.That(restore.Success, Is.False);
                Assert.That(restore.Error, Does.Contain("windows-update-backup.json"));
                Assert.That(File.ReadAllText(legacyPath), Is.EqualTo(oldBackup));
            });
        }
        finally
        {
            File.Delete(Path.Combine(directory.FullName, "windows-update-backup.json"));
            directory.Delete();
        }
    }

    [Test]
    public async Task ProtectedSnapshotIsValidatedBeforeChangesAndKeptWhenRestoreFails()
    {
        var scriptPath = PowerShellService.Literal(Path.Combine(AppContext.BaseDirectory, "Scripts", "WindowsUpdatePreset.ps1"));
        var result = await RunPowerShellAsync($$"""
            $ErrorActionPreference = 'Stop'
            $ast = [System.Management.Automation.Language.Parser]::ParseFile({{scriptPath}}, [ref]$null, [ref]$null)
            $assignments = $ast.FindAll({
                param($node)
                $node -is [System.Management.Automation.Language.AssignmentStatementAst] -and
                $node.Left.Extent.Text -in '$serviceNames', '$taskPaths', '$backupName', '$backupKey'
            }, $true)
            foreach ($assignment in $assignments) { Invoke-Expression $assignment.Extent.Text }
            foreach ($name in 'Test-UpdateTaskPath', 'Assert-UpdateBackup', 'Save-Backup', 'Restore-Backup') {
                $definition = $ast.Find({
                    param($node)
                    $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
                }, $true)
                Invoke-Expression $definition.Extent.Text
            }

            $key = [pscustomobject]@{ Snapshot = $null }
            $key | Add-Member ScriptMethod GetValue { param($name) $this.Snapshot }
            $key | Add-Member ScriptMethod SetValue { param($name, $data, $kind) $this.Snapshot = $data }
            $key | Add-Member ScriptMethod DeleteValue { param($name) $this.Snapshot = $null }
            $key | Add-Member ScriptMethod Dispose {}
            $machine = [pscustomobject]@{ Key = $key }
            $machine | Add-Member ScriptMethod CreateSubKey { param($path) $this.Key }
            $machine | Add-Member ScriptMethod OpenSubKey { param($path, $writable) $this.Key }

            $service = [pscustomobject]@{ StartMode = 'Manual'; State = 'Stopped' }
            $task = [pscustomobject]@{
                TaskPath = '\Microsoft\Windows\WindowsUpdate\'; TaskName = 'Fixture'
                Settings = [pscustomobject]@{ Enabled = $true }
            }
            function Get-UpdateServices { @{ BITS = $service } }
            function Get-UpdateTasks { $task }
            function Set-Service {
                param($Name, $StartupType)
                if ($rejectWrite) { throw 'Simulated service refusal' }
                $service.StartMode = $StartupType
            }
            function Enable-ScheduledTask {
                param([Parameter(ValueFromPipeline)] $Task)
                $Task.Settings.Enabled = $true
            }

            $null = Save-Backup
            $original = $key.Snapshot
            $service.StartMode = 'Disabled'
            $task.Settings.Enabled = $false
            $null = Save-Backup
            if ($key.Snapshot -ne $original) { throw 'The original snapshot changed.' }

            $invalid = ConvertFrom-Json $original
            $invalid.Tasks[0].TaskPath = '\Fixture\'
            $key.Snapshot = ConvertTo-Json $invalid -Depth 4
            try { Restore-Backup; throw 'An unexpected task path was accepted.' }
            catch { if ($_.Exception.Message -ne 'Invalid scheduled task in the Windows Update backup.') { throw } }
            if ($service.StartMode -ne 'Disabled') { throw 'Restore wrote before validating the whole snapshot.' }

            $key.Snapshot = $original
            $rejectWrite = $true
            try { Restore-Backup; throw 'A failed write was reported as successful.' }
            catch { if ($_.Exception.Message -ne 'Simulated service refusal') { throw } }
            if ($key.Snapshot -ne $original) { throw 'Restore removed the backup after a failed write.' }

            $rejectWrite = $false
            Restore-Backup
            if ($null -ne $key.Snapshot -or $service.StartMode -ne 'Manual' -or -not $task.Settings.Enabled) {
                throw 'Restore did not finish the round trip.'
            }
            """);
        Assert.That(result.ExitCode, Is.Zero, result.Error + result.Output);
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
    public async Task SetPolicyKeepsTheValuesAlreadyInTheKey()
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
        var result = await RunPowerShellAsync(command);
        Assert.That(result.ExitCode, Is.Zero, result.Error);
        Assert.That(result.Output.Trim(), Is.EqualTo("First,Second"));
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunPowerShellAsync(string command)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = SystemTools.PowerShell,
            Arguments = "-NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(command)),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        })!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(timeout.Token);
        return (process.ExitCode, await output, await error);
    }
}
