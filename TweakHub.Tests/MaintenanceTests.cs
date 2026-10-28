using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using TweakHub.Services;
using TweakHub.Views;
using System.Windows;
using System.Windows.Controls;

namespace TweakHub.Tests;

public class MaintenanceTests
{
    private string _directory = null!;
    [SetUp] public void SetUp() => _directory = Path.Combine(Path.GetTempPath(), "TweakHubMaintenance-" + Guid.NewGuid().ToString("N"));
    [TearDown] public void TearDown() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static PowerShellResult Result(bool success = true) => new() { Success = success };

    [Test]
    public void NewEditorsLoadWithoutRunningAnySystemAction()
    {
        if (Application.Current is null)
        {
            var app = new App();
            app.InitializeComponent();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
        if (CpuTopology.Supported)
        {
            var topology = CpuTopology.Read();
            Assert.That(topology.Count, Is.EqualTo(CpuTopology.Count), "Native topology must cover every logical CPU");
            Assert.That(topology.Select(cpu => cpu.Index).Distinct().Count(), Is.EqualTo(topology.Count));
            Assert.That(InterruptService.CanAssign(1), Is.True);
        }
        Assert.That(InterruptService.CanAssign(0), Is.False);
        var affinity = new InterruptView { AffinityOnly = true };
        var msi = new InterruptView();
        var affinityGrid = (DataGrid)affinity.FindName("DevicesGrid");
        Assert.Multiple(() =>
        {
            Assert.That(affinityGrid.Columns[2].Visibility, Is.EqualTo(Visibility.Collapsed));
            Assert.That(((FrameworkElement)affinity.FindName("AffinityEditor")).Visibility, Is.EqualTo(Visibility.Visible));
            Assert.That(((FrameworkElement)msi.FindName("AffinityEditor")).Visibility, Is.EqualTo(Visibility.Collapsed));
            Assert.That(((FrameworkElement)affinity.FindName("MsiTile")).Visibility, Is.EqualTo(Visibility.Collapsed));
            Assert.That(new OptionalServicesView(), Is.Not.Null);
            Assert.That(new SecurityPage(), Is.Not.Null);
            Assert.That(new DevicesPage(), Is.Not.Null);
            var customTweak = new TweakHub.Views.Dialogs.CustomTweakDialog(new TweakHub.Models.CustomRegistryTweak { RiskClass = "Caution" });
            Assert.That(((ComboBox)customTweak.FindName("RiskComboBox")).SelectedValue, Is.EqualTo("Caution"));
            customTweak.Close();
        });
        // Only constructors: no Loaded events, network downloads, service commands or registry writes.
    }

    [Test]
    public async Task GeneratedMaintenanceScriptsParseWithoutExecutingThem()
    {
        foreach (var script in new[] { HibernationService.BuildScript(new(1, 2, 75)),
            OptionalServicesService.BuildScript("MapsBroker", 3, null), OptionalServicesService.ScanScript,
            TronService.BuildRunScript(@"C:\Users\O'Brien\Tron v12.0.8 (2025-01-09).exe", new string('a', 64)) })
        {
            var parseOnly = "$tokens = $null; $errors = $null; [System.Management.Automation.Language.Parser]::ParseInput("
                + PowerShellService.Literal(script) + ", [ref]$tokens, [ref]$errors) | Out-Null; if ($errors.Count) { $errors | Out-String | Write-Error; exit 1 }";
            var result = await PowerShellService.Instance.ExecuteScriptAsync(parseOnly);
            Assert.That(result.Success, Is.True, result.Error);
        }
    }

    [Test]
    public void PortableDataMigrationRetainsTheNewUndoFiles()
    {
        var source = Path.Combine(_directory, "old");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "hibernation-backup.json"), """{"Original":{"Enabled":1,"Type":2,"Size":75}}""");
        File.WriteAllText(Path.Combine(source, "optional-services.json"), """{"RetailDemo":{"Name":"RetailDemo","Start":2,"Delayed":1,"Status":"Running","Exists":true}}""");
        File.WriteAllText(Path.Combine(source, "applied-tweaks.json"), """["disable_game_bar"]""");
        Directory.CreateDirectory(Path.Combine(source, "startup-backups"));
        File.WriteAllText(Path.Combine(source, "startup-backups", "abc.json"), "{}");
        var destination = new UserDataService(Path.Combine(_directory, "new"));
        var result = destination.ImportPreviousData(source, Path.Combine(_directory, "Apps"));
        Assert.That(result.UndoFiles, Is.EqualTo(4));
        Assert.That(File.Exists(Path.Combine(destination.DataDirectory, "startup-backups", "abc.json")), Is.True);
        foreach (var name in new[] { "hibernation-backup.json", "optional-services.json" })
            Assert.That(File.ReadAllText(Path.Combine(destination.DataDirectory, name)), Is.EqualTo(File.ReadAllText(Path.Combine(source, name))));
    }

    [Test]
    public void HibernationUsesPowercfgAndRestoresFullOrReducedFilesInTheCorrectOrder()
    {
        var full = HibernationService.BuildScript(new(1, 2, 75));
        var reduced = HibernationService.BuildScript(new(1, 1, 0));
        Assert.Multiple(() =>
        {
            Assert.That(full, Does.Contain("/type full").And.Contain("/size 75"));
            Assert.That(full.IndexOf("/type full", StringComparison.Ordinal), Is.LessThan(full.IndexOf("/size 75", StringComparison.Ordinal)));
            Assert.That(reduced.IndexOf("/size 0", StringComparison.Ordinal), Is.LessThan(reduced.IndexOf("/type reduced", StringComparison.Ordinal)));
            Assert.That(HibernationService.BuildScript(null), Does.Contain("/hibernate off").And.Contain("$LASTEXITCODE"));
            Assert.That(HibernationService.CanRestore(new(1, null, 0)), Is.False);
            Assert.That(HibernationService.CanRestore(new(1, 1, 60)), Is.False);
            Assert.Throws<ArgumentException>(() => HibernationService.BuildScript(new(1, 9, -1)));
        });
    }

    [Test]
    public async Task HibernationBackupSurvivesRestartFailedRestoreAndRepeatedApply()
    {
        var original = new HibernationService.State(1, 2, 75);
        var current = original;
        var fail = false;
        var calls = 0;
        Task<PowerShellResult> Run(string script)
        {
            calls++;
            Assert.That(File.Exists(Path.Combine(_directory, "hibernation-backup.json")), Is.True, "Backup before mutation");
            if (!fail) current = script.Contains("/hibernate off") ? new(0, null, 0) : original;
            return Task.FromResult(Result(!fail));
        }
        var service = new HibernationService(_directory, () => current, Run);
        Assert.That(await service.SetDisabledAsync(true), Is.True);
        Assert.That(await service.SetDisabledAsync(true), Is.True);
        var restarted = new HibernationService(_directory, () => current, Run);
        Assert.That(restarted.HasBackup, Is.True);
        fail = true;
        Assert.That(await restarted.SetDisabledAsync(false), Is.False);
        Assert.That(restarted.HasBackup, Is.True);
        fail = false;
        Assert.That(await restarted.SetDisabledAsync(false), Is.True);
        Assert.That(current, Is.EqualTo(original));
        Assert.That(restarted.HasBackup, Is.False);
        Assert.That(calls, Is.EqualTo(4));
    }

    [Test]
    public async Task UnknownHibernationConfigurationNeverRunsACommand()
    {
        var service = new HibernationService(_directory, () => new(1, null, null), _ => throw new AssertionException("Must not mutate"));
        Assert.That(await service.SetDisabledAsync(true), Is.False);
        Assert.That(service.HasBackup, Is.False);
        Assert.That(service.UnavailableReason, Is.Not.Empty);
    }

    [Test]
    public void OptionalServicesKeepDiagnosticsExistingTweaksAndTriggersOutOfBulkChanges()
    {
        Assert.Multiple(() =>
        {
            foreach (var name in new[] { "SysMain", "DiagTrack", "WerSvc", "WinDefend", "MapsBroker'; bad" })
                Assert.Throws<ArgumentException>(() => OptionalServicesService.BuildScript(name, 4, null));
            var manual = OptionalServicesService.BuildScript("MapsBroker", 3, null);
            Assert.That(manual, Does.Contain("start= demand").And.Contain("$LASTEXITCODE").And.Contain("Remove-ItemProperty"));
            Assert.That(manual, Does.Not.Contain("Stop-Service").And.Not.Contain("-Force").And.Not.Contain("TriggerInfo"));
            Assert.That(OptionalServicesService.BuildScript("RetailDemo", 2, 1), Does.Contain("start= delayed-auto"));
            Assert.Throws<ArgumentException>(() => OptionalServicesService.BuildScript("RetailDemo", 1, 0));
        });
    }

    [Test]
    public async Task OptionalServicesRestoreDelayedStartupWithoutTouchingTheRunningState()
    {
        var original = new OptionalService("RetailDemo", 2, 1, "Running", true);
        var current = original;
        var fail = false;
        var legacyManaged = false;
        Task<IReadOnlyList<OptionalService>> Scan() => Task.FromResult<IReadOnlyList<OptionalService>>([current]);
        Task<PowerShellResult> Run(string script)
        {
            Assert.That(File.Exists(Path.Combine(_directory, "optional-services.json")), Is.True);
            if (!fail) current = current with { Start = script.Contains("start= demand") ? 3 : script.Contains("start= disabled") ? 4 : 2 };
            return Task.FromResult(Result(!fail));
        }
        var service = new OptionalServicesService(_directory, Scan, Run, _ => legacyManaged);
        Assert.That(await service.SetModeAsync(current, 3), Is.True);
        Assert.That(await service.SetModeAsync(current, 4), Is.True);
        var restarted = new OptionalServicesService(_directory, Scan, Run, _ => legacyManaged);
        legacyManaged = true; // Imported legacy backups must block new edits, not make restoration impossible.
        Assert.That(await restarted.SetModeAsync(current, 3), Is.False);
        fail = true;
        Assert.That(await restarted.SetModeAsync(current, null), Is.False);
        Assert.That(restarted.HasBackup("RetailDemo"), Is.True);
        fail = false;
        Assert.That(await restarted.SetModeAsync(current, null), Is.True);
        Assert.That(current, Is.EqualTo(original));
        Assert.That(restarted.HasBackup("RetailDemo"), Is.False);
    }

    [Test]
    public async Task ServicesWithAnExistingLegacyBackupAreNeverChanged()
    {
        var selected = new OptionalService("RetailDemo", 2, 0, "Stopped", true);
        var service = new OptionalServicesService(_directory,
            () => Task.FromResult<IReadOnlyList<OptionalService>>([selected]),
            _ => throw new AssertionException("Must not change a service managed elsewhere"), _ => true);
        Assert.That(await service.SetModeAsync(selected, 4), Is.False);
        Assert.That(service.HasBackup(selected.Name), Is.False);
    }

    [Test]
    public void MissingAndUnapprovedServicesCannotBeManaged()
    {
        var services = OptionalServicesService.Parse("""
            [{"Name":"MapsBroker","Exists":false},{"Name":"WinDefend","Exists":true,"Start":2}]
            """);
        Assert.That(services, Has.Count.EqualTo(1));
        Assert.That(services[0].CanChange, Is.False);
    }

    [Test]
    public void TopologyGroupsSmtSiblingsAndRejectsMalformedOrOtherGroupData()
    {
        static byte[] Core(ulong mask, byte efficiency)
        {
            var data = new byte[48];
            BitConverter.GetBytes(48).CopyTo(data, 4);
            data[9] = efficiency;
            BitConverter.GetBytes((ushort)1).CopyTo(data, 30);
            BitConverter.GetBytes(mask).CopyTo(data, 32);
            return data;
        }
        var cpus = CpuTopology.Parse([.. Core(3, 0), .. Core(12, 1)]);
        Assert.That(cpus.Select(cpu => (cpu.Index, cpu.Core)), Is.EqualTo(new[] { (0, 0), (1, 0), (2, 1), (3, 1) }));
        Assert.That(cpus[3].EfficiencyClass, Is.EqualTo(1));
        var bad = Core(3, 0);
        bad[40] = 1;
        Assert.That(CpuTopology.Parse(bad), Is.Empty);
        Assert.That(CpuTopology.Parse([0, 0, 0, 0, 0, 0, 0, 0]), Is.Empty);
        Assert.That(CpuTopology.Parse([.. Core(3, 0), 0]), Is.Empty);
    }

    [Test]
    public void TronChoosesTheNewestCompletePublisherPackageAndRejectsUnknownMetadata()
    {
        var hash = new string('a', 64);
        var package = TronService.LatestPackage($"# header\n10,{hash},Tron v12.0.7 (2024-05-28).exe\n20,{hash},Tron v12.0.8 (2025-01-09).exe");
        Assert.That(package.Version, Is.EqualTo("12.0.8"));
        Assert.Throws<InvalidDataException>(() => TronService.LatestPackage($"10,{hash},../../evil.exe"));
        Assert.Throws<InvalidDataException>(() => TronService.LatestPackage($"900000000,{hash},Tron v12.0.8 (2025-01-09).exe"));
        Assert.That(TronService.RunArguments, Is.EqualTo("/d /k tron.bat -p -spr -m -sor -se -scs"));
    }

    [Test]
    public void TronRunsOnlyAHashCheckedCopyInAnAdministratorOnlyFolder()
    {
        var hash = new string('b', 64);
        var script = TronService.BuildRunScript(@"C:\Users\O'Brien\AppData\Roaming\TweakHub\Tools\Tron\Tron v12.0.8 (2025-01-09).exe", hash);
        Assert.That(script, Does.Contain("'C:\\Users\\O''Brien\\AppData\\Roaming\\TweakHub\\Tools\\Tron\\Tron v12.0.8 (2025-01-09).exe'"));
        Assert.That(script, Does.Contain(hash.ToUpperInvariant()));
        Assert.That(script, Does.Contain("SetAccessRuleProtection($true, $false)"));
        int At(string text) => script.IndexOf(text, StringComparison.Ordinal);
        Assert.That(At("CreateDirectory($folder, $acl)"), Is.LessThan(At("Copy-Item")));
        Assert.That(At("Get-FileHash"), Is.LessThan(At("Start-Process -FilePath $package")));
        Assert.That(script, Does.Contain("-WorkingDirectory $root").And.Not.Contain("Extracted"));
        // Only packages the publisher lists can be run, whatever package.json says.
        var listed = TronService.ListedPackages($"20,{hash},Tron v12.0.8 (2025-01-09).exe").ToList();
        Assert.That(listed, Does.Contain(new TronService.Package("Tron v12.0.8 (2025-01-09).exe", hash, 20, "12.0.8")));
        Assert.That(listed, Does.Not.Contain(new TronService.Package("Tron v12.0.8 (2025-01-09).exe", new string('c', 64), 20, "12.0.8")));
    }

    [TestCase("../escape.bat")]
    [TestCase("C:\\outside\\tron.bat")]
    [TestCase("resources\\..\\..\\escape.bat")]
    [TestCase("tron.bat:stream")]
    [TestCase(".. /escape.bat")]
    [TestCase("resources./escape.bat")]
    public void TronRejectsPathsOutsideTheVerifiedPackage(string relative) =>
        Assert.Throws<InvalidDataException>(() => TronService.CheckedPath(_directory, relative));

    private sealed class DownloadHandler(byte[] data, string manifest) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = request.RequestUri!.AbsolutePath.EndsWith("sha256sums.txt", StringComparison.Ordinal)
                    ? new StringContent(manifest) : new ByteArrayContent(data)
            });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TronDownloadsWithoutExecutingAndRejectsAlteredBytes(bool tamper)
    {
        byte[] original = Encoding.UTF8.GetBytes("Not an executable: download test only");
        var hash = Convert.ToHexString(SHA256.HashData(original)).ToLowerInvariant();
        var data = original.ToArray();
        if (tamper) data[0]++;
        using var http = new HttpClient(new DownloadHandler(data, $"{data.Length},{hash},Tron v12.0.8 (2025-01-09).exe"));
        var service = new TronService(_directory, http);
        if (tamper) await Assert.ThatAsync(() => service.DownloadAsync(), Throws.TypeOf<InvalidDataException>());
        else Assert.That(await service.DownloadAsync(), Does.Contain(hash));
        Assert.That(Directory.GetFiles(_directory, "*.exe"), Has.Length.EqualTo(tamper ? 0 : 1));
        Assert.That(Directory.GetFiles(_directory, "*.partial"), Is.Empty);
        Assert.That(Directory.Exists(Path.Combine(_directory, "Extracted")), Is.False);
    }

    [Test]
    public async Task TronRemembersAnActiveConsoleAcrossServiceInstances()
    {
        Directory.CreateDirectory(_directory);
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        await File.WriteAllTextAsync(Path.Combine(_directory, "package.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            ProcessId = process.Id, StartedUtcTicks = process.StartTime.ToUniversalTime().Ticks
        }));
        using var http = new HttpClient(new DownloadHandler([], "invalid"));
        var service = new TronService(_directory, http);
        await Assert.ThatAsync(() => service.DownloadAsync(), Throws.TypeOf<InvalidOperationException>());
        await Assert.ThatAsync(() => service.ExtractAsync(), Throws.TypeOf<InvalidOperationException>());
        await Assert.ThatAsync(() => service.RunAsync(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task TronChecksEveryExtractedFileAndRejectsTamperingOrExtraScripts()
    {
        // The real v12.0.8 layout: integrity_verification\ beside tron\, paths relative to the extraction folder, and
        // hashdeep's entry for checksums.txt itself taken before the file was final.
        var root = Path.Combine(_directory, "tron");
        Directory.CreateDirectory(Path.Combine(_directory, "integrity_verification"));
        Directory.CreateDirectory(Path.Combine(root, "resources", "functions"));
        var manifest = new StringBuilder("%%%% HASHDEEP-1.0\n%%%% size,sha256,filename\n## \n");
        manifest.AppendLine($"79358,{new string('1', 64)},.\\integrity_verification\\checksums.txt");
        foreach (var file in new[] { @"tron\tron.bat", @"tron\resources\functions\tron_settings.bat" })
        {
            var path = TronService.CheckedPath(_directory, file);
            await File.WriteAllTextAsync(path, "rem test file");
            var bytes = await File.ReadAllBytesAsync(path);
            manifest.AppendLine($"{bytes.Length},{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()},.\\{file}");
        }
        await File.WriteAllTextAsync(Path.Combine(_directory, "integrity_verification", "checksums.txt"), manifest.ToString());
        await TronService.VerifyFilesAsync(_directory, root);
        var extra = Path.Combine(root, "resources", "extra.bat");
        await File.WriteAllTextAsync(extra, "rem extra");
        await Assert.ThatAsync(() => TronService.VerifyFilesAsync(_directory, root), Throws.TypeOf<InvalidDataException>());
        File.Delete(extra);
        await File.AppendAllTextAsync(Path.Combine(root, "tron.bat"), "rem tampered");
        await Assert.ThatAsync(() => TronService.VerifyFilesAsync(_directory, root), Throws.TypeOf<InvalidDataException>());
    }
}
