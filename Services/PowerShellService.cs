using System.Diagnostics;
using System.IO;
using System.Text;

namespace TweakHub.Services
{
    public class PowerShellService
    {
        private static PowerShellService? _instance;
        public static PowerShellService Instance => _instance ??= new PowerShellService();

        private PowerShellService() { }


        public async Task<PowerShellResult> ExecuteScriptAsync(
            string script,
            bool requireAdministrator = false,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid().ToString("N");
            var scriptPath = Path.Combine(Path.GetTempPath(), $"TweakHub-{id}.ps1");
            var outputPath = Path.Combine(Path.GetTempPath(), $"TweakHub-{id}.out");
            var errorPath = Path.Combine(Path.GetTempPath(), $"TweakHub-{id}.err");
            var wrapperPath = Path.Combine(Path.GetTempPath(), $"TweakHub-{id}-elevated.ps1");
            var stopwatch = Stopwatch.StartNew();

            using var timeoutSource = timeout is null ? null : new CancellationTokenSource(timeout.Value);
            using var linkedSource = timeoutSource is null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

            try
            {
                await File.WriteAllTextAsync(scriptPath, script, new UTF8Encoding(false), linkedSource.Token);
                var elevated = requireAdministrator && !Elevation.IsAdministrator;
                var startInfo = elevated
                    ? await CreateElevatedStartInfo(scriptPath, wrapperPath, outputPath, errorPath, linkedSource.Token)
                    : CreateStartInfo(scriptPath);

                using var process = new Process { StartInfo = startInfo };
                if (!process.Start()) return Failure("Failed to start PowerShell process", stopwatch.Elapsed);

                Task<string>? outputTask = null;
                Task<string>? errorTask = null;
                if (!elevated)
                {
                    outputTask = process.StandardOutput.ReadToEndAsync(linkedSource.Token);
                    errorTask = process.StandardError.ReadToEndAsync(linkedSource.Token);
                }

                try
                {
                    if (elevated)
                        await process.WaitForExitAsync();
                    else
                        await process.WaitForExitAsync(linkedSource.Token);
                }
                catch (OperationCanceledException)
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    return new PowerShellResult
                    {
                        Success = false,
                        Error = timeoutSource?.IsCancellationRequested == true ? "Script timed out." : "Script cancelled.",
                        ExitCode = -1,
                        TimedOut = timeoutSource?.IsCancellationRequested == true,
                        Duration = stopwatch.Elapsed
                    };
                }

                var output = elevated
                    ? await ReadIfExists(outputPath)
                    : await outputTask!;
                var error = elevated
                    ? await ReadIfExists(errorPath)
                    : await errorTask!;

                return new PowerShellResult
                {
                    Success = process.ExitCode == 0,
                    Output = output,
                    Error = error,
                    ExitCode = process.ExitCode,
                    Duration = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                return Failure(ex.Message, stopwatch.Elapsed);
            }
            finally
            {
                foreach (var path in new[] { scriptPath, wrapperPath, outputPath, errorPath })
                    try { File.Delete(path); } catch { }
            }
        }


        private static ProcessStartInfo CreateStartInfo(string scriptPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            AddPowerShellArguments(startInfo, scriptPath);
            return startInfo;
        }

        private static async Task<ProcessStartInfo> CreateElevatedStartInfo(
            string scriptPath,
            string wrapperPath,
            string outputPath,
            string errorPath,
            CancellationToken cancellationToken)
        {
            static string Quote(string value) => value.Replace("'", "''");
            var wrapper = $$"""
                $ErrorActionPreference = 'Stop'
                try {
                    & '{{Quote(scriptPath)}}' *> '{{Quote(outputPath)}}'
                    exit $LASTEXITCODE
                } catch {
                    $_ | Out-String | Set-Content -LiteralPath '{{Quote(errorPath)}}'
                    exit 1
                }
                """;
            await File.WriteAllTextAsync(wrapperPath, wrapper, new UTF8Encoding(false), cancellationToken);
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = true,
                Verb = "runas"
            };
            AddPowerShellArguments(startInfo, wrapperPath);
            return startInfo;
        }

        private static void AddPowerShellArguments(ProcessStartInfo startInfo, string scriptPath)
        {
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(scriptPath);
        }

        private static async Task<string> ReadIfExists(string path) =>
            File.Exists(path) ? await File.ReadAllTextAsync(path) : string.Empty;

        private static PowerShellResult Failure(string error, TimeSpan duration) => new()
        {
            Success = false,
            Error = error,
            ExitCode = -1,
            Duration = duration
        };
    }

    public class PowerShellResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public bool TimedOut { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
