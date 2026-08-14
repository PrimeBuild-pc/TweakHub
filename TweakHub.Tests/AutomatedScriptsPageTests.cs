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
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("ctt-winutil"),
                Is.EqualTo("irm christitus.com/win | iex"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("gaming-runtimes"),
                Does.Contain("Microsoft.VCRedist.2015+.x64").And.Contain("Microsoft.DirectX")
                    .And.Contain("CreativeTechnology.OpenAL").And.Contain("Nvidia.PhysX").And.Contain("Microsoft.XNARedist"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("dotnet-developer-setup"),
                Does.Contain("Microsoft.DotNet.SDK.10").And.Contain("Microsoft.VisualStudio.2022.Community")
                    .And.Contain("Microsoft.VisualStudio.Workload.ManagedDesktop").And.Contain("Git.Git"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("change-local-password"),
                Does.Contain("Get-LocalUser").And.Contain("net.exe user").And.Contain("Read-Host"));
            var interactive = AutomatedScriptsPage.CreateInteractivePowerShellStartInfo("Write-Output ok", true);
            Assert.That(interactive.UseShellExecute, Is.True);
            Assert.That(interactive.Verb, Is.EqualTo("runas"));
            Assert.That(interactive.WindowStyle, Is.EqualTo(ProcessWindowStyle.Normal));
            Assert.That(interactive.Arguments, Does.Not.Contain("-WindowStyle Hidden").And.Not.Contain("-NonInteractive"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("dism-sfc-chkdsk"),
                Does.Contain("RestoreHealth").And.Contain("sfc.exe").And.Contain("chkdsk.exe"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("component-cleanup"),
                Does.Contain("StartComponentCleanup"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("network-reset"),
                Does.Contain("winsock reset").And.Contain("int ip reset"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("windows-update-reset"),
                Does.Contain("SoftwareDistribution").And.Contain("catroot2"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("winget"),
                Does.Contain("Repair-WinGetPackageManager").And.Contain("VERIFIED:"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("microsoft-store-repair"),
                Does.Contain("Reset-AppxPackage").And.Contain("VERIFIED:").And.Contain("Source:"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("gaming-services-repair"),
                Does.Contain("https://aka.ms/GamingRepairTool").And.Contain("Get-AuthenticodeSignature")
                    .And.Contain("VERIFIED:").And.Contain("Source:"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("print-spooler-repair"),
                Does.Contain("Spooler").And.Contain("PRINTERS").And.Contain("VERIFIED:"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("onedrive-repair"),
                Does.Contain("/reset").And.Contain("VERIFIED:"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("wsl-setup"),
                Does.Contain("VirtualMachinePlatform").And.Contain("Ubuntu").And.Contain("VERIFIED:"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("empty-standby-list"),
                Does.Contain("RAMMap").And.Contain("-Et"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("adobe-hosts-block"),
                Does.Contain("TweakHub Adobe block START"));
            Assert.That(AutomatedScriptsPage.GetBuiltInScript("adobe-hosts-unblock"),
                Does.Contain("TweakHub Adobe block START"));
        });
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
