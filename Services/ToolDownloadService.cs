using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;
using TweakHub.Localization;
using TweakHub.Models;
using TweakHub.Views.Dialogs;

namespace TweakHub.Services
{
    public sealed record ToolProgress(string ToolName, int Percentage, string Message, bool IsCompleted = false, bool Success = false);

    public class ToolDownloadService
    {
        public static ToolDownloadService Instance { get; } = new();

        private ToolDownloadService() { }

        public async Task<bool> InstallWithWinget(ExternalTool tool, IProgress<ToolProgress>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(tool.WingetId)) return false;
            var arguments = $"install --id \"{tool.WingetId}\" --exact --accept-source-agreements --accept-package-agreements";
            var success = await RunWinget(tool, arguments, "installation", progress);
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
                await AppDialog.ShowAsync(Application.Current.MainWindow, L.Format("Tools:ToolInstalledTitle", tool.Name),
                    BuildLaunchHint(tool, path, aliasAvailable));
            }
            return success;
        }

        public Task<bool> UninstallWithWinget(ExternalTool tool, IProgress<ToolProgress>? progress = null) =>
            RunWinget(
                tool,
                $"uninstall --id \"{tool.WingetId}\" --exact --accept-source-agreements",
                "uninstall",
                progress);

        public async Task<bool> DownloadOrOpenTool(ExternalTool tool, IProgress<ToolProgress>? progress = null)
        {
            if (!string.IsNullOrWhiteSpace(tool.PowerShellCommand))
            {
                var preview = tool.PowerShellCommand.Length > 1200 ? tool.PowerShellCommand[..1200] + "…" : tool.PowerShellCommand;
                if (!await AppDialog.ConfirmAsync(Application.Current.MainWindow, L.Get("Tools:RunPowerShellTitle"),
                        L.Format("Tools:RunPowerShellMessage", preview), L.Get("Tools:Run"), L.Get("Tools:Cancel")))
                    return false;

                Report(progress, tool.Name, 0, L.Get("Tools:RunningPowerShell"));
                var result = await PowerShellService.Instance.ExecuteScriptAsync(
                    tool.PowerShellCommand, tool.RequiresAdministrator, TimeSpan.FromMinutes(15));
                var details = result.Success ? result.Output : result.Error;
                if (details.Length > 3000) details = details[^3000..];
                Complete(progress, tool.Name, result.Success, L.Get(result.Success ? "Tools:CommandCompleted" : "Tools:CommandFailed"));
                await AppDialog.ShowAsync(Application.Current.MainWindow,
                    L.Get(result.Success ? "Tools:CommandCompletedTitle" : "Tools:CommandFailedTitle"),
                    L.Format("Tools:CommandFinishedMessage", tool.Name, result.Duration.TotalSeconds, details).Trim());
                return result.Success;
            }

            if (!string.IsNullOrWhiteSpace(tool.WingetId))
                return await InstallWithWinget(tool, progress);

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
                    Complete(progress, tool.Name, false, ex.Message);
                }
            }

            return false;
        }

        private async Task<bool> RunWinget(ExternalTool tool, string arguments, string action, IProgress<ToolProgress>? progress)
        {
            if (string.IsNullOrWhiteSpace(arguments)) return false;

            var actionText = L.Get(action == "installation" ? "Tools:ActionInstallation" : "Tools:ActionUninstall");
            Report(progress, tool.Name, 0, L.Format("Tools:StartingAction", actionText));

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

                process.OutputDataReceived += (_, e) => ReportWingetOutput(progress, tool.Name, e.Data);
                process.ErrorDataReceived += (_, e) => ReportWingetOutput(progress, tool.Name, e.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                var success = process.ExitCode == 0;
                Complete(
                    progress,
                    tool.Name,
                    success,
                    L.Format(success ? "Tools:ActionCompleted" : "Tools:ActionFailed", actionText, tool.Name));
                return success;
            }
            catch (Exception ex)
            {
                Complete(progress, tool.Name, false, ex.Message);
                return false;
            }
        }

        internal static string BuildLaunchHint(ExternalTool tool, string? resolvedPath, bool terminalCommandAvailable = true)
        {
            var lines = new List<string> { L.Get("Tools:InstallationCompleted") };
            if (terminalCommandAvailable && !string.IsNullOrWhiteSpace(tool.TerminalCommand))
                lines.Add(L.Format("Tools:TerminalCommand", tool.TerminalCommand));
            if (!string.IsNullOrWhiteSpace(resolvedPath))
                lines.Add(L.Format(Directory.Exists(resolvedPath) ? "Tools:InstallationLocation" : "Tools:ExecutableLocation", resolvedPath));
            if (lines.Count == 1)
            {
                lines.Add(L.Get("Tools:WingetPathMissing"));
                lines.Add(L.Format("Tools:PackageId", tool.WingetId));
                lines.Add(L.Format("Tools:CheckWingetCommand", tool.WingetId));
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

        private static void ReportWingetOutput(IProgress<ToolProgress>? progress, string toolName, string? data)
        {
            if (string.IsNullOrWhiteSpace(data)) return;
            var percentage = TryParsePercent(data);
            Report(progress, toolName, percentage < 0 ? 0 : percentage, data);
        }

        internal static int TryParsePercent(string data)
        {
            var match = Regex.Match(data, @"(\d+)%");
            return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? Math.Clamp(value, 0, 100) : -1;
        }

        private static void Report(IProgress<ToolProgress>? progress, string toolName, int percentage, string message) =>
            progress?.Report(new(toolName, percentage, message));

        private static void Complete(IProgress<ToolProgress>? progress, string toolName, bool success, string message) =>
            progress?.Report(new(toolName, success ? 100 : 0, message, IsCompleted: true, Success: success));
    }
}
