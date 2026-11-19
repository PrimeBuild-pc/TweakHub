using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class UpdateServiceTests
{
    [TestCase(".zip")]
    [TestCase(".exe")]
    public async Task VerifiedDownloadCanBeReadAndStaysLockedAgainstWrites(string extension)
    {
        var directory = Directory.CreateTempSubdirectory("TweakHubDownload-");
        try
        {
            var package = new UpdateService.GitHubAsset { Name = "fixture" + extension, DownloadUrl = PackageUrl, Size = PackageBytes.Length };
            var checksum = new UpdateService.GitHubAsset { DownloadUrl = ChecksumUrl };
            using var client = new HttpClient(new DownloadHandler(Convert.ToHexString(SHA256.HashData(PackageBytes))));
            await using var verified = await UpdateService.DownloadAndVerifyAsync(client, package, checksum,
                new DownloadProgress(), CancellationToken.None, directory.FullName);

            using var bytes = new MemoryStream();
            await verified.CopyToAsync(bytes);
            Assert.That(bytes.ToArray(), Is.EqualTo(PackageBytes));
            Assert.Throws<IOException>(() =>
            {
                using var writer = new FileStream(verified.Name, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            });
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [TestCase("corrupt")]
    [TestCase("truncated")]
    [TestCase("cancelled")]
    public void FailedDownloadRemovesTheTemporaryFile(string failure)
    {
        var directory = Directory.CreateTempSubdirectory("TweakHubDownload-");
        try
        {
            var size = failure == "truncated" ? PackageBytes.Length + 1 : PackageBytes.Length;
            var hash = failure == "corrupt" ? new string('0', 64) : Convert.ToHexString(SHA256.HashData(PackageBytes));
            using var client = new HttpClient(new DownloadHandler(hash));
            using var cancellation = new CancellationTokenSource();
            var package = new UpdateService.GitHubAsset { Name = "fixture.zip", DownloadUrl = PackageUrl, Size = size };
            var checksum = new UpdateService.GitHubAsset { DownloadUrl = ChecksumUrl };
            var progress = new DownloadProgress(failure == "cancelled" ? cancellation : null);

            async Task DownloadAsync()
            {
                await using var file = await UpdateService.DownloadAndVerifyAsync(client, package, checksum, progress,
                    cancellation.Token, directory.FullName);
            }

            if (failure == "cancelled") Assert.CatchAsync<OperationCanceledException>(DownloadAsync);
            else Assert.ThrowsAsync<InvalidDataException>(DownloadAsync);
            Assert.That(directory.EnumerateFileSystemInfos(), Is.Empty);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private const string PackageUrl = "https://github.com/fixture/package";
    private const string ChecksumUrl = "https://github.com/fixture/package.sha256";
    private static readonly byte[] PackageBytes = [1, 2, 3, 4];

    private sealed class DownloadHandler(string hash) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = request.RequestUri!.AbsoluteUri == ChecksumUrl
                    ? new StringContent(hash)
                    : new ByteArrayContent(PackageBytes)
            });
    }

    private sealed class DownloadProgress(CancellationTokenSource? cancellation = null) : IProgress<double>
    {
        public void Report(double value) => cancellation?.Cancel();
    }

    [Test]
    public void VersionComparisonHandlesStableAndNumericPrereleases()
    {
        Assert.Multiple(() =>
        {
            Assert.That(UpdateService.IsNewerVersion("0.2.0-beta2", "0.2.0-beta10"), Is.True);
            Assert.That(UpdateService.IsNewerVersion("0.2.0-beta", "0.2.0"), Is.True);
            Assert.That(UpdateService.IsNewerVersion("0.2.0", "0.2.0-beta1"), Is.False);
            Assert.That(UpdateService.IsNewerVersion("0.2.0", "0.3.0"), Is.True);
            Assert.That(UpdateService.IsNewerVersion("1.0.0", "1.0.0"), Is.False);
        });
    }

    [Test]
    public void PortableUpdaterWaitsAndPreservesDataAndExistingApps()
    {
        var script = UpdateService.CreatePortableUpdateScript(@"C:\source", @"E:\TweakHub", @"C:\temp", @"C:\update.zip", 123);
        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("Wait-Process -Id 123"));
            Assert.That(script, Does.Contain("Name -ne 'Data'"));
            Assert.That(script, Does.Contain(@"E:\TweakHub\TweakHub.exe"));
            Assert.That(script, Does.Not.Contain("Remove-Item -LiteralPath 'E:\\TweakHub\\Apps'"));
        });
    }

    [Test]
    public async Task PortableUpdaterParsesWithTypographicQuotesAndNonAsciiPaths()
    {
        var script = UpdateService.CreatePortableUpdateScript(
            @"C:\Temp\src", "C:\\Users\\D\u2019Angelo\\Città", @"C:\Temp\it's", @"C:\Temp\u.zip", 1);
        var result = await PowerShellService.Instance.ExecuteScriptAsync(
            $"$errors = $null; [System.Management.Automation.Language.Parser]::ParseInput({PowerShellService.Literal(script)}, [ref]$null, [ref]$errors) | Out-Null; $errors.Count");
        Assert.That(result.Output.Trim(), Is.EqualTo("0"), result.Output + result.Error);
    }
}
