using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using TweakHub.Views;
using TweakHub.Views.Dialogs;

namespace TweakHub.Services;

public sealed class UpdateService
{
    private const string ReleasesApi = "https://api.github.com/repos/PrimeBuild-pc/TweakHub/releases?per_page=20";
    public static string CurrentVersion { get; } =
        typeof(UpdateService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? typeof(UpdateService).Assembly.GetName().Version?.ToString(3)
        ?? "0.0.0";
    private static readonly HttpClient HttpClient = CreateClient();
    private static UpdateService? _instance;

    public static UpdateService Instance => _instance ??= new UpdateService();

    private UpdateService() { }

    public async Task CheckAndPromptAsync(Window owner, bool showNoUpdate, CancellationToken cancellationToken = default)
    {
        var accepted = false;
        ProgressWindow? progressWindow = null;
        try
        {
            var update = await FindUpdateAsync(cancellationToken);
            if (update == null)
            {
                if (showNoUpdate) StyledMessageDialog.ShowOk(owner, "Check for Updates", "You're running the latest version.");
                return;
            }

            accepted = StyledMessageDialog.ShowConfirm(owner, "Update Available",
                $"TweakHub {update.Version} is available.\n\nInstalled: {CurrentVersion}\n\nDownload, verify and install it now?",
                "Update", "Later");
            if (!accepted) return;

            var package = AppDataPath.IsPortable
                ? update.Assets.FirstOrDefault(asset => asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                    && asset.Name.Contains("portable", StringComparison.OrdinalIgnoreCase))
                : update.Assets.FirstOrDefault(asset => asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    && asset.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase));
            var checksum = package == null ? null : update.Assets.FirstOrDefault(asset =>
                asset.Name.Equals(package.Name + ".sha256", StringComparison.OrdinalIgnoreCase));
            if (package == null || checksum == null)
                throw new InvalidOperationException("This release does not contain a verified package for this installation mode.");

            progressWindow = new ProgressWindow("Updating TweakHub") { Owner = owner };
            progressWindow.UpdateStatus("Downloading verified update...");
            progressWindow.Show();
            var progress = new Progress<double>(progressWindow.UpdateProgress);
            var packagePath = await DownloadAndVerifyAsync(package, checksum, progress, cancellationToken);
            progressWindow.Close();
            progressWindow = null;

            if (AppDataPath.IsPortable)
                LaunchPortableUpdate(packagePath);
            else
                Process.Start(new ProcessStartInfo(packagePath)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS"
                });
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            progressWindow?.Close();
            if (showNoUpdate || accepted)
                StyledMessageDialog.ShowOk(owner, "Update Failed", $"TweakHub could not update automatically.\n\n{ex.Message}");
        }
    }

    private static async Task<AvailableUpdate?> FindUpdateAsync(CancellationToken cancellationToken)
    {
        var json = await HttpClient.GetStringAsync(ReleasesApi, cancellationToken);
        var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json) ?? [];
        var betaChannel = CurrentVersion.Contains('-');
        var release = releases.FirstOrDefault(item => !item.Draft && item.Prerelease == betaChannel
            && IsNewerVersion(CurrentVersion, item.TagName.TrimStart('v')));
        return release == null ? null : new AvailableUpdate(release.TagName.TrimStart('v'), release.Assets);
    }

    private static async Task<string> DownloadAndVerifyAsync(
        GitHubAsset package, GitHubAsset checksum, IProgress<double> progress, CancellationToken cancellationToken)
    {
        ValidateGitHubAsset(package);
        ValidateGitHubAsset(checksum);
        var checksumText = await HttpClient.GetStringAsync(checksum.DownloadUrl, cancellationToken);
        var expectedHash = Regex.Match(checksumText, "[A-Fa-f0-9]{64}").Value;
        if (expectedHash.Length != 64) throw new InvalidDataException("The release checksum is missing or invalid.");

        var path = Path.Combine(Path.GetTempPath(), $"TweakHub-{Guid.NewGuid():N}{Path.GetExtension(package.Name)}");
        try
        {
            using var response = await HttpClient.GetAsync(package.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var total = package.Size > 0 ? package.Size : response.Content.Headers.ContentLength ?? 0;
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            var buffer = new byte[81920];
            long downloaded = 0;
            int read;
            while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                downloaded += read;
                if (total > 0) progress.Report(downloaded * 100d / total);
            }
            await output.FlushAsync(cancellationToken);
            if (package.Size > 0 && downloaded != package.Size) throw new InvalidDataException("The update download is incomplete.");

            await using var file = File.OpenRead(path);
            var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(file, cancellationToken));
            if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The update checksum does not match the release.");
            progress.Report(100);
            return path;
        }
        catch
        {
            try { File.Delete(path); } catch { }
            throw;
        }
    }

    private static void LaunchPortableUpdate(string zipPath)
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHub-update-{Guid.NewGuid():N}");
        var payload = Path.Combine(root, "payload");
        Directory.CreateDirectory(payload);
        ZipFile.ExtractToDirectory(zipPath, payload);
        var executable = Directory.GetFiles(payload, "TweakHub.exe", SearchOption.AllDirectories).SingleOrDefault()
            ?? throw new InvalidDataException("The portable archive does not contain TweakHub.exe.");
        var source = Path.GetDirectoryName(executable)!;
        var target = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var scriptPath = Path.Combine(root, "update.ps1");
        File.WriteAllText(scriptPath, CreatePortableUpdateScript(source, target, root, zipPath, Environment.ProcessId));
        Process.Start(new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            ArgumentList = { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", scriptPath }
        });
    }

    internal static string CreatePortableUpdateScript(string source, string target, string root, string zipPath, int processId)
    {
        static string Quote(string value) => value.Replace("'", "''");
        return $$"""
            $ErrorActionPreference = 'Stop'
            Wait-Process -Id {{processId}} -ErrorAction SilentlyContinue
            Get-ChildItem -LiteralPath '{{Quote(source)}}' | Where-Object Name -ne 'Data' | Copy-Item -Destination '{{Quote(target)}}' -Recurse -Force
            Start-Process -FilePath '{{Quote(Path.Combine(target, "TweakHub.exe"))}}'
            Start-Process powershell.exe -WindowStyle Hidden -ArgumentList '-NoProfile','-Command',"Start-Sleep 2; Remove-Item -LiteralPath ''{{Quote(root)}}'' -Recurse -Force; Remove-Item -LiteralPath ''{{Quote(zipPath)}}'' -Force"
            """;
    }

    private static void ValidateGitHubAsset(GitHubAsset asset)
    {
        if (!Uri.TryCreate(asset.DownloadUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The release contains an untrusted download URL.");
    }

    public static bool IsNewerVersion(string current, string latest)
    {
        var currentParts = current.TrimStart('v').Split('-', 2);
        var latestParts = latest.TrimStart('v').Split('-', 2);
        if (!Version.TryParse(currentParts[0], out var currentVersion)
            || !Version.TryParse(latestParts[0], out var latestVersion)) return false;

        var comparison = latestVersion.CompareTo(currentVersion);
        if (comparison != 0) return comparison > 0;
        if (currentParts.Length != latestParts.Length) return currentParts.Length == 2;
        if (currentParts.Length == 1) return false;
        return ComparePrerelease(currentParts[1], latestParts[1]) < 0;
    }

    private static int ComparePrerelease(string left, string right)
    {
        var leftParts = Regex.Matches(left, "[A-Za-z]+|[0-9]+").Select(match => match.Value).ToArray();
        var rightParts = Regex.Matches(right, "[A-Za-z]+|[0-9]+").Select(match => match.Value).ToArray();
        for (var index = 0; index < Math.Max(leftParts.Length, rightParts.Length); index++)
        {
            if (index >= leftParts.Length) return -1;
            if (index >= rightParts.Length) return 1;
            var leftNumber = int.TryParse(leftParts[index], out var l);
            var rightNumber = int.TryParse(rightParts[index], out var r);
            var comparison = leftNumber && rightNumber ? l.CompareTo(r)
                : leftNumber ? -1
                : rightNumber ? 1
                : string.Compare(leftParts[index], rightParts[index], StringComparison.OrdinalIgnoreCase);
            if (comparison != 0) return comparison;
        }
        return 0;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"TweakHub/{CurrentVersion}");
        return client;
    }

    private sealed record AvailableUpdate(string Version, List<GitHubAsset> Assets);

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string TagName { get; set; } = string.Empty;
        [JsonPropertyName("draft")] public bool Draft { get; set; }
        [JsonPropertyName("prerelease")] public bool Prerelease { get; set; }
        [JsonPropertyName("assets")] public List<GitHubAsset> Assets { get; set; } = [];
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("browser_download_url")] public string DownloadUrl { get; set; } = string.Empty;
        [JsonPropertyName("size")] public long Size { get; set; }
    }
}
