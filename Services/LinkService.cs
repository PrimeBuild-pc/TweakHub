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

public sealed record LinkReport(string Board, string Cpu, ChipsetCheck Chipset, IReadOnlyList<LinkCheck> Devices);

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

    public static ChipsetInfo? DetectChipset(IReadOnlyList<PnpDeviceInfo> devices, string board, string cpu)
    {
        var name = ChipsetName.Match(board) is { Success: true } match ? match.Groups[1].Value.ToUpperInvariant() : null;
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
        string board, string cpu)
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

        var chipset = DetectChipset(devices, board, cpu);
        var fast = checks.Where(check => check.Attach == LinkAttach.Chipset && check.Gen > 0
            && (check.Kind is LinkKind.Gpu or LinkKind.Nvme || GBps(check.Gen, check.Width) >= 1.9)).ToList();
        var used = fast.Sum(check => GBps(check.Gen, check.Width));
        var chipsetCheck = chipset is null ? new ChipsetCheck(null, fast.Count, used, LinkStatus.Unknown, "ChipsetUnknown")
            : fast.Count >= 2 && used > chipset.GBps ? new ChipsetCheck(chipset, fast.Count, used, LinkStatus.Check, "ChipsetShared")
            : new ChipsetCheck(chipset, fast.Count, used, LinkStatus.Ok, fast.Count == 0 ? "ChipsetIdle" : "ChipsetOk");

        return new LinkReport(board, cpu, chipsetCheck, checks.OrderBy(check => check.Status).ThenBy(check => check.Kind)
            .ThenBy(check => check.Name, StringComparer.CurrentCultureIgnoreCase).ToList());
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
        var board = RegistryService.Instance.GetRegistryValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct") as string ?? string.Empty;
        var cpu = RegistryService.Instance.GetRegistryValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString") as string ?? string.Empty;
        return Evaluate(devices, vendorLinks, board.Trim(), cpu.Trim());
    }
}
