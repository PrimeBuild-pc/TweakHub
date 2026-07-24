using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
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

        public Task<bool> InstallWithWinget(ExternalTool tool, IProgress<ToolProgress>? progress = null) =>
            string.IsNullOrWhiteSpace(tool.WingetId)
                ? Task.FromResult(false)
                : RunWinget(tool, $"install --id \"{tool.WingetId}\" --exact --accept-source-agreements --accept-package-agreements", "installation", progress);

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
