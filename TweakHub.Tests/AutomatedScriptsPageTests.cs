using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using TweakHub.Models;
using TweakHub.Services;
using TweakHub.Views;

namespace TweakHub.Tests;

public class AutomatedScriptsPageTests
{
    [Test]
    public void BuiltInScriptsContainRequestedCommands()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BuiltInScripts.Body("ctt-winutil"),
                Is.EqualTo("irm christitus.com/win | iex"));
            Assert.That(BuiltInScripts.Body("gaming-runtimes"),
                Does.Contain("Microsoft.VCRedist.2015+.x64").And.Contain("Microsoft.DirectX")
                    .And.Contain("CreativeTechnology.OpenAL").And.Contain("Nvidia.PhysX").And.Contain("Microsoft.XNARedist"));
            Assert.That(BuiltInScripts.Body("dotnet-developer-setup"),
                Does.Contain("Microsoft.DotNet.SDK.10").And.Contain("Microsoft.VisualStudio.2022.Community")
                    .And.Contain("Microsoft.VisualStudio.Workload.ManagedDesktop").And.Contain("Git.Git"));
            Assert.That(BuiltInScripts.Body("change-local-password"),
                Does.Contain("Get-LocalUser").And.Contain("net.exe user").And.Contain("Read-Host"));
            var interactive = AutomatedScriptsPage.CreateInteractivePowerShellStartInfo("Write-Output ok", true);
            Assert.That(interactive.UseShellExecute, Is.True);
            Assert.That(interactive.Verb, Is.EqualTo("runas"));
            Assert.That(interactive.WindowStyle, Is.EqualTo(ProcessWindowStyle.Normal));
            Assert.That(interactive.Arguments, Does.Not.Contain("-WindowStyle Hidden").And.Not.Contain("-NonInteractive"));
            Assert.That(BuiltInScripts.Body("dism-sfc-chkdsk"),
                Does.Contain("RestoreHealth").And.Contain("sfc.exe").And.Contain("chkdsk.exe"));
            Assert.That(BuiltInScripts.Body("component-cleanup"),
                Does.Contain("StartComponentCleanup"));
            Assert.That(BuiltInScripts.Body("temp-cleanup"),
                Does.Contain("ReparsePoint").And.Contain("AddDays(-1)").And.Contain("Delete-DeliveryOptimizationCache")
                    .And.StartWith($"$userTemp = '{Path.GetTempPath()}'"), "The elevated script cleans the caller's temp folder");
            Assert.That(BuiltInScripts.Body("network-reset"),
                Does.Contain("winsock reset").And.Contain("int ip reset"));
            Assert.That(BuiltInScripts.Body("windows-update-reset"),
                Does.Contain("SoftwareDistribution").And.Contain("catroot2"));
            Assert.That(BuiltInScripts.Body("winget"),
                Does.Contain("Repair-WinGetPackageManager").And.Contain("VERIFIED:"));
            Assert.That(BuiltInScripts.Body("microsoft-store-repair"),
                Does.Contain("Reset-AppxPackage").And.Contain("VERIFIED:").And.Contain("Source:"));
            Assert.That(BuiltInScripts.Body("gaming-services-repair"),
                Does.Contain("https://aka.ms/GamingRepairTool").And.Contain("Get-AuthenticodeSignature")
                    .And.Contain("VERIFIED:").And.Contain("Source:"));
            Assert.That(BuiltInScripts.Body("print-spooler-repair"),
                Does.Contain("Spooler").And.Contain("PRINTERS").And.Contain("VERIFIED:"));
            Assert.That(BuiltInScripts.Body("onedrive-repair"),
                Does.Contain("/reset").And.Contain("VERIFIED:"));
            Assert.That(BuiltInScripts.Body("wsl-setup"),
                Does.Contain("VirtualMachinePlatform").And.Contain("Ubuntu").And.Contain("VERIFIED:"));
            Assert.That(BuiltInScripts.Body("empty-standby-list"),
                Does.Contain("RAMMap").And.Contain("-Et"));
            Assert.That(BuiltInScripts.Body("adobe-hosts-block"),
                Does.Contain("TweakHub Adobe block START"));
            Assert.That(BuiltInScripts.Body("adobe-hosts-unblock"),
                Does.Contain("TweakHub Adobe block START"));
            Assert.That(BuiltInScripts.Body("shader-cache-cleanup"),
                Does.Contain("D3DSCache").And.Contain(@"AMD\DxcCache").And.Contain(@"NVIDIA\DXCache").And.Contain("ReparsePoint")
                    .And.Not.Contain("Prefetch").And.Not.Contain("SoftwareDistribution"));
            Assert.That(BuiltInScripts.Body("driver-backup"),
                Does.StartWith($"$destination = '{BuiltInScripts.DriverBackupPath}'")
                    .And.Contain("/export-driver *").And.Contain("VERIFIED:"), "The backup goes to the caller's data folder");
        });
    }

    [Test]
    public async Task NewMaintenanceScriptsParseWithoutExecutingThem()
    {
        foreach (var id in new[] { "shader-cache-cleanup", "driver-backup", "driver-restore" })
        {
            var parseOnly = "$tokens = $null; $errors = $null; [System.Management.Automation.Language.Parser]::ParseInput("
                + PowerShellService.Literal(BuiltInScripts.Body(id)) + ", [ref]$tokens, [ref]$errors) | Out-Null; if ($errors.Count) { $errors | Out-String | Write-Error; exit 1 }";
            var result = await PowerShellService.Instance.ExecuteScriptAsync(parseOnly);
            Assert.That(result.Success, Is.True, $"{id}: {result.Error}");
        }
    }

    [Test]
    public void OnlyFullyRevertedTweaksTheUserAppliedAreOfferedForReapply()
    {
        PerformanceTweak Tweak(bool enabled, bool partial = false, string unavailable = "") =>
            new() { IsEnabled = enabled, IsPartiallyApplied = partial, UnavailableReason = unavailable };
        Assert.Multiple(() =>
        {
            Assert.That(TweakService.IsReverted(Tweak(false), true, _ => true), Is.True, "applied by the user, now off");
            Assert.That(TweakService.IsReverted(Tweak(false), false, _ => true), Is.False, "backup from the Backup button only");
            Assert.That(TweakService.IsReverted(Tweak(true), true, _ => true), Is.False, "still in effect");
            Assert.That(TweakService.IsReverted(Tweak(false), true, _ => false), Is.False, "already restored");
            Assert.That(TweakService.IsReverted(Tweak(false, partial: true), true, _ => true), Is.False, "half applied keeps its own warning");
            Assert.That(TweakService.IsReverted(Tweak(false, unavailable: "no adapter"), true, _ => true), Is.False, "cannot be reapplied here");
        });
    }

    [Test]
    public void AppliedMarkerIsAddedAndRemovedPerTweak()
    {
        var original = TweakService.AppliedTweaksPath;
        var directory = Path.Combine(Path.GetTempPath(), $"TweakHubTests-{Guid.NewGuid():N}");
        TweakService.AppliedTweaksPath = Path.Combine(directory, "applied-tweaks.json");
        try
        {
            TweakService.MarkApplied("a", true);
            TweakService.MarkApplied("b", true);
            TweakService.MarkApplied("a", false);
            Assert.That(File.ReadAllText(TweakService.AppliedTweaksPath), Does.Contain("\"b\"").And.Not.Contain("\"a\""));
        }
        finally
        {
            TweakService.AppliedTweaksPath = original;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public async Task CustomScriptsReceivePortablePathsAndMissingExecutablesFail()
    {
        var context = PowerShellService.BuildScriptContext("Write-Output ok");
        Assert.That(context, Does.Contain("TWEAKHUB_ROOT").And.Contain("TWEAKHUB_APPS").And.Contain("Set-Location"));

        var missingName = $"missing-{Guid.NewGuid():N}.exe";
        var result = await PowerShellService.Instance.ExecuteCustomScriptAsync(new CustomScript
        {
            Name = "Missing portable app",
            Content = $"& (Join-Path $env:TWEAKHUB_APPS '{missingName}')"
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(string.Concat(result.Error.Where(character => !char.IsWhiteSpace(character))), Does.Contain(missingName));
        });
    }

    [Test]
    public async Task PlaybookExecutionStopsOnFirstFailureAndPreservesOrder()
    {
        var steps = new[]
        {
            new PlaybookStep { Id = "one", Name = "One" },
            new PlaybookStep { Id = "two", Name = "Two" },
            new PlaybookStep { Id = "three", Name = "Three" }
        };
        var visited = new List<string>();
        var completed = await PlaybookService.RunSequentiallyAsync(steps, step =>
        {
            visited.Add(step.Id);
            return Task.FromResult(step.Id != "two");
        });
        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(visited, Is.EqualTo(new[] { "one", "two" }));
        });
    }

    [Test]
    public void PlaybookPreflightFlagsMissingReferencesInvalidWingetAndEmptySteps()
    {
        var broken = PlaybookService.Instance.Preflight(new Playbook
        {
            Name = "Broken",
            Steps =
            [
                new() { Type = PlaybookStepType.Tweak, ReferenceId = "builtin:not-a-real-tweak", Name = "Missing tweak" },
                new() { Type = PlaybookStepType.Script, ReferenceId = "not-on-this-pc", Name = "Missing script" },
                new() { Type = PlaybookStepType.Winget, WingetId = "not a winget id", Name = "Bad winget" }
            ]
        });
        Assert.Multiple(() =>
        {
            Assert.That(broken.CanRun, Is.False);
            Assert.That(broken.Errors, Has.Count.EqualTo(3));
        });

        var empty = PlaybookService.Instance.Preflight(new Playbook { Name = "Empty" });
        Assert.Multiple(() =>
        {
            Assert.That(empty.CanRun, Is.False);
            Assert.That(empty.Errors, Has.Count.EqualTo(1));
        });

        var valid = PlaybookService.Instance.Preflight(new Playbook
        {
            Name = "Valid",
            Steps =
            [
                new() { Type = PlaybookStepType.Tweak, ReferenceId = "builtin:cpu_priority_separation", Name = "CPU priority" },
                new() { Type = PlaybookStepType.Winget, WingetId = "Microsoft.PowerToys", Name = "PowerToys" }
            ]
        });
        Assert.Multiple(() =>
        {
            Assert.That(valid.CanRun, Is.True);
            Assert.That(valid.Errors, Is.Empty);
            Assert.That(valid.Lines, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void StructurallyValidPlaybookPreservesUnavailableReferences()
    {
        var playbook = new Playbook
        {
            Id = "portable",
            Name = "Portable setup",
            Steps = [new() { Id = "missing", Type = PlaybookStepType.Script, Name = "Optional app", ReferenceId = "not-on-this-pc" }]
        };
        Assert.DoesNotThrow(() => UserDataService.ValidatePlaybook(playbook));
    }

    [Test]
    public void BuiltInPowerShellParses()
    {
        foreach (var id in new[]
        {
            "ctt-winutil", "gaming-runtimes", "dotnet-developer-setup", "change-local-password",
            "dism-sfc-chkdsk", "component-cleanup", "network-reset", "windows-update-reset",
            "winget", "microsoft-store-repair", "gaming-services-repair", "print-spooler-repair",
            "onedrive-repair", "wsl-setup", "empty-standby-list", "remove-windows-ai",
            "adobe-hosts-block", "adobe-hosts-unblock"
        })
        {
            var path = Path.Combine(Path.GetTempPath(), $"TweakHub-{Guid.NewGuid():N}.ps1");
            try
            {
                File.WriteAllText(path, BuiltInScripts.Body(id));
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
