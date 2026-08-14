using NUnit.Framework;
using TweakHub.Models;
using TweakHub.Services;
using TweakHub.Views;

namespace TweakHub.Tests;

public class ShortcutServiceTests
{
    [Test]
    public void ExternalToolCatalogueUsesOnlyCuratedWingetIdsOrHttpsPages()
    {
        var service = ShortcutService.Instance;
        service.Initialize();
        var tools = service.ExternalTools.Where(tool => !tool.IsCustom).ToList();
        var categories = new[]
        {
            "System Utilities", "CPU & Memory", "Firmware & Power", "Monitoring & Diagnostics",
            "GPU & Display", "Gaming & Input", "Storage & USB", "Network", "Audio",
            "Benchmarks & Stability", "AI Tools", "Development & DevOps", "Recovery & Forensics",
            "OSINT & Security"
        };

        Assert.That(tools, Has.Count.InRange(100, 160));
        Assert.That(tools.Select(tool => tool.Name), Is.Unique);
        Assert.That(tools.Select(tool => tool.Category).Distinct(), Is.SubsetOf(categories));
        Assert.That(tools.Select(tool => tool.Name), Does.Contain("Flow Launcher").And.Contain("FancyWM").And.Contain("Power Settings Explorer")
            .And.Contain("DRAM Calculator for Ryzen").And.Contain("OptiScaler Client").And.Contain("ZapTweaks")
            .And.Contain("Special K").And.Contain("CompactGUI").And.Contain("Upscale It").And.Contain("ShareX")
            .And.Contain("AME Wizard").And.Contain("AMD Chipset Drivers").And.Contain("NVIDIA Drivers")
            .And.Contain("NVIDIA App").And.Contain("AMD Graphics Drivers").And.Contain("AMD Cleanup Utility")
            .And.Contain("ThrottleStop").And.Contain("Visual C++ Redistributable Runtimes All-in-One")
            .And.Contain("Arduino IDE").And.Contain("Docker Desktop").And.Contain("Podman Desktop")
            .And.Contain("Windows File Recovery").And.Contain("TestDisk & PhotoRec").And.Contain("Autopsy")
            .And.Contain("ExifTool").And.Contain("SpiderFoot").And.Contain("Sherlock").And.Contain("OWASP Amass")
            .And.Contain("UEFITool").And.Contain("Nmap").And.Contain("WinSCP").And.Contain("Rufus").And.Contain("Ventoy"));
        Assert.That(tools.Single(tool => tool.Name == "ThrottleStop").DownloadUrl,
            Is.EqualTo("https://www.techpowerup.com/download/techpowerup-throttlestop/"));
        Assert.That(tools.Single(tool => tool.Name == "AMD Cleanup Utility").DownloadUrl,
            Is.EqualTo("https://drivers.amd.com/drivers/amdcleanuputility.exe"));
        Assert.That(tools.Single(tool => tool.Name == "Power Settings Explorer").DownloadUrl,
            Is.EqualTo("https://www.mediafire.com/file/wt37sbsejk7iepm/PowerSettingsExplorer.zip/file"));
        Assert.That(tools.Where(tool => tool.WingetId.Length > 0).Select(tool => tool.WingetId), Is.Unique);
        Assert.That(tools, Has.All.Matches<ExternalTool>(tool =>
        {
            var hasWingetId = tool.WingetId.Length > 0;
            var hasHttpsUrl = Uri.TryCreate(tool.DownloadUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
            var safePage = !tool.DownloadUrl.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)
                           && !tool.DownloadUrl.Contains("get.activated.win", StringComparison.OrdinalIgnoreCase);
            return hasWingetId ^ hasHttpsUrl && safePage;
        }));
    }

    [Test]
    public void CategoryMetadataProvidesSharedOrderingIconsAndLocalization()
    {
        Assert.That(ShortcutService.CategoryOrder("System Utilities"), Is.LessThan(ShortcutService.CategoryOrder("AI Tools")));
        Assert.That(ShortcutService.CategoryOrder("AI Tools"), Is.LessThan(ShortcutService.CategoryOrder("Development & DevOps")));
        Assert.That(ShortcutService.CategoryIcon("Network"), Is.EqualTo("\uE968"));
        Assert.That(ShortcutService.LocalizeCategory("Custom category"), Is.EqualTo("Custom category"));
    }

    [Test]
    public void QuickAccessIncludesProfessionalWindowsConsolesWithArguments()
    {
        var service = ShortcutService.Instance;
        service.Initialize();

        Assert.Multiple(() =>
        {
            Assert.That(service.SystemShortcuts.Single(shortcut => shortcut.Name == "Group Policy Editor").Command,
                Is.EqualTo("gpedit.msc"));
            Assert.That(service.SystemShortcuts.Single(shortcut => shortcut.Name == "Task Scheduler").Command,
                Is.EqualTo("taskschd.msc"));
            Assert.That(service.SystemShortcuts.Single(shortcut => shortcut.Name == "Reliability Monitor").Arguments,
                Is.EqualTo("/rel"));
            Assert.That(service.SystemShortcuts.Single(shortcut => shortcut.Name == "Indexing Options").Arguments,
                Is.EqualTo("/name Microsoft.IndexingOptions"));
            Assert.That(service.SystemShortcuts.Select(shortcut => shortcut.Name),
                Does.Contain("Computer Management").And.Contain("Performance Monitor")
                    .And.Contain("Windows Defender Firewall with Advanced Security")
                    .And.Contain("Local Security Policy").And.Contain("Local Users and Groups")
                    .And.Contain("Current User Certificates").And.Contain("Local Computer Certificates"));
        });
    }

    [Test]
    public void ExternalToolFilterHandlesFavoritesAndSearchFields()
    {
        ExternalTool[] tools =
        [
            new() { Name = "Alpha", Description = "Hardware monitor", Category = "Network", IsFavorite = true },
            new() { Name = "Beta", Description = "Packet inspector", Category = "Storage & USB" },
            new() { Name = "Gamma", Description = "Launcher", Category = "Audio" }
        ];

        Assert.Multiple(() =>
        {
            Assert.That(ExternalToolsPage.FilterTools(tools, "", true).Select(tool => tool.Name), Is.EqualTo(["Alpha"]));
            Assert.That(ExternalToolsPage.FilterTools(tools, "ALPHA", false).Select(tool => tool.Name), Is.EqualTo(["Alpha"]));
            Assert.That(ExternalToolsPage.FilterTools(tools, "packet", false).Select(tool => tool.Name), Is.EqualTo(["Beta"]));
            Assert.That(ExternalToolsPage.FilterTools(tools, "storage", false).Select(tool => tool.Name), Is.EqualTo(["Beta"]));
            Assert.That(ExternalToolsPage.FilterTools(tools, "missing", false), Is.Empty);
        });
    }
}
