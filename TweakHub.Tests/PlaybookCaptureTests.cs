using System.IO;
using System.Text.Json;
using NUnit.Framework;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Tests;

public class PlaybookCaptureTests
{
    private const string Gpu = @"PCI\VEN_10DE&DEV_2684&SUBSYS_16F310DE&REV_A1\4&2B2F9F5A&0&0008";
    private static readonly string Msi = $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\{Gpu}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";
    private static readonly string Affinity = $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\{Gpu}\Device Parameters\Interrupt Management\Affinity Policy";
    private static readonly string Usb = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\USB\VID_046D&PID_C08B\6&1234&0&1\Device Parameters";

    [Test]
    public void RegistryStepsOnlyCarryTheDeviceValuesTweakHubWrites()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PlaybookService.IsAllowedRegistryValue(Msi, "MSISupported"), Is.True);
            Assert.That(PlaybookService.IsAllowedRegistryValue(Affinity, "AssignmentSetOverride"), Is.True);
            Assert.That(PlaybookService.IsAllowedRegistryValue(Usb, "SelectiveSuspendEnabled"), Is.True);
            Assert.That(PlaybookService.IsAllowedRegistryValue(Msi, "DevicePriority"), Is.False, "Value names are tied to their key");
            Assert.That(PlaybookService.IsAllowedRegistryValue(Usb + @"\..\..\Services", "Start"), Is.False);
            Assert.That(PlaybookService.IsAllowedRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "MSISupported"), Is.False);
            Assert.That(PlaybookService.DeviceIdOf(Affinity), Is.EqualTo(Gpu));
        });
    }

    [Test]
    public void ImportedRegistryStepsAreRejectedWhenTheyLeaveTheWhitelistOrTheirDevice()
    {
        static PlaybookStep Step(string referenceId, params PlaybookRegistryValue[] values) => new()
        {
            Type = PlaybookStepType.Registry, Name = "GPU", ReferenceId = referenceId, Data = JsonSerializer.Serialize(values)
        };

        Assert.DoesNotThrow(() => PlaybookService.ValidateStepData(Step(Gpu,
            new PlaybookRegistryValue(Msi, "MSISupported", "DWord", "1"),
            new PlaybookRegistryValue(Affinity, "AssignmentSetOverride", "Binary", Convert.ToBase64String(BitConverter.GetBytes(0b1100UL))))));
        Assert.Throws<InvalidDataException>(() => PlaybookService.ValidateStepData(Step(Gpu,
            new PlaybookRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Defender", "Start", "DWord", "4"))));
        Assert.Throws<InvalidDataException>(() => PlaybookService.ValidateStepData(Step(@"USB\OTHER\1", new PlaybookRegistryValue(Msi, "MSISupported", "DWord", "1"))),
            "Values must belong to the step's own device");
        Assert.Throws<InvalidDataException>(() => PlaybookService.ValidateStepData(Step(Gpu, new PlaybookRegistryValue(Msi, "MSISupported", "String", "1"))));
        Assert.Throws<InvalidDataException>(() => PlaybookService.ValidateStepData(Step(Gpu,
            new PlaybookRegistryValue(Affinity, "AssignmentSetOverride", "Binary", Convert.ToBase64String(new byte[64])))));
    }

    [Test]
    public void PowerPlanStepsNeedAGuidAndSettings()
    {
        var values = JsonSerializer.Serialize(new[] { new PowerSettingValue(Guid.NewGuid(), Guid.NewGuid(), 100, 5) });
        Assert.DoesNotThrow(() => PlaybookService.ValidateStepData(new PlaybookStep { Type = PlaybookStepType.PowerPlan, ReferenceId = Guid.NewGuid().ToString(), Data = values }));
        Assert.Throws<InvalidDataException>(() => PlaybookService.ValidateStepData(new PlaybookStep { Type = PlaybookStepType.PowerPlan, ReferenceId = "plan", Data = values }));
        Assert.Throws<InvalidDataException>(() => PlaybookService.ValidateStepData(new PlaybookStep { Type = PlaybookStepType.PowerPlan, ReferenceId = Guid.NewGuid().ToString(), Data = "[]" }));
    }

    [Test]
    public void DeviceToggleFallsBackToTheOnlyDeviceWithTheSameClassAndQuotedName()
    {
        var script = DeviceService.BuildMatchingToggleScript(@"PCI\VEN_1\1", "Realtek's Audio", "MEDIA", enable: false);
        Assert.That(script, Does.Contain("-InstanceId 'PCI\\VEN_1\\1'"));
        Assert.That(script, Does.Contain("FriendlyName -eq 'Realtek''s Audio'"), "Names are PowerShell literals");
        Assert.That(script, Does.Contain("$device.Count -ne 1) { 'SKIPPED'; exit 0 }"));
        Assert.That(script, Does.Contain("Disable-PnpDevice -InstanceId $device.InstanceId"));
    }

    [Test]
    public void ThirdPartyScriptsRunTheDocumentedCommandInAWindowThatStaysOpen()
    {
        var script = ThirdPartyScriptService.MariusHeier.Single(item => item.Id == "CpuDirect");
        var arguments = ThirdPartyScriptService.BuildArguments(script);
        Assert.That(arguments, Does.StartWith("-NoExit -NoProfile"));
        Assert.That(arguments, Does.Contain("irm https://tools.mariusheier.com/cpudirect.ps1 | iex"));
        Assert.That(ThirdPartyScriptService.MariusHeier.All(item => item.ScriptUrl.StartsWith("https://tools.mariusheier.com/")), Is.True);
    }
}
