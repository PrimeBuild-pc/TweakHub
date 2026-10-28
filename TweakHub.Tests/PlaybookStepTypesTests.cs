using System.IO;
using NUnit.Framework;
using TweakHub.Models;
using TweakHub.Services;
using TweakHub.Views;

namespace TweakHub.Tests;

/// <summary>The step types added for Tweak Check, Security, Windows Update, optional services, DNS and built-in scripts.</summary>
public class PlaybookStepTypesTests
{
    private static Playbook EverySection() => new()
    {
        Id = "new-pc",
        Name = "New PC",
        Steps =
        [
            new() { Id = "check", Type = PlaybookStepType.TweakCheck, Name = "MPO", ReferenceId = "Mpo" },
            new() { Id = "security", Type = PlaybookStepType.SecurityFix, Name = "Firewall", ReferenceId = "Firewall" },
            new() { Id = "update", Type = PlaybookStepType.WindowsUpdate, Name = "Security", ReferenceId = "Security" },
            new() { Id = "service", Type = PlaybookStepType.Service, Name = "MapsBroker", ReferenceId = "MapsBroker", Data = "4" },
            PlaybookService.DnsStep(["9.9.9.9", "149.112.112.112", "2620:fe::fe", "2620:fe::9"]),
            new() { Id = "runtimes", Type = PlaybookStepType.Script, Name = "Runtimes", ReferenceId = "builtin:gaming-runtimes" }
        ]
    };

    [Test]
    public void EverySectionSurvivesPlaybookExportAndImport()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubSections-{Guid.NewGuid():N}");
        var file = root + ".json";
        try
        {
            PlaybookService.Instance.Export(EverySection(), file, new UserDataService(Path.Combine(root, "source")));
            var destination = new UserDataService(Path.Combine(root, "destination"));
            var imported = PlaybookService.Instance.Import(file, destination).Playbook;

            Assert.That(imported.Steps.Select(step => (step.Type, step.ReferenceId, step.Data)), Is.EqualTo(EverySection().Steps.Select(step => (step.Type, step.ReferenceId, step.Data))));
            Assert.That(imported.Steps.Single(step => step.Type == PlaybookStepType.Dns).ReferenceId, Is.EqualTo("Quad9"), "Preset servers keep the preset name");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
            File.Delete(file);
        }
    }

    [TestCase(PlaybookStepType.TweakCheck, "NotACheck", "")]
    [TestCase(PlaybookStepType.SecurityFix, "BitLocker", "")]
    [TestCase(PlaybookStepType.WindowsUpdate, "Custom", "")]
    [TestCase(PlaybookStepType.Service, "SysMain", "4")]
    [TestCase(PlaybookStepType.Service, "MapsBroker", "2")]
    [TestCase(PlaybookStepType.Dns, "Custom", "1.1.1.1; Remove-Item C:\\")]
    [TestCase(PlaybookStepType.Script, "builtin:ctt-winutil", "")]
    public void ImportedStepsOutsideTheAllowedValuesAreRejected(PlaybookStepType type, string reference, string data)
    {
        var playbook = new Playbook { Id = "bad", Name = "Bad", Steps = [new() { Id = "step", Type = type, Name = "Step", ReferenceId = reference, Data = data }] };
        Assert.Throws<InvalidDataException>(() => UserDataService.ValidatePlaybook(playbook));
    }

    // The Import button only catches InvalidDataException: anything else reaches the crash handler, which closes TweakHub.
    [TestCase("""{"Playbook":{"Id":"p","Name":"P","Steps":[null]}}""")]
    [TestCase("""{"Playbook":{"Id":"p","Name":"P","Steps":[]},"CustomTweaks":[null]}""")]
    [TestCase("""{"Playbook":{"Id":"p","Name":"P","Steps":[]},"CustomScripts":[null]}""")]
    [TestCase("""{"Playbook":{"Id":"p","Name":"P","Steps":[]},"CustomTweaks":[{"Id":"t","Name":"T","RegistryPath":"HKCU\\Software\\T","RegistryKey":"V","ValueType":"REG_DWORD","Data":"abc"}]}""")]
    [TestCase("""{"Playbook":{"Id":"p","Name":"P","Steps":[]},"CustomTweaks":[{"Id":"t","Name":"T","RegistryPath":"HKCU\\Software\\T","RegistryKey":"V","ValueType":"REG_DWORD","Data":"99999999999"}]}""")]
    [TestCase("""{"Playbook":{"Id":"p","Name":"P","Steps":[]},"CustomTweaks":[{"Id":"t","Name":"T","RegistryPath":"NOPE\\T","RegistryKey":"V","ValueType":"REG_SZ","Data":"x"}]}""")]
    [TestCase("""{"Playbook":{"Id":"p","Name":"P","Steps":[{"Id":"s","Type":99,"Name":"S","ReferenceId":"x"}]}}""")]
    public void MalformedPlaybookPackagesAreRejectedAsInvalidData(string json)
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubBadPlaybook-{Guid.NewGuid():N}");
        var file = root + ".json";
        try
        {
            File.WriteAllText(file, json);
            var destination = new UserDataService(root);
            Assert.Throws<InvalidDataException>(() => PlaybookService.Instance.Import(file, destination));
            Assert.That(destination.LoadPlaybooks(), Is.Empty, "Nothing is saved from a rejected package");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
            File.Delete(file);
        }
    }

    [Test]
    public void RegistryStepValuesThatDoNotFitADwordAreInvalidData()
    {
        const string key = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\PCI\VEN_1\4&1\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";
        var step = new PlaybookStep
        {
            Type = PlaybookStepType.Registry, Name = "GPU", ReferenceId = @"PCI\VEN_1\4&1",
            Data = System.Text.Json.JsonSerializer.Serialize(new[] { new PlaybookRegistryValue(key, "MSISupported", "DWord", "99999999999") })
        };
        Assert.Throws<InvalidDataException>(() => PlaybookService.ValidateStepData(step));
    }

    [Test]
    public void ProfileWithoutAccentColorStillImports()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubAccent-{Guid.NewGuid():N}");
        var file = root + ".json";
        try
        {
            File.WriteAllText(file, """{"Version":2,"Appearance":{"Theme":"Dark","AccentColor":null,"Language":"System"}}""");
            Assert.That(new UserDataService(root).ImportProfile(file).Appearance.AccentColor, Is.Empty);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
            File.Delete(file);
        }
    }

    [Test]
    public void AllowListsMatchWhatTheServicesProduce()
    {
        Assert.That(TweakCheckService.Evaluate(_ => null, [], "Normal").Select(check => check.Id), Is.EqualTo(TweakCheckService.Ids));
        var failing = SecurityService.ParseSnapshot("""
            {"Defender":{"Mode":"Normal","RealTime":false,"SignatureAge":30},"Firewall":[0,1,1],"EnableLUA":0,"ConsentPrompt":0,
             "Smb1":1,"RdpDenied":0,"BitLocker":2,"SecureBoot":0,"LastUpdateDays":90,"MemoryIntegrity":0}
            """);
        Assert.That(failing.Checks.Where(check => check.CanFix).Select(check => check.Id), Is.EquivalentTo(SecurityService.FixableIds));
    }

    [Test]
    public void PlaybookBuiltInScriptsExistAndAreNamed()
    {
        foreach (var id in BuiltInScripts.PlaybookIds)
        {
            Assert.That(BuiltInScripts.Body(id), Is.Not.Empty, id);
            Assert.That(PlaybookService.BuiltInScriptName(id), Does.Not.Contain("_Name"), id);
        }
    }

    [Test]
    public void TweakCheckPreviewShowsWhatIsReadAndWhatRepairRuns()
    {
        const string dwm = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Dwm";
        var flagged = TweakCheckService.Evaluate(name => name.Key == dwm && name.Value == "OverlayTestMode" ? 5 : null, [], "Normal")
            .Single(check => check.Id == "Mpo");
        var timers = TweakCheckService.Evaluate(_ => null, ["useplatformclock"], "Normal").Single(check => check.Id == "BcdTimers");

        Assert.Multiple(() =>
        {
            Assert.That(flagged.Preview, Does.Contain($"reg query \"{dwm}\" /v \"OverlayTestMode\""));
            Assert.That(flagged.Preview, Does.Contain($"reg delete \"{dwm}\" /v \"OverlayTestMode\" /f"));
            Assert.That(timers.Preview, Does.Contain("bcdedit /enum {current}"));
            Assert.That(timers.Preview, Does.Contain("/deletevalue '{current}' useplatformclock"));
            Assert.That(TweakCheckService.Evaluate(_ => null, [], "Normal").Single(check => check.Id == "Mpo").Preview,
                Does.Not.Contain("reg delete"), "A check at the Windows default changes nothing");
        });
    }

    [Test]
    public void ImportReviewShowsOnlyTheCodeThisPcDoesNotHaveYet()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubReview-{Guid.NewGuid():N}");
        try
        {
            var data = new UserDataService(root);
            data.SaveCustomScripts([new CustomScript { Id = "known", Name = "Known", Content = "Write-Output known" }]);
            var package = new PlaybookPackage
            {
                Playbook = new Playbook { Id = "p", Name = "P" },
                CustomScripts =
                [
                    new CustomScript { Id = "known", Name = "Known", Content = "Write-Output known" },
                    new CustomScript { Id = "new", Name = "Cleanup", Content = "Remove-Item C:\\Temp\\x\nWrite-Output done", RequiresAdministrator = true }
                ],
                CustomTweaks = [new CustomRegistryTweak { Id = "t", Name = "Run key", RegistryPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run", RegistryKey = "x", Data = "evil.exe", ValueType = "REG_SZ" }]
            };
            var text = PlaybookService.DescribeNewCode(package, data);
            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("Cleanup").And.Contain(@"Remove-Item C:\Temp\x").And.Contain("Write-Output done"));
                Assert.That(text, Does.Contain(@"CurrentVersion\Run\x = evil.exe"));
                Assert.That(text, Does.Not.Contain("Write-Output known"), "scripts this PC already has are not imported");
                Assert.That(PlaybookService.DescribeNewCode(new PlaybookPackage { Playbook = package.Playbook }, data), Is.Empty);
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
