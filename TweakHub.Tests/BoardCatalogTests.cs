using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class BoardCatalogTests
{
    private static readonly string[] Chipsets =
        ["B450", "B550", "X570", "B650", "B650E", "X670", "X670E", "B850", "X870", "X870E",
         "Z490", "B460", "Z590", "B560", "Z690", "B660", "Z790", "B760", "Z890", "B860"];

    [Test]
    public void EveryBoardIsPlausible()
    {
        var keys = new HashSet<string>();
        Assert.That(BoardCatalog.Boards, Is.Not.Empty);
        foreach (var board in BoardCatalog.Boards)
        {
            var labels = board.M2.Concat(board.Slots).Select(slot => slot.Label).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(Chipsets, Does.Contain(board.Chipset), board.Name);
                Assert.That(board.Source, Does.StartWith("https://"), board.Name);
                Assert.That(board.Match, Is.Not.Empty, board.Name);
                foreach (var match in board.Match) Assert.That(keys.Add(BoardCatalog.Normalize(match)), Is.True, $"{board.Name}: '{match}' is listed twice");
                Assert.That(board.M2, Is.Not.Empty, board.Name);
                Assert.That(board.MainSlot, Is.Not.Null, $"{board.Name}: no CPU x16 slot");
                Assert.That(labels, Is.Unique, board.Name);
                foreach (var slot in board.M2.Concat(board.Slots))
                {
                    Assert.That(slot.Source, Is.EqualTo("cpu").Or.EqualTo("chipset"), $"{board.Name} {slot.Label}");
                    Assert.That(slot.Gen, Is.InRange(2, 5), $"{board.Name} {slot.Label}");
                    Assert.That(new[] { 1, 2, 4, 8, 16 }, Does.Contain(slot.Lanes), $"{board.Name} {slot.Label}");
                }
                Assert.That(board.M2.Select(slot => slot.Lanes), Has.All.LessThanOrEqualTo(4), board.Name);
                foreach (var rule in board.Sharing)
                {
                    Assert.That(rule.When, Is.Not.Empty, board.Name);
                    Assert.That(rule.Affects, Is.Not.Empty, board.Name);
                    Assert.That(new[] { 0, 1, 2, 4, 8 }, Does.Contain(rule.Lanes), $"{board.Name} {rule.When}");
                }
            });
        }
    }

    [TestCase("MPG X570 GAMING EDGE WIFI (MS-7C37)", "MSI MPG X570 Gaming Edge WiFi")]
    [TestCase("mpg x570 gaming edge wifi", "MSI MPG X570 Gaming Edge WiFi")]
    [TestCase("MPG X570 GAMING EDGE", null)]
    [TestCase("", null)]
    public void BoardsMatchExactlyIgnoringCaseAndModelNumber(string product, string? expected) =>
        Assert.That(BoardCatalog.Find(product)?.Name, Is.EqualTo(expected));

    private static readonly BoardInfo Board = new(["TEST"], "Test board", "B650", "https://example.com",
        [new("M2_1", "cpu", 5, 4), new("M2_2", "cpu", 4, 4), new("M2_3", "chipset", 4, 4)],
        [new("PCIE_1", "cpu", 4, 16), new("PCIE_2", "chipset", 4, 4)],
        [new("M2_2", "PCIE_1", 8), new("M2_3", "SATA_1", 0)]);

    private static LinkCheck Check(LinkKind kind, LinkAttach attach, int gen, int width, int maxGen, int maxWidth, LinkStatus status, string advice) =>
        new(new PnpDeviceInfo(@"PCI\X", "X", "", "OK", true, 0, "", "", ""), kind, attach, null, gen, width, maxGen, maxWidth, status, advice, []);

    [Test]
    public void BoardAddsSharingFasterSlotsAndPlatformLimits()
    {
        var gpuX8 = LinkService.ApplyBoard(Check(LinkKind.Gpu, LinkAttach.Cpu, 4, 8, 4, 16, LinkStatus.Bad, "LanesMissing"), Board, 1);
        var nvmeGen3 = LinkService.ApplyBoard(Check(LinkKind.Nvme, LinkAttach.Chipset, 3, 4, 4, 4, LinkStatus.Check, "GenLower"), Board, 1);
        var gen5Gpu = LinkService.ApplyBoard(Check(LinkKind.Gpu, LinkAttach.Cpu, 4, 16, 5, 16, LinkStatus.Check, "GpuGenLower"), Board, 1);
        var idleGpu = LinkService.ApplyBoard(Check(LinkKind.Gpu, LinkAttach.Cpu, 1, 16, 4, 16, LinkStatus.Check, "GpuGenLower"), Board, 1);
        var chipsetDrive = LinkService.ApplyBoard(Check(LinkKind.Nvme, LinkAttach.Chipset, 4, 4, 4, 4, LinkStatus.Ok, "OkChipset"), Board, 1);
        var cpuFull = LinkService.ApplyBoard(Check(LinkKind.Nvme, LinkAttach.Chipset, 4, 4, 4, 4, LinkStatus.Ok, "OkChipset"), Board, 2);

        Assert.Multiple(() =>
        {
            Assert.That(gpuX8.BoardHint, Does.Contain("PCIE_1").And.Contain("M2_2"), "The rule that halves the GPU slot");
            Assert.That(nvmeGen3.Status, Is.EqualTo(LinkStatus.Check));
            Assert.That(nvmeGen3.BoardHint, Does.Contain("M2_1").And.Contain("M2_3"));
            Assert.That(gen5Gpu.Status, Is.EqualTo(LinkStatus.Ok), "A Gen5 card in a Gen4 main slot is the board's limit");
            Assert.That(gen5Gpu.Advice, Is.EqualTo("SlotLimit"));
            Assert.That(idleGpu.Status, Is.EqualTo(LinkStatus.Check), "Gen 1 on a Gen 4 slot is idle power saving, not the board");
            Assert.That(chipsetDrive.BoardHint, Does.Contain("M2_1").And.Contain("PCIE_1"), "Free CPU slots, with what M2_2 costs");
            Assert.That(cpuFull.BoardHint, Is.Empty, "Both CPU slots already hold a drive");
        });
    }

    [Test]
    public void DriveInAnX2SlotIsTheSlotsLimit()
    {
        BoardInfo board = new(["TEST"], "Test board", "B550", "https://example.com",
            [new("M2_1", "cpu", 4, 4), new("M2_2", "chipset", 3, 2)], [new("PCIE1", "cpu", 4, 16)], []);
        var x2 = LinkService.ApplyBoard(Check(LinkKind.Nvme, LinkAttach.Chipset, 3, 2, 3, 4, LinkStatus.Bad, "LanesMissing"), board, 0);

        Assert.Multiple(() =>
        {
            Assert.That(x2.Status, Is.EqualTo(LinkStatus.Check));
            Assert.That(x2.Advice, Is.EqualTo("SlotLanes"));
            Assert.That(x2.BoardHint, Does.Contain("M2_1"));
        });
    }
}
