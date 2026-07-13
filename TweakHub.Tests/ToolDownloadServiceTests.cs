using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class ToolDownloadServiceTests
{
    [TestCase("Downloading 42%", 42)]
    [TestCase("100% complete", 100)]
    [TestCase("250%", 100)]
    [TestCase("No progress", -1)]
    public void ParsesWingetProgress(string output, int expected) =>
        Assert.That(ToolDownloadService.TryParsePercent(output), Is.EqualTo(expected));
}
