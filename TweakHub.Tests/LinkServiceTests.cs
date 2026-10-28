using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class LinkServiceTests
{
    private const string RootComplex = @"ACPI\PNP0A08\0";

    private static PnpDeviceInfo Pci(string id, string parent, string cls = "System", string name = "", int bus = 0, int address = 0,
        int gen = 0, int width = 0, int maxGen = 0, int maxWidth = 0, string service = "") =>
        new(id, name, cls, "OK", true, 0, parent, service, "")
        {
            BusNumber = bus, Address = address, LinkGen = gen, LinkWidth = width, MaxLinkGen = maxGen, MaxLinkWidth = maxWidth
        };

    private static PnpDeviceInfo Nvme(string id, string parent, int gen, int width, int maxGen = 0, int maxWidth = 4) =>
        Pci(id, parent, "SCSIAdapter", "Standard NVM Express Controller", 1, 0, gen, width, maxGen == 0 ? gen : maxGen, maxWidth, "stornvme");

    // Lorenzo's PC (MSI X570 Gaming Edge WiFi, RX 6950 XT): parent chains as Windows reports them.
    private static List<PnpDeviceInfo> X570Pc(int gpuGen = 4, int gpuWidth = 16) =>
    [
        Pci(RootComplex, @"HTREE\ROOT\0"),
        Pci(@"PCI\VEN_1022&DEV_1483\rp11", RootComplex, address: 1 << 16 | 1),
        Nvme(@"PCI\VEN_C0A9&DEV_5412\crucial", @"PCI\VEN_1022&DEV_1483\rp11", 3, 4),
        Pci(@"PCI\VEN_1022&DEV_1483\rp12", RootComplex, address: 1 << 16 | 2),
        Pci(@"PCI\VEN_1022&DEV_57AD\up", @"PCI\VEN_1022&DEV_1483\rp12", bus: 32),
        Pci(@"PCI\VEN_1022&DEV_57A3\down", @"PCI\VEN_1022&DEV_57AD\up", bus: 33),
        Nvme(@"PCI\VEN_144D&DEV_A809\samsung", @"PCI\VEN_1022&DEV_57A3\down", 3, 4),
        Pci(@"SCSI\DISK&VEN_NVME&PROD_SAMSUNG_SSD_980\disk", @"PCI\VEN_144D&DEV_A809\samsung", "DiskDrive", "Samsung SSD 980 1TB"),
        Pci(@"PCI\VEN_1022&DEV_1483\rp31", RootComplex, address: 3 << 16 | 1),
        Pci(@"PCI\VEN_1002&DEV_1478\gpuup", @"PCI\VEN_1022&DEV_1483\rp31", bus: 46),
        Pci(@"PCI\VEN_1002&DEV_1479\gpudown", @"PCI\VEN_1002&DEV_1478\gpuup", bus: 47),
        // Behind the card's own switch Windows reads the internal link, always x16.
        Pci(@"PCI\VEN_1002&DEV_73A5\gpu", @"PCI\VEN_1002&DEV_1479\gpudown", "Display", "AMD Radeon RX 6950 XT", 48, 0, 4, 16, 4, 16),
        Pci(@"PCI\VEN_1002&DEV_AB28\audio", @"PCI\VEN_1002&DEV_1479\gpudown", "MEDIA", "AMD High Definition Audio", 48, 1, 4, 16, 4, 16)
    ];

    private static readonly Dictionary<(string Hub, int Port), UsbPortSpeed> NoUsb = [];

    private static Dictionary<string, (int Gen, int Width)> Adlx(int gen, int width) => new() { [@"PCI\VEN_1002&DEV_73A5\gpu"] = (gen, width) };

    private static LinkCheck Find(LinkReport report, string id) => report.Devices.Single(check => check.Device.Id.EndsWith(id));

    [Test]
    public void LorenzosX570ReadsCpuChipsetAndRealGpuLink()
    {
        var report = LinkService.Evaluate(X570Pc(), Adlx(4, 16), NoUsb, "MPG X570 GAMING EDGE WIFI (MS-7C37)", "AMD Ryzen 7 5800X3D 8-Core Processor");

        Assert.Multiple(() =>
        {
            Assert.That(report.Devices, Has.Count.EqualTo(3), "GPU audio shares the GPU's link and is not listed");
            Assert.That(Find(report, "crucial").Attach, Is.EqualTo(LinkAttach.Cpu));
            Assert.That(Find(report, "crucial").Status, Is.EqualTo(LinkStatus.Ok));
            Assert.That(Find(report, "samsung").Attach, Is.EqualTo(LinkAttach.Chipset));
            Assert.That(Find(report, "samsung").Advice, Is.EqualTo("OkChipset"));
            Assert.That(Find(report, "samsung").Name, Is.EqualTo("Samsung SSD 980 1TB"));
            Assert.That(Find(report, "crucial").Name, Is.EqualTo("Standard NVM Express Controller"), "No disk child: controller name");
            Assert.That(Find(report, "gpu").Attach, Is.EqualTo(LinkAttach.Cpu));
            Assert.That(Find(report, "gpu").Status, Is.EqualTo(LinkStatus.Ok));
            Assert.That(LinkService.PciAddress(Find(report, "gpu").RootPort!), Is.EqualTo("0:3.1"));
            Assert.That(report.Chipset.Chipset, Is.EqualTo(new ChipsetInfo("X570", 4, 4, false)));
            Assert.That(report.Chipset.Advice, Is.EqualTo("ChipsetOk"));
            Assert.That(Find(report, "gpu").LinkText, Is.EqualTo("PCIe x16 4.0 @ x16 4.0"));
        });
    }

    [Test]
    public void RadeonLinkComesFromTheDriverNotFromTheInternalSwitch()
    {
        var hidden = Find(LinkService.Evaluate(X570Pc(), new Dictionary<string, (int, int)>(), NoUsb, "", ""), "gpu");
        var x8 = Find(LinkService.Evaluate(X570Pc(), Adlx(4, 8), NoUsb, "", ""), "gpu");

        Assert.Multiple(() =>
        {
            Assert.That(hidden.Status, Is.EqualTo(LinkStatus.Unknown));
            Assert.That(hidden.Advice, Is.EqualTo("Hidden"));
            Assert.That(x8.Status, Is.EqualTo(LinkStatus.Bad));
            Assert.That(x8.Advice, Is.EqualTo("LanesMissing"));
            Assert.That(x8.Args, Is.EqualTo(new object[] { 8, 16 }));
        });
    }

    [Test]
    public void NarrowRadeonIsNotFlaggedForItsOwnWidth()
    {
        var devices = X570Pc().Select(device => device.Id.EndsWith("gpu")
            ? device with { Id = @"PCI\VEN_1002&DEV_73FF\gpu" } : device).ToList();
        var check = Find(LinkService.Evaluate(devices, new Dictionary<string, (int, int)> { [@"PCI\VEN_1002&DEV_73FF\gpu"] = (4, 8) }, NoUsb, "", ""), "gpu");

        Assert.That(check.Status, Is.EqualTo(LinkStatus.Ok), "RX 6600 is an x8 card");
    }

    [Test]
    public void TwoFastDrivesOverTheChipsetUplinkAreFlagged()
    {
        var devices = X570Pc();
        devices.Add(Nvme(@"PCI\VEN_144D&DEV_A80C\second", @"PCI\VEN_1022&DEV_57A3\down", 4, 4));
        devices.Add(Nvme(@"PCI\VEN_144D&DEV_A80C\narrow", @"PCI\VEN_1022&DEV_1483\rp11", 4, 2));
        var report = LinkService.Evaluate(devices, Adlx(4, 16), NoUsb, "", "");

        Assert.Multiple(() =>
        {
            Assert.That(report.Chipset.Status, Is.EqualTo(LinkStatus.Check));
            Assert.That(report.Chipset.FastDevices, Is.EqualTo(2));
            Assert.That(report.Chipset.DevicesGBps, Is.EqualTo(LinkService.GBps(3, 4) + LinkService.GBps(4, 4)).Within(0.001));
            Assert.That(Find(report, "narrow").Status, Is.EqualTo(LinkStatus.Bad));
            Assert.That(report.Devices[0].Status, Is.EqualTo(LinkStatus.Bad), "Worst first");
        });
    }

    [Test]
    public void IntelPchRootPortsAreChipsetAndOthersAreCpu()
    {
        List<PnpDeviceInfo> devices =
        [
            Pci(RootComplex, @"HTREE\ROOT\0"),
            Pci(@"PCI\VEN_8086&DEV_7A04\espi", RootComplex, address: 31 << 16),
            Pci(@"PCI\VEN_8086&DEV_A70D\peg", RootComplex, address: 1 << 16),
            Pci(@"PCI\VEN_10DE&DEV_2684\gpu", @"PCI\VEN_8086&DEV_A70D\peg", "Display", "NVIDIA GeForce RTX 4090", 1, 0, 1, 16, 4, 16),
            Pci(@"PCI\VEN_8086&DEV_7A38\pch1", RootComplex, address: 28 << 16),
            Nvme(@"PCI\VEN_144D&DEV_A80C\ssd", @"PCI\VEN_8086&DEV_7A38\pch1", 4, 4),
            Pci(@"PCI\VEN_8086&DEV_7A39\pch2", RootComplex, address: 28 << 16 | 1),
            Pci(@"PCI\VEN_10DE&DEV_2786\gpu2", @"PCI\VEN_8086&DEV_7A39\pch2", "Display", "NVIDIA GeForce RTX 4070", 2, 0, 4, 4, 4, 16)
        ];
        var report = LinkService.Evaluate(devices, new Dictionary<string, (int, int)>(), NoUsb, "ROG STRIX Z790-E GAMING WIFI", "13th Gen Intel(R) Core(TM) i9-13900K");

        Assert.Multiple(() =>
        {
            Assert.That(Find(report, "ssd").Attach, Is.EqualTo(LinkAttach.Chipset));
            Assert.That(Find(report, "\\gpu").Attach, Is.EqualTo(LinkAttach.Cpu));
            Assert.That(Find(report, "\\gpu").Advice, Is.EqualTo("GpuGenLower"), "Idle GPUs drop to gen 1");
            Assert.That(Find(report, "gpu2").Advice, Is.EqualTo("GpuOnChipset"));
            Assert.That(report.Chipset.Chipset, Is.EqualTo(new ChipsetInfo("Z790", 4, 8, true)));
        });
    }

    [TestCase("11th Gen Intel(R) Core(TM) i7-11700K", 8)]
    [TestCase("Intel(R) Core(TM) i9-10900K CPU", 4)]
    public void Z590UplinkDependsOnTheCpu(string cpu, int lanes) =>
        Assert.That(LinkService.DetectChipset([Pci(@"PCI\VEN_8086&DEV_4385\espi", RootComplex)], "", cpu)?.Lanes, Is.EqualTo(lanes));

    [TestCase("B650 AORUS ELITE AX", "B650")]
    [TestCase("ROG CROSSHAIR X670E HERO", "X670E")]
    [TestCase("PRO B850-P WIFI (MS-7E56)", "B850")]
    [TestCase("ROG CROSSHAIR X870E HERO", "X870E")]
    [TestCase("ProArt Something", "AMD 600 / 800")]
    public void PromontoryChipsetNameComesFromTheBoard(string board, string expected) =>
        Assert.That(LinkService.DetectChipset([Pci(@"PCI\VEN_1022&DEV_43F4\up", RootComplex)], board, "")?.Name, Is.EqualTo(expected));

    private static PnpDeviceInfo Usb(string id, string parent, int port, string cls = "USB", string busName = "", string service = "") =>
        new(id, "", cls, "OK", true, 0, parent, service, "") { Address = port, BusName = busName };

    [TestCase("ASMedia", "ASM107x", "ASMedia · ASM107x")]
    [TestCase("ASMedia", "ASMedia Hub", "ASMedia Hub")]
    [TestCase("", "", "ASM107x")]
    public void UsbNamesUseManufacturerAndProductDescriptorsWithPnpFallback(string manufacturer, string product, string expected)
    {
        const string root = @"USB\ROOT_HUB30\test";
        List<PnpDeviceInfo> devices =
        [
            Pci(@"PCI\VEN_1022\xhci", RootComplex, "USB"),
            Usb(root, @"PCI\VEN_1022\xhci", 0),
            Usb(@"USB\VID_174C&PID_2074\test", root, 1, busName: "ASM107x", service: "USBHUB3")
        ];
        var speed = new UsbPortSpeed(2, true, false) { Manufacturer = manufacturer, Product = product };
        var check = LinkService.Evaluate(devices, new Dictionary<string, (int, int)>(),
            new Dictionary<(string, int), UsbPortSpeed> { [(root, 1)] = speed }, "", "").Usb.Single();
        Assert.That(check.Name, Is.EqualTo(expected));
    }

    [Test]
    public void Usb2CompanionHubIsNotReportedAsASlowPeripheral()
    {
        const string root = @"USB\ROOT_HUB30\test";
        const string hub = @"USB\VID_174C&PID_2074\test";
        List<PnpDeviceInfo> devices =
        [
            Pci(@"PCI\VEN_1022\xhci", RootComplex, "USB"),
            Usb(root, @"PCI\VEN_1022\xhci", 0),
            Usb(hub, root, 1, busName: "ASM107x", service: "USBHUB3")
        ];
        var check = LinkService.Evaluate(devices, new Dictionary<string, (int, int)>(),
            new Dictionary<(string, int), UsbPortSpeed> { [(root, 1)] = new(2, true, false) }, "", "").Usb.Single();

        Assert.Multiple(() =>
        {
            Assert.That(check.Kind, Is.EqualTo(UsbKind.Hub));
            Assert.That(check.Status, Is.EqualTo(LinkStatus.Ok));
            Assert.That(check.Advice, Is.EqualTo("UsbHub"), "A USB 3 hub also exposes a USB 2 hub; speed alone cannot prove a bad connection");
        });
    }

    [Test]
    public void UsbFlagsSlowPortsInputBehindHubsAndSharedControllers()
    {
        var devices = X570Pc();
        const string root = @"USB\ROOT_HUB30\chipset";
        devices.AddRange(
        [
            Pci(@"PCI\VEN_1022&DEV_149C\xhci", @"PCI\VEN_1022&DEV_57A3\down", "USB", "AMD USB 3.10 eXtensible Host Controller", 43, 1),
            Usb(root, @"PCI\VEN_1022&DEV_149C\xhci", 0),
            // The StreamCam on Lorenzo's PC: SuperSpeed capable, running at high speed.
            Usb(@"USB\VID_046D&PID_0893\cam", root, 7, busName: "Logitech StreamCam", service: "usbccgp"),
            Usb(@"USB\VID_046D&PID_0893&MI_00\cam0", @"USB\VID_046D&PID_0893\cam", 0, "Camera", service: "usbvideo"),
            Usb(@"USB\VID_05E3&PID_0610\hub", root, 4, busName: "USB2.0 Hub", service: "USBHUB3"),
            Usb(@"USB\VID_046D&PID_C54D\rx", @"USB\VID_05E3&PID_0610\hub", 1, busName: "USB Receiver", service: "usbccgp"),
            Usb(@"USB\VID_046D&PID_C54D&MI_00\rx0", @"USB\VID_046D&PID_C54D\rx", 0, "HIDClass"),
            Usb(@"HID\VID_046D&PID_C54D&MI_00\mouse", @"USB\VID_046D&PID_C54D&MI_00\rx0", 0, "Mouse"),
            // A receiver's virtual children are not on a hub port.
            Usb(@"USB\VID_046D&PID_C54D&MI_02\virtual", @"USB\VID_046D&PID_C54D\rx", 3),
            Usb(@"USB\VID_0BC2&PID_2344\disk", root, 2, busName: "Expansion HDD", service: "UASPStor"),
            // Two drives behind the same external hub share its uplink; the HDD on its own root port does not.
            Usb(@"USB\VID_0781&PID_5583\stick1", @"USB\VID_05E3&PID_0610\hub", 2, busName: "Ultra Fit", service: "USBSTOR"),
            Usb(@"USB\VID_0781&PID_5583\stick2", @"USB\VID_05E3&PID_0610\hub", 3, busName: "Cruzer", service: "USBSTOR")
        ]);
        Dictionary<(string Hub, int Port), UsbPortSpeed> speeds = new()
        {
            [(root, 7)] = new(2, true, false),
            [(root, 4)] = new(2, false, false),
            [(@"USB\VID_05E3&PID_0610\hub", 1)] = new(1, false, false),
            [(root, 2)] = new(3, true, false)
        };
        var usb = LinkService.Evaluate(devices, Adlx(4, 16), speeds, "", "").Usb.ToDictionary(check => check.Name);

        Assert.Multiple(() =>
        {
            Assert.That(usb.Keys, Is.EquivalentTo(new[] { "Logitech StreamCam", "USB2.0 Hub", "USB Receiver", "Expansion HDD", "Ultra Fit", "Cruzer" }));
            Assert.That(usb["Ultra Fit"].Advice, Is.EqualTo("UsbSharedHub"));
            Assert.That(usb["Ultra Fit"].Args, Is.EqualTo(new object[] { "Cruzer" }));
            Assert.That(usb["Logitech StreamCam"].Kind, Is.EqualTo(UsbKind.Video));
            Assert.That(usb["Logitech StreamCam"].Status, Is.EqualTo(LinkStatus.Bad), "Camera on USB 2 although it supports USB 3");
            Assert.That(usb["Logitech StreamCam"].Advice, Is.EqualTo("UsbSlowPort"));
            Assert.That(usb["USB Receiver"].Kind, Is.EqualTo(UsbKind.Input));
            Assert.That(usb["USB Receiver"].Advice, Is.EqualTo("UsbInputHub"));
            Assert.That(usb["USB Receiver"].Hubs, Is.EqualTo(1));
            Assert.That(usb["USB2.0 Hub"].Status, Is.EqualTo(LinkStatus.Ok));
            Assert.That(usb["Expansion HDD"].Advice, Is.EqualTo("UsbOk"), "Same controller as the camera, but its own root port");
            Assert.That(usb["Expansion HDD"].Attach, Is.EqualTo(LinkAttach.Chipset));
            Assert.That(usb["Expansion HDD"].LinkText, Is.EqualTo("USB 3 · 5 Gb/s"));
        });
    }
}
