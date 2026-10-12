using System.Windows.Controls;
using System.Windows.Input;
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
            "OSINT & Security", "Latency & Input"
        };

        Assert.That(tools, Has.Count.InRange(100, 200));
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

    [TestCase("WPD", "https://wpd.app/", "OSINT & Security")]
    [TestCase("TapMap", "https://github.com/olalie/tapmap", "OSINT & Security")]
    [TestCase("SecurityToolkit", "https://github.com/kobasky17/SecurityToolkit", "OSINT & Security")]
    [TestCase("EmailOSINT", "https://emailosint.org/", "OSINT & Security")]
    [TestCase("Sigcheck", "", "OSINT & Security", "Microsoft.Sysinternals.Sigcheck")]
    [TestCase("Microsoft Safety Scanner", "https://learn.microsoft.com/defender-endpoint/safety-scanner-download", "OSINT & Security")]
    [TestCase("mVolt", "https://github.com/b00nz/mVolt", "GPU & Display")]
    [TestCase("Sparkle", "https://github.com/thedogecraft/sparkle", "System Utilities")]
    [TestCase("Sergei Strelec WinPE", "https://sergeistrelec.name/winpe-10-8-sergei-strelec-english/", "Recovery & Forensics")]
    [TestCase("Hiren's BootCD PE", "https://www.hirensbootcd.org/", "Recovery & Forensics")]
    [TestCase("SCSKiller", "https://github.com/BlueHeisenberg/SCSKiller/releases/latest", "Gaming & Input")]
    [TestCase("OpenBoardView", "https://github.com/OpenBoardView/OpenBoardView/releases/latest", "Monitoring & Diagnostics")]
    [TestCase("GoInterruptPolicy", "https://github.com/spddl/GoInterruptPolicy", "Latency & Input")]
    [TestCase("USB Latency Analyzer (CPU Direct)", "https://tools.mariusheier.com/cpudirect.html", "Latency & Input")]
    [TestCase("DeepPoll", "https://tools.mariusheier.com/deeppoll.html", "Latency & Input")]
    [TestCase("DeepLog", "https://tools.mariusheier.com/deeplog.html", "Latency & Input")]
    [TestCase("USB Rig Profiles", "https://tools.mariusheier.com/rig.html", "Latency & Input")]
    [TestCase("USB HID Analyzer", "https://tools.mariusheier.com/poll_checker.html", "Latency & Input")]
    [TestCase("hidusbf", "https://github.com/LordOfMice/hidusbf", "Latency & Input")]
    [TestCase("DualShock Tools", "https://dualshock-tools.github.io/", "Latency & Input")]
    [TestCase("Battle Beaver controller overclocking", "https://battlebeavercustoms.com/pages/overclocking", "Latency & Input")]
    [TestCase("PowerPlanSettingsEditor", "https://github.com/zoicware/PowerPlanSettingsEditor", "Latency & Input")]
    [TestCase("Advanced Power Options guide", "https://github.com/IRedDragonICY/Advanced-Power-Option-Windows", "Latency & Input")]
    [TestCase("RepairBadTweaks", "https://github.com/zoicware/RepairBadTweaks", "Latency & Input")]
    [TestCase("Follow the White Rabbit", "https://start.me/p/aNrBJX/follow-the-white-rabbit", "OSINT & Security")]
    [TestCase("Triage", "https://tria.ge/", "OSINT & Security")]
    [TestCase("User Scanner", "https://github.com/kaifcodec/user-scanner", "OSINT & Security")]
    [TestCase("Awesome Privacy", "https://github.com/pluja/awesome-privacy", "OSINT & Security")]
    [TestCase("privacy.sexy", "", "OSINT & Security", "undergroundwires.privacy.sexy")]
    [TestCase("Pi-hole", "https://github.com/pi-hole/pi-hole", "OSINT & Security")]
    [TestCase("Big-Ass Data Broker Opt-Out List", "https://github.com/yaelwrites/big-ass-data-broker-opt-out-list", "OSINT & Security")]
    [TestCase("JustDeleteMe", "https://backgroundchecks.org/justdeleteme/", "OSINT & Security")]
    [TestCase("ungoogled-chromium", "", "OSINT & Security", "eloston.ungoogled-chromium")]
    [TestCase("Personal Security Checklist", "https://github.com/lissy93/personal-security-checklist", "OSINT & Security")]
    [TestCase("ffuf", "https://github.com/ffuf/ffuf", "OSINT & Security")]
    [TestCase("Xalgorix", "https://github.com/xalgorix/xalgorix", "OSINT & Security")]
    [TestCase("hackingtool", "https://github.com/Z4nzu/hackingtool", "OSINT & Security")]
    [TestCase("Have I Been Pwned", "https://haveibeenpwned.com/", "OSINT & Security")]
    [TestCase("Cover Your Tracks", "https://coveryourtracks.eff.org/", "OSINT & Security")]
    [TestCase("Privacy Guides", "https://www.privacyguides.org/", "OSINT & Security")]
    [TestCase("urlscan.io", "https://urlscan.io/", "OSINT & Security")]
    [TestCase("Nuclei", "https://github.com/projectdiscovery/nuclei", "OSINT & Security")]
    public void NewReferencesAreUniqueAndNeverRunScripts(string name, string url, string category, string wingetId = "")
    {
        var service = ShortcutService.Instance;
        service.Initialize();
        var tool = service.ExternalTools.Single(tool => tool.Name == name);

        Assert.Multiple(() =>
        {
            Assert.That(tool.DownloadUrl, Is.EqualTo(url));
            Assert.That(service.ExternalTools.Count(tool => url.Length > 0 ? tool.DownloadUrl == url : tool.WingetId == wingetId), Is.EqualTo(1));
            Assert.That(tool.Category, Is.EqualTo(category));
            Assert.That(tool.WingetId, Is.EqualTo(wingetId));
            Assert.That(tool.PowerShellCommand, Is.Empty);
            Assert.That(tool.RequiresAdministrator, Is.False);
            Assert.That(tool.IsCustom, Is.False);
            Assert.That(tool.Description, Is.Not.Empty.And.Not.StartWith("Description"));
        });
    }

    [Test]
    public void ExistingSecurityAndNetworkToolsAreNotDuplicated()
    {
        ShortcutService.Instance.Initialize();
        foreach (var name in new[] { "Autoruns", "Process Explorer", "Process Monitor", "TCPView", "Wireshark", "Nmap" })
            Assert.That(ShortcutService.Instance.ExternalTools.Count(tool => tool.Name == name), Is.EqualTo(1), name);
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
    public void SecurityFilterScopesSearchAndFavoritesWithoutChangingDefaultCatalogue()
    {
        ExternalTool[] tools =
        [
            new() { Name = "Security favorite", Description = "Signature hashes", Category = "OSINT & Security", IsFavorite = true },
            new() { Name = "Security other", Description = "Email research", Category = "osint & security" },
            new() { Name = "Network favorite", Description = "Signature hashes", Category = "Network", IsFavorite = true },
            new() { Name = "Noncanonical", Category = "Security" }
        ];

        Assert.Multiple(() =>
        {
            Assert.That(ExternalToolsPage.FilterTools(tools, "", false), Is.EqualTo(tools));
            Assert.That(ExternalToolsPage.FilterTools(tools, "", false, "OSINT & Security").Select(tool => tool.Name),
                Is.EqualTo(new[] { "Security favorite", "Security other" }));
            Assert.That(ExternalToolsPage.FilterTools(tools, "", true, "OSINT & Security").Select(tool => tool.Name),
                Is.EqualTo(new[] { "Security favorite" }));
            Assert.That(ExternalToolsPage.FilterTools(tools, "HASHES", false, "OSINT & Security").Select(tool => tool.Name),
                Is.EqualTo(new[] { "Security favorite" }));
            Assert.That(ExternalToolsPage.FilterTools(tools, "research", true, "OSINT & Security"), Is.Empty);
            Assert.That(ExternalToolsPage.FilterTools(tools, "Network", false, "OSINT & Security"), Is.Empty);
            Assert.That(ExternalToolsPage.FilterTools(tools, ShortcutService.LocalizeCategory("OSINT & Security"), false, "OSINT & Security"), Has.Exactly(2).Items);
        });
    }

    [TestCase(Key.Enter)]
    [TestCase(Key.Space)]
    public void CardActivationDoesNotHandleKeysFromFavoriteOrOtherNestedButtons(Key key)
    {
        var favorite = new Button();
        var card = new Border { Child = favorite };
        Assert.That(ExternalToolsPage.IsToolCardActivation(card, favorite, key), Is.False);
        Assert.That(ExternalToolsPage.IsToolCardActivation(card, card, key), Is.True);
        Assert.That(ExternalToolsPage.IsToolCardActivation(card, card, Key.Tab), Is.False);
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
