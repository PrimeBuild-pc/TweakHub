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

    [Test]
    public void LaunchHintShowsTerminalCommandAndQuotedExecutable()
    {
        var tool = new TweakHub.Models.ExternalTool
        {
            Name = "Example",
            WingetId = "Vendor.Example",
            TerminalCommand = "example.exe"
        };

        var hint = ToolDownloadService.BuildLaunchHint(tool, @"C:\Program Files\Example\example.exe");

        Assert.That(hint, Does.Contain("Terminal command: example.exe")
            .And.Contain("Executable: \"C:\\Program Files\\Example\\example.exe\""));
    }

    [Test]
    public void LaunchHintHasHonestFallbackWhenWingetExposesNoPath()
    {
        var tool = new TweakHub.Models.ExternalTool { Name = "Example", WingetId = "Vendor.Example" };
        Assert.That(ToolDownloadService.BuildLaunchHint(tool, null),
            Does.Contain("did not expose").And.Contain("winget list --id \"Vendor.Example\" --exact"));
    }
}
