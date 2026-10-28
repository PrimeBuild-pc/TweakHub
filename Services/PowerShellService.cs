using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
            CancellationToken cancellationToken = default,
            bool userModules = false)
        {
            var id = Guid.NewGuid().ToString("N");
            // Already elevated: the script is run by path, so keep it where unelevated processes cannot rewrite it.
            var scriptFolder = Elevation.IsAdministrator
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
                : Path.GetTempPath();
            var scriptPath = Path.Combine(scriptFolder, $"TweakHub-{id}.ps1");
            var pipeName = $"TweakHub-{id}";
            var stopwatch = Stopwatch.StartNew();

            using var timeoutSource = timeout is null ? null : new CancellationTokenSource(timeout.Value);
            using var linkedSource = timeoutSource is null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

            try
            {
                // Windows PowerShell 5.1 reads BOM-less scripts as ANSI, which mangles non-ASCII paths and text.
                var encoding = new UTF8Encoding(true);
                var scriptBytes = encoding.GetPreamble().Concat(encoding.GetBytes(BuildScriptContext(script, userModules))).ToArray();
                await File.WriteAllBytesAsync(scriptPath, scriptBytes, linkedSource.Token);
                var elevated = requireAdministrator && !Elevation.IsAdministrator;
                var startInfo = elevated
                    ? CreateElevatedStartInfo(scriptPath, scriptBytes, pipeName)
                    : CreateStartInfo(scriptPath);

                using var process = new Process { StartInfo = startInfo };
                if (!process.Start()) return Failure("Failed to start PowerShell process", stopwatch.Elapsed);

                Task<string>? outputTask;
                Task<string>? errorTask = null;
                if (elevated)
                    outputTask = ReadElevatedOutputAsync(pipeName, process, linkedSource.Token);
                else
                {
                    outputTask = process.StandardOutput.ReadToEndAsync(linkedSource.Token);
                    errorTask = process.StandardError.ReadToEndAsync(linkedSource.Token);
                }

                string output, error;
                try
                {
                    await process.WaitForExitAsync(linkedSource.Token);
                    (output, error) = elevated ? SplitElevatedOutput(await outputTask) : (await outputTask, await errorTask!);
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
                try { File.Delete(scriptPath); } catch { }
            }
        }

        // The user's own scripts may import modules installed in their profile.
        public async Task<PowerShellResult> ExecuteCustomScriptAsync(CustomScript script, CancellationToken cancellationToken = default)
        {
            if (script.Language == ScriptLanguage.PowerShell)
                return await ExecuteScriptAsync(script.Content, script.RequiresAdministrator,
                    TimeSpan.FromMinutes(15), cancellationToken, userModules: true);

            return await ExecuteScriptAsync(BuildBatchRunner(script.Content), script.RequiresAdministrator,
                TimeSpan.FromMinutes(15), cancellationToken, userModules: true);
        }

        /// <summary>
        /// Reads what the elevated wrapper sends on its pipe. The wrapper waits for this connection before it runs the
        /// script, so nothing is lost; if the process ends without ever opening the pipe the result is empty.
        /// </summary>
        internal static async Task<string> ReadElevatedOutputAsync(string pipeName, Process process, CancellationToken token)
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
            var connect = pipe.ConnectAsync(stop.Token);
            if (await Task.WhenAny(connect, process.WaitForExitAsync(token)) != connect) stop.Cancel();
            try { await connect; }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) { return string.Empty; }
            using var reader = new StreamReader(pipe, new UTF8Encoding(false));
            return await reader.ReadToEndAsync(token);
        }

        // The wrapper sends the output, then a NUL and the error text if the script failed.
        internal static (string Output, string Error) SplitElevatedOutput(string text)
        {
            var separator = text.IndexOf('\0');
            return separator < 0 ? (text, string.Empty) : (text[..separator], text[(separator + 1)..]);
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

        /// <summary>
        /// The user can redefine windir, ProgramData and the rest in their own environment, which an elevated PowerShell
        /// inherits. Reset them from Windows itself (kernel and HKLM) before any script builds a path from them.
        /// </summary>
        internal const string SystemFolders = """
            $env:SystemRoot = $env:windir = [Environment]::GetFolderPath('Windows')
            $env:SystemDrive = [IO.Path]::GetPathRoot($env:windir).TrimEnd('\')
            $machine = [Microsoft.Win32.RegistryKey]::OpenBaseKey('LocalMachine', 'Registry64')
            $folders = $machine.OpenSubKey('SOFTWARE\Microsoft\Windows\CurrentVersion')
            $env:ProgramFiles = $folders.GetValue('ProgramFilesDir')
            ${env:ProgramFiles(x86)} = $folders.GetValue('ProgramFilesDir (x86)')
            $env:ProgramData = $machine.OpenSubKey('SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList').GetValue('ProgramData', $null, 'DoNotExpandEnvironmentNames') -replace '%SystemDrive%', $env:SystemDrive
            $folders.Close(); $machine.Close(); Remove-Variable folders, machine
            """;

        // Module autoload would otherwise look in the user's Documents first, where any unelevated process can plant one.
        internal const string SystemModulesOnly =
            "$env:PSModulePath = (Join-Path $env:windir 'System32\\WindowsPowerShell\\v1.0\\Modules') + ';' + (Join-Path $env:ProgramFiles 'WindowsPowerShell\\Modules')";

        internal static string BuildScriptContext(string script, bool userModules = false)
        {
            return $$"""
                $ErrorActionPreference = 'Stop'
                {{SystemFolders}}
                {{(userModules ? string.Empty : SystemModulesOnly)}}
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
        /// The output goes back on a pipe the elevated process creates itself, one instance and readable only by the
        /// calling user: no file in the user's temp folder that a link could redirect to a system file.
        /// The pipe is opened before the script runs and each line is sent as it comes, so a script that calls exit
        /// still delivers everything it wrote.
        /// </summary>
        internal static string BuildElevatedWrapper(string scriptPath, byte[] scriptBytes, string pipeName, string clientSid) => $$"""
            $ErrorActionPreference = 'Stop'
            $access = New-Object IO.Pipes.PipeSecurity
            foreach ($rule in @(@('S-1-5-32-544', 'FullControl'), @('S-1-5-18', 'FullControl'), @({{Literal(clientSid)}}, 'Read'))) {
                $access.AddAccessRule((New-Object IO.Pipes.PipeAccessRule([Security.Principal.SecurityIdentifier]$rule[0], $rule[1], 'Allow')))
            }
            $pipe = New-Object IO.Pipes.NamedPipeServerStream({{Literal(pipeName)}}, 'Out', 1, 'Byte', 'Asynchronous', 0, 0, $access)
            $wait = $pipe.BeginWaitForConnection($null, $null)
            $writer = $null
            if ($wait.AsyncWaitHandle.WaitOne(30000)) {
                $pipe.EndWaitForConnection($wait)
                $writer = New-Object IO.StreamWriter($pipe, (New-Object Text.UTF8Encoding($false)))
                $writer.AutoFlush = $true
            }
            function Send([string]$text) { if ($script:writer) { try { $script:writer.Write($text) } catch { $script:writer = $null } } }
            try {
                $bytes = [IO.File]::ReadAllBytes({{Literal(scriptPath)}})
                $hash = [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($bytes)).Replace('-', '')
                if ($hash -ne '{{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(scriptBytes))}}') {
                    throw 'The script changed after it was approved; nothing was run.'
                }
                $script = [scriptblock]::Create([Text.Encoding]::UTF8.GetString($bytes).TrimStart([char]0xFEFF))
                & $script *>&1 | Out-String -Stream | ForEach-Object { Send ($_ + "`r`n") }
                $code = 0
            } catch {
                Send ([string][char]0 + ($_ | Out-String))
                $code = 1
            }
            try { if ($writer) { $pipe.WaitForPipeDrain() } } catch { }
            $pipe.Dispose()
            exit $code
            """;

        private static ProcessStartInfo CreateElevatedStartInfo(string scriptPath, byte[] scriptBytes, string pipeName)
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
                BuildElevatedWrapper(scriptPath, scriptBytes, pipeName, CurrentUserSid))));
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

        internal static string CurrentUserSid
        {
            get
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                return identity.User!.Value;
            }
        }

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
