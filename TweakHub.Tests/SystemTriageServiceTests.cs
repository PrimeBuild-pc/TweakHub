using System.IO;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class SystemTriageServiceTests
{
    private static readonly string System32 = Environment.SystemDirectory;

    [TestCase("8.8.8.8", true)]
    [TestCase("2606:4700:4700::1111", true)]
    [TestCase("::ffff:1.1.1.1", true)]
    [TestCase("172.32.0.1", true)]
    [TestCase("10.0.0.5", false)]
    [TestCase("172.16.4.1", false)]
    [TestCase("192.168.1.10", false)]
    [TestCase("100.64.0.1", false)]
    [TestCase("169.254.1.1", false)]
    [TestCase("127.0.0.1", false)]
    [TestCase("0.0.0.0", false)]
    [TestCase("239.255.255.250", false)]
    [TestCase("::1", false)]
    [TestCase("::", false)]
    [TestCase("fe80::1", false)]
    [TestCase("fd00::1", false)]
    [TestCase("*", false)]
    public void OnlyInternetAddressesOfferAReputationLookup(string address, bool expected)
    {
        Assert.That(SystemTriageService.IsPublicAddress(address), Is.EqualTo(expected));
    }

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
    public void ParseInventoryReadsTaskRoutinesAndMissingFiles()
    {
        const string json = """
            {"Startup":[
              {"Kind":"Task","Name":"ScheduledDefrag","Command":"{2A7E8F3E-0000-0000-0000-000000000000}","Image":"","Location":"\\Microsoft\\Windows\\Defrag\\",
               "Enabled":true,"Triggers":[9,8,1],"User":"SYSTEM","LastRun":"2026-10-20T09:15:00.0000000+02:00","NextRun":""},
              {"Kind":"Task","Name":"Orphan","Command":"\"C:\\Gone\\tool.exe\" /x","Image":null,"Location":"\\","Triggers":[4]},
              {"Kind":"Wmi","Name":"Consumer","Command":"Set o = CreateObject(\"x\")","Image":"","Location":"__FilterToConsumerBinding.x","Trigger":"SELECT * FROM __InstanceModificationEvent"},
              {"Kind":"ActiveSetup","Name":"Themes Setup","Command":"/UserInstall","Location":"HKLM:\\x"},
              {"Kind":"ShellEx","Name":"7-Zip","Command":"{23170F69}","Image":"C:\\Program Files\\Gone Tool\\7-zip.dll","Location":"HKLM:\\x"}]}
            """;
        var startup = SystemTriageService.ParseInventory(json).Startup;

        Assert.Multiple(() =>
        {
            Assert.That(startup[0].Category, Is.EqualTo("Tasks"));
            Assert.That(startup[0].Path, Is.Empty, "an empty Image means the entry starts no file of its own");
            Assert.That(startup[0].Trigger, Is.EqualTo("Logon, Boot, Time"));
            Assert.That(startup[0].User, Is.EqualTo("SYSTEM"));
            Assert.That(startup[0].LastRun, Is.EqualTo(new DateTimeOffset(2026, 10, 20, 9, 15, 0, TimeSpan.FromHours(2))));
            Assert.That(startup[0].NextRun, Is.Null);
            Assert.That(startup[0].IsMissing, Is.False);
            Assert.That(startup[1].Path, Is.EqualTo("C:\\Gone\\tool.exe"), "a null Image falls back to the command line");
            Assert.That(startup[1].IsMissing, Is.True);
            Assert.That(startup[1].Trigger, Is.EqualTo("Monthly"));
            Assert.That(startup[2].Category, Is.EqualTo("Wmi"));
            Assert.That(startup[2].Trigger, Is.EqualTo("SELECT * FROM __InstanceModificationEvent"));
            Assert.That(startup[2].IsMissing, Is.False);
            Assert.That(startup[3].IsMissing, Is.False, "an argument is not a missing file");
            Assert.That(startup[4].Path, Is.EqualTo("C:\\Program Files\\Gone Tool\\7-zip.dll"), "an Image is a path, never cut at a space");
        });
    }

    private static readonly string Windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    private static StartupEntry Entry(string kind, string location, string path, TrustState trust, string signer = "") =>
        new(kind, "Item", "", location, path) { Trust = trust, Signer = signer };

    [Test]
    public void CriticalEntriesAreNeverDeletedAndWindowsServicesNeverToggled()
    {
        var svchost = Path.Combine(System32, "svchost.exe");
        var hklmWinlogon = "HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon";
        var cases = new (StartupEntry Entry, string Protection, bool CanDelete, bool CanToggle)[]
        {
            (Entry("Driver", "disk", Path.Combine(System32, "drivers", "disk.sys"), TrustState.Signed, "Microsoft Windows"), "Driver", false, false),
            (Entry("Winlogon", hklmWinlogon, Path.Combine(System32, "userinit.exe"), TrustState.Signed, "Microsoft Windows"), "Winlogon", false, false),
            (Entry("Winlogon", "HKCU:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "C:\\Users\\a\\shell.exe", TrustState.Unsigned), "", true, false),
            (Entry("Service", "Dhcp", svchost, TrustState.Signed, "Microsoft Windows"), "WindowsService", false, false),
            (Entry("Service", "Dhcp", svchost, TrustState.Unknown), "WindowsService", false, false),
            (Entry("Service", "RtkAudio", Path.Combine(System32, "RtkAudUService64.exe"), TrustState.Signed, "Realtek Semiconductor Corp."), "", true, true),
            (Entry("Run", "HKLM:\\Run", Path.Combine(System32, "SecurityHealthSystray.exe"), TrustState.Signed, "Microsoft Windows") with { ApprovedKey = "HKLM:\\A" }, "Windows", false, true),
            (Entry("Run", "HKCU:\\Run", Path.Combine(System32, "rundll32.exe"), TrustState.ScriptHost) with { ApprovedKey = "HKCU:\\A" }, "", true, true),
            (Entry("Task", "\\Microsoft\\Windows\\Defrag\\", "", TrustState.Unknown), "Windows", false, true),
            (Entry("Task", "\\Microsoft\\Windows\\Defrag\\", "C:\\Users\\a\\evil.exe", TrustState.Unsigned), "", true, true),
            (Entry("Task", "\\Microsoft\\Windows\\AppxDeploymentClient\\", Path.Combine(System32, "rundll32.exe"), TrustState.ScriptHost)
                with { Command = "\"%windir%\\system32\\rundll32.exe\" %windir%\\system32\\AppxDeploymentClient.dll,AppxPreStageCleanupRunTask" }, "Windows", false, true),
            (Entry("Task", "\\Microsoft\\Windows\\DiskDiagnostic\\", Path.Combine(System32, "rundll32.exe"), TrustState.ScriptHost)
                with { Command = "\"%windir%\\system32\\rundll32.exe\" dfdts.dll,DfdGetDefaultPolicyAndSMART" }, "Windows", false, true),
            // A launcher from System32 is not enough: what it runs decides.
            (Entry("Task", "\\Microsoft\\Windows\\Fake\\", Path.Combine(System32, "rundll32.exe"), TrustState.ScriptHost)
                with { Command = "\"C:\\Windows\\system32\\rundll32.exe\" C:\\ProgramData\\x.dll,Run" }, "", true, true),
            (Entry("Task", "\\Microsoft\\Windows\\Fake\\", Path.Combine(System32, "WindowsPowerShell", "v1.0", "powershell.exe"), TrustState.ScriptHost)
                with { Command = "powershell.exe -enc AAAA" }, "", true, true),
            // Folders users can write to, and a network file whose name alone looks like a System32 one, are not Windows.
            (Entry("Task", @"\Microsoft\Windows\Fake\", Path.Combine(System32, "cmd.exe"), TrustState.ScriptHost)
                with { Command = @"C:\Windows\System32\cmd.exe /c C:\Windows\Temp\p.bat" }, "", true, true),
            (Entry("Task", @"\Microsoft\Windows\Fake\", Path.Combine(System32, "cmd.exe"), TrustState.ScriptHost)
                with { Command = @"C:\Windows\System32\cmd.exe /c \\host\s\evil.exe" }, "", true, true),
            (Entry("Task", "\\", Path.Combine(System32, "schtasks.exe"), TrustState.Signed, "Microsoft Windows"), "", true, true),
            (new StartupEntry("Wmi", "SCM Event Log Consumer", "NTEventLogEventConsumer", "b", ""), "Windows", false, false),
            (new StartupEntry("Wmi", "Updater", "powershell -enc AAAA", "b", ""), "", true, false),
        };

        Assert.Multiple(() =>
        {
            foreach (var (entry, protection, canDelete, canToggle) in cases)
            {
                Assert.That(entry.Protection, Is.EqualTo(protection), $"{entry.Kind} {entry.Location} {entry.Path}");
                Assert.That(entry.CanDelete, Is.EqualTo(canDelete), $"delete {entry.Kind} {entry.Location} {entry.Path}");
                Assert.That(entry.CanToggle, Is.EqualTo(canToggle), $"toggle {entry.Kind} {entry.Location} {entry.Path}");
            }
        });
    }

    [Test]
    public void MicrosoftFilterHidesSignedMicrosoftProgramsAndWindowsComponents()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Entry("Run", "HKCU:\\Run", "C:\\Program Files\\Microsoft OneDrive\\OneDrive.exe", TrustState.Signed, "Microsoft Corporation").IsMicrosoft, Is.True);
            Assert.That(Entry("Service", "Dhcp", Path.Combine(System32, "svchost.exe"), TrustState.Unknown).IsMicrosoft, Is.True);
            Assert.That(Entry("Run", "HKCU:\\Run", Path.Combine(System32, "rundll32.exe"), TrustState.ScriptHost).IsMicrosoft, Is.False);
            Assert.That(Entry("Run", "HKCU:\\Run", Path.Combine(Windows, "Temp", "x.exe"), TrustState.Unsigned).IsMicrosoft, Is.False);
            Assert.That(Entry("Run", "HKCU:\\Run", "C:\\Tools\\app.exe", TrustState.Signed, "Contoso").IsMicrosoft, Is.False);
        });
    }

    private const string HkcuRun = @"HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private static readonly string UserStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
    private static readonly string CommonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);

    private static void AssertBackupBeforeDelete(string script, string capture, string delete)
    {
        Assert.That(script, Does.Contain(capture));
        Assert.That(script, Does.Contain(delete));
        Assert.That(script.IndexOf("ConvertTo-Json", StringComparison.Ordinal), Is.GreaterThan(script.IndexOf(capture, StringComparison.Ordinal)));
        Assert.That(script.IndexOf(delete, StringComparison.Ordinal), Is.GreaterThan(script.IndexOf("ConvertTo-Json", StringComparison.Ordinal)),
            "the backup is written out before anything is deleted");
    }

    [Test]
    public void DeleteScriptsWriteTheBackupFirstAndQuoteNames()
    {
        Assert.Multiple(() =>
        {
            var run = SystemTriageService.BuildDeleteScript(new StartupEntry("Run", "It's", "", HkcuRun, ""));
            Assert.That(run, Does.Contain("OpenBaseKey('CurrentUser', 'Registry64')"));
            AssertBackupBeforeDelete(run, @"OpenSubKey('SOFTWARE\Microsoft\Windows\CurrentVersion\Run', $true)", "DeleteValue('It''s')");

            // COM matches the exact name; the ScheduledTasks cmdlets read [ ] in a name as a wildcard pattern.
            var task = SystemTriageService.BuildDeleteScript(new StartupEntry("Task", "Up[d]'ate", "", "\\Vendor\\", ""));
            Assert.That(task, Does.Contain("GetFolder('\\Vendor')"));
            AssertBackupBeforeDelete(task, "GetTask('Up[d]''ate').Xml", "DeleteTask('Up[d]''ate', 0)");
            Assert.That(task, Does.Not.Contain("ScheduledTask "));

            var link = Path.Combine(UserStartup, "App.lnk");
            var folder = SystemTriageService.BuildDeleteScript(new StartupEntry("Folder", "App.lnk", "", UserStartup, ""));
            Assert.That(folder, Does.Contain("ReparsePoint"), "links are refused, never followed");
            AssertBackupBeforeDelete(folder, $"Get-Item -LiteralPath {PowerShellService.Literal(link)} -Force", "Remove-Item -LiteralPath $file.FullName -Force");

            AssertBackupBeforeDelete(SystemTriageService.BuildDeleteScript(new StartupEntry("Service", "Agent", "", "agent", "")),
                @"OpenSubKey('SYSTEM\CurrentControlSet\Services\agent')", "sc.exe\" delete 'agent'");

            AssertBackupBeforeDelete(SystemTriageService.BuildDeleteScript(new StartupEntry("Ifeo", "sethc.exe", "", @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\sethc.exe", "")),
                @"OpenSubKey('SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\sethc.exe', $true)", "DeleteValue('Debugger')");

            AssertBackupBeforeDelete(SystemTriageService.BuildDeleteScript(new StartupEntry("ShellEx", "Tool", "", @"HKCU:\SOFTWARE\Classes\Directory\Background\ShellEx\ContextMenuHandlers\Tool", "")),
                @"OpenSubKey('SOFTWARE\Classes\Directory\Background\ShellEx\ContextMenuHandlers\Tool')", @"DeleteSubKeyTree('SOFTWARE\Classes\Directory\Background\ShellEx\ContextMenuHandlers\Tool')");

            var binding = "__FilterToConsumerBinding.Consumer=\"CommandLineEventConsumer.Name=\\\"x\\\"\",Filter=\"__EventFilter.Name=\\\"y\\\"\"";
            var wmi = SystemTriageService.BuildDeleteScript(new StartupEntry("Wmi", "x", "", binding, ""));
            AssertBackupBeforeDelete(wmi, "GetText('Mof')", "$binding.Delete()");
            Assert.That(wmi, Does.Contain(@"$path = 'root\subscription:' + $path"), "relative filter paths resolve in root\\subscription");
        });
    }

    [Test]
    public void DeleteRefusesProtectedEntriesAndLocationsOutsideTheStartupPoints()
    {
        var refused = new[]
        {
            new StartupEntry("Driver", "disk", "", "disk", ""),
            new StartupEntry("Winlogon", "Shell", "", @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", ""),
            new StartupEntry("Run", "x", "", @"HKLM:\SOFTWARE\Classes\exefile\shell\open\command", ""),
            new StartupEntry("ShellEx", "x", "", @"HKLM:\SOFTWARE\Classes\exefile\shell\open", ""),
            new StartupEntry("Ifeo", "x", "", @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\a\b", ""),
            new StartupEntry("Folder", "x.lnk", "", System32, ""),
            new StartupEntry("Folder", "..\\x.lnk", "", UserStartup, ""),
            new StartupEntry("Service", "x", "", "a\\b", ""),
            new StartupEntry("Task", "a\\b", "", "\\", ""),
            new StartupEntry("Wmi", "x", "", "__EventFilter.Name=\"y\"", ""),
        };
        Assert.Multiple(() =>
        {
            foreach (var entry in refused)
                Assert.Throws<InvalidOperationException>(() => SystemTriageService.BuildDeleteScript(entry), $"{entry.Kind} {entry.Location} {entry.Name}");
        });
    }

    private const string BackupId = "20261022-140206-feb342236f3b431692924e8313dab534";

    private static StartupBackup Backup(string kind, string name, string location, string payload) =>
        new(BackupId, DateTime.Now, kind, name, "", location, "", "", payload) { Machine = true };

    [Test]
    public void MachineWideBackupsAreStoredWhereOnlyAdministratorsCanWrite()
    {
        var record = Backup("Run", "App", @"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "");
        var machine = SystemTriageService.BuildDeleteScript(record.Entry, record);
        var user = SystemTriageService.BuildDeleteScript(new StartupEntry("Run", "App", "", HkcuRun, ""), record);

        Assert.Multiple(() =>
        {
            Assert.That(machine, Does.Contain($@"CreateSubKey('SOFTWARE\TweakHub\StartupBackups\{BackupId}')"));
            Assert.That(machine.IndexOf("SetValue('Payload', $payload)", StringComparison.Ordinal),
                Is.GreaterThan(0).And.LessThan(machine.IndexOf("DeleteValue('App')", StringComparison.Ordinal)), "saved before the delete");
            Assert.That(user, Does.Not.Contain("StartupBackups"), "user entries are restored unelevated from the user's own file");
            Assert.That(user.IndexOf($"{BackupId}.json", StringComparison.Ordinal),
                Is.GreaterThan(0).And.LessThan(user.IndexOf("DeleteValue('App')", StringComparison.Ordinal)), "the backup file is written before the delete");
        });
    }

    [Test]
    public void ElevatedRestoresOnlyTrustTheAdministratorStore()
    {
        var task = Backup("Task", "T", "\\", """{"Xml":"<Task/>"}""");
        var userRun = Backup("Run", "App", HkcuRun, """{"Values":[{"Name":"App","Kind":"String","Data":"x"}],"Keys":[]}""") with { Machine = false };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => SystemTriageService.BuildRestoreScript(task with { Machine = false }),
                "a backup file in the user's folder could hold any task, run as SYSTEM after one UAC prompt");
            Assert.That(SystemTriageService.BuildRestoreScript(task),
                Does.EndWith($@"DeleteSubKeyTree('SOFTWARE\TweakHub\StartupBackups\{BackupId}', $false)"));
            Assert.That(SystemTriageService.BuildRestoreScript(userRun), Does.Contain("SetValue('App', 'x', 'String')"));
            Assert.Throws<InvalidOperationException>(() => SystemTriageService.BuildRestoreScript(task with { Id = "..\\x" }));
        });
    }

    [Test]
    public void ValueRestoresWriteOnlyTheDeletedValue()
    {
        var script = SystemTriageService.BuildRestoreScript(Backup("AppInit", "AppInit_DLLs", @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows",
            """{"Values":[{"Name":"AppInit_DLLs","Kind":"String","Data":"a.dll"},{"Name":"LoadAppInit_DLLs","Kind":"DWord","Data":1}],"Keys":[{"Name":"S","Values":[],"Keys":[]}]}"""));
        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("SetValue('AppInit_DLLs', 'a.dll', 'String')"));
            Assert.That(script, Does.Not.Contain("LoadAppInit_DLLs"));
            Assert.That(script, Does.Not.Contain(@"Windows\S'"));
        });
    }

    [Test]
    public void RestoreScriptsRebuildEveryValueTypeWithoutOverwriting()
    {
        const string tree = """
            {"Name":"","Values":[{"Name":"ImagePath","Kind":"ExpandString","Data":"%SystemRoot%\\it's.exe"},{"Name":"Start","Kind":"DWord","Data":2},
              {"Name":"Big","Kind":"QWord","Data":5000000000},{"Name":"DependOnService","Kind":"MultiString","Data":["RpcSs","Tcpip"]},
              {"Name":"Blob","Kind":"Binary","Data":"AQID"},{"Name":"Odd","Kind":"Unknown","Data":"AA=="}],
             "Keys":[{"Name":"Parameters","Values":[{"Name":"","Kind":"String","Data":"x"}],"Keys":[]}]}
            """;
        var service = SystemTriageService.BuildRestoreScript(Backup("Service", "Agent", "agent", tree));
        var run = SystemTriageService.BuildRestoreScript(Backup("Run", "It's", HkcuRun,
            """{"Name":"","Values":[{"Name":"It's","Kind":"String","Data":"\"C:\\a.exe\" -q"}],"Keys":[]}"""));

        Assert.Multiple(() =>
        {
            Assert.That(service, Does.Contain(@"if ($hive.OpenSubKey('SYSTEM\CurrentControlSet\Services\agent')) { throw"), "an existing key is never overwritten");
            Assert.That(service, Does.Contain(@"$key = $hive.CreateSubKey('SYSTEM\CurrentControlSet\Services\agent')"));
            Assert.That(service, Does.Contain("$key.SetValue('ImagePath', '%SystemRoot%\\it''s.exe', 'ExpandString')"));
            Assert.That(service, Does.Contain("$key.SetValue('Start', [int]2, 'DWord')"));
            Assert.That(service, Does.Contain("$key.SetValue('Big', [long]5000000000, 'QWord')"));
            Assert.That(service, Does.Contain("$key.SetValue('DependOnService', [string[]]@('RpcSs', 'Tcpip'), 'MultiString')"));
            Assert.That(service, Does.Contain("$key.SetValue('Blob', [byte[]][Convert]::FromBase64String('AQID'), 'Binary')"));
            Assert.That(service, Does.Contain("$key.SetValue('Odd', [byte[]][Convert]::FromBase64String('AA=='), 'Binary')"));
            Assert.That(service, Does.Contain(@"$key = $hive.CreateSubKey('SYSTEM\CurrentControlSet\Services\agent\Parameters')"));
            Assert.That(service, Does.Contain("$key.SetValue('', 'x', 'String')"));

            Assert.That(run, Does.Contain("OpenBaseKey('CurrentUser', 'Registry64')"));
            Assert.That(run, Does.Contain("if ($null -ne $key.GetValue('It''s')) { throw"));
            Assert.That(run, Does.Contain("$key.SetValue('It''s', '\"C:\\a.exe\" -q', 'String')"));
        });
    }

    [Test]
    public void RestoreScriptsForTasksFilesAndWmi()
    {
        var task = SystemTriageService.BuildRestoreScript(Backup("Task", "Up'date", "\\Vendor\\", """{"Xml":"<Task a='1'/>"}"""));
        var file = SystemTriageService.BuildRestoreScript(Backup("Folder", "App.lnk", UserStartup, """{"Data":"TEs="}"""));
        var wmi = SystemTriageService.BuildRestoreScript(Backup("Wmi", "x", "__FilterToConsumerBinding.x",
            """{"Mof":"instance of __EventFilter\n{\n\tCreatorSID = {1, 5, 0};\n\tName = \"y\";\n};"}"""));

        Assert.Multiple(() =>
        {
            Assert.That(task, Does.StartWith("Register-ScheduledTask -Xml '<Task a=''1''/>' -TaskPath '\\Vendor\\' -TaskName 'Up''date' | Out-Null"));
            Assert.That(file, Does.Contain($"$path = {PowerShellService.Literal(Path.Combine(UserStartup, "App.lnk"))}"));
            Assert.That(file, Does.Contain("if (Test-Path -LiteralPath $path) { throw"));
            Assert.That(file, Does.Contain("[IO.File]::WriteAllBytes($path, [Convert]::FromBase64String('TEs='))"));
            Assert.That(wmi, Does.Contain("#pragma namespace"));
            Assert.That(wmi, Does.Not.Contain("CreatorSID"), "Windows sets the creator; restoring it is refused");
            Assert.That(wmi, Does.Contain("Name = \"y\";"));
            Assert.That(wmi, Does.Contain("wbem\\mofcomp.exe"));
        });
    }

    [Test]
    public void RestoreRefusesBackupsThatPointOutsideTheStartupPoints() =>
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => SystemTriageService.BuildRestoreScript(
                Backup("Run", "x", @"HKLM:\SOFTWARE\Classes\exefile\shell\open\command", """{"Values":[],"Keys":[]}""")));
            Assert.Throws<InvalidOperationException>(() => SystemTriageService.BuildRestoreScript(
                Backup("Folder", "evil.dll", System32, """{"Data":"AA=="}""")));
            Assert.Throws<InvalidOperationException>(() => SystemTriageService.BuildRestoreScript(
                Backup("Driver", "x", "x", """{"Values":[],"Keys":[]}""")));
        });

    [Test]
    public async Task GeneratedScriptsParseInWindowsPowerShell()
    {
        const string tree = """{"Name":"","Values":[{"Name":"A","Kind":"DWord","Data":1},{"Name":"M","Kind":"MultiString","Data":["x"]}],"Keys":[{"Name":"S","Values":[],"Keys":[]}]}""";
        var binding = "__FilterToConsumerBinding.Consumer=\"CommandLineEventConsumer.Name=\\\"x\\\"\"";
        var entries = new[]
        {
            new StartupEntry("Run", "It's", "", HkcuRun, ""),
            new StartupEntry("Service", "Agent", "", "agent", ""),
            new StartupEntry("Task", "T", "", "\\", ""),
            new StartupEntry("Folder", "A.lnk", "", UserStartup, ""),
            new StartupEntry("Wmi", "x", "", binding, ""),
        };
        var scripts = entries.Select(entry => SystemTriageService.BuildDeleteScript(entry)).Concat(
        [
            SystemTriageService.BuildRestoreScript(Backup("Run", "It's", HkcuRun, tree)),
            SystemTriageService.BuildRestoreScript(Backup("Service", "Agent", "agent", tree)),
            SystemTriageService.BuildRestoreScript(Backup("Task", "T", "\\", """{"Xml":"<Task/>"}""")),
            SystemTriageService.BuildRestoreScript(Backup("Folder", "A.lnk", UserStartup, """{"Data":"AA=="}""")),
            SystemTriageService.BuildRestoreScript(Backup("Wmi", "x", binding, """{"Mof":"instance of __EventFilter { Name = \"y\"; };"}""")),
        ]).ToList();

        var list = string.Join(",", scripts.Select(script => $"'{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(script))}'"));
        var check = $$"""
            foreach ($encoded in @({{list}})) {
                $errors = $null
                [void][System.Management.Automation.Language.Parser]::ParseInput([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($encoded)), [ref]$null, [ref]$errors)
                $errors | ForEach-Object { $_.Message }
            }
            """;
        var start = new System.Diagnostics.ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(check)) })
            start.ArgumentList.Add(argument);
        using var process = System.Diagnostics.Process.Start(start)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.That(output.Trim(), Is.Empty);
    }

    [Test]
    public void OnlyMachineWideEntriesAskForAdministratorRights()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new StartupEntry("Run", "a", "", HkcuRun, "").DeleteNeedsAdministrator, Is.False, "HKCU elevated would be the administrator's hive");
            Assert.That(new StartupEntry("Run", "a", "", @"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "").DeleteNeedsAdministrator, Is.True);
            Assert.That(new StartupEntry("Folder", "a.lnk", "", UserStartup, "").DeleteNeedsAdministrator, Is.False);
            Assert.That(new StartupEntry("Folder", "a.lnk", "", CommonStartup, "").DeleteNeedsAdministrator, Is.True);
            Assert.That(new StartupEntry("Task", "a", "", "\\", "").DeleteNeedsAdministrator, Is.True);
            Assert.That(new StartupEntry("Wmi", "a", "", "b", "").DeleteNeedsAdministrator, Is.True);
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
            var disableTask = SystemTriageService.BuildToggleScript(task, enable: false);
            Assert.That(disableTask, Does.Contain("GetFolder('\\Vendor')"));
            Assert.That(disableTask, Does.Contain("$folder.GetTask('Updater').Enabled = $false"));
            Assert.That(disableTask, Does.Not.Contain("ScheduledTask "), "the cmdlets match [ ] in names as wildcards");
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
    public async Task StartupInventorySurvivesTheElevatedWrapper()
    {
        // The wrapper runs scripts inside try/catch, where SilentlyContinue no longer hides a call on $null (missing Run keys).
        var folder = Directory.CreateTempSubdirectory("TweakHubInventory-");
        try
        {
            var script = Path.Combine(folder.FullName, "s.ps1");
            var output = Path.Combine(folder.FullName, "out.txt");
            var error = Path.Combine(folder.FullName, "err.txt");
            var encoding = new System.Text.UTF8Encoding(true);
            var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(
                PowerShellService.BuildScriptContext(SystemTriageService.StartupScript.Replace("__TRACKED_SERVICES__", "")))).ToArray();
            await File.WriteAllBytesAsync(script, bytes);
            var start = new System.Diagnostics.ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
            foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand",
                         Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(PowerShellService.BuildElevatedWrapper(script, bytes, output, error))) })
                start.ArgumentList.Add(argument);
            using var process = System.Diagnostics.Process.Start(start)!;
            await process.WaitForExitAsync();

            Assert.That(process.ExitCode, Is.Zero, File.Exists(error) ? await File.ReadAllTextAsync(error) : "");
            var json = (await File.ReadAllLinesAsync(output)).Last(line => line.StartsWith('{'));
            Assert.That(SystemTriageService.ParseInventory(json).Startup.Any(entry => entry.Kind == "Task"), Is.True);
        }
        finally { folder.Delete(true); }
    }

    [Test]
    public async Task SignaturesAreReadForSystemBinaries()
    {
        var notepad = Path.Combine(System32, "notepad.exe");
        var signatures = await SystemTriageService.GetSignaturesAsync([notepad]);
        Assert.That(signatures[notepad].Status, Is.EqualTo("Valid"));
    }
}
