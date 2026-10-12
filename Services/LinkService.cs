using System.Globalization;
using System.Text.RegularExpressions;
using TweakHub.Localization;

namespace TweakHub.Services;

public enum LinkAttach { Cpu, Chipset, Unknown }

public enum LinkKind { Gpu, Nvme, Network, Other }

/// <summary>Badge colour: green, yellow (check or improve), red (losing performance), grey (cannot be read).</summary>
public enum LinkStatus { Bad, Check, Unknown, Ok }

public sealed record LinkCheck(PnpDeviceInfo Device, LinkKind Kind, LinkAttach Attach, PnpDeviceInfo? RootPort,
    int Gen, int Width, int MaxGen, int MaxWidth, LinkStatus Status, string Advice, object[] Args)
{
    /// <summary>The drive model for NVMe controllers, which Windows names "Standard NVM Express Controller".</summary>
    public string Name { get; init; } = Device.DisplayName;
    /// <summary>GPU-Z style: what the device supports @ what it is running at.</summary>
    public string LinkText => Gen == 0 ? $"PCIe x{MaxWidth} {LinkService.GenText(MaxGen)} @ ?"
        : $"PCIe x{MaxWidth} {LinkService.GenText(MaxGen)} @ x{Width} {LinkService.GenText(Gen)}";
    public string KindText => L.Get($"Devices:LinkKind{Kind}");
    public string StatusText => L.Get($"Devices:LinkStatus{Status}");
    // Chipset devices all hang off the same CPU port, so its address says nothing about the slot.
    public string AttachText => RootPort is null || Attach != LinkAttach.Cpu ? L.Get($"Devices:Attach{Attach}")
        : $"{L.Get($"Devices:Attach{Attach}")} · {L.Format("Devices:RootPort", LinkService.PciAddress(RootPort))}";
    public string AdviceText => L.Format($"Devices:Link{Advice}", Args);
    /// <summary>What the motherboard's manual adds: lane sharing, faster or CPU-wired slots. Empty for unknown boards.</summary>
    public string BoardHint { get; init; } = string.Empty;
}

/// <summary>Uplink between CPU and chipset: Intel calls it DMI, it is a PCIe link with the same per-lane speed.</summary>
public sealed record ChipsetInfo(string Name, int Gen, int Lanes, bool Dmi)
{
    public double GBps => LinkService.GBps(Gen, Lanes);
    public string UplinkText => $"{(Dmi ? "DMI" : "PCIe")} {LinkService.GenText(Gen)} x{Lanes}";
}

public sealed record ChipsetCheck(ChipsetInfo? Chipset, int FastDevices, double DevicesGBps, LinkStatus Status, string Advice)
{
    public string StatusText => L.Get($"Devices:LinkStatus{Status}");
    public string AdviceText => Chipset is null ? L.Get($"Devices:Link{Advice}")
        : L.Format($"Devices:Link{Advice}", FastDevices, DevicesGBps, Chipset.GBps, Chipset.UplinkText);
}

public enum UsbKind { Hub, Storage, Video, Input, Audio, Other }

public sealed record UsbCheck(PnpDeviceInfo Device, string Name, UsbKind Kind, PnpDeviceInfo? Controller, LinkAttach Attach,
    UsbPortSpeed? Speed, int Hubs, LinkStatus Status, string Advice, object[] Args)
{
    public string LinkText => Speed?.Speed switch
    {
        0 => "USB 1.0 · 1.5 Mb/s",
        1 => "USB 1.1 · 12 Mb/s",
        2 => "USB 2.0 · 480 Mb/s",
        3 => Speed.SuperSpeedPlus ? "USB 3 · 10 Gb/s" : "USB 3 · 5 Gb/s",
        _ => "USB"
    };
    public string KindText => L.Get($"Devices:UsbKind{Kind}");
    public string StatusText => L.Get($"Devices:LinkStatus{Status}");
    public string AttachText => Controller is null ? L.Get($"Devices:Attach{Attach}")
        : $"{Controller.DisplayName} ({LinkService.PciAddress(Controller)}) · {L.Get($"Devices:Attach{Attach}")}";
    public string AdviceText => L.Format($"Devices:Link{Advice}", Args);
    public string BoardHint => string.Empty;
}

public sealed record LinkReport(string Board, string Cpu, ChipsetCheck Chipset, IReadOnlyList<LinkCheck> Devices, IReadOnlyList<UsbCheck> Usb)
{
    public BoardInfo? BoardInfo { get; init; }
}

public static class LinkService
{
    private static readonly Regex VendorDevice = new(@"(?i)VEN_([0-9A-F]{4})&DEV_([0-9A-F]{4})");

    internal static (int Vendor, int Device) Ids(string instanceId) => VendorDevice.Match(instanceId) is { Success: true } match
        ? (int.Parse(match.Groups[1].Value, NumberStyles.HexNumber), int.Parse(match.Groups[2].Value, NumberStyles.HexNumber))
        : (0, 0);

    private const int Amd = 0x1022, AmdGraphics = 0x1002, Intel = 0x8086;

    // AMD chipset switch ports (pci.ids): 300, 400 (B450/X470), 500 (B550/A520), X570, Promontory 21 (600 and 800 series).
    private static readonly HashSet<int> AmdChipsetBridges = [0x43B0, 0x43B4, 0x43C6, 0x43C7, 0x43E9, 0x43EA, 0x57AD, 0x57A3, 0x57A4, 0x43F4, 0x43F5];

    // Intel PCH root ports (pci.ids) for the 400, 500, 600, 700 and 800 series chipsets; every other root port is the CPU's.
    private static readonly (int First, int Last)[] IntelPchRootPorts = [(0x06AC, 0x06C3), (0x43B0, 0x43C7), (0x7AB0, 0x7ACB), (0x7A30, 0x7A4B), (0x7F30, 0x7F47)];

    // Intel eSPI/LPC controller → chipset (pci.ids).
    private static readonly Dictionary<int, string> IntelChipsets = new()
    {
        [0x0685] = "Z490", [0x0684] = "H470", [0xA3C8] = "B460", [0x4385] = "Z590", [0x4386] = "H570", [0x4387] = "B560",
        [0x7A84] = "Z690", [0x7A85] = "H670", [0x7A86] = "B660", [0x7A04] = "Z790", [0x7A05] = "H770", [0x7A06] = "B760",
        [0x7F04] = "Z890", [0x7F06] = "B860"
    };

    // Radeon cards behind their own switch report an internal x16 link; these are narrower (Navi 23 and 33: x8, Navi 24: x4).
    private static readonly Dictionary<int, int> NarrowRadeons = new()
    {
        [0x73E0] = 8, [0x73E1] = 8, [0x73E3] = 8, [0x73EF] = 8, [0x73FF] = 8,
        [0x7480] = 8, [0x7489] = 8, [0x7499] = 8, [0x749F] = 8,
        [0x7421] = 4, [0x7422] = 4, [0x7423] = 4, [0x7424] = 4, [0x743F] = 4
    };

    private static readonly Regex ChipsetName = new(@"(?i)\b([ABXHZ][3-9][0-9]0E?)\b");

    public static string GenText(int gen) => gen switch { 1 => "1.1", > 1 => $"{gen}.0", _ => "?" };

    /// <summary>Usable GB/s of a link: per-lane rate after line encoding (8b/10b up to gen 2, 128b/130b after).</summary>
    public static double GBps(int gen, int lanes) => lanes * gen switch { 1 => 0.25, 2 => 0.5, 3 => 0.985, 4 => 1.969, 5 => 3.938, 6 => 7.563, _ => 0 };

    public static string PciAddress(PnpDeviceInfo device) => $"{device.BusNumber}:{device.Address >> 16}.{device.Address & 0xFFFF}";

    private static bool IsPci(PnpDeviceInfo device) => device.Id.StartsWith(@"PCI\", StringComparison.OrdinalIgnoreCase);

    /// <summary>Walks up to the root port: CPU unless a chipset switch or a PCH root port is on the way.</summary>
    internal static (LinkAttach Attach, PnpDeviceInfo? RootPort) Classify(PnpDeviceInfo device, IReadOnlyDictionary<string, PnpDeviceInfo> byId)
    {
        PnpDeviceInfo? root = null;
        var chipset = false;
        var current = device;
        for (var hops = 0; hops < 16 && byId.TryGetValue(current.Parent, out var parent) && IsPci(parent); hops++, current = parent)
        {
            root = parent;
            var (vendor, id) = Ids(parent.Id);
            chipset |= vendor == Amd && AmdChipsetBridges.Contains(id);
        }
        if (root is null) return (LinkAttach.Unknown, null);
        var (rootVendor, rootId) = Ids(root.Id);
        chipset |= rootVendor == Intel && IntelPchRootPorts.Any(range => rootId >= range.First && rootId <= range.Last);
        return (chipset ? LinkAttach.Chipset : LinkAttach.Cpu, root);
    }

    private static LinkKind? KindOf(PnpDeviceInfo device)
    {
        var vendor = Ids(device.Id).Vendor;
        return device.Class switch
        {
            "Display" => LinkKind.Gpu,
            "SCSIAdapter" when Regex.IsMatch($"{device.Service}|{device.Name}", @"(?i)nvme|nvm express") => LinkKind.Nvme,
            "Net" => LinkKind.Network,
            // Add-in capture, sound, USB and storage cards; the platform's own controllers are not slot devices.
            "MEDIA" or "USB" or "HDC" or "SCSIAdapter" when vendor is not (Amd or Intel or AmdGraphics) => LinkKind.Other,
            _ => null
        };
    }

    public static ChipsetInfo? DetectChipset(IReadOnlyList<PnpDeviceInfo> devices, string board, string cpu, string? boardChipset = null)
    {
        var name = boardChipset ?? (ChipsetName.Match(board) is { Success: true } match ? match.Groups[1].Value.ToUpperInvariant() : null);
        foreach (var device in devices.Where(IsPci))
        {
            var (vendor, id) = Ids(device.Id);
            if (vendor == Intel && IntelChipsets.TryGetValue(id, out var intel))
                return intel switch
                {
                    "Z490" or "H470" or "B460" or "B560" => new(intel, 3, 4, true),
                    // DMI x8 needs an 11th gen Core; a 10th gen on the same board runs x4.
                    "Z590" or "H570" => new(intel, 3, cpu.Contains("11th Gen", StringComparison.OrdinalIgnoreCase) ? 8 : 4, true),
                    "Z690" or "H670" or "Z790" or "H770" or "Z890" => new(intel, 4, 8, true),
                    _ => new(intel, 4, 4, true)
                };
            if (vendor != Amd) continue;
            switch (id)
            {
                // shortcut: APU and Ryzen 2000 detection by name only, AM4 non-G Cezanne (5500, 5600GT) still reads Gen4.
                case 0x57AD: return new("X570", cpu.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ? 3 : 4, 4, false);
                case 0x43E9: return new(name is "B550" or "A520" ? name : "B550 / A520", 3, 4, false);
                case 0x43F4: return new(name ?? "AMD 600 / 800", 4, 4, false);
                case 0x43C6: return new(name ?? "AMD 400", 3, 4, false);
                case 0x43B0: return new(name ?? "AMD 300", 3, 4, false);
            }
        }
        return null;
    }

    public static LinkReport Evaluate(IReadOnlyList<PnpDeviceInfo> devices, IReadOnlyDictionary<string, (int Gen, int Width)> vendorLinks,
        IReadOnlyDictionary<(string Hub, int Port), UsbPortSpeed> usbSpeeds, string board, string cpu)
    {
        var byId = devices.GroupBy(device => device.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var checks = new List<LinkCheck>();
        foreach (var device in devices)
        {
            // Other functions of a card (GPU audio, USB-C) share function 0's link.
            if (!device.Present || device.IsDisabled || device.MaxLinkWidth == 0 || (device.Address & 0xFFFF) != 0 || KindOf(device) is not { } kind) continue;
            var (attach, root) = Classify(device, byId);
            var disk = kind == LinkKind.Nvme ? devices.FirstOrDefault(child => child.Present && child.Class == "DiskDrive"
                && child.Parent.Equals(device.Id, StringComparison.OrdinalIgnoreCase)) : null;
            checks.Add(Check(device, kind, attach, root, byId, vendorLinks) with { Name = disk?.DisplayName ?? device.DisplayName });
        }

        var boardInfo = BoardCatalog.Find(board);
        if (boardInfo is not null)
        {
            var cpuDrives = checks.Count(check => check.Kind == LinkKind.Nvme && check.Attach == LinkAttach.Cpu);
            checks = checks.Select(check => ApplyBoard(check, boardInfo, cpuDrives)).ToList();
        }
        var chipset = DetectChipset(devices, board, cpu, boardInfo?.Chipset);
        var fast = checks.Where(check => check.Attach == LinkAttach.Chipset && check.Gen > 0
            && (check.Kind is LinkKind.Gpu or LinkKind.Nvme || GBps(check.Gen, check.Width) >= 1.9)).ToList();
        var used = fast.Sum(check => GBps(check.Gen, check.Width));
        var chipsetCheck = chipset is null ? new ChipsetCheck(null, fast.Count, used, LinkStatus.Unknown, "ChipsetUnknown")
            : fast.Count >= 2 && used > chipset.GBps ? new ChipsetCheck(chipset, fast.Count, used, LinkStatus.Check, "ChipsetShared")
            : new ChipsetCheck(chipset, fast.Count, used, LinkStatus.Ok, fast.Count == 0 ? "ChipsetIdle" : "ChipsetOk");

        return new LinkReport(board, cpu, chipsetCheck, checks.OrderBy(check => check.Status).ThenBy(check => check.Kind)
            .ThenBy(check => check.Name, StringComparer.CurrentCultureIgnoreCase).ToList(), EvaluateUsb(devices, byId, usbSpeeds)) { BoardInfo = boardInfo };
    }

    /// <summary>Refines a check with the manual: a lower generation that matches the slot is the board's limit, not a fault.</summary>
    internal static LinkCheck ApplyBoard(LinkCheck check, BoardInfo board, int cpuDrives)
    {
        var drive = check.Kind == LinkKind.Nvme;
        var slots = drive ? board.M2 : board.Slots;
        string Sharing() => string.Join("; ", board.Sharing.Where(rule => slots.Any(slot => rule.Affects.Contains(slot.Label, StringComparison.OrdinalIgnoreCase))).Select(rule => rule.Text));
        string Labels(IEnumerable<BoardSlot> list) => string.Join(", ", list.Select(slot =>
            board.Sharing.Where(rule => rule.When == slot.Label).Select(rule => rule.Text).ToList() is { Count: > 0 } costs
                ? $"{slot.Label} ({string.Join("; ", costs)})" : slot.Label));

        switch (check.Advice)
        {
            case "LanesMissing" or "GpuOnChipset" when Sharing() is { Length: > 0 } sharing:
                return check with { BoardHint = L.Format("Devices:HintSharing", sharing) };
            // Some M.2 slots are wired x2: the drive is fine, the slot is narrow.
            case "LanesMissing" when drive && board.M2.Any(slot => slot.Lanes == check.Width && slot.IsCpu == (check.Attach == LinkAttach.Cpu)):
                var wide = board.M2.Where(slot => slot.Lanes >= check.MaxWidth).ToList();
                return check with
                {
                    Status = LinkStatus.Check, Advice = "SlotLanes", Args = [check.Width],
                    BoardHint = wide.Count > 0 ? L.Format("Devices:HintWiderM2", check.MaxWidth, Labels(wide)) : string.Empty
                };
            case "GenLower" when drive:
                // A faster M.2 elsewhere on the board is advice; none at all means the board is the limit.
                if (board.M2.Where(slot => slot.Gen > check.Gen).ToList() is { Count: > 0 } faster)
                    return check with { BoardHint = L.Format("Devices:HintFasterM2", GenText(faster.Max(slot => slot.Gen)), Labels(faster)) };
                return board.M2.Length > 0 ? check with { Status = LinkStatus.Ok, Advice = "SlotLimit", Args = [GenText(check.Gen)] } : check;
            case "GpuGenLower" when board.MainSlot is { } main:
                if (check.Gen >= main.Gen) return check with { Status = LinkStatus.Ok, Advice = "SlotLimit", Args = [GenText(check.Gen)] };
                return main.Gen < check.MaxGen ? check with { BoardHint = L.Format("Devices:HintSlotGen", main.Label, GenText(main.Gen)) } : check;
            case "OkChipset" when drive && board.M2.Where(slot => slot.IsCpu).ToList() is { } cpuSlots && cpuSlots.Count > cpuDrives:
                return check with { BoardHint = L.Format("Devices:HintCpuM2", Labels(cpuSlots)) };
            default:
                return check;
        }
    }

    private static bool IsRootHub(PnpDeviceInfo device) => device.Id.StartsWith(@"USB\ROOT_HUB", StringComparison.OrdinalIgnoreCase);

    /// <summary>Physical USB devices: the nodes plugged into a hub port, not their interfaces or a receiver's virtual children.</summary>
    internal static IEnumerable<PnpDeviceInfo> UsbPortDevices(IReadOnlyList<PnpDeviceInfo> devices, IReadOnlyDictionary<string, PnpDeviceInfo> byId) =>
        devices.Where(device => device.Present && !device.IsDisabled && device.Address > 0
            && device.Id.StartsWith(@"USB\VID_", StringComparison.OrdinalIgnoreCase)
            && byId.TryGetValue(device.Parent, out var parent) && (IsRootHub(parent) || UsbService.IsHub(parent)));

    private static UsbKind UsbKindOf(PnpDeviceInfo device, ILookup<string, PnpDeviceInfo> children)
    {
        if (UsbService.IsHub(device)) return UsbKind.Hub;
        // Interfaces and their functions sit up to three levels below the device (composite → MI_xx → HID/disk).
        var nodes = new List<PnpDeviceInfo> { device };
        for (var level = 0; level < 3; level++) nodes.AddRange(nodes.SelectMany(node => children[node.Id]).Except(nodes).ToList());
        bool Has(Func<PnpDeviceInfo, bool> test) => nodes.Any(test);
        if (Has(node => node.Class == "DiskDrive" || node.Service is "USBSTOR" or "UASPStor")) return UsbKind.Storage;
        if (Has(node => node.Class is "Camera" or "Image" || node.Service.Equals("usbvideo", StringComparison.OrdinalIgnoreCase))) return UsbKind.Video;
        if (Has(node => node.Class is "Mouse" or "Keyboard")) return UsbKind.Input;
        return Has(node => node.Class is "MEDIA" or "AudioEndpoint") ? UsbKind.Audio : UsbKind.Other;
    }

    private static List<UsbCheck> EvaluateUsb(IReadOnlyList<PnpDeviceInfo> devices, IReadOnlyDictionary<string, PnpDeviceInfo> byId,
        IReadOnlyDictionary<(string Hub, int Port), UsbPortSpeed> usbSpeeds)
    {
        var children = devices.Where(device => device.Present).ToLookup(device => device.Parent, StringComparer.OrdinalIgnoreCase);
        var items = UsbPortDevices(devices, byId).Select(device =>
        {
            var hubs = 0;
            var current = device;
            for (var hops = 0; hops < 16 && byId.TryGetValue(current.Parent, out var parent) && !IsPci(parent); hops++, current = parent)
                if (UsbService.IsHub(parent)) hubs++;
            var controller = byId.GetValueOrDefault(current.Parent) is { } pci && IsPci(pci) ? pci : null;
            return (Device: device, Name: device.BusName.Length > 0 ? device.BusName : device.DisplayName, Kind: UsbKindOf(device, children),
                Controller: controller, Hubs: hubs, Speed: usbSpeeds.GetValueOrDefault((device.Parent, device.Address)));
        }).ToList();

        // xHCI gives every root port its own bandwidth; an external hub has one uplink for everything behind it.
        var heavy = items.Where(item => item.Kind is UsbKind.Storage or UsbKind.Video && byId.TryGetValue(item.Device.Parent, out var hub) && UsbService.IsHub(hub)).ToList();
        return items.Select(item =>
        {
            var attach = item.Controller is null ? LinkAttach.Unknown : Classify(item.Controller, byId).Attach;
            UsbCheck Result(LinkStatus status, string advice, params object[] args) =>
                new(item.Device, item.Name, item.Kind, item.Controller, attach, item.Speed, item.Hubs, status, advice, args);

            if (item.Speed is { SuperSpeedCapable: true, Speed: < 3 })
                return Result(item.Kind is UsbKind.Storage or UsbKind.Video ? LinkStatus.Bad : LinkStatus.Check, "UsbSlowPort");
            if (item.Kind == UsbKind.Input && item.Hubs > 0) return Result(LinkStatus.Check, "UsbInputHub", item.Hubs);
            var sharing = heavy.Where(other => other.Device != item.Device && other.Device.Parent == item.Device.Parent).ToList();
            if (heavy.Contains(item) && sharing.Count > 0)
                return Result(LinkStatus.Check, "UsbSharedHub", string.Join(", ", sharing.Select(other => other.Name)));
            return Result(item.Speed is null ? LinkStatus.Unknown : LinkStatus.Ok, item.Speed is null ? "UsbUnknown" : "UsbOk");
        }).OrderBy(check => check.Status).ThenBy(check => check.Kind).ThenBy(check => check.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static LinkCheck Check(PnpDeviceInfo device, LinkKind kind, LinkAttach attach, PnpDeviceInfo? root,
        IReadOnlyDictionary<string, PnpDeviceInfo> byId, IReadOnlyDictionary<string, (int Gen, int Width)> vendorLinks)
    {
        int gen = device.LinkGen, width = device.LinkWidth, maxGen = device.MaxLinkGen, maxWidth = device.MaxLinkWidth;
        var (vendor, id) = Ids(device.Id);
        // A GPU whose parent bridge has the GPU's own vendor sits behind the card's switch: Windows only sees the internal link.
        if (kind == LinkKind.Gpu && byId.TryGetValue(device.Parent, out var bridge) && IsPci(bridge) && Ids(bridge.Id).Vendor == vendor)
        {
            if (vendorLinks.TryGetValue(device.Id, out var real)) (gen, width) = real;
            else gen = width = 0;
            if (vendor == AmdGraphics && NarrowRadeons.TryGetValue(id, out var narrow)) maxWidth = narrow;
        }

        LinkCheck Result(LinkStatus status, string advice, params object[] args) =>
            new(device, kind, attach, root, gen, width, maxGen, maxWidth, status, advice, args);

        if (gen == 0) return Result(LinkStatus.Unknown, "Hidden");
        if (kind == LinkKind.Gpu && attach == LinkAttach.Chipset) return Result(LinkStatus.Bad, "GpuOnChipset");
        if (width < maxWidth) return Result(LinkStatus.Bad, "LanesMissing", width, maxWidth);
        if (gen < maxGen) return Result(LinkStatus.Check, kind == LinkKind.Gpu ? "GpuGenLower" : "GenLower", GenText(gen), GenText(maxGen));
        return Result(LinkStatus.Ok, attach == LinkAttach.Chipset && kind is LinkKind.Gpu or LinkKind.Nvme ? "OkChipset" : "Ok");
    }

    public static async Task<LinkReport> ScanAsync(IReadOnlyList<PnpDeviceInfo> devices)
    {
        var vendorLinks = await Task.Run(AdlxService.ReadLinks);
        var byId = devices.GroupBy(device => device.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var usbSpeeds = await Task.Run(() => UsbSpeedService.Read(UsbPortDevices(devices, byId).Select(device => (device.Parent, device.Address)).ToList()));
        var board = RegistryService.Instance.GetRegistryValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct") as string ?? string.Empty;
        var cpu = RegistryService.Instance.GetRegistryValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString") as string ?? string.Empty;
        return Evaluate(devices, vendorLinks, usbSpeeds, board.Trim(), cpu.Trim());
    }
}
