using System.IO;
using NUnit.Framework;
using TweakHub.Localization;
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
    public void TheSlowerOfTwoLiveConnectionsIsSuggestedUnticked()
    {
        var context = DeviceService.ParseUsageContext("""
            {"RemoteDesktopOff":false,"DialUpEntries":1,"Nics":[
              {"Id":"PCI\\WIFI","Name":"Wi-Fi 6E","Connected":true,"Wireless":true,"Speed":2400000000},
              {"Id":"PCI\\ETH1","Name":"Ethernet 1G","Connected":true,"Wireless":false,"Speed":1000000000},
              {"Id":"PCI\\ETH25","Name":"Ethernet 2.5G","Connected":true,"Wireless":false,"Speed":2500000000}]}
            """);
        // Wired beats a faster Wi-Fi link; among wired cards the fastest stays.
        Assert.That(context.BestNic?.Id, Is.EqualTo(@"PCI\ETH25"));
        Assert.That(DeviceService.UnusedReason(Device(@"PCI\WIFI", "Wi-Fi 6E"), context), Is.EqualTo("Devices:UnusedNicSlower"));
        Assert.That(DeviceService.UnusedReason(Device(@"PCI\ETH1", "Ethernet 1G"), context), Is.EqualTo("Devices:UnusedNicSlower"));
        Assert.That(DeviceService.UnusedReason(Device(@"PCI\ETH25", "Ethernet 2.5G"), context), Is.Null);
        Assert.That(DeviceService.TickedByDefault("Devices:UnusedNicSlower"), Is.False);
    }

    [Test]
    public void AudioFunctionsNobodyListensToAreSuggested()
    {
        PnpDeviceInfo Pci(string id, string cls, int bus, int address) => Device(id, id, cls) with { BusNumber = bus, Address = address };
        PnpDeviceInfo Child(string id, string parent, int problem = 0) =>
            new(id, id, "MEDIA", problem == 0 ? "OK" : "Error", true, problem, parent, "", "");
        var devices = new List<PnpDeviceInfo>
        {
            Pci(@"PCI\VEN_1002&DEV_73A5\GPU", "Display", 48, 0),
            Pci(@"PCI\VEN_1002&DEV_AB28\HDMI", "System", 48, 1),
            Child(@"HDAUDIO\FUNC_01&VEN_1002\HDMI", @"PCI\VEN_1002&DEV_AB28\HDMI"),
            Pci(@"PCI\VEN_1022&DEV_1487\HDA", "System", 10, 0x10003),
            Child(@"HDAUDIO\FUNC_01&VEN_10EC\REALTEK", @"PCI\VEN_1022&DEV_1487\HDA"),
            Child(@"USB\VID_06CB\DAC", @"USB\ROOT"),
            Child(@"SWD\MMDEVAPI\{0.0.0.00000000}.{dac}", @"USB\VID_06CB\DAC"),
        };
        var context = DeviceService.WithAudio(Unused, devices, ["{0.0.0.00000000}.{dac}"]);
        Assert.That(DeviceService.UnusedReason(devices[2], context), Is.EqualTo("Devices:UnusedGpuAudio"));
        Assert.That(DeviceService.UnusedReason(devices[4], context), Is.EqualTo("Devices:UnusedOnboardAudio"));
        // The Realtek plays the default output: it stays.
        var realtek = devices.Append(Child(@"SWD\MMDEVAPI\{0.0.0.00000000}.{spk}", @"HDAUDIO\FUNC_01&VEN_10EC\REALTEK")).ToList();
        Assert.That(DeviceService.UnusedReason(devices[4], DeviceService.WithAudio(Unused, realtek, ["{0.0.0.00000000}.{spk}"])), Is.Null);
        // No default audio device known: nothing is suggested.
        Assert.That(DeviceService.WithAudio(Unused, devices, []).SpareOnboardAudio, Is.Empty);
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
    public void GhostRemovalListsOnlyNotPresentDevicesAndTicksOnlyHardwareOnes()
    {
        var devices = new[]
        {
            Device(@"USB\VID_046D&PID_C52B\5&1", "Old mouse", "HIDClass", present: false),
            Device(@"PCI\VEN_8086&DEV_125C\2", "Live NIC"),
            Device(@"SWD\MSRRAS\MS_PPTPMINIPORT", "WAN Miniport (PPTP)", present: false),
            Device(@"ROOT\NET\0001", "Virtual adapter", present: false),
            Device(@"{5d624f94-8850-40c3-a3fa-a4fd2080baf3}\vwifimp_wfd\5&1", "Wi-Fi Direct", present: false),
            Device(@"USB\VID_1234&PID_0001\1", "Disabled by TweakHub", present: false) with { DisabledByTweakHub = true }
        };
        var ghosts = DeviceService.GhostDevices(devices);
        Assert.That(ghosts.Select(device => device.Name), Is.EqualTo(new[] { "Old mouse", "WAN Miniport (PPTP)", "Virtual adapter", "Wi-Fi Direct" }));
        Assert.That(ghosts.Where(DeviceService.GhostTickedByDefault).Select(device => device.Name), Is.EqualTo(new[] { "Old mouse" }));
        foreach (var id in new[] { @"HTREE\ROOT\0", @"SW\{x}\1", @"umb\umb\1&1" })
            Assert.That(DeviceService.GhostTickedByDefault(Device(id, "x", present: false)), Is.False, id);
        Assert.That(DeviceService.BuildRemoveGhostsScript([@"USB\VID_046D&PID_C52B\5&1"]),
            Does.Contain("CM_Get_DevNode_Status").And.Contain("-ne 13").And.Contain("CM_Uninstall_DevNode($node, 0)").And.Contain(@"'USB\VID_046D&PID_C52B\5&1'"));
    }

    /// <summary>In-memory cfgmgr32: each id has a locate result and a status; every uninstall is recorded.</summary>
    private sealed class FakeDevNodes : IDevNodeApi
    {
        private readonly List<(string Id, int Locate, int Status, DateTime? Arrival, DateTime? Removal)> _nodes = [];
        public int UninstallResult;
        public readonly List<string> Uninstalled = [];

        public FakeDevNodes Add(string id, int locate = 0, int status = GhostDevices.CrNoSuchDevNode, DateTime? arrival = null, DateTime? removal = null)
        {
            _nodes.Add((id, locate, status, arrival, removal));
            return this;
        }

        public IReadOnlyList<string> DeviceIds() => [.. _nodes.Select(node => node.Id)];
        public int Locate(string id, out int node)
        {
            node = _nodes.FindIndex(item => item.Id == id);
            return _nodes[node].Locate;
        }
        public int Status(int node) => _nodes[node].Status;
        public DateTime? FileTime(int node, uint pid) => pid switch
        {
            GhostDevices.LastArrivalPid => _nodes[node].Arrival,
            GhostDevices.LastRemovalPid => _nodes[node].Removal,
            GhostDevices.FirstInstallPid => FirstInstall,
            _ => null
        };
        public int Uninstall(int node)
        {
            Uninstalled.Add(_nodes[node].Id);
            return UninstallResult;
        }
        public string ClassDescription(int node) => "Keyboards";
        public string Manufacturer(int node) => "Logitech";
        public DateTime? FirstInstall;
    }

    [Test]
    public void GhostScanCountsOnlyNoSuchDevNodeAndKeepsTheLaterDate()
    {
        DateTime early = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), late = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var api = new FakeDevNodes()
            .Add("GHOST", arrival: early, removal: late)
            .Add("GHOST-NODATE")
            .Add("PRESENT", status: 0)
            .Add("UNKNOWN", status: 0x1F) // CR_FAILURE: an unreadable status counts as present
            .Add("GONE", locate: GhostDevices.CrNoSuchDevNode);
        api.FirstInstall = early;
        var ghosts = GhostDevices.Scan(api);
        Assert.That(ghosts.Keys, Is.EquivalentTo(new[] { "GHOST", "GHOST-NODATE" }));
        Assert.That(ghosts["GHOST"], Is.EqualTo(new GhostInfo(late, "Keyboards", "Logitech", early)));
        Assert.That(ghosts["GHOST-NODATE"].LastUsed, Is.Null);
        Assert.That(GhostDevices.LastUsed(late, early), Is.EqualTo(late));
        Assert.That(GhostDevices.LastUsed(null, early), Is.EqualTo(early));
        Assert.That(GhostDevices.LastUsed(late, null), Is.EqualTo(late));
    }

    [Test]
    public void GhostUninstallReChecksAndRemovesOnlyAbsentDevices()
    {
        var api = new FakeDevNodes()
            .Add("GHOST")
            .Add("RECONNECTED", status: 0)
            .Add("UNKNOWN", status: 0x1F)
            .Add("GONE", locate: GhostDevices.CrNoSuchDevNode);
        Assert.That(GhostDevices.Uninstall(api, "GHOST"), Is.EqualTo("OK|GHOST"));
        Assert.That(GhostDevices.Uninstall(api, "RECONNECTED"), Is.EqualTo("SKIP|RECONNECTED"));
        Assert.That(GhostDevices.Uninstall(api, "UNKNOWN"), Is.EqualTo("SKIP|UNKNOWN"));
        Assert.That(GhostDevices.Uninstall(api, "GONE"), Is.EqualTo("FAIL|CM_Locate_DevNode 13|GONE"));
        Assert.That(api.Uninstalled, Is.EqualTo(new[] { "GHOST" }));

        api.UninstallResult = 0x17; // CR_REMOVE_VETOED
        Assert.That(GhostDevices.Uninstall(api, "GHOST"), Is.EqualTo("FAIL|CM_Uninstall_DevNode 23|GHOST"));

        // A corrupt FILETIME past DateTime.MaxValue reads as "no date" instead of throwing.
        Assert.That(GhostDevices.FileTimeToUtc(DateTime.MaxValue.ToFileTimeUtc() + 1), Is.Null);
        Assert.That(GhostDevices.FileTimeToUtc(long.MaxValue), Is.Null);
        Assert.That(GhostDevices.FileTimeToUtc(0), Is.Null);
        Assert.That(GhostDevices.FileTimeToUtc(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToFileTimeUtc()),
            Is.EqualTo(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void GhostListNeedsInventoryAndCfgMgrToAgreeAndRecentUseHidesRows()
    {
        var now = new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc);
        var devices = new[]
        {
            Device(@"USB\OLD\1", "Old mouse", "HIDClass", present: false),
            Device(@"USB\STALE\1", "Present for cfgmgr32", present: false),
            Device(@"PCI\LIVE\1", "Live NIC")
        };
        var ghosts = DeviceService.ConfirmedGhosts(devices, new Dictionary<string, GhostInfo>
        {
            [@"USB\OLD\1"] = new(now.AddDays(-5), "Mice"), [@"PCI\LIVE\1"] = new(null, "")
        });
        Assert.That(ghosts.Select(device => device.Name), Is.EqualTo(new[] { "Old mouse" }));
        Assert.That(ghosts[0].LastUsed, Is.EqualTo(now.AddDays(-5)));
        Assert.That(ghosts[0].TypeName, Is.EqualTo("Mice"));

        Assert.That(GhostDevices.UsedWithin(now.AddDays(-5), 30, now), Is.True);
        Assert.That(GhostDevices.UsedWithin(now.AddDays(-45), 30, now), Is.False);
        Assert.That(GhostDevices.UsedWithin(null, 30, now), Is.False);
        Assert.That(GhostDevices.UsedWithin(now.AddDays(-45), int.MaxValue, now), Is.True, "a huge day count must not throw");
    }

    // Driver Store Explorer: a UI Automation toggle skipped the check callback, so the confirm count was off by one.
    [Test]
    public void GhostDialogReturnsExactlyTheVisibleTickedRowsIncludingAutomationToggles()
    {
        if (System.Windows.Application.Current is null)
        {
            var app = new App();
            app.InitializeComponent();
            app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
        }
        var now = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        var ghosts = new[]
        {
            Device(@"USB\VID_31E3&PID_1322&MI_01\1", "HID Keyboard Device", "Keyboard", present: false) with { LastUsed = now.AddDays(-2), TypeName = "Keyboards" },
            Device(@"USB\VID_046D&PID_C52B\2", "Old mouse", "Mouse", present: false) with { LastUsed = now.AddDays(-90), TypeName = "Mice" },
            Device(@"SWD\WPDBUSENUM\3", @"F:\", "WPD", present: false) with { TypeName = "Portable Devices" }
        };
        var dialog = new TweakHub.Views.Dialogs.GhostDevicesDialog(ghosts, ghosts, now);
        var boxes = ((System.Windows.Controls.Panel)dialog.FindName("RowsPanel")).Children.OfType<System.Windows.Controls.Grid>()
            .Select(row => row.Children.OfType<System.Windows.Controls.CheckBox>().Single()).ToList();
        Assert.That(boxes[0].Visibility, Is.EqualTo(System.Windows.Visibility.Collapsed), "used 2 days ago: hidden by the 30-day filter");
        Assert.That(boxes[2].IsChecked, Is.False, "software device: not ticked by default");

        var toggle = (System.Windows.Automation.Provider.IToggleProvider)
            System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(boxes[2]).GetPattern(System.Windows.Automation.Peers.PatternInterface.Toggle);
        toggle.Toggle();
        Assert.That(dialog.CheckedRows, Is.EqualTo(new[] { 1, 2 }), "the hidden row is ticked but must not be returned");
        Assert.That(((System.Windows.Controls.Button)dialog.FindName("RemoveButton")).Content, Is.EqualTo(L.Format("Devices:GhostRemoveButton", 2)));

        ((System.Windows.Controls.CheckBox)dialog.FindName("FilterBox")).IsChecked = false;
        Assert.That(dialog.CheckedRows, Is.EqualTo(new[] { 0, 1, 2 }));

        // Select none / all act on the rows on screen only; the hidden row keeps its tick but is still never returned.
        ((System.Windows.Controls.CheckBox)dialog.FindName("FilterBox")).IsChecked = true;
        Click(dialog, "SelectNoneButton");
        Assert.That(dialog.CheckedRows, Is.Empty);
        Assert.That(boxes[0].IsChecked, Is.True);
        Click(dialog, "SelectAllButton");
        Assert.That(dialog.CheckedRows, Is.EqualTo(new[] { 1, 2 }));

        // Bad day counts never turn the filter off: the recent row stays hidden.
        var daysBox = (System.Windows.Controls.TextBox)dialog.FindName("FilterDaysBox");
        foreach (var text in new[] { "", "0", "-5", "abc", "2147483648", "99999999999999999999999" })
        {
            daysBox.Text = text;
            Assert.That(boxes[0].Visibility, Is.EqualTo(System.Windows.Visibility.Collapsed), $"days = '{text}'");
            Assert.That(dialog.CheckedRows, Does.Not.Contain(0), $"days = '{text}'");
        }
        daysBox.Text = "1";
        Assert.That(boxes[0].Visibility, Is.EqualTo(System.Windows.Visibility.Visible), "used 2 days ago, filter 1 day");
        dialog.Close();

        static void Click(System.Windows.Window window, string name) => ((System.Windows.Controls.Button)window.FindName(name))
            .RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
    }

    [Test]
    public void GhostRowsUseTheProductNameTheUsbParentReported()
    {
        // Two units of the same model (same VID/PID) that report different names: each child takes its own parent's.
        var keyboardB = Device(@"HID\VID_31E3&PID_1322&MI_01\B&1", "HID Keyboard Device", "Keyboard", present: false) with { Parent = @"USB\VID_31E3&PID_1322&MI_01\B" };
        var all = new[]
        {
            Device(@"USB\VID_31E3&PID_1322\UNIT_A", "USB Composite Device", "USB", present: false) with { BusName = "Unit A" },
            Device(@"USB\VID_31E3&PID_1322&MI_01\A", "USB Input Device", "HIDClass", present: false) with { BusName = "Part A", Parent = @"USB\VID_31E3&PID_1322\UNIT_A" },
            Device(@"USB\VID_31E3&PID_1322\UNIT_B", "USB Composite Device", "USB", present: false) with { BusName = "Unit B" },
            Device(@"USB\VID_31E3&PID_1322&MI_01\B", "USB Input Device", "HIDClass", present: false) with { BusName = "Part B", Parent = @"USB\VID_31E3&PID_1322\UNIT_B" },
            keyboardB
        };
        var byId = DeviceService.ById(all);
        Assert.That(DeviceService.ProductName(keyboardB, byId), Is.EqualTo("Unit B"), "the composite above names the product");
        Assert.That(DeviceService.ProductName(all[3], byId), Is.EqualTo("Part B"), "its own name wins");
        Assert.That(DeviceService.ProductName(keyboardB with { Parent = "" }, byId), Is.Empty, "no parent: never borrow another unit's name");
        Assert.That(DeviceService.ProductName(Device(@"DISPLAY\AUS27F4\1", "Generic Monitor", "Monitor", present: false), byId), Is.Empty);
        // A parent loop must not hang.
        var looped = DeviceService.ById([all[0] with { BusName = "", Parent = all[1].Id }, all[1] with { BusName = "", Parent = all[0].Id }]);
        Assert.That(DeviceService.ProductName(keyboardB with { Parent = all[1].Id }, looped), Is.Empty);

        var now = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        Assert.That(TweakHub.Views.Dialogs.GhostDevicesDialog.RelativeDate(now.AddDays(-12), now), Is.EqualTo(L.Format("Devices:GhostDaysAgo", 12)));
        Assert.That(TweakHub.Views.Dialogs.GhostDevicesDialog.RelativeDate(null, now), Is.EqualTo(L.Get("Devices:GhostLastUsedUnknown")));
    }

    [Test]
    public void GhostRemovalReportCountsOnlyOkLinesAndKeepsFailureReasons()
    {
        var devices = new[] { Device("A", "Alpha", present: false), Device("B", "Beta", present: false), Device("C", "Gamma", present: false) };
        var (removed, notRemoved) = DeviceService.ParseRemoveGhosts("OK|A\r\nSKIP|B\r\nFAIL|CM_Uninstall_DevNode 23|C\r\n", devices);
        Assert.That(removed, Is.EqualTo(1));
        Assert.That(notRemoved, Is.EqualTo(new[] { "Beta", "Gamma (CM_Uninstall_DevNode 23)" }));

        // PnP instance ids may contain '|': the id is the rest of the line.
        var piped = new[] { Device(@"USB\X\A|B", "Pipe", present: false), Device(@"USB\Y\C|D", "Pipe 2", present: false) };
        (removed, notRemoved) = DeviceService.ParseRemoveGhosts("OK|USB\\X\\A|B\nFAIL|CM_Uninstall_DevNode 23|USB\\Y\\C|D\n", piped);
        Assert.That(removed, Is.EqualTo(1));
        Assert.That(notRemoved, Is.EqualTo(new[] { "Pipe 2 (CM_Uninstall_DevNode 23)" }));
        var api = new FakeDevNodes().Add(@"USB\X\A|B");
        Assert.That(DeviceService.ParseRemoveGhosts(GhostDevices.Uninstall(api, @"USB\X\A|B"), piped[..1]).Removed, Is.EqualTo(1));
    }

    [Test]
    public async Task DeviceAndDriverScriptsParseWithoutRunningThem()
    {
        foreach (var script in new[]
        {
            DeviceService.UsageScript, DeviceService.BuildDisableManyScript([@"ROOT\KDNIC\0000", "it's"]), DriverStoreService.InventoryScript,
            DeviceService.BuildRemoveGhostsScript([@"USB\VID_046D&PID_C52B\5&1", "it's"]),
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
