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

        public PowerShellResult ExecuteScript(string script) =>
            ExecuteScriptAsync(script).GetAwaiter().GetResult();

        public PowerShellResult ExecuteCommand(string command) => ExecuteScript(command);

        public async Task<PowerShellResult> ExecuteScriptAsync(string script)
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), $"TweakHub-{Guid.NewGuid():N}.ps1");

            try
            {
                await File.WriteAllTextAsync(scriptPath, script, new UTF8Encoding(false));

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                process.StartInfo.ArgumentList.Add("-NoProfile");
                process.StartInfo.ArgumentList.Add("-NonInteractive");
                process.StartInfo.ArgumentList.Add("-ExecutionPolicy");
                process.StartInfo.ArgumentList.Add("Bypass");
                process.StartInfo.ArgumentList.Add("-File");
                process.StartInfo.ArgumentList.Add(scriptPath);

                if (!process.Start())
                    return Failure("Failed to start PowerShell process");

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                return new PowerShellResult
                {
                    Success = process.ExitCode == 0,
                    Output = await outputTask,
                    Error = await errorTask,
                    ExitCode = process.ExitCode
                };
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
            finally
            {
                try { File.Delete(scriptPath); } catch { }
            }
        }

        public bool IsAdministrator()
        {
            var result = ExecuteCommand("([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')");
            return result.Success && result.Output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
        }

        private static PowerShellResult Failure(string error) => new()
        {
            Success = false,
            Error = error,
            ExitCode = -1
        };
    }

    public class PowerShellResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }
    }
}
