using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

/// <summary>Fixes from the security review: elevated scripts, backups and JSON writes.</summary>
public class ElevationHardeningTests
{
    internal static async Task<(int Code, string Output, string Error)> RunWrapperAsync(string wrapper, string pipeName)
    {
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand",
                     Convert.ToBase64String(Encoding.Unicode.GetBytes(wrapper)) })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var text = await PowerShellService.ReadElevatedOutputAsync(pipeName, process, timeout.Token);
        await process.WaitForExitAsync(timeout.Token);
        var (output, error) = PowerShellService.SplitElevatedOutput(text);
        return (process.ExitCode, output, error);
    }

    // The elevated wrapper, run unelevated here: the pipe and the hash check work the same way.
    internal static async Task<(int Code, string Output, string Error)> RunThroughWrapperAsync(string body)
    {
        var folder = Directory.CreateTempSubdirectory("TweakHubWrapper-");
        try
        {
            var script = Path.Combine(folder.FullName, "script.ps1");
            var encoding = new UTF8Encoding(true);
            var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(PowerShellService.BuildScriptContext(body))).ToArray();
            await File.WriteAllBytesAsync(script, bytes);
            var pipe = "TweakHubTest-" + Guid.NewGuid().ToString("N");
            return await RunWrapperAsync(PowerShellService.BuildElevatedWrapper(script, bytes, pipe, PowerShellService.CurrentUserSid), pipe);
        }
        finally { folder.Delete(true); }
    }

    [Test]
    public async Task ThirdPartyScriptRunsOnlyTheApprovedCopy()
    {
        var folder = Directory.CreateTempSubdirectory("TweakHubThirdParty-");
        try
        {
            var script = ThirdPartyScriptService.MariusHeier.First();
            async Task<string> Run(string content, string approvedHash)
            {
                var path = Path.Combine(folder.FullName, $"{Guid.NewGuid():N}.ps1");
                await File.WriteAllTextAsync(path, content, new UTF8Encoding(true));
                var start = new ProcessStartInfo("powershell.exe",
                    ThirdPartyScriptService.BuildArguments(script, path, approvedHash).Replace("-NoExit ", string.Empty))
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var process = Process.Start(start)!;
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                Assert.That(File.Exists(path), Is.False, "the copy is deleted before anything runs");
                return output;
            }

            const string approved = "Write-Output 'approved-run'";
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(approved)).ToArray()));
            Assert.That(await Run(approved, hash), Does.Contain("approved-run"));
            var swapped = await Run("Write-Output 'swapped-run'", hash);
            Assert.That(swapped, Does.Not.Contain("swapped-run"));
            Assert.That(swapped, Does.Contain("changed after it was approved"));
        }
        finally { folder.Delete(true); }
    }

    [Test]
    public void BackupFileWithNullEntriesDoesNotStopStartup()
    {
        var folder = Directory.CreateTempSubdirectory("TweakHubBackups-");
        try
        {
            File.WriteAllText(Path.Combine(folder.FullName, "registry-backup.json"),
                """[null, {"KeyPath":null,"ValueName":"x"}, {"KeyPath":"HKCU\\Software\\T","ValueName":"V","Existed":false,"Kind":4}]""");
            var service = new RegistryService(folder.FullName);
            service.Initialize();
            Assert.That(service.BackedUpValues, Has.Count.EqualTo(1));
        }
        finally { folder.Delete(true); }
    }

    [Test]
    public void ParallelSavesOfOneFileNeverFailOrLeaveTempFiles()
    {
        var folder = Directory.CreateTempSubdirectory("TweakHubJson-");
        try
        {
            var path = Path.Combine(folder.FullName, "data.json");
            Parallel.For(0, 64, index => JsonStore.Save(path, new[] { index }));
            Assert.That(JsonStore.Load<List<int>>(path), Has.Count.EqualTo(1));
            Assert.That(Directory.GetFiles(folder.FullName, "*.tmp"), Is.Empty);
        }
        finally { folder.Delete(true); }
    }

    [Test]
    public async Task ElevatedWrapperRunsTheApprovedScriptAndRefusesASwappedOne()
    {
        var folder = Directory.CreateTempSubdirectory("TweakHubWrapper-");
        try
        {
            var script = Path.Combine(folder.FullName, "script.ps1");
            var approved = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes("Write-Output 'approved-run'")).ToArray();
            await File.WriteAllBytesAsync(script, approved);
            var pipe = "TweakHubTest-" + Guid.NewGuid().ToString("N");
            var wrapper = PowerShellService.BuildElevatedWrapper(script, approved, pipe, PowerShellService.CurrentUserSid);

            var (code, output, _) = await RunWrapperAsync(wrapper, pipe);
            Assert.That(code, Is.Zero);
            Assert.That(output, Does.Contain("approved-run"));

            await File.WriteAllTextAsync(script, "Write-Output 'swapped-run'");
            (code, output, var error) = await RunWrapperAsync(wrapper, pipe);
            Assert.That(code, Is.EqualTo(1));
            Assert.That(error, Does.Contain("changed after it was approved"));
            Assert.That(output, Does.Not.Contain("swapped-run"));
        }
        finally { folder.Delete(true); }
    }

    [Test]
    public async Task ElevatedOutputSurvivesExitErrorsAndNonAsciiText()
    {
        var (code, output, error) = await RunThroughWrapperAsync("Write-Output 'before-exit'; Write-Host 'host-line'; exit 7");
        Assert.That(code, Is.EqualTo(7));
        Assert.That(output, Does.Contain("before-exit").And.Contain("host-line"));
        Assert.That(error, Is.Empty);

        (code, output, error) = await RunThroughWrapperAsync("Write-Output 'partial'; throw 'boom-failure'");
        Assert.That(code, Is.EqualTo(1));
        Assert.That(output, Does.Contain("partial"));
        Assert.That(error, Does.Contain("boom-failure"));

        (code, output, _) = await RunThroughWrapperAsync("Write-Output 'Città – 日本語 – Привет'");
        Assert.That(code, Is.Zero);
        Assert.That(output, Does.Contain("Città – 日本語 – Привет"));
    }

    [Test]
    public async Task ScriptsUseWindowsFoldersEvenWhenTheUserRedefinesThem()
    {
        // A redefined SystemRoot stops powershell.exe from starting at all, so it is not a way in.
        var names = new[] { "windir", "ProgramData", "ProgramFiles" };
        var saved = names.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var name in names) Environment.SetEnvironmentVariable(name, @"C:\Users\Public\Fake" + name);
            var (code, output, error) = await RunThroughWrapperAsync(
                "Write-Output \"$env:windir|$env:SystemRoot|$env:ProgramData|$env:ProgramFiles|$env:SystemDrive|$env:PSModulePath\"");
            Assert.That(code, Is.Zero, error);
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var drive = Path.GetPathRoot(windows)!.TrimEnd('\\');
            var expected = string.Join("|", windows, windows, drive + @"\ProgramData",
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), drive) + "|";
            Assert.That(output.Trim(), Does.StartWith(expected));
            Assert.That(output, Does.Not.Contain("Fake").And.Not.Contain("Documents"));
        }
        finally
        {
            foreach (var (name, value) in saved) Environment.SetEnvironmentVariable(name, value);
        }
    }
}
