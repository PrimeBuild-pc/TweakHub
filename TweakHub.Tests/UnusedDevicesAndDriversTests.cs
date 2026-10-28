using System.IO;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class UnusedDevicesAndDriversTests
{
    private static PnpDeviceInfo Device(string id, string name, string cls = "Net", int problem = 0, bool present = true) =>
        new(id, name, cls, problem == 0 ? "OK" : "Error", present, problem, "", "", "") { Rule = DeviceService.MatchRule(id, name) };

    private static readonly DeviceService.UsageContext Unused = new(true, true,
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"PCI\VEN_8086&DEV_125C\2" }, AnyNicConnected: true);

    [Test]
    public void UnusedDevicesFollowWhatThisPcActuallyUses()
    {
        var used = Unused with { RemoteDesktopOff = false, NoDialUpEntries = false, AnyNicConnected = false };
        Assert.Multiple(() =>
        {
            Assert.That(DeviceService.UnusedReason(Device(@"ROOT\KDNIC\0000", "Microsoft Kernel Debug Network Adapter"), used), Is.EqualTo("Devices:UnusedAlways"));
            Assert.That(DeviceService.UnusedReason(Device(@"ACPI\PNP0800\4&1", "System speaker", "System"), used), Is.EqualTo("Devices:UnusedAlways"));
            Assert.That(DeviceService.UnusedReason(Device(@"ROOT\RDPBUS\0000", "Remote Desktop Device Redirector Bus", "System"), Unused), Is.EqualTo("Devices:UnusedRemoteDesktop"));
            Assert.That(DeviceService.UnusedReason(Device(@"ROOT\RDPBUS\0000", "Remote Desktop Device Redirector Bus", "System"), used), Is.Null);
            Assert.That(DeviceService.UnusedReason(Device(@"SWD\MSRRAS\MS_AGILEVPNMINIPORT", "WAN Miniport (IKEv2)"), Unused), Is.EqualTo("Devices:UnusedVpn"));
            Assert.That(DeviceService.UnusedReason(Device(@"SWD\MSRRAS\MS_PPPOEMINIPORT", "WAN Miniport (PPPOE)"), used), Is.Null);
            // IP, IPv6 and Network Monitor miniports serve other features.
            Assert.That(DeviceService.UnusedReason(Device(@"SWD\MSRRAS\MS_NDISWANIP", "WAN Miniport (IP)"), Unused), Is.Null);
            Assert.That(DeviceService.UnusedReason(Device(@"PCI\VEN_8086&DEV_125C\2", "Intel(R) Ethernet Controller I226-V #2"), Unused), Is.EqualTo("Devices:UnusedNic"));
            // Never the last way online.
            Assert.That(DeviceService.UnusedReason(Device(@"PCI\VEN_8086&DEV_125C\2", "Intel(R) Ethernet Controller I226-V #2"), Unused with { AnyNicConnected = false }), Is.Null);
            // Features that may be in use are never "unused".
            Assert.That(DeviceService.UnusedReason(Device(@"ROOT\VID\0000", "Microsoft Hyper-V Virtualization Infrastructure Driver", "System"), Unused), Is.Null);
            Assert.That(DeviceService.UnusedReason(Device(@"ROOT\VDRVROOT\0000", "Microsoft Virtual Drive Enumerator", "System"), Unused), Is.Null);
            // Already off or not connected: nothing to do.
            Assert.That(DeviceService.UnusedReason(Device(@"ROOT\KDNIC\0000", "Microsoft Kernel Debug Network Adapter", problem: 22), Unused), Is.Null);
            Assert.That(DeviceService.UnusedReason(Device(@"ROOT\KDNIC\0000", "Microsoft Kernel Debug Network Adapter", present: false), Unused), Is.Null);
        });
    }

    [Test]
    public void UsageContextReadsTheScriptOutput()
    {
        var context = DeviceService.ParseUsageContext("""
            {"RemoteDesktopOff":true,"DialUpEntries":0,"Nics":[{"Id":"PCI\\A","Connected":true},{"Id":"PCI\\B","Connected":false}]}
            """);
        Assert.That(context.RemoteDesktopOff && context.NoDialUpEntries && context.AnyNicConnected, Is.True);
        Assert.That(context.DisconnectedNics, Is.EquivalentTo(new[] { @"PCI\B" }));
        Assert.That(DeviceService.ParseUsageContext("""{"RemoteDesktopOff":false,"DialUpEntries":2,"Nics":[]}""").NoDialUpEntries, Is.False);
    }

    [Test]
    public void AnotherAccountsVpnIsNamedAndLeftUnticked()
    {
        var context = DeviceService.ParseUsageContext("""
            {"RemoteDesktopOff":true,"DialUpEntries":0,"Nics":[],
             "Others":[{"User":"PC\\Anna","Entries":2},{"User":"PC\\Marco","Entries":-1},{"User":"PC\\Ospite","Entries":0}]}
            """);
        Assert.That(context.OtherVpnUsers, Is.EqualTo(new[] { @"PC\Anna" }));
        Assert.That(context.UncheckedUsers, Is.EqualTo(new[] { @"PC\Marco" }));
        var miniport = Device(@"SWD\MSRRAS\MS_SSTPMINIPORT", "WAN Miniport (SSTP)");
        var reason = DeviceService.UnusedReason(miniport, context)!;
        Assert.That(reason, Is.EqualTo("Devices:UnusedVpnOtherUser"));
        Assert.That(DeviceService.UnusedText(reason, context), Does.Contain(@"PC\Anna"));
        Assert.That(DeviceService.TickedByDefault(reason), Is.False);
        var unchecked_ = context with { OtherVpnUsers = [] };
        Assert.That(DeviceService.UnusedReason(miniport, unchecked_), Is.EqualTo("Devices:UnusedVpnUnchecked"));
        Assert.That(DeviceService.UnusedText("Devices:UnusedVpnUnchecked", unchecked_), Does.Contain(@"PC\Marco"));
        Assert.That(DeviceService.TickedByDefault("Devices:UnusedVpn") && DeviceService.TickedByDefault("Devices:UnusedAlways"), Is.True);
    }

    private static DriverPackage Package(string inf, string name, string version, string date, bool bound = false, string provider = "AMD", string cls = "System") =>
        new(inf, name, provider, cls, version, date, bound);

    [Test]
    public void OldDriversAreOlderVersionsThatNoDeviceUses()
    {
        var old = DriverStoreService.OldPackages(
        [
            Package("oem19.inf", "amdgpio2.inf", "2.2.0.136", "2023-01-01"),
            Package("oem31.inf", "amdgpio2.inf", "2.2.0.140", "2024-01-01", bound: true),
            Package("oem14.inf", "ovpn-dco.inf", "2.8.3.0", "2024-05-01", provider: "OpenVPN, Inc", cls: "Net"),
            Package("oem50.inf", "ovpn-dco.inf", "2.10.0.0", "2025-02-01", provider: "OpenVPN, Inc", cls: "Net"),
            // A device still uses the older version: keep it.
            Package("oem60.inf", "rt640x64.inf", "10.1", "2022-01-01", bound: true, provider: "Realtek", cls: "Net"),
            Package("oem61.inf", "rt640x64.inf", "10.2", "2023-01-01", provider: "Realtek", cls: "Net"),
            // Same file name from another provider is another driver.
            Package("oem70.inf", "amdgpio2.inf", "1.0", "2020-01-01", provider: "Other"),
            // An exact duplicate of the newest is not "older".
            Package("oem80.inf", "dup.inf", "1.0", "2020-01-01"),
            Package("oem81.inf", "dup.inf", "1.0", "2020-01-01"),
            Package("oem2.inf", "vboxsup.inf", "7.2.20.25154", "2024-01-01")
        ]);
        Assert.That(old.Select(package => package.Inf), Is.EquivalentTo(new[] { "oem19.inf", "oem14.inf" }));
        Assert.That(old.Single(package => package.Inf == "oem14.inf").NewerVersion, Is.EqualTo("2.10.0.0"));
    }

    [Test]
    public void DriverInventoryKeepsOnlyThirdPartyPackagesAndMarksBoundOnes()
    {
        var packages = DriverStoreService.ParsePackages("""
            {"Packages":[{"Inf":"oem3.inf","Name":"a.inf","Provider":"P","Class":"Net","Version":"1.0","Date":"2024-01-01"},
                         {"Inf":"netwtw.inf","Name":"b.inf","Provider":"P","Class":"Net","Version":"1.0","Date":""}],
             "Bound":["OEM3.INF","c:\\x.inf"]}
            """);
        Assert.That(packages.Select(package => (package.Inf, package.Bound)), Is.EqualTo(new[] { ("oem3.inf", true) }));
    }

    [Test]
    public void DriverRemovalExportsFirstAndRefusesAnythingButOemPackages()
    {
        var script = DriverStoreService.BuildRemoveScript([Package("oem19.inf", "it's.inf", "2.2", "2023-01-01")]);
        int At(string text) => script.IndexOf(text, StringComparison.Ordinal);
        Assert.That(At("/export-driver"), Is.LessThan(At("/delete-driver")));
        Assert.That(script, Does.Not.Contain("/force"));
        Assert.That(script, Does.Contain("'it''s.inf"));
        Assert.That(script, Does.Contain("SetAccessRuleProtection($true, $false)"));
        Assert.Throws<ArgumentException>(() => DriverStoreService.BuildRemoveScript([Package("oem1.inf'; calc; '", "x.inf", "1", "")]));
        Assert.Throws<ArgumentException>(() => DriverStoreService.BuildRemoveScript([Package("netwtw.inf", "x.inf", "1", "")]));
        var restore = DriverStoreService.BuildRestoreScript(@"C:\ProgramData\TweakHub-DriverBackups\20261027-120000\oem19.inf");
        Assert.That(restore, Does.Contain("Assert-Root").And.Contain("/add-driver").And.Not.Contain("/install"));
    }

    [Test]
    public async Task DeviceAndDriverScriptsParseWithoutRunningThem()
    {
        foreach (var script in new[]
        {
            DeviceService.UsageScript, DeviceService.BuildDisableManyScript([@"ROOT\KDNIC\0000", "it's"]), DriverStoreService.InventoryScript,
            DriverStoreService.BuildRemoveScript([Package("oem19.inf", "amdgpio2.inf", "2.2", "2023-01-01")]),
            DriverStoreService.BuildRestoreScript(@"C:\ProgramData\TweakHub-DriverBackups\20261027-120000\oem19.inf")
        })
        {
            var parseOnly = "$tokens = $null; $errors = $null; [System.Management.Automation.Language.Parser]::ParseInput("
                + PowerShellService.Literal(script) + ", [ref]$tokens, [ref]$errors) | Out-Null; if ($errors.Count) { $errors | Out-String | Write-Error; exit 1 }";
            var result = await PowerShellService.Instance.ExecuteScriptAsync(parseOnly);
            Assert.That(result.Success, Is.True, result.Error);
        }
    }

    [Test]
    public void BackupsAreListedFromTheirFolderNewestFirst()
    {
        var root = Path.Combine(Path.GetTempPath(), "th-drv-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var (batch, inf) in new[] { ("20261001-100000", "oem14.inf"), ("20261027-120000", "oem19.inf"), ("notadate", "oem1.inf"), ("20261027-120000", "evil") })
            {
                var folder = Path.Combine(root, batch, inf);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "tweakhub.txt"), $"{inf}.name\tProv\t1.0");
            }
            var backups = DriverStoreService.LoadBackups(root);
            Assert.That(backups.Select(backup => backup.Inf), Is.EqualTo(new[] { "oem19.inf", "oem14.inf" }));
            Assert.That(DriverStoreService.LoadBackups(Path.Combine(root, "missing")), Is.Empty);
        }
        finally { Directory.Delete(root, true); }
    }
}
