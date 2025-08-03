using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TweakHub.Services;

namespace TweakHub.Views
{
    public partial class SystemMonitorPage : Page
    {
        private readonly SystemMonitoringService _systemMonitoringService;
        private readonly HardwareMonitoringService _hardwareMonitoringService;
        private readonly DispatcherTimer _updateTimer;
        private bool _isPageVisible = false;

        public SystemMonitorPage()
        {
            InitializeComponent();
            _systemMonitoringService = SystemMonitoringService.Instance;
            _hardwareMonitoringService = HardwareMonitoringService.Instance;

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _updateTimer.Tick += UpdateTimer_Tick;

            // Subscribe to hardware monitoring events
            _hardwareMonitoringService.HardwareDataUpdated += OnHardwareDataUpdated;

            Loaded += SystemMonitorPage_Loaded;
            Unloaded += SystemMonitorPage_Unloaded;
            IsVisibleChanged += SystemMonitorPage_IsVisibleChanged;
        }

        private void SystemMonitorPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSystemInformation();
            _isPageVisible = true;
            StartMonitoring();
        }

        private void SystemMonitorPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _isPageVisible = false;
            StopMonitoring();
        }

        private void SystemMonitorPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _isPageVisible = IsVisible;

            if (_isPageVisible)
            {
                StartMonitoring();
            }
            else
            {
                StopMonitoring();
            }
        }

        private void StartMonitoring()
        {
            if (_isPageVisible)
            {
                _updateTimer.Start();
                _hardwareMonitoringService.StartMonitoring();
            }
        }

        private void StopMonitoring()
        {
            _updateTimer.Stop();
            _hardwareMonitoringService.StopMonitoring();
        }

        private void OnHardwareDataUpdated(object? sender, HardwareDataEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateHardwareDisplay(e.Data);
            });
        }

        private void LoadSystemInformation()
        {
            var metrics = _systemMonitoringService.SystemMetrics;

            // Load static hardware information
            var cpuMetric = metrics.FirstOrDefault(m => m.Name == "CPU");
            if (cpuMetric != null)
            {
                CpuNameText.Text = cpuMetric.Value;
            }

            var cpuCoresMetric = metrics.FirstOrDefault(m => m.Name == "CPU Cores");
            if (cpuCoresMetric != null)
            {
                CpuCoresText.Text = cpuCoresMetric.Value;
            }

            var cpuThreadsMetric = metrics.FirstOrDefault(m => m.Name == "CPU Threads");
            if (cpuThreadsMetric != null)
            {
                CpuThreadsText.Text = cpuThreadsMetric.Value;
            }

            var gpuMetric = metrics.FirstOrDefault(m => m.Name == "GPU");
            if (gpuMetric != null)
            {
                GpuNameText.Text = gpuMetric.Value;
            }

            var osMetric = metrics.FirstOrDefault(m => m.Name == "Operating System");
            if (osMetric != null)
            {
                OsNameText.Text = osMetric.Value;
            }

            var osVersionMetric = metrics.FirstOrDefault(m => m.Name == "OS Version");
            if (osVersionMetric != null)
            {
                OsVersionText.Text = $"Version {osVersionMetric.Value}";
            }

            var totalRamMetric = metrics.FirstOrDefault(m => m.Name == "Total RAM");
            if (totalRamMetric != null)
            {
                TotalRamText.Text = totalRamMetric.Value;
            }
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            UpdatePerformanceMetrics();
            UpdateSystemUptime();
        }

        private void UpdateHardwareDisplay(HardwareData data)
        {
            try
            {
                // Update CPU temperature if available
                if (data.CpuTemperature > 0)
                {
                    // Find CPU temperature display element (we'll need to add this to XAML)
                    if (FindName("CpuTempValueText") is TextBlock cpuTempText)
                    {
                        cpuTempText.Text = $"{data.CpuTemperature:F1}°C";

                        // Change color based on temperature
                        if (data.CpuTemperature > 80)
                            cpuTempText.Foreground = new SolidColorBrush(Colors.Red);
                        else if (data.CpuTemperature > 70)
                            cpuTempText.Foreground = new SolidColorBrush(Colors.Orange);
                        else
                            cpuTempText.Foreground = new SolidColorBrush(Colors.LightGreen);
                    }
                }

                // Update real-time CPU usage
                if (data.CpuUsage > 0)
                {
                    CpuValueText.Text = $"{data.CpuUsage:F0}%";
                    UpdateProgressRing(CpuProgressRing, data.CpuUsage);
                }

                // Update real-time memory usage
                if (data.MemoryUsage > 0)
                {
                    RamValueText.Text = $"{data.MemoryUsage:F0}%";
                    UpdateProgressRing(RamProgressRing, data.MemoryUsage);
                }

                // Update disk usage
                if (data.DiskUsage > 0)
                {
                    if (FindName("DiskValueText") is TextBlock diskText)
                    {
                        diskText.Text = $"{data.DiskUsage:F0}%";
                    }
                    if (FindName("DiskProgressRing") is Border diskProgress)
                    {
                        UpdateProgressRing(diskProgress, data.DiskUsage);
                    }
                }

                // Update network usage
                if (data.NetworkUsage > 0)
                {
                    if (FindName("NetworkValueText") is TextBlock networkText)
                    {
                        networkText.Text = $"{data.NetworkUsage:F1}%";
                    }
                }

                // Update GPU usage
                if (data.GpuUsage > 0)
                {
                    if (FindName("GpuUsageText") is TextBlock gpuText)
                    {
                        gpuText.Text = $"{data.GpuUsage:F0}%";
                    }
                }

                // Update system uptime
                if (data.SystemUptime.TotalSeconds > 0)
                {
                    if (FindName("UptimeText") is TextBlock uptimeText)
                    {
                        uptimeText.Text = data.SystemUptimeDisplay;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating hardware display: {ex.Message}");
            }
        }

        private void UpdatePerformanceMetrics()
        {
            var metrics = _systemMonitoringService.SystemMetrics;

            // Update CPU usage
            var cpuUsageMetric = metrics.FirstOrDefault(m => m.Name == "CPU Usage");
            if (cpuUsageMetric != null)
            {
                CpuValueText.Text = $"{cpuUsageMetric.NumericValue:F0}%";
                UpdateProgressRing(CpuProgressRing, cpuUsageMetric.NumericValue);
            }

            // Update RAM usage
            var ramUsageMetric = metrics.FirstOrDefault(m => m.Name == "RAM Usage");
            var availableRamMetric = metrics.FirstOrDefault(m => m.Name == "Available RAM");
            var totalRamMetric = metrics.FirstOrDefault(m => m.Name == "Total RAM");

            if (ramUsageMetric != null)
            {
                RamValueText.Text = $"{ramUsageMetric.NumericValue:F0}%";
                UpdateProgressRing(RamProgressRing, ramUsageMetric.NumericValue);
            }

            if (availableRamMetric != null)
            {
                AvailableRamText.Text = availableRamMetric.Value;
            }

            if (totalRamMetric != null && availableRamMetric != null)
            {
                var totalRam = totalRamMetric.NumericValue;
                var availableRam = availableRamMetric.NumericValue;
                var usedRam = totalRam - availableRam;
                RamDetailsText.Text = $"{usedRam:F1} GB / {totalRam:F1} GB";
            }
        }

        private void UpdateProgressRing(Border progressRing, double percentage)
        {
            // Simple visual update - in a real implementation, you might use a custom control
            // or animation to show the actual progress ring
            var opacity = Math.Min(1.0, percentage / 100.0);
            progressRing.Opacity = 0.3 + (opacity * 0.7);
        }

        private void UpdateSystemUptime()
        {
            try
            {
                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                UptimeText.Text = $"{uptime.Days}d {uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
            }
            catch
            {
                UptimeText.Text = "Unknown";
            }
        }
    }
}
