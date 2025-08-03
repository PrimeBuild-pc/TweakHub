using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace TweakHub.Services
{
    public class AudioDeviceService
    {
        private static AudioDeviceService? _instance;
        public static AudioDeviceService Instance => _instance ??= new AudioDeviceService();

        private AudioDeviceService() { }

        public List<AudioDevice> GetAudioOutputDevices()
        {
            var devices = new List<AudioDevice>();

            try
            {
                // Use WMI to get audio devices
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_SoundDevice WHERE Status = 'OK'");
                
                foreach (ManagementObject device in searcher.Get())
                {
                    var name = device["Name"]?.ToString();
                    var deviceId = device["DeviceID"]?.ToString();
                    var status = device["Status"]?.ToString();

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(deviceId) && status == "OK")
                    {
                        devices.Add(new AudioDevice
                        {
                            Name = name,
                            DeviceId = deviceId,
                            IsDefault = false // We'll determine default separately
                        });
                    }
                }

                // Also try to get devices from registry/MMDevice API approach
                var mmDevices = GetMMDeviceAudioDevices();
                foreach (var mmDevice in mmDevices)
                {
                    if (!devices.Any(d => d.Name.Equals(mmDevice.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        devices.Add(mmDevice);
                    }
                }

                // Set default device
                var defaultDevice = GetDefaultAudioDevice();
                if (!string.IsNullOrEmpty(defaultDevice))
                {
                    var defaultDev = devices.FirstOrDefault(d => 
                        d.Name.Contains(defaultDevice, StringComparison.OrdinalIgnoreCase) ||
                        defaultDevice.Contains(d.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (defaultDev != null)
                    {
                        defaultDev.IsDefault = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting audio devices: {ex.Message}");
            }

            return devices.OrderByDescending(d => d.IsDefault).ThenBy(d => d.Name).ToList();
        }

        private List<AudioDevice> GetMMDeviceAudioDevices()
        {
            var devices = new List<AudioDevice>();

            try
            {
                // Use PowerShell to get audio devices as fallback
                var psScript = @"
                    Get-CimInstance -ClassName Win32_SoundDevice | 
                    Where-Object { $_.Status -eq 'OK' } | 
                    Select-Object Name, DeviceID | 
                    ConvertTo-Json
                ";

                var result = PowerShellService.Instance.ExecuteScript(psScript);
                if (result.Success && !string.IsNullOrEmpty(result.Output))
                {
                    // Parse JSON result if needed
                    // For now, we'll rely on the WMI approach above
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting MMDevice audio devices: {ex.Message}");
            }

            return devices;
        }

        private string GetDefaultAudioDevice()
        {
            try
            {
                var psScript = @"
                    $defaultDevice = Get-CimInstance -ClassName Win32_SoundDevice | 
                    Where-Object { $_.Status -eq 'OK' } | 
                    Select-Object -First 1 -ExpandProperty Name
                    Write-Output $defaultDevice
                ";

                var result = PowerShellService.Instance.ExecuteScript(psScript);
                if (result.Success)
                {
                    return result.Output?.Trim() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting default audio device: {ex.Message}");
            }

            return string.Empty;
        }

        public bool EnableLoudnessEqualization(string deviceName)
        {
            try
            {
                var script = $@"
                    # Download the loudness equalization script
                    $scriptPath = ""$env:HOMEPATH\EnableLoudness.ps1""
                    
                    try {{
                        Write-Host ""Downloading loudness equalization script...""
                        Invoke-WebRequest -Uri ""https://raw.githubusercontent.com/Falcosc/enable-loudness-equalisation/main/EnableLoudness.ps1"" -OutFile $scriptPath -UseBasicParsing
                        
                        if (Test-Path $scriptPath) {{
                            Write-Host ""Script downloaded successfully""
                            
                            # Set execution policy
                            Write-Host ""Setting execution policy...""
                            Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
                            
                            # Execute the script with the specified device
                            Write-Host ""Enabling loudness equalization for device: {deviceName}""
                            
                            # Create a script block that will handle the interactive prompts
                            $scriptBlock = {{
                                & $scriptPath -releaseTime 2
                            }}
                            
                            # Execute with automatic responses
                            $process = Start-Process -FilePath ""powershell.exe"" -ArgumentList ""-ExecutionPolicy"", ""RemoteSigned"", ""-Command"", ""& '$scriptPath' -releaseTime 2"" -PassThru -WindowStyle Hidden -RedirectStandardInput -RedirectStandardOutput -RedirectStandardError
                            
                            # Send the device name and confirmations
                            $process.StandardInput.WriteLine(""{deviceName}"")
                            $process.StandardInput.WriteLine(""S"")
                            $process.StandardInput.WriteLine("""")
                            $process.StandardInput.Close()
                            
                            $process.WaitForExit(30000) # Wait up to 30 seconds
                            
                            if ($process.ExitCode -eq 0) {{
                                Write-Host ""Loudness equalization enabled successfully""
                                return $true
                            }} else {{
                                Write-Host ""Script execution failed with exit code: $($process.ExitCode)""
                                return $false
                            }}
                        }} else {{
                            Write-Host ""Failed to download script""
                            return $false
                        }}
                    }} catch {{
                        Write-Host ""Error: $($_.Exception.Message)""
                        return $false
                    }}
                ";

                var result = PowerShellService.Instance.ExecuteScript(script);
                return result.Success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling loudness equalization: {ex.Message}");
                return false;
            }
        }
    }

    public class AudioDevice
    {
        public string Name { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        
        public string DisplayName => IsDefault ? $"{Name} (Default)" : Name;
    }
}
