using System.Text.Json;
using System.Text.RegularExpressions;
using TweakHub.Localization;

namespace TweakHub.Services;

/// <summary>An M.2 or x16-size PCIe slot as the manual lists it; Gen and Lanes are the best the slot runs with a supported CPU.</summary>
public sealed record BoardSlot(string Label, string Source, int Gen, int Lanes)
{
    public bool IsCpu => Source == "cpu";
    public string Text => $"{Label} · {L.Get(IsCpu ? "Devices:SourceCpu" : "Devices:SourceChipset")} · PCIe {LinkService.GenText(Gen)} x{Lanes}";
}

/// <summary>Using <see cref="When"/> takes lanes from <see cref="Affects"/>: Lanes 0 means disabled, otherwise the lanes left.</summary>
public sealed record BoardSharing(string When, string Affects, int Lanes)
{
    public string Text => Lanes == 0 ? L.Format("Devices:SharingDisables", When, Affects) : L.Format("Devices:SharingLanes", When, Affects, Lanes);
}

public sealed record BoardInfo(string[] Match, string Name, string Chipset, string Source, BoardSlot[] M2, BoardSlot[] Slots, BoardSharing[] Sharing)
{
    /// <summary>The primary x16 slot: listed first among the CPU slots.</summary>
    public BoardSlot? MainSlot => Slots.FirstOrDefault(slot => slot.IsCpu);
}

/// <summary>Motherboards from Resources/Boards.json, compiled from the manufacturers' manuals.</summary>
public static class BoardCatalog
{
    private sealed record BoardFile(BoardInfo[] Boards);

    private static readonly Lazy<BoardInfo[]> All = new(() =>
    {
        using var stream = typeof(BoardCatalog).Assembly.GetManifestResourceStream("TweakHub.Resources.Boards.json")
            ?? throw new InvalidOperationException("Boards.json is not embedded");
        return JsonSerializer.Deserialize<BoardFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!.Boards;
    });

    public static IReadOnlyList<BoardInfo> Boards => All.Value;

    /// <summary>Upper case letters and digits only, without "(MS-7C37)" or "(WI-FI)": the same board reads differently across BIOS versions.</summary>
    internal static string Normalize(string product) => Regex.Replace(Regex.Replace(product.ToUpperInvariant(), @"\([^)]*\)", ""), "[^A-Z0-9]", "");

    // Exact match only: "B550 AORUS ELITE" and "B550 AORUS ELITE AX V2" have different slots, so no hint beats a wrong one.
    public static BoardInfo? Find(string product)
    {
        var key = Normalize(product);
        return key.Length == 0 ? null : Boards.FirstOrDefault(board => board.Match.Any(match => Normalize(match) == key));
    }
}
