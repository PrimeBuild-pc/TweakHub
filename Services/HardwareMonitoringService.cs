using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace TweakHub.Services
{
    public class HardwareMonitoringService
    {
        private static HardwareMonitoringService? _instance;
        public static HardwareMonitoringService Instance => _instance ??= new HardwareMonitoringService();

        private Timer? _monitoringTimer;
        private bool _isMonitoring = false;
        private readonly object _lockObject = new object();

        public event EventHandler<HardwareDataEventArgs>? HardwareDataUpdated;

        private HardwareMonitoringService() { }

        public void StartMonitoring()
        {
            lock (_lockObject)
            {
                if (_isMonitoring) return;

                _isMonitoring = true;
                _monitoringTimer = new Timer(UpdateHardwareData, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
            }
        }

        public void StopMonitoring()
        {
            lock (_lockObject)
            {
                if (!_isMonitoring) return;

                _isMonitoring = false;
                _monitoringTimer?.Dispose();
                _monitoringTimer = null;
            }
        }

        private async void UpdateHardwareData(object? state)
        {
            try
            {
                var hardwareData = await GetHardwareDataAsync();
                HardwareDataUpdated?.Invoke(this, new HardwareDataEventArgs(hardwareData));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating hardware data: {ex.Message}");
            }
        }

        private async Task<HardwareData> GetHardwareDataAsync()
        {
            return await Task.Run(() =>
            {
                var data = new HardwareData();

                try
                {
                    // Get CPU temperature using WMI
                    data.CpuTemperature = GetCpuTemperature();

                    // Get CPU usage
                    data.CpuUsage = GetCpuUsage();

                    // Get memory usage
                    data.MemoryUsage = GetMemoryUsage();

                    // Get disk usage
                    data.DiskUsage = GetDiskUsage();

                    // Get network usage
                    data.NetworkUsage = GetNetworkUsage();

                    // Get GPU usage
                    data.GpuUsage = GetGpuUsage();

                    // Get system uptime
                    data.SystemUptime = GetSystemUptime();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting hardware data: {ex.Message}");
                }

                return data;
            });
        }

        private double GetCpuTemperature()
        {
            try
            {
                // Try multiple methods to get CPU temperature with enhanced detection

                // Method 1: Try MSAcpi_ThermalZoneTemperature (most common)
                var temp1 = TryGetTemperatureFromMSAcpi();
                if (temp1 > 0) return temp1;

                // Method 2: Try Win32_TemperatureProbe
                var temp2 = TryGetTemperatureFromWin32Probe();
                if (temp2 > 0) return temp2;

                // Method 3: Enhanced PowerShell with multiple WMI classes
                var temp3 = TryGetTemperatureFromPowerShell();
                if (temp3 > 0) return temp3;

                // Method 4: Try OpenHardwareMonitor WMI namespace
                var temp4 = TryGetTemperatureFromOpenHardwareMonitor();
                if (temp4 > 0) return temp4;

                // Method 5: Try LibreHardwareMonitor WMI namespace
                var temp5 = TryGetTemperatureFromLibreHardwareMonitor();
                if (temp5 > 0) return temp5;

                // Method 6: Try AIDA64 or HWiNFO shared memory (if available)
                var temp6 = TryGetTemperatureFromThirdPartyTools();
                if (temp6 > 0) return temp6;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting CPU temperature: {ex.Message}");
            }

            return 0; // Return 0 if temperature cannot be determined
        }

        private double TryGetTemperatureFromMSAcpi()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\WMI",
                    "SELECT * FROM MSAcpi_ThermalZoneTemperature");

                foreach (ManagementObject obj in searcher.Get())
                {
                    var temp = Convert.ToDouble(obj["CurrentTemperature"]);
                    // Convert from tenths of Kelvin to Celsius
                    var celsius = (temp / 10.0) - 273.15;
                    if (celsius > 0 && celsius < 150) // Sanity check
                        return celsius;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MSAcpi method failed: {ex.Message}");
            }
            return 0;
        }

        private double TryGetTemperatureFromWin32Probe()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_TemperatureProbe");

                foreach (ManagementObject obj in searcher.Get())
                {
                    var temp = obj["CurrentReading"];
                    if (temp != null)
                    {
                        var celsius = Convert.ToDouble(temp) / 10.0;
                        if (celsius > 0 && celsius < 150) // Sanity check
                            return celsius;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Win32_TemperatureProbe method failed: {ex.Message}");
            }
            return 0;
        }

        private double TryGetTemperatureFromPowerShell()
        {
            try
            {
                var psScript = @"
                    try {
                        # Try multiple WMI classes for temperature
                        $temp = $null

                        # Method 1: MSAcpi_ThermalZoneTemperature
                        try {
                            $temp = Get-WmiObject -Namespace 'root\WMI' -Class 'MSAcpi_ThermalZoneTemperature' -ErrorAction SilentlyContinue |
                                    Select-Object -First 1 -ExpandProperty CurrentTemperature
                            if ($temp -and $temp -gt 0) {
                                $celsius = ($temp / 10) - 273.15
                                if ($celsius -gt 0 -and $celsius -lt 150) {
                                    Write-Output $celsius
                                    exit
                                }
                            }
                        } catch { }

                        # Method 2: Try CIM instance
                        try {
                            $temp = Get-CimInstance -Namespace 'root\WMI' -ClassName 'MSAcpi_ThermalZoneTemperature' -ErrorAction SilentlyContinue |
                                    Select-Object -First 1 -ExpandProperty CurrentTemperature
                            if ($temp -and $temp -gt 0) {
                                $celsius = ($temp / 10) - 273.15
                                if ($celsius -gt 0 -and $celsius -lt 150) {
                                    Write-Output $celsius
                                    exit
                                }
                            }
                        } catch { }

                        Write-Output '0'
                    } catch {
                        Write-Output '0'
                    }
                ";

                var result = PowerShellService.Instance.ExecuteScript(psScript);
                if (result.Success && double.TryParse(result.Output?.Trim(), out double temperature))
                {
                    return temperature > 0 ? temperature : 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PowerShell temperature method failed: {ex.Message}");
            }
            return 0;
        }

        private double TryGetTemperatureFromOpenHardwareMonitor()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\OpenHardwareMonitor",
                    "SELECT * FROM Sensor WHERE SensorType='Temperature' AND Name LIKE '%CPU%'");

                foreach (ManagementObject obj in searcher.Get())
                {
                    var temp = obj["Value"];
                    if (temp != null)
                    {
                        var celsius = Convert.ToDouble(temp);
                        if (celsius > 0 && celsius < 150) // Sanity check
                            return celsius;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenHardwareMonitor method failed: {ex.Message}");
            }
            return 0;
        }

        private double TryGetTemperatureFromLibreHardwareMonitor()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\LibreHardwareMonitor",
                    "SELECT * FROM Sensor WHERE SensorType='Temperature' AND Name LIKE '%CPU%'");

                foreach (ManagementObject obj in searcher.Get())
                {
                    var temp = obj["Value"];
                    if (temp != null)
                    {
                        var celsius = Convert.ToDouble(temp);
                        if (celsius > 0 && celsius < 150) // Sanity check
                            return celsius;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LibreHardwareMonitor method failed: {ex.Message}");
            }
            return 0;
        }

        private double TryGetTemperatureFromThirdPartyTools()
        {
            try
            {
                // Try to get temperature from common third-party monitoring tools via PowerShell
                var psScript = @"
                    try {
                        # Check if HWiNFO is running and has shared memory
                        $hwinfo = Get-Process -Name 'HWiNFO*' -ErrorAction SilentlyContinue
                        if ($hwinfo) {
                            # HWiNFO detected but we'd need specific registry keys or shared memory access
                            # This is a placeholder for future implementation
                        }

                        # Check for AIDA64
                        $aida = Get-Process -Name 'aida64' -ErrorAction SilentlyContinue
                        if ($aida) {
                            # AIDA64 detected but we'd need specific registry keys or shared memory access
                            # This is a placeholder for future implementation
                        }

                        Write-Output '0'
                    } catch {
                        Write-Output '0'
                    }
                ";

                var result = PowerShellService.Instance.ExecuteScript(psScript);
                if (result.Success && double.TryParse(result.Output?.Trim(), out double temperature))
                {
                    return temperature > 0 ? temperature : 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Third-party tools method failed: {ex.Message}");
            }
            return 0;
        }

        private double GetCpuUsage()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var usage = obj["LoadPercentage"];
                    if (usage != null)
                    {
                        return Convert.ToDouble(usage);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting CPU usage: {ex.Message}");
            }

            return 0;
        }

        private double GetMemoryUsage()
        {
            try
            {
                // Try multiple methods for more accurate memory detection
                var memoryInfo = GetEnhancedMemoryInfo();
                if (memoryInfo.TotalMemory > 0)
                {
                    return (double)memoryInfo.UsedMemory / memoryInfo.TotalMemory * 100;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting memory usage: {ex.Message}");
            }

            return 0;
        }

        private MemoryInfo GetEnhancedMemoryInfo()
        {
            var memoryInfo = new MemoryInfo();

            try
            {
                // Method 1: Try Win32_ComputerSystem and Win32_OperatingSystem (most reliable)
                var method1 = TryGetMemoryFromWin32();
                if (method1.TotalMemory > 0)
                {
                    return method1;
                }

                // Method 2: Try Performance Counters
                var method2 = TryGetMemoryFromPerformanceCounters();
                if (method2.TotalMemory > 0)
                {
                    return method2;
                }

                // Method 3: Try PowerShell with enhanced queries
                var method3 = TryGetMemoryFromPowerShell();
                if (method3.TotalMemory > 0)
                {
                    return method3;
                }

                // Method 4: Try GlobalMemoryStatusEx via P/Invoke (fallback)
                var method4 = TryGetMemoryFromGlobalMemoryStatus();
                if (method4.TotalMemory > 0)
                {
                    return method4;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetEnhancedMemoryInfo: {ex.Message}");
            }

            return memoryInfo;
        }

        private MemoryInfo TryGetMemoryFromWin32()
        {
            var memoryInfo = new MemoryInfo();
            try
            {
                // Get total physical memory
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        memoryInfo.TotalMemory = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                        break;
                    }
                }

                // Get available memory (more accurate than FreePhysicalMemory)
                using (var searcher = new ManagementObjectSearcher("SELECT AvailableBytes FROM Win32_PerfRawData_PerfOS_Memory"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        memoryInfo.AvailableMemory = Convert.ToUInt64(obj["AvailableBytes"]);
                        break;
                    }
                }

                // Fallback to FreePhysicalMemory if AvailableBytes not found
                if (memoryInfo.AvailableMemory == 0)
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            memoryInfo.AvailableMemory = Convert.ToUInt64(obj["FreePhysicalMemory"]) * 1024; // Convert KB to bytes
                            break;
                        }
                    }
                }

                if (memoryInfo.TotalMemory > 0 && memoryInfo.AvailableMemory > 0)
                {
                    memoryInfo.UsedMemory = memoryInfo.TotalMemory - memoryInfo.AvailableMemory;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Win32 memory method failed: {ex.Message}");
            }
            return memoryInfo;
        }

        private MemoryInfo TryGetMemoryFromPerformanceCounters()
        {
            var memoryInfo = new MemoryInfo();
            try
            {
                using var totalMemoryCounter = new PerformanceCounter("Memory", "Available Bytes");
                memoryInfo.AvailableMemory = (ulong)totalMemoryCounter.NextValue();

                // Get total physical memory from registry or WMI as fallback
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        memoryInfo.TotalMemory = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                        break;
                    }
                }

                if (memoryInfo.TotalMemory > 0 && memoryInfo.AvailableMemory > 0)
                {
                    memoryInfo.UsedMemory = memoryInfo.TotalMemory - memoryInfo.AvailableMemory;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Performance counter memory method failed: {ex.Message}");
            }
            return memoryInfo;
        }

        private MemoryInfo TryGetMemoryFromPowerShell()
        {
            var memoryInfo = new MemoryInfo();
            try
            {
                var psScript = @"
                    try {
                        # Get comprehensive memory information
                        $os = Get-CimInstance -ClassName Win32_OperatingSystem
                        $cs = Get-CimInstance -ClassName Win32_ComputerSystem

                        $totalMemory = $cs.TotalPhysicalMemory
                        $freeMemory = $os.FreePhysicalMemory * 1024  # Convert KB to bytes
                        $usedMemory = $totalMemory - $freeMemory

                        # Also get available memory from performance counter
                        try {
                            $availableBytes = (Get-Counter '\Memory\Available Bytes').CounterSamples[0].CookedValue
                            if ($availableBytes -gt 0) {
                                $freeMemory = $availableBytes
                                $usedMemory = $totalMemory - $freeMemory
                            }
                        } catch { }

                        Write-Output ""$totalMemory|$freeMemory|$usedMemory""
                    } catch {
                        Write-Output ""0|0|0""
                    }
                ";

                var result = PowerShellService.Instance.ExecuteScript(psScript);
                if (result.Success && !string.IsNullOrEmpty(result.Output))
                {
                    var parts = result.Output.Trim().Split('|');
                    if (parts.Length == 3)
                    {
                        if (ulong.TryParse(parts[0], out var total) &&
                            ulong.TryParse(parts[1], out var available) &&
                            ulong.TryParse(parts[2], out var used))
                        {
                            memoryInfo.TotalMemory = total;
                            memoryInfo.AvailableMemory = available;
                            memoryInfo.UsedMemory = used;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PowerShell memory method failed: {ex.Message}");
            }
            return memoryInfo;
        }

        private MemoryInfo TryGetMemoryFromGlobalMemoryStatus()
        {
            var memoryInfo = new MemoryInfo();
            try
            {
                // This would require P/Invoke to kernel32.dll GlobalMemoryStatusEx
                // For now, we'll use a PowerShell approach to get similar information
                var psScript = @"
                    try {
                        Add-Type -TypeDefinition @'
                            using System;
                            using System.Runtime.InteropServices;
                            public struct MEMORYSTATUSEX {
                                public uint dwLength;
                                public uint dwMemoryLoad;
                                public ulong ullTotalPhys;
                                public ulong ullAvailPhys;
                                public ulong ullTotalPageFile;
                                public ulong ullAvailPageFile;
                                public ulong ullTotalVirtual;
                                public ulong ullAvailVirtual;
                                public ulong ullAvailExtendedVirtual;
                            }
                            public class MemoryAPI {
                                [DllImport(""kernel32.dll"", SetLastError = true)]
                                public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
                            }
'@
                        $memStatus = New-Object MEMORYSTATUSEX
                        $memStatus.dwLength = [System.Runtime.InteropServices.Marshal]::SizeOf($memStatus)

                        if ([MemoryAPI]::GlobalMemoryStatusEx([ref]$memStatus)) {
                            Write-Output ""$($memStatus.ullTotalPhys)|$($memStatus.ullAvailPhys)|$($memStatus.ullTotalPhys - $memStatus.ullAvailPhys)""
                        } else {
                            Write-Output ""0|0|0""
                        }
                    } catch {
                        Write-Output ""0|0|0""
                    }
                ";

                var result = PowerShellService.Instance.ExecuteScript(psScript);
                if (result.Success && !string.IsNullOrEmpty(result.Output))
                {
                    var parts = result.Output.Trim().Split('|');
                    if (parts.Length == 3)
                    {
                        if (ulong.TryParse(parts[0], out var total) &&
                            ulong.TryParse(parts[1], out var available) &&
                            ulong.TryParse(parts[2], out var used))
                        {
                            memoryInfo.TotalMemory = total;
                            memoryInfo.AvailableMemory = available;
                            memoryInfo.UsedMemory = used;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GlobalMemoryStatus method failed: {ex.Message}");
            }
            return memoryInfo;
        }

        private double GetDiskUsage()
        {
            try
            {
                // Get usage for the system drive (C:) or the drive with highest usage
                double maxUsage = 0;

                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var deviceId = obj["DeviceID"]?.ToString();
                    var size = Convert.ToUInt64(obj["Size"]);
                    var freeSpace = Convert.ToUInt64(obj["FreeSpace"]);

                    if (size > 0)
                    {
                        var usedSpace = size - freeSpace;
                        var usage = (double)usedSpace / size * 100;

                        // Prioritize C: drive, otherwise take the highest usage
                        if (deviceId == "C:" || usage > maxUsage)
                        {
                            maxUsage = usage;
                            if (deviceId == "C:") break; // Prefer C: drive
                        }
                    }
                }

                return maxUsage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting disk usage: {ex.Message}");
            }

            return 0;
        }

        private double GetNetworkUsage()
        {
            try
            {
                // Get network utilization using performance counters
                var psScript = @"
                    try {
                        # Get network interface with highest utilization
                        $maxUtilization = 0
                        $interfaces = Get-Counter '\Network Interface(*)\Bytes Total/sec' -ErrorAction SilentlyContinue

                        if ($interfaces) {
                            foreach ($sample in $interfaces.CounterSamples) {
                                if ($sample.InstanceName -ne '_Total' -and $sample.InstanceName -ne 'Loopback Pseudo-Interface 1') {
                                    $bytesPerSec = $sample.CookedValue
                                    # Convert to percentage based on typical network speeds (assume 100 Mbps = 12.5 MB/s)
                                    $utilization = ($bytesPerSec / (12.5 * 1024 * 1024)) * 100
                                    if ($utilization -gt $maxUtilization) {
                                        $maxUtilization = $utilization
                                    }
                                }
                            }
                        }

                        # Cap at 100%
                        if ($maxUtilization -gt 100) { $maxUtilization = 100 }
                        Write-Output $maxUtilization
                    } catch {
                        Write-Output '0'
                    }
                ";

                var result = PowerShellService.Instance.ExecuteScript(psScript);
                if (result.Success && double.TryParse(result.Output?.Trim(), out double usage))
                {
                    return usage;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting network usage: {ex.Message}");
            }

            return 0;
        }

        private double GetGpuUsage()
        {
            try
            {
                // Try to get GPU usage using multiple methods
                var psScript = @"
                    try {
                        $maxGpuUsage = 0

                        # Method 1: Try NVIDIA GPU usage
                        try {
                            $nvidiaUsage = nvidia-smi --query-gpu=utilization.gpu --format=csv,noheader,nounits 2>$null
                            if ($nvidiaUsage -and $nvidiaUsage -match '^\d+$') {
                                $usage = [int]$nvidiaUsage
                                if ($usage -gt $maxGpuUsage) { $maxGpuUsage = $usage }
                            }
                        } catch { }

                        # Method 2: Try WMI for GPU performance (Windows 10+)
                        try {
                            $gpuCounters = Get-Counter '\GPU Engine(*)\Utilization Percentage' -ErrorAction SilentlyContinue
                            if ($gpuCounters) {
                                foreach ($sample in $gpuCounters.CounterSamples) {
                                    $usage = $sample.CookedValue
                                    if ($usage -gt $maxGpuUsage) { $maxGpuUsage = $usage }
                                }
                            }
                        } catch { }

                        # Method 3: Try Task Manager GPU data
                        try {
                            $processes = Get-Process | Where-Object { $_.ProcessName -eq 'dwm' -or $_.ProcessName -like '*game*' }
                            if ($processes) {
                                # Estimate GPU usage based on DWM and gaming processes
                                $estimatedUsage = ($processes | Measure-Object CPU -Sum).Sum / 4
                                if ($estimatedUsage -gt $maxGpuUsage) { $maxGpuUsage = $estimatedUsage }
                            }
                        } catch { }

                        Write-Output $maxGpuUsage
                    } catch {
                        Write-Output '0'
                    }
                ";

                var result = PowerShellService.Instance.ExecuteScript(psScript);
                if (result.Success && double.TryParse(result.Output?.Trim(), out double usage))
                {
                    return Math.Min(usage, 100); // Cap at 100%
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting GPU usage: {ex.Message}");
            }

            return 0;
        }

        private TimeSpan GetSystemUptime()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var bootTime = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString());
                    return DateTime.Now - bootTime;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting system uptime: {ex.Message}");
            }

            return TimeSpan.Zero;
        }
    }

    public class HardwareData
    {
        public double CpuTemperature { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public double NetworkUsage { get; set; }
        public double GpuUsage { get; set; }
        public TimeSpan SystemUptime { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Formatted properties for display
        public string CpuTemperatureDisplay => CpuTemperature > 0 ? $"{CpuTemperature:F1}°C" : "N/A";
        public string SystemUptimeDisplay => SystemUptime.TotalDays >= 1
            ? $"{SystemUptime.Days}d {SystemUptime.Hours}h {SystemUptime.Minutes}m"
            : $"{SystemUptime.Hours}h {SystemUptime.Minutes}m";
    }

    public class HardwareDataEventArgs : EventArgs
    {
        public HardwareData Data { get; }

        public HardwareDataEventArgs(HardwareData data)
        {
            Data = data;
        }
    }

    public class MemoryInfo
    {
        public ulong TotalMemory { get; set; }
        public ulong AvailableMemory { get; set; }
        public ulong UsedMemory { get; set; }

        public double UsagePercentage => TotalMemory > 0 ? (double)UsedMemory / TotalMemory * 100 : 0;
        public double TotalMemoryGB => TotalMemory / (1024.0 * 1024.0 * 1024.0);
        public double AvailableMemoryGB => AvailableMemory / (1024.0 * 1024.0 * 1024.0);
        public double UsedMemoryGB => UsedMemory / (1024.0 * 1024.0 * 1024.0);
    }
}
