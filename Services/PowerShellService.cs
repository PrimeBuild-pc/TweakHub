using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace TweakHub.Services
{
    public class PowerShellService
    {
        private static PowerShellService? _instance;
        public static PowerShellService Instance => _instance ??= new PowerShellService();

        private PowerShellService() { }

        public PowerShellResult ExecuteScript(string script)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Verb = "runas" // Run as administrator
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return new PowerShellResult
                    {
                        Success = false,
                        Output = string.Empty,
                        Error = "Failed to start PowerShell process"
                    };
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();

                return new PowerShellResult
                {
                    Success = process.ExitCode == 0,
                    Output = output,
                    Error = error,
                    ExitCode = process.ExitCode
                };
            }
            catch (Exception ex)
            {
                return new PowerShellResult
                {
                    Success = false,
                    Output = string.Empty,
                    Error = ex.Message
                };
            }
        }

        public async Task<PowerShellResult> ExecuteScriptAsync(string script)
        {
            return await Task.Run(() => ExecuteScript(script));
        }

        public PowerShellResult ExecuteCommand(string command)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -Command \"{command.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return new PowerShellResult
                    {
                        Success = false,
                        Output = string.Empty,
                        Error = "Failed to start PowerShell process"
                    };
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();

                return new PowerShellResult
                {
                    Success = process.ExitCode == 0,
                    Output = output,
                    Error = error,
                    ExitCode = process.ExitCode
                };
            }
            catch (Exception ex)
            {
                return new PowerShellResult
                {
                    Success = false,
                    Output = string.Empty,
                    Error = ex.Message
                };
            }
        }

        public bool IsAdministrator()
        {
            try
            {
                var result = ExecuteCommand("([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')");
                return result.Success && result.Output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }

    public class PowerShellResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }
    }
}
