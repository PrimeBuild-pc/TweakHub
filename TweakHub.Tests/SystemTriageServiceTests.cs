using System.IO;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class SystemTriageServiceTests
{
    private static readonly string System32 = Environment.SystemDirectory;

    [TestCase("\"C:\\Program Files\\App\\app.exe\" --minimized", "C:\\Program Files\\App\\app.exe")]
    [TestCase("\"\"C:\\Program Files\\Google\\helper.exe\"\" --metrics", "C:\\Program Files\\Google\\helper.exe")]
    [TestCase("C:\\Program Files\\OpenVPN\\bin\\openvpn-gui.exe", "C:\\Program Files\\OpenVPN\\bin\\openvpn-gui.exe")]
    [TestCase("C:\\Tools\\agent.EXE /service", "C:\\Tools\\agent.EXE")]
    [TestCase("\\??\\C:\\Drivers\\filter.sys", "C:\\Drivers\\filter.sys")]
    [TestCase("", "")]
    public void ExtractExecutableReadsCommandLinesLikeCreateProcess(string command, string expected) =>
        Assert.That(SystemTriageService.ExtractExecutable(command), Is.EqualTo(expected));

    [Test]
    public void ExtractExecutableResolvesEnvironmentAndBareSystemNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SystemTriageService.ExtractExecutable("%SystemRoot%\\system32\\SecurityHealthSystray.exe"),
                Is.EqualTo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "system32", "SecurityHealthSystray.exe")));
            Assert.That(SystemTriageService.ExtractExecutable("rundll32.exe shell32.dll,Control_RunDLL"), Is.EqualTo(Path.Combine(System32, "rundll32.exe")));
            Assert.That(SystemTriageService.ExtractExecutable("\\SystemRoot\\System32\\drivers\\x.sys"),
                Is.EqualTo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32\\drivers\\x.sys")));
        });
    }

    [Test]
    public void ParseInventoryHandlesSingleObjectsAndMissingFields()
    {
        const string json = """
            {"Connections":{"Protocol":"TCP","Local":"0.0.0.0","LocalPort":135,"Remote":"0.0.0.0","RemotePort":0,"State":"Listen","Pid":1200,"Process":"","Path":""},
             "Startup":[{"Kind":"Run","Name":"Updater","Command":"\"C:\\Temp\\upd.exe\" -q","Location":"HKCU:\\Run"},
                        {"Kind":"Task","Name":"Script","Command":"\"C:\\ProgramData\\job.cmd\"","Location":"\\"}]}
            """;
        var (connections, startup) = SystemTriageService.ParseInventory(json);

        Assert.Multiple(() =>
        {
            Assert.That(connections.Single().Process, Is.EqualTo("System"), "Processes without a name are kernel-owned");
            Assert.That(connections.Single().IsListening, Is.True);
            Assert.That(connections.Single().RemoteText, Is.Empty);
            Assert.That(startup.Select(entry => entry.Path), Is.EqualTo(new[] { "C:\\Temp\\upd.exe", "C:\\ProgramData\\job.cmd" }));
        });
    }

    [Test]
    public void ParseInventoryReadsStartupStateAndToggleSupport()
    {
        const string json = """
            {"Startup":[
              {"Kind":"Run","Name":"App","Command":"C:\\a.exe","Location":"HKCU:\\Run","Approved":"HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run","Enabled":false},
              {"Kind":"Run","Name":"Once","Command":"C:\\b.exe","Location":"HKLM:\\RunOnce","Approved":"","Enabled":true},
              {"Kind":"Service","Name":"Agent","Command":"C:\\c.exe","Location":"agent","Enabled":true,"Delayed":true},
              {"Kind":"Winlogon","Name":"Shell","Command":"evil.exe","Location":"Winlogon"}]}
            """;
        var startup = SystemTriageService.ParseInventory(json).Startup;

        Assert.Multiple(() =>
        {
            Assert.That(startup[0].Enabled, Is.False);
            Assert.That(startup[0].CanToggle, Is.True);
            Assert.That(startup[0].ToggleNeedsAdministrator, Is.False, "HKCU entries are per-user");
            Assert.That(startup[1].CanToggle, Is.False, "RunOnce has no StartupApproved switch");
            Assert.That(startup[2].Delayed, Is.True);
            Assert.That(startup[2].ToggleNeedsAdministrator, Is.True);
            Assert.That(startup[3].Enabled, Is.True);
            Assert.That(startup[3].CanToggle, Is.False, "Winlogon values are never toggled");
        });
    }

    [Test]
    public void ToggleScriptsUseWindowsMechanismsAndQuoteNames()
    {
        var run = new StartupEntry("Run", "It's", "", "HKCU:\\Run", "") { ApprovedKey = "HKCU:\\StartupApproved\\Run" };
        var task = new StartupEntry("Task", "Updater", "", "\\Vendor\\", "");
        var service = new StartupEntry("Service", "Agent", "", "agent", "") { Delayed = true };

        Assert.Multiple(() =>
        {
            var disable = SystemTriageService.BuildToggleScript(run, enable: false);
            Assert.That(disable, Does.Contain("-Name 'It''s'"));
            Assert.That(disable, Does.Contain("@(3,0,0,0,0,0,0,0,0,0,0,0)"));
            Assert.That(disable, Does.Not.Contain("-Force"), "New-Item -Force would wipe the StartupApproved key");
            Assert.That(SystemTriageService.BuildToggleScript(run, enable: true), Does.Contain("@(2,0,0,0,0,0,0,0,0,0,0,0)"));
            Assert.That(SystemTriageService.BuildToggleScript(task, enable: false), Is.EqualTo("Disable-ScheduledTask -TaskPath '\\Vendor\\' -TaskName 'Updater' | Out-Null"));
            Assert.That(SystemTriageService.BuildToggleScript(service, enable: false), Is.EqualTo("Set-Service -Name 'agent' -StartupType Manual"));
            Assert.That(SystemTriageService.BuildToggleScript(service, enable: true, "delayed-auto"), Does.Contain("start= delayed-auto"));
            Assert.That(SystemTriageService.BuildToggleScript(service, enable: true), Does.Contain("start= auto"));
        });
    }

    [Test]
    public void LoopbackDetectionCoversRemoteAndLocalListeners()
    {
        static NetworkConnection Connection(string local, string remote, string state) =>
            new("TCP", local, 1, remote, 2, state, 1, "p", "");

        Assert.Multiple(() =>
        {
            Assert.That(Connection("127.0.0.1", "127.0.0.1", "Established").IsLoopback, Is.True);
            Assert.That(Connection("::1", "", "Listen").IsLoopback, Is.True);
            Assert.That(Connection("0.0.0.0", "", "Listen").IsLoopback, Is.False);
            Assert.That(Connection("192.168.1.5", "140.82.112.3", "Established").IsLoopback, Is.False);
        });
    }

    [Test]
    public void CsvExportNeutralizesSpreadsheetFormulas()
    {
        var triage = new SystemTriage([],
            [new StartupEntry("Run", "=HYPERLINK(\"http://x\")", "cmd /c \"calc\"", "HKCU", "C:\\x.exe")], false);
        var csv = SystemTriageService.ToCsv(triage);

        Assert.Multiple(() =>
        {
            Assert.That(csv, Does.Contain("\"'=HYPERLINK(\"\"http://x\"\")\""));
            Assert.That(csv, Does.Contain("\"cmd /c \"\"calc\"\"\""));
        });
    }

    [Test]
    public async Task SignaturesAreReadForSystemBinaries()
    {
        var notepad = Path.Combine(System32, "notepad.exe");
        var signatures = await SystemTriageService.GetSignaturesAsync([notepad]);
        Assert.That(signatures[notepad].Status, Is.EqualTo("Valid"));
    }
}
