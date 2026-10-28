using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

/// <summary>Fixes from the security review: elevated scripts, backups and JSON writes.</summary>
public class ElevationHardeningTests
{
    private static async Task<(int Code, string Output, string Error)> RunWrapperAsync(string wrapper)
    {
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand",
                     Convert.ToBase64String(Encoding.Unicode.GetBytes(wrapper)) })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        await process.WaitForExitAsync();
        return (process.ExitCode, string.Empty, string.Empty);
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
            var output = Path.Combine(folder.FullName, "out.txt");
            var error = Path.Combine(folder.FullName, "err.txt");
            var approved = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes("Write-Output 'approved-run'")).ToArray();
            await File.WriteAllBytesAsync(script, approved);
            var wrapper = PowerShellService.BuildElevatedWrapper(script, approved, output, error);

            var (code, _, _) = await RunWrapperAsync(wrapper);
            Assert.That(code, Is.Zero);
            Assert.That(await File.ReadAllTextAsync(output), Does.Contain("approved-run"));

            File.Delete(output);
            await File.WriteAllTextAsync(script, "Write-Output 'swapped-run'");
            (code, _, _) = await RunWrapperAsync(wrapper);
            Assert.That(code, Is.EqualTo(1));
            Assert.That(await File.ReadAllTextAsync(error), Does.Contain("changed after it was approved"));
            Assert.That(File.Exists(output) ? await File.ReadAllTextAsync(output) : string.Empty, Does.Not.Contain("swapped-run"));
        }
        finally { folder.Delete(true); }
    }
}
