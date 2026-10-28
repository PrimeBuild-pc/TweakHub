using System.Diagnostics;
using System.IO;
using System.Text;
using TweakHub.Models;

namespace TweakHub.Services
{
    public class PowerShellService
    {
        public static PowerShellService Instance { get; } = new();

        private PowerShellService() { }


        public async Task<PowerShellResult> ExecuteScriptAsync(
            string script,
            bool requireAdministrator = false,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid().ToString("N");
            // Already elevated: the script is run by path, so keep it where unelevated processes cannot rewrite it.
            var scriptFolder = Elevation.IsAdministrator
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
                : Path.GetTempPath();
            var scriptPath = Path.Combine(scriptFolder, $"TweakHub-{id}.ps1");
            var outputPath = Path.Combine(Path.GetTempPath(), $"TweakHub-{id}.out");
            var errorPath = Path.Combine(Path.GetTempPath(), $"TweakHub-{id}.err");
            var stopwatch = Stopwatch.StartNew();

            using var timeoutSource = timeout is null ? null : new CancellationTokenSource(timeout.Value);
            using var linkedSource = timeoutSource is null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

            try
            {
                // Windows PowerShell 5.1 reads BOM-less scripts as ANSI, which mangles non-ASCII paths and text.
                var encoding = new UTF8Encoding(true);
                var scriptBytes = encoding.GetPreamble().Concat(encoding.GetBytes(BuildScriptContext(script))).ToArray();
                await File.WriteAllBytesAsync(scriptPath, scriptBytes, linkedSource.Token);
                var elevated = requireAdministrator && !Elevation.IsAdministrator;
                var startInfo = elevated
                    ? CreateElevatedStartInfo(scriptPath, scriptBytes, outputPath, errorPath)
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
                foreach (var path in new[] { scriptPath, outputPath, errorPath })
                    try { File.Delete(path); } catch { }
            }
        }


        public async Task<PowerShellResult> ExecuteCustomScriptAsync(CustomScript script, CancellationToken cancellationToken = default)
        {
            if (script.Language == ScriptLanguage.PowerShell)
                return await ExecuteScriptAsync(script.Content, script.RequiresAdministrator,
                    TimeSpan.FromMinutes(15), cancellationToken);

            return await ExecuteScriptAsync(BuildBatchRunner(script.Content), script.RequiresAdministrator,
                TimeSpan.FromMinutes(15), cancellationToken);
        }

        /// <summary>
        /// The batch file is written by the (hash-checked) PowerShell script itself; elevated, it goes to the Windows
        /// temp folder, where the unelevated user cannot replace it between writing and running.
        /// cmd.exe reads batch files in the console code page: chcp 65001 makes non-ASCII paths survive.
        /// </summary>
        internal static string BuildBatchRunner(string content) => $$"""
            $administrator = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
            $folder = if ($administrator) { Join-Path $env:windir 'Temp' } else { [IO.Path]::GetTempPath() }
            $batch = Join-Path $folder ('TweakHub-' + [guid]::NewGuid().ToString('N') + '.cmd')
            [IO.File]::WriteAllText($batch, "@chcp 65001 >nul`r`n" + {{Literal(content)}}, [Text.UTF8Encoding]::new($false))
            try { & cmd.exe /d /c $batch; $code = $LASTEXITCODE } finally { Remove-Item -LiteralPath $batch -Force -ErrorAction SilentlyContinue }
            exit $code
            """;

        // Appended to ConvertTo-Json: escapes non-ASCII so console code pages cannot corrupt the JSON on stdout.
        public const string AsciiJson = @" | ForEach-Object { [regex]::Replace($_, '[^\x00-\x7F]', { param($m) '\u{0:x4}' -f [int][char]$m.Value }) }";

        // PowerShell also closes single-quoted strings on typographic quotes, so double every variant.
        public static string Literal(string value) =>
            "'" + System.Text.RegularExpressions.Regex.Replace(value, "['‘’‚‛]", "$0$0") + "'";

        internal static string BuildScriptContext(string script)
        {
            return $$"""
                $ErrorActionPreference = 'Stop'
                $env:TWEAKHUB_ROOT = {{Literal(AppDataPath.RootPath)}}
                $env:TWEAKHUB_APPS = {{Literal(AppDataPath.AppsPath)}}
                Set-Location -LiteralPath $env:TWEAKHUB_ROOT
                {{script}}
                """;
        }

        private static ProcessStartInfo CreateStartInfo(string scriptPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = SystemTools.PowerShell,
                WorkingDirectory = AppDataPath.RootPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            // A PSModulePath inherited from PowerShell 7 makes Windows PowerShell autoload incompatible modules.
            startInfo.Environment.Remove("PSModulePath");
            AddPowerShellArguments(startInfo, scriptPath);
            return startInfo;
        }

        /// <summary>
        /// The temp files can be replaced by any process of the user after the UAC prompt; the command line cannot.
        /// So the wrapper travels on the command line with the hash of the script it was approved for, reads the file
        /// once and runs those exact bytes: a script swapped after approval is refused, never run.
        /// </summary>
        internal static string BuildElevatedWrapper(string scriptPath, byte[] scriptBytes, string outputPath, string errorPath) => $$"""
            $ErrorActionPreference = 'Stop'
            try {
                $bytes = [IO.File]::ReadAllBytes({{Literal(scriptPath)}})
                $hash = [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($bytes)).Replace('-', '')
                if ($hash -ne '{{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(scriptBytes))}}') {
                    throw 'The script changed after it was approved; nothing was run.'
                }
                $script = [scriptblock]::Create([Text.Encoding]::UTF8.GetString($bytes).TrimStart([char]0xFEFF))
                & $script *> {{Literal(outputPath)}}
                if (-not $?) { exit 1 }
                exit 0
            } catch {
                $_ | Out-String | Set-Content -LiteralPath {{Literal(errorPath)}}
                exit 1
            }
            """;

        private static ProcessStartInfo CreateElevatedStartInfo(string scriptPath, byte[] scriptBytes, string outputPath, string errorPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = SystemTools.PowerShell,
                WorkingDirectory = AppDataPath.RootPath,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            foreach (var argument in new[] { "-WindowStyle", "Hidden", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand" })
                startInfo.ArgumentList.Add(argument);
            startInfo.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(
                BuildElevatedWrapper(scriptPath, scriptBytes, outputPath, errorPath))));
            return startInfo;
        }

        private static void AddPowerShellArguments(ProcessStartInfo startInfo, string scriptPath)
        {
            startInfo.ArgumentList.Add("-WindowStyle");
            startInfo.ArgumentList.Add("Hidden");
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
