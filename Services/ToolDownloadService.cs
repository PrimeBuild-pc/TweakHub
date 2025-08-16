using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using TweakHub.Models;

namespace TweakHub.Services
{
    public class ToolDownloadService
    {
        private static ToolDownloadService? _instance;
        public static ToolDownloadService Instance => _instance ??= new ToolDownloadService();

        private readonly HttpClient _httpClient;
        private readonly string _toolsBasePath = @"C:\TweakHub\Tools";

        public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
        public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;

        private ToolDownloadService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TweakHub/1.0");
        }

        public async Task<bool> InstallWithWinget(ExternalTool tool)
        {
            try
            {
                var command = !string.IsNullOrWhiteSpace(tool.WingetId)
                    ? $"install {tool.WingetId} --accept-source-agreements --accept-package-agreements"
                    : tool.WingetCommand;

                if (string.IsNullOrWhiteSpace(command))
                {
                    return false;
                }

                OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, 0, "Starting installation..."));

                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                process.OutputDataReceived += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data)) return;
                    var percent = TryParsePercent(e.Data);
                    if (percent >= 0)
                    {
                        OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, percent, e.Data));
                    }
                    else
                    {
                        OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, 0, e.Data));
                    }
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, 0, e.Data));
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                var success = process.ExitCode == 0;
                var message = success ? $"Installation completed: {tool.Name}" : $"Installation failed: {tool.Name}";
                OnDownloadCompleted(new DownloadCompletedEventArgs(tool.Name, success, message, tool.PostInstallMessage));
                return success;
            }
            catch (Exception ex)
            {
                OnDownloadCompleted(new DownloadCompletedEventArgs(tool.Name, false, ex.Message));
                return false;
            }
        }

        public async Task<bool> UninstallWithWinget(ExternalTool tool)
        {
            if (string.IsNullOrWhiteSpace(tool.WingetId)) return false;
            try
            {
                OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, 0, "Starting uninstall..."));

                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = $"uninstall {tool.WingetId} --accept-source-agreements --accept-package-agreements",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
                process.OutputDataReceived += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data)) return;
                    var percent = TryParsePercent(e.Data);
                    if (percent >= 0)
                        OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, percent, e.Data));
                    else
                        OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, 0, e.Data));
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                        OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, 0, e.Data));
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                var success = process.ExitCode == 0;
                var message = success ? $"Uninstall completed: {tool.Name}" : $"Uninstall failed: {tool.Name}";
                OnDownloadCompleted(new DownloadCompletedEventArgs(tool.Name, success, message));
                return success;
            }
            catch (Exception ex)
            {
                OnDownloadCompleted(new DownloadCompletedEventArgs(tool.Name, false, ex.Message));
                return false;
            }
        }

        private int TryParsePercent(string data)
        {
            // Look for patterns like " 50%" or "50%" in output
            for (int i = 0; i < data.Length - 2; i++)
            {
                if (char.IsDigit(data[i]))
                {
                    int j = i;
                    int val = 0;
                    while (j < data.Length && char.IsDigit(data[j]))
                    {
                        val = val * 10 + (data[j] - '0');
                        j++;
                    }
                    if (j < data.Length && data[j] == '%')
                    {
                        return Math.Clamp(val, 0, 100);
                    }
                }
            }
            return -1;
        }

        public async Task<bool> DownloadOrOpenTool(ExternalTool tool)
        {
            try
            {
                // Execute PowerShell command directly if defined
                if (!string.IsNullOrWhiteSpace(tool.PowerShellCommand))
                {
                    return await ExecutePowerShellCommand(tool);
                }

                // Use winget if a package ID or command is provided
                if (!string.IsNullOrWhiteSpace(tool.WingetId) || !string.IsNullOrWhiteSpace(tool.WingetCommand))
                {
                    return await InstallWithWinget(tool);
                }

                if (tool.IsDirectDownload && !string.IsNullOrEmpty(tool.DirectDownloadUrl))
                {
                    return await DownloadTool(tool);
                }
                else if (!string.IsNullOrEmpty(tool.DownloadUrl) && tool.DownloadUrl.Contains("github.com") && tool.DownloadUrl.Contains("/releases"))
                {
                    return await DownloadFromGitHub(tool);
                }
                else if (!string.IsNullOrEmpty(tool.DownloadUrl))
                {
                    OpenInBrowser(tool.DownloadUrl);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing tool {tool.Name}: {ex.Message}",
                    "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async Task<bool> DownloadTool(ExternalTool tool)
        {
            try
            {
                var toolPath = CreateToolDirectory(tool);
                var fileName = GetFileNameFromUrl(tool.DirectDownloadUrl);
                var filePath = Path.Combine(toolPath, fileName);

                OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, 0, "Starting download..."));

                using var response = await _httpClient.GetAsync(tool.DirectDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        var progress = (int)((downloadedBytes * 100) / totalBytes);
                        OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, progress, 
                            $"Downloaded {FormatBytes(downloadedBytes)} of {FormatBytes(totalBytes)}"));
                    }
                }

                OnDownloadCompleted(new DownloadCompletedEventArgs(tool.Name, true, $"Download completed: {tool.Name}"));

                // Try to run installer if it's an executable
                if (fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    var result = MessageBox.Show($"Download completed! Would you like to run the installer for {tool.Name}?",
                        "Installation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnDownloadCompleted(new DownloadCompletedEventArgs(tool.Name, false, ex.Message));
                return false;
            }
        }

        private async Task<bool> DownloadFromGitHub(ExternalTool tool)
        {
            try
            {
                var repoUrl = ExtractGitHubRepoUrl(tool.DownloadUrl);
                var apiUrl = $"https://api.github.com/repos/{repoUrl}/releases/latest";

                OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, 0, "Fetching latest release..."));

                var response = await _httpClient.GetStringAsync(apiUrl);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (release?.Assets != null && release.Assets.Count > 0)
                {
                    // Try to find the best asset (prefer .exe, .msi, .zip)
                    var asset = FindBestAsset(release.Assets);
                    
                    if (asset != null)
                    {
                        var modifiedTool = new ExternalTool
                        {
                            Name = tool.Name,
                            Category = tool.Category,
                            DirectDownloadUrl = asset.BrowserDownloadUrl,
                            IsDirectDownload = true
                        };

                        return await DownloadTool(modifiedTool);
                    }
                }

                // Fallback to opening in browser
                OpenInBrowser(tool.DownloadUrl);
                return true;
            }
            catch (Exception)
            {
                // Fallback to opening in browser
                OpenInBrowser(tool.DownloadUrl);
                return true;
            }
        }

        private string CreateToolDirectory(ExternalTool tool)
        {
            var categoryPath = Path.Combine(_toolsBasePath, SanitizeFileName(tool.Category));
            var toolPath = Path.Combine(categoryPath, SanitizeFileName(tool.Name));
            
            Directory.CreateDirectory(toolPath);
            return toolPath;
        }

        private void OpenInBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening URL: {ex.Message}", "Browser Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ExtractGitHubRepoUrl(string url)
        {
            // Extract owner/repo from GitHub URL
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (segments.Length >= 2)
            {
                return $"{segments[0]}/{segments[1]}";
            }
            
            throw new ArgumentException("Invalid GitHub URL format");
        }

        private GitHubAsset? FindBestAsset(List<GitHubAsset> assets)
        {
            // Priority order: .exe, .msi, .zip, others
            var priorities = new[] { ".exe", ".msi", ".zip" };
            
            foreach (var priority in priorities)
            {
                var asset = assets.FirstOrDefault(a => a.Name.EndsWith(priority, StringComparison.OrdinalIgnoreCase));
                if (asset != null) return asset;
            }
            
            return assets.FirstOrDefault();
        }

        private string GetFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var fileName = Path.GetFileName(uri.LocalPath);
                return string.IsNullOrEmpty(fileName) ? "download" : fileName;
            }
            catch
            {
                return "download";
            }
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }

        private void OnDownloadProgress(DownloadProgressEventArgs e)
        {
            DownloadProgress?.Invoke(this, e);
        }

        private void OnDownloadCompleted(DownloadCompletedEventArgs e)
        {
            DownloadCompleted?.Invoke(this, e);
        }

        private async Task<bool> ExecutePowerShellCommand(ExternalTool tool)
        {
            try
            {
                OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, 0, "Starting PowerShell script..."));

                if (tool.RequiresAdmin)
                {
                    var psiAdmin = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{tool.PowerShellCommand}\"",
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = false
                    };

                    Process.Start(psiAdmin);
                    OnDownloadCompleted(new DownloadCompletedEventArgs(tool.Name, true, $"Launched elevated PowerShell for {tool.Name}."));
                    return true;
                }
                else
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{tool.PowerShellCommand}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                            OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, 0, e.Data));
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                            OnDownloadProgress(new DownloadProgressEventArgs(tool.Name, 0, e.Data));
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();

                    var success = process.ExitCode == 0;
                    OnDownloadCompleted(new DownloadCompletedEventArgs(tool.Name, success, success ? $"Script completed: {tool.Name}" : $"Script failed: {tool.Name}"));
                    return success;
                }
            }
            catch (Exception ex)
            {
                OnDownloadCompleted(new DownloadCompletedEventArgs(tool.Name, false, ex.Message));
                return false;
            }
        }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public string ToolName { get; }
        public int ProgressPercentage { get; }
        public string StatusMessage { get; }

        public DownloadProgressEventArgs(string toolName, int progressPercentage, string statusMessage)
        {
            ToolName = toolName;
            ProgressPercentage = progressPercentage;
            StatusMessage = statusMessage;
        }
    }

    public class DownloadCompletedEventArgs : EventArgs
    {
        public string ToolName { get; }
        public bool Success { get; }
        public string Message { get; }
        public string PostInstallMessage { get; }

        public DownloadCompletedEventArgs(string toolName, bool success, string message, string postInstallMessage = "")
        {
            ToolName = toolName;
            Success = success;
            Message = message;
            PostInstallMessage = postInstallMessage;
        }
    }

    public class GitHubRelease
    {
        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    public class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
