using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;
using TweakHub.Models;
using TweakHub.Views.Dialogs;

namespace TweakHub.Services
{
    public class ToolDownloadService
    {
        public static ToolDownloadService Instance { get; } = new();

        public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
        public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;

        private ToolDownloadService() { }

        public async Task<bool> InstallWithWinget(ExternalTool tool)
        {
            if (string.IsNullOrWhiteSpace(tool.WingetId)) return false;
            var arguments = $"install --id \"{tool.WingetId}\" --exact --accept-source-agreements --accept-package-agreements";
            var success = await RunWinget(tool, arguments, "installation");
            if (success && tool.Category.Equals("System Utilities", StringComparison.OrdinalIgnoreCase))
            {
                string? path = null;
                var aliasAvailable = false;
                try
                {
                    path = ResolveExecutable(tool);
                    aliasAvailable = !string.IsNullOrWhiteSpace(tool.TerminalCommand) && ResolveOnPath(tool.TerminalCommand) != null;
                }
                catch { }
                await AppDialog.ShowAsync(Application.Current.MainWindow, $"{tool.Name} Installed",
                    BuildLaunchHint(tool, path, aliasAvailable));
            }
            return success;
        }

        public Task<bool> UninstallWithWinget(ExternalTool tool) =>
            RunWinget(
                tool,
                $"uninstall --id \"{tool.WingetId}\" --exact --accept-source-agreements",
                "uninstall");

        public async Task<bool> DownloadOrOpenTool(ExternalTool tool)
        {
            if (!string.IsNullOrWhiteSpace(tool.PowerShellCommand))
            {
                var preview = tool.PowerShellCommand.Length > 1200 ? tool.PowerShellCommand[..1200] + "…" : tool.PowerShellCommand;
                if (!await AppDialog.ConfirmAsync(Application.Current.MainWindow, "Run Custom PowerShell Command",
                        $"Only run commands you trust. This command can download or modify software.\n\n{preview}", "Run", "Cancel"))
                    return false;

                Progress(tool.Name, 0, "Running PowerShell command...");
                var result = await PowerShellService.Instance.ExecuteScriptAsync(
                    tool.PowerShellCommand, tool.RequiresAdministrator, TimeSpan.FromMinutes(15));
                var details = result.Success ? result.Output : result.Error;
                if (details.Length > 3000) details = details[^3000..];
                Complete(tool.Name, result.Success, result.Success ? "Command completed." : "Command failed.");
                await AppDialog.ShowAsync(Application.Current.MainWindow,
                    result.Success ? "Command Completed" : "Command Failed",
                    $"{tool.Name} finished in {result.Duration.TotalSeconds:F1} seconds.\n\n{details}".Trim());
                return result.Success;
            }

            if (!string.IsNullOrWhiteSpace(tool.WingetId))
                return await InstallWithWinget(tool);

            if (Uri.TryCreate(tool.DownloadUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
                    return true;
                }
                catch (Exception ex)
                {
                    Complete(tool.Name, false, ex.Message);
                }
            }

            return false;
        }

        private async Task<bool> RunWinget(ExternalTool tool, string arguments, string action)
        {
            if (string.IsNullOrWhiteSpace(arguments)) return false;

            Progress(tool.Name, 0, $"Starting {action}...");

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.OutputDataReceived += (_, e) => ReportWingetOutput(tool.Name, e.Data);
                process.ErrorDataReceived += (_, e) => ReportWingetOutput(tool.Name, e.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                var success = process.ExitCode == 0;
                Complete(
                    tool.Name,
                    success,
                    success ? $"{action} completed: {tool.Name}" : $"{action} failed: {tool.Name}");
                return success;
            }
            catch (Exception ex)
            {
                Complete(tool.Name, false, ex.Message);
                return false;
            }
        }

        internal static string BuildLaunchHint(ExternalTool tool, string? resolvedPath, bool terminalCommandAvailable = true)
        {
            var lines = new List<string> { "Installation completed." };
            if (terminalCommandAvailable && !string.IsNullOrWhiteSpace(tool.TerminalCommand))
                lines.Add($"Terminal command: {tool.TerminalCommand}");
            if (!string.IsNullOrWhiteSpace(resolvedPath))
                lines.Add($"{(Directory.Exists(resolvedPath) ? "Installation location" : "Executable")}: \"{resolvedPath}\"");
            if (lines.Count == 1)
            {
                lines.Add("WinGet did not expose a terminal alias or executable path.");
                lines.Add($"Package: {tool.WingetId}");
                lines.Add($"Check with: winget list --id \"{tool.WingetId}\" --exact");
            }
            return string.Join(Environment.NewLine, lines);
        }

        private static string? ResolveExecutable(ExternalTool tool)
        {
            var names = new[] { tool.TerminalCommand, tool.ExecutableName }
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
            {
                var path = ResolveOnPath(name) ?? ResolveAppPath(name);
                if (path != null) return path;
            }
            return ResolveInstallLocation(tool, tool.ExecutableName);
        }

        private static string? ResolveOnPath(string executable)
        {
            if (Path.IsPathRooted(executable) && File.Exists(executable)) return Path.GetFullPath(executable);
            var directories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Links"));
            var names = Path.HasExtension(executable)
                ? new[] { executable }
                : new[] { executable + ".exe", executable + ".cmd", executable + ".bat" };
            return directories.SelectMany(directory => names.Select(name => Path.Combine(directory.Trim('"'), name)))
                .FirstOrDefault(File.Exists);
        }

        private static string? ResolveAppPath(string executable)
        {
            foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                foreach (var prefix in new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\"
                })
                {
                    using var key = root.OpenSubKey(prefix + executable);
                    if (key?.GetValue(null) is string path && File.Exists(path)) return path;
                }
            }
            return null;
        }

        private static string? ResolveInstallLocation(ExternalTool tool, string executable)
        {
            foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                foreach (var prefix in new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                })
                {
                    using var uninstall = root.OpenSubKey(prefix);
                    if (uninstall == null) continue;
                    foreach (var name in uninstall.GetSubKeyNames())
                    {
                        using var entry = uninstall.OpenSubKey(name);
                        var displayName = entry?.GetValue("DisplayName") as string ?? string.Empty;
                        if (!displayName.Contains(tool.Name, StringComparison.OrdinalIgnoreCase)
                            && !name.Contains(tool.WingetId, StringComparison.OrdinalIgnoreCase)) continue;
                        var location = entry?.GetValue("InstallLocation") as string;
                        if (string.IsNullOrWhiteSpace(location)) continue;
                        var candidate = string.IsNullOrWhiteSpace(executable) ? null : Path.Combine(location, executable);
                        return candidate != null && File.Exists(candidate) ? candidate : location;
                    }
                }
            }
            return null;
        }

        private void ReportWingetOutput(string toolName, string? data)
        {
            if (string.IsNullOrWhiteSpace(data)) return;
            var percentage = TryParsePercent(data);
            Progress(toolName, percentage < 0 ? 0 : percentage, data);
        }

        internal static int TryParsePercent(string data)
        {
            var match = Regex.Match(data, @"(\d+)%");
            return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? Math.Clamp(value, 0, 100) : -1;
        }

        private void Progress(string toolName, int percentage, string message) =>
            DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(toolName, percentage, message));

        private void Complete(string toolName, bool success, string message) =>
            DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs(toolName, success, message));
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

        public DownloadCompletedEventArgs(string toolName, bool success, string message)
        {
            ToolName = toolName;
            Success = success;
            Message = message;
        }
    }
}
