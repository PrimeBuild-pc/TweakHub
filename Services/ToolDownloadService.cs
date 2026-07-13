using System.Diagnostics;
using System.Windows;
using TweakHub.Models;
using TweakHub.Views.Dialogs;

namespace TweakHub.Services
{
    public class ToolDownloadService
    {
        private static ToolDownloadService? _instance;
        public static ToolDownloadService Instance => _instance ??= new ToolDownloadService();

        public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
        public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;

        private ToolDownloadService() { }

        public Task<bool> InstallWithWinget(ExternalTool tool)
        {
            if (string.IsNullOrWhiteSpace(tool.WingetId)) return Task.FromResult(false);
            var arguments = $"install --id \"{tool.WingetId}\" --exact --accept-source-agreements --accept-package-agreements";
            return RunWinget(tool, arguments, "installation");
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
                if (!StyledMessageDialog.ShowConfirm(Application.Current.MainWindow, "Run Custom PowerShell Command",
                        $"Only run commands you trust. This command can download or modify software.\n\n{preview}", "Run", "Cancel"))
                    return false;

                Progress(tool.Name, 0, "Running PowerShell command...");
                var result = await PowerShellService.Instance.ExecuteScriptAsync(
                    tool.PowerShellCommand, tool.RequiresAdministrator, TimeSpan.FromMinutes(15));
                var details = result.Success ? result.Output : result.Error;
                if (details.Length > 3000) details = details[^3000..];
                Complete(tool.Name, result.Success, result.Success ? "Command completed." : "Command failed.");
                StyledMessageDialog.ShowOk(Application.Current.MainWindow,
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

        private void ReportWingetOutput(string toolName, string? data)
        {
            if (string.IsNullOrWhiteSpace(data)) return;
            var percentage = TryParsePercent(data);
            Progress(toolName, percentage < 0 ? 0 : percentage, data);
        }

        private static int TryParsePercent(string data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                if (!char.IsDigit(data[i])) continue;

                var end = i;
                var value = 0;
                while (end < data.Length && char.IsDigit(data[end]))
                {
                    value = value * 10 + data[end] - '0';
                    end++;
                }

                if (end < data.Length && data[end] == '%')
                    return Math.Clamp(value, 0, 100);

                i = end;
            }

            return -1;
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
