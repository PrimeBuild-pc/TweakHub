using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using TweakHub.Models;

namespace TweakHub.Services
{
    public class SystemMonitoringService : INotifyPropertyChanged
    {
        private static SystemMonitoringService? _instance;
        private readonly DispatcherTimer _updateTimer;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _ramCounter;

        public static SystemMonitoringService Instance => _instance ??= new SystemMonitoringService();

        public ObservableCollection<SystemMetric> SystemMetrics { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        private SystemMonitoringService()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
        }

        public void Initialize()
        {
            LoadSystemInfo();
            _updateTimer.Start();
        }

        public void Stop()
        {
            _updateTimer.Stop();
        }

        private void LoadSystemInfo()
        {
            try
            {
                SystemMetrics.Clear();
                
                // Get CPU information
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        SystemMetrics.Add(new SystemMetric
                        {
                            Name = "CPU",
                            Value = obj["Name"]?.ToString() ?? "Unknown",
                            Category = "Hardware",
                            Unit = ""
                        });
                        
                        SystemMetrics.Add(new SystemMetric
                        {
                            Name = "CPU Cores",
                            Value = obj["NumberOfCores"]?.ToString() ?? "Unknown",
                            Category = "Hardware",
                            Unit = "cores"
                        });
                        
                        SystemMetrics.Add(new SystemMetric
                        {
                            Name = "CPU Threads",
                            Value = obj["NumberOfLogicalProcessors"]?.ToString() ?? "Unknown",
                            Category = "Hardware",
                            Unit = "threads"
                        });
                        break; // Only get first processor
                    }
                }

                // Get RAM information
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var totalRam = Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024 * 1024 * 1024);
                        SystemMetrics.Add(new SystemMetric
                        {
                            Name = "Total RAM",
                            Value = $"{totalRam:F1}",
                            Category = "Hardware",
                            Unit = "GB",
                            NumericValue = totalRam
                        });
                        break;
                    }
                }

                // Get GPU information
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var name = obj["Name"]?.ToString();
                        if (!string.IsNullOrEmpty(name) && !name.Contains("Microsoft Basic"))
                        {
                            SystemMetrics.Add(new SystemMetric
                            {
                                Name = "GPU",
                                Value = name,
                                Category = "Hardware",
                                Unit = ""
                            });
                            break; // Only get first dedicated GPU
                        }
                    }
                }

                // Get OS information
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        SystemMetrics.Add(new SystemMetric
                        {
                            Name = "Operating System",
                            Value = obj["Caption"]?.ToString() ?? "Unknown",
                            Category = "System",
                            Unit = ""
                        });
                        
                        SystemMetrics.Add(new SystemMetric
                        {
                            Name = "OS Version",
                            Value = obj["Version"]?.ToString() ?? "Unknown",
                            Category = "System",
                            Unit = ""
                        });
                        break;
                    }
                }

                // Add dynamic metrics placeholders
                SystemMetrics.Add(new SystemMetric
                {
                    Name = "CPU Usage",
                    Value = "0",
                    Category = "Performance",
                    Unit = "%",
                    NumericValue = 0
                });

                SystemMetrics.Add(new SystemMetric
                {
                    Name = "Available RAM",
                    Value = "0",
                    Category = "Performance",
                    Unit = "GB",
                    NumericValue = 0
                });

                SystemMetrics.Add(new SystemMetric
                {
                    Name = "RAM Usage",
                    Value = "0",
                    Category = "Performance",
                    Unit = "%",
                    NumericValue = 0
                });
            }
            catch (Exception ex)
            {
                // Handle errors silently for now
                System.Diagnostics.Debug.WriteLine($"Error loading system info: {ex.Message}");
            }
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Update CPU usage
                var cpuUsage = _cpuCounter.NextValue();
                UpdateMetric("CPU Usage", cpuUsage.ToString("F1"), cpuUsage);

                // Update RAM usage
                var availableRam = _ramCounter.NextValue() / 1024.0; // Convert to GB
                var totalRamMetric = SystemMetrics.FirstOrDefault(m => m.Name == "Total RAM");
                if (totalRamMetric != null)
                {
                    var totalRam = totalRamMetric.NumericValue;
                    var usedRam = totalRam - availableRam;
                    var ramUsagePercent = (usedRam / totalRam) * 100;

                    UpdateMetric("Available RAM", availableRam.ToString("F1"), availableRam);
                    UpdateMetric("RAM Usage", ramUsagePercent.ToString("F1"), ramUsagePercent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating metrics: {ex.Message}");
            }
        }

        private void UpdateMetric(string name, string value, double numericValue)
        {
            var metric = SystemMetrics.FirstOrDefault(m => m.Name == name);
            if (metric != null)
            {
                metric.Value = value;
                metric.NumericValue = numericValue;
                metric.LastUpdated = DateTime.Now;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
