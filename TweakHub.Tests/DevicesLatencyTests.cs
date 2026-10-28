using System.IO;
using Microsoft.Win32;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class DevicesLatencyTests
{
    private static PnpDeviceInfo Device(string id, string name, string cls, string parent, string service = "", string busName = "") =>
        new(id, name, cls, "OK", true, 0, parent, service, "") { BusName = busName };

    [Test]
    public void DevicesGetCategoriesChildrenRulesAndRisk()
    {
        const string json = """
            [{"Id":"ROOT\\KDNIC\\0000","Name":"Microsoft Kernel Debug Network Adapter","Class":"Net","Status":"OK","Present":true,"Problem":0,"Parent":"HTREE\\ROOT\\0"},
             {"Id":"ACPI\\PNP0103\\0","Name":"High precision event timer","Class":"System","Status":"OK","Present":true,"Problem":0,"Parent":"ACPI_HAL\\PNP0C08\\0"},
             {"Id":"ROOT\\UMBUS\\0000","Name":"UMBus Root Bus Enumerator","Class":"System","Status":"OK","Present":true,"Problem":0,"Parent":"HTREE\\ROOT\\0"},
             {"Id":"ACPI\\PNP0800\\0","Name":"System speaker","Class":"System","Status":"Error","Present":true,"Problem":22,"Parent":"ACPI\\PNP0A08\\0"},
             {"Id":"USB\\VID_046D&PID_C08B\\X","Name":"G502","Class":"USB","Status":"Unknown","Present":false,"Problem":45,"Parent":"USB\\ROOT_HUB30\\4"},
             {"Id":"PCI\\VEN_10DE&DEV_2684\\4","Name":"NVIDIA GeForce RTX 4090","Class":"Display","Status":"Error","Present":true,"Problem":43,"Parent":"PCI\\VEN_1022\\3"},
             {"Id":"HDAUDIO\\FUNC_01\\1","Name":"Speakers","Class":"MEDIA","Status":"OK","Present":true,"Problem":0,"Parent":"ROOT\\UMBUS\\0000"}]
            """;
        var devices = DeviceService.ParseDevices(json, ["ACPI\\PNP0800\\0"]).ToDictionary(device => device.Id);

        Assert.Multiple(() =>
        {
            Assert.That(devices[@"ROOT\KDNIC\0000"].Rule?.Advice, Is.EqualTo(DeviceAdvice.Common));
            Assert.That(devices[@"ROOT\KDNIC\0000"].HighRisk, Is.False, "Common rules override the ROOT\\ risk heuristic");
            Assert.That(devices[@"ACPI\PNP0103\0"].Rule?.Advice, Is.EqualTo(DeviceAdvice.Avoid));
            Assert.That(devices[@"ACPI\PNP0103\0"].HighRisk, Is.True);
            Assert.That(devices[@"ROOT\UMBUS\0000"].Children, Is.EqualTo(1), "Children come from parent links");
            Assert.That(devices[@"ROOT\UMBUS\0000"].Category, Is.EqualTo(DeviceCategory.Healthy), "A bus with a child is not idle");
            Assert.That(devices[@"ACPI\PNP0800\0"].Category, Is.EqualTo(DeviceCategory.Disabled));
            Assert.That(devices[@"ACPI\PNP0800\0"].DisabledByTweakHub, Is.True);
            Assert.That(devices[@"USB\VID_046D&PID_C08B\X"].Category, Is.EqualTo(DeviceCategory.NotPresent));
            Assert.That(devices[@"PCI\VEN_10DE&DEV_2684\4"].Category, Is.EqualTo(DeviceCategory.Problem));
            Assert.That(devices[@"PCI\VEN_10DE&DEV_2684\4"].Rule, Is.Null);
        });
    }

    [Test]
    public async Task InventoryUsesDevicePropertiesWhenCimFieldsAreMissing()
    {
        const string mocks = """
            function Get-PnpDevice { [pscustomobject]@{ InstanceId = 'USB\TEST\1'; FriendlyName = 'Hub'; Class = 'USB'; Status = 'Error' } }
            function Get-PnpDeviceProperty {
                param([Parameter(ValueFromPipeline=$true)]$Device, $KeyName)
                process {
                    [pscustomobject]@{ InstanceId = $Device.InstanceId; KeyName = 'DEVPKEY_Device_IsPresent'; Data = $true }
                    [pscustomobject]@{ InstanceId = $Device.InstanceId; KeyName = 'DEVPKEY_Device_ProblemCode'; Data = 22 }
                }
            }
            """;
        var result = await PowerShellService.Instance.ExecuteScriptAsync(mocks + "\n" + DeviceService.InventoryScript);
        Assert.That(result.Success, Is.True, result.Error);
        var device = DeviceService.ParseDevices(result.Output, []).Single();
        Assert.Multiple(() =>
        {
            Assert.That(device.Present, Is.True);
            Assert.That(device.Problem, Is.EqualTo(22));
            Assert.That(device.Category, Is.EqualTo(DeviceCategory.Disabled));
        });
    }

    [Test]
    public void AbsentDevicesAreNotSuggestedAndHubsHaveExplicitSafetyAdvice()
    {
        var devices = DeviceService.ParseDevices("""
            [{"Id":"ROOT\\KDNIC\\old","Name":"Kernel Debug Network Adapter","Present":false,"Problem":45},
             {"Id":"USB\\VID_174C&PID_2074\\1","Name":"Hub USB generico","Class":"USB","Service":"USBHUB3","Present":true,"Status":"OK"}]
            """, []);
        var absent = devices.Single(device => !device.Present);
        var hub = devices.Single(device => device.Present);
        Assert.Multiple(() =>
        {
            Assert.That(absent.CanToggle, Is.False);
            Assert.That(absent.IsSuggestion, Is.False);
            Assert.That(hub.Rule?.Id, Is.EqualTo("UsbHub"));
            Assert.That(hub.Rule?.Advice, Is.EqualTo(DeviceAdvice.Avoid));
            Assert.That(hub.HighRisk, Is.True);
        });
    }

    [Test]
    public void IdleBusNeedsZeroChildrenAndABusName()
    {
        var devices = DeviceService.ParseDevices("""[{"Id":"ROOT\\COMPOSITEBUS\\0000","Name":"Composite Bus Enumerator","Class":"System","Status":"OK","Present":true,"Problem":0}]""", []);
        Assert.That(devices.Single().Category, Is.EqualTo(DeviceCategory.IdleBus));
    }

    [Test]
    public void DeviceToggleScriptQuotesInstanceIds()
    {
        var script = DeviceService.BuildToggleScript([@"USB\VID_1&PID_2\it's"], enable: false);
        Assert.That(script, Is.EqualTo(@"Disable-PnpDevice -InstanceId 'USB\VID_1&PID_2\it''s' -Confirm:$false -ErrorAction Stop"));
    }

    [Test]
    public void InterruptDevicesReadMsiPriorityAndAffinity()
    {
        var mask = Convert.ToBase64String(BitConverter.GetBytes(0b1100UL));
        var json = $$"""
            [{"Id":"PCI\\VEN_10DE\\1","Name":"GPU","Class":"Display","MsiCapable":true,"Msi":1,"Limit":4,"Priority":3,"Policy":4,"Mask":"{{mask}}"},
             {"Id":"PCI\\VEN_1022\\2","Name":"Root port","Class":"System","MsiCapable":false},
             {"Id":"PCI\\VEN_8086\\3","Name":"NIC","Class":"Net","MsiCapable":true,"Msi":0}]
            """;
        var devices = InterruptService.ParseDevices(json);

        Assert.Multiple(() =>
        {
            Assert.That(devices.Select(device => device.Name), Is.EqualTo(new[] { "GPU", "NIC" }), "System-class devices without MSI are hidden");
            Assert.That(devices[0].MsiEnabled, Is.True);
            Assert.That(devices[0].Priority, Is.EqualTo(InterruptPriority.High));
            Assert.That(devices[0].HasAffinity, Is.True);
            Assert.That(devices[0].AffinityText, Is.EqualTo("CPU 2, CPU 3"));
            Assert.That(devices[1].MsiEnabled, Is.False);
            Assert.That(devices[1].HasAffinity, Is.False);
        });
    }

    [Test]
    public void InterruptChangesTargetTheDeviceKeys()
    {
        var device = new InterruptDevice(@"PCI\VEN_10DE&DEV_2684\4&1", "GPU", "Display", true, false, null, InterruptPriority.Undefined, 0, 0);
        var changes = InterruptService.BuildChanges(device, msi: true, InterruptPriority.High, affinityMask: 0b10).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(changes.Select(change => change.ValueName), Is.EqualTo(new[] { "MSISupported", "DevicePriority", "DevicePolicy", "AssignmentSetOverride" }));
            Assert.That(changes[0].KeyPath, Does.EndWith(@"Enum\PCI\VEN_10DE&DEV_2684\4&1\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"));
            Assert.That(changes[2].Value, Is.EqualTo(4));
            Assert.That(changes[3].Value, Is.EqualTo(BitConverter.GetBytes(2UL)));
            Assert.That(changes[3].Kind, Is.EqualTo(RegistryValueKind.Binary));
            Assert.That(InterruptService.BuildChanges(device, null, null, null), Is.Empty, "No affinity request leaves it unchanged");
            var automatic = InterruptService.BuildChanges(device, null, null, 0);
            Assert.That(automatic.Select(change => change.ValueName), Is.EqualTo(new[] { "DevicePolicy", "AssignmentSetOverride" }));
            Assert.That(automatic.All(change => change.Value is null), Is.True, "Automatic deletes overrides, never writes an empty mask");
        });
    }

    [Test]
    public void UsbChainsCountExternalHubsAndKeepOneRowPerDevice()
    {
        PnpDeviceInfo[] devices =
        [
            Device(@"PCI\VEN_1022&DEV_15B6\3", "AMD USB 3.10 eXtensible Host Controller", "USB", @"PCI\ROOT"),
            Device(@"USB\ROOT_HUB30\4", "USB Root Hub (USB 3.0)", "USB", @"PCI\VEN_1022&DEV_15B6\3", "USBHUB3"),
            Device(@"USB\VID_05E3&PID_0610\5", "Generic USB Hub", "USB", @"USB\ROOT_HUB30\4", "USBHUB3", "USB2.1 Hub"),
            Device(@"USB\VID_046D&PID_C08B\6", "USB Composite Device", "USB", @"USB\VID_05E3&PID_0610\5", "usbccgp", "G502 HERO"),
            Device(@"USB\VID_046D&PID_C08B&MI_00\6a", "USB Input Device", "HIDClass", @"USB\VID_046D&PID_C08B\6", "HidUsb", "G502 HERO"),
            Device(@"HID\VID_046D&PID_C08B&MI_00\7", "HID-compliant mouse", "Mouse", @"USB\VID_046D&PID_C08B&MI_00\6a", "mouhid"),
            Device(@"HID\VID_046D&PID_C08B&MI_01\8", "HID Keyboard Device", "Keyboard", @"USB\VID_046D&PID_C08B\6", "kbdhid"),
            Device(@"USB\VID_3434&PID_0361\9", "USB Input Device", "HIDClass", @"USB\ROOT_HUB30\4", "HidUsb"),
            Device(@"HID\VID_3434&PID_0361\10", "HID Keyboard Device", "Keyboard", @"USB\VID_3434&PID_0361\9", "kbdhid"),
            Device(@"ACPI\PNP0303\11", "Standard PS/2 Keyboard", "Keyboard", @"ACPI\PNP0A08\0")
        ];
        var chains = UsbService.BuildChains(devices);
        var mouse = chains.Single(chain => chain.Kind == InputKind.Mouse);
        var keyboard = chains.Single(chain => chain.UsbDevice?.Id == @"USB\VID_3434&PID_0361\9");

        Assert.Multiple(() =>
        {
            Assert.That(chains, Has.Count.EqualTo(3), "The G502 mouse and keyboard collections are one physical device");
            Assert.That(mouse.Name, Is.EqualTo("G502 HERO"), "The bus-reported name beats generic Windows names");
            Assert.That(mouse.UsbDevice?.Id, Is.EqualTo(@"USB\VID_046D&PID_C08B\6"), "Power settings target the composite device, not an interface");
            Assert.That(mouse.Hubs, Is.EqualTo(1), "Only the hub-driver node counts, not the composite parent");
            Assert.That(mouse.Kinds, Is.EqualTo(new[] { InputKind.Mouse, InputKind.Keyboard }));
            Assert.That(mouse.Controller?.Name, Does.StartWith("AMD USB"));
            Assert.That(keyboard.Hubs, Is.EqualTo(0), "The root hub belongs to the controller");
            Assert.That(keyboard.Name, Is.EqualTo("HID Keyboard Device"), "Without a bus-reported name the input name is used");
            Assert.That(chains.Single(chain => !chain.IsUsb).Input.Name, Is.EqualTo("Standard PS/2 Keyboard"));
        });
    }

    [Test]
    public void PollingAnalysisUsesTheMedianAndIgnoresPauses()
    {
        var intervals = Enumerable.Repeat(1.0, 200).Concat(Enumerable.Repeat(0.125, 50)).Append(400).ToList();
        var (hz, histogram) = UsbService.AnalyzeIntervals(intervals);

        Assert.Multiple(() =>
        {
            Assert.That(hz, Is.EqualTo(1000).Within(1));
            Assert.That(histogram.Sum(), Is.EqualTo(250), "The 400 ms pause is not a polling interval");
            Assert.That(histogram[Array.IndexOf(UsbService.PollingBuckets, 1000)], Is.EqualTo(200));
            Assert.That(histogram[Array.IndexOf(UsbService.PollingBuckets, 8000)], Is.EqualTo(50));
            Assert.That(UsbService.AnalyzeIntervals([1, 1, 1]).Hz, Is.Zero, "Too few samples give no estimate");
        });
    }

    [Test]
    public void TweakChecksFlagBadValuesAndBuildRepairs()
    {
        var values = new Dictionary<(string, string), object>
        {
            [(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride")] = 3,
            [(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache")] = 1,
            [(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB")] = 0x380000,
            [(@"HKEY_CURRENT_USER\Control Panel\Mouse", "RawMouseThrottleEnabled")] = 1,
            [(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\893dee8e-2bef-41e0-89c6-b55d0929964c", "ValueMax")] = 0
        };
        var checks = TweakCheckService.Evaluate(name => values.GetValueOrDefault((name.Key, name.Value)), ["useplatformclock"], "Restricted")
            .ToDictionary(check => check.Id);

        Assert.Multiple(() =>
        {
            Assert.That(checks["Mitigations"].Status, Is.EqualTo(TweakCheckStatus.Flagged));
            Assert.That(checks["Mitigations"].Repair.Select(change => (change.ValueName, change.Value)), Is.EqualTo(new[] { ("FeatureSettingsOverride", (object?)null) }));
            Assert.That(checks["MemoryManagement"].Repair.Single().Value, Is.EqualTo(0));
            Assert.That(checks["SvcHostSplit"].Status, Is.EqualTo(TweakCheckStatus.Ok), "The Windows default is not flagged");
            Assert.That(checks["RawMouseThrottle"].Status, Is.EqualTo(TweakCheckStatus.Flagged));
            Assert.That(checks["BcdTimers"].RepairScript, Is.EqualTo("& bcdedit.exe /deletevalue '{current}' useplatformclock"));
            Assert.That(checks["AutoTuning"].RepairScript, Does.Contain("autotuninglevel=normal"));
            Assert.That(checks["ThreadDpc"].Status, Is.EqualTo(TweakCheckStatus.Ok));
            Assert.That(checks["InputQueues"].Managed, Has.Count.EqualTo(2), "Undo can find backups even after the repair");
            Assert.That(checks["ProcessorStateRange"].Repair.Single().Value, Is.EqualTo(100), "A lowered limit goes back to the Windows default");
        });

        var withoutAdmin = TweakCheckService.Evaluate(_ => null, null, "Normal").Single(check => check.Id == "BcdTimers");
        Assert.That(withoutAdmin.Status, Is.EqualTo(TweakCheckStatus.NeedsAdministrator));
        Assert.That(TweakCheckService.Evaluate(_ => 100, [], "Normal").Single(check => check.Id == "ProcessorStateRange").Status,
            Is.EqualTo(TweakCheckStatus.Ok), "The Windows default is not flagged");
        // Read-only, against the real registry: every Windows defines this setting, so a typo in the key path fails here.
        Assert.That(RegistryService.Instance.GetRegistryValue(checks["ProcessorStateRange"].Repair.Single().KeyPath, "ValueMax"), Is.Not.Null);
    }

    [Test]
    public async Task RegistryBatchCanDeleteValuesAndRestoreThem()
    {
        var id = Guid.NewGuid().ToString("N");
        var subKey = $@"Software\TweakHubTests\{id}";
        var path = $@"HKCU\{subKey}";
        var dataPath = Path.Combine(Path.GetTempPath(), $"TweakHubTests-{id}");
        try
        {
            using (var key = Registry.CurrentUser.CreateSubKey(subKey))
                key!.SetValue("Tweak", 5, RegistryValueKind.DWord);
            var service = new RegistryService(dataPath);
            service.Initialize();
            RegistryValueChange[] delete = [new(path, "Tweak", null), new(path, "AlreadyMissing", null)];

            Assert.That(await service.ApplyValuesWithBackupAsync(delete), Is.True, "Deleting a missing value is already the wanted state");
            using (var deleted = Registry.CurrentUser.OpenSubKey(subKey))
                Assert.That(deleted!.GetValueNames(), Does.Not.Contain("Tweak"));
            Assert.That(await service.RestoreValuesAsync(delete), Is.True);
            using var restored = Registry.CurrentUser.OpenSubKey(subKey);
            Assert.That(restored!.GetValue("Tweak"), Is.EqualTo(5));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(subKey, false);
            if (Directory.Exists(dataPath)) Directory.Delete(dataPath, true);
        }
    }
}
