using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TweakHub.Models;

namespace TweakHub.Services
{
    public class ShortcutService : INotifyPropertyChanged
    {
        private static ShortcutService? _instance;
        
        public static ShortcutService Instance => _instance ??= new ShortcutService();

        public ObservableCollection<SystemShortcut> SystemShortcuts { get; } = new();
        public ObservableCollection<ExternalTool> ExternalTools { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        private ShortcutService() { }

        public void Initialize()
        {
            LoadSystemShortcuts();
            LoadExternalTools();
        }

        private void LoadSystemShortcuts()
        {
            SystemShortcuts.Clear();

            // System Settings
            SystemShortcuts.Add(new SystemShortcut
            {
                Name = "Device Manager",
                Description = "Manage hardware devices and drivers",
                Command = "devmgmt.msc",
                Arguments = "",
                Icon = "🔧",
                Category = "System Management"
            });

            SystemShortcuts.Add(new SystemShortcut
            {
                Name = "System Information",
                Description = "View detailed system information",
                Command = "msinfo32",
                Arguments = "",
                Icon = "ℹ️",
                Category = "System Information"
            });

            SystemShortcuts.Add(new SystemShortcut
            {
                Name = "Registry Editor",
                Description = "Edit Windows registry (Advanced users only)",
                Command = "regedit",
                Arguments = "",
                Icon = "📝",
                Category = "Advanced Tools"
            });

            SystemShortcuts.Add(new SystemShortcut
            {
                Name = "Services",
                Description = "Manage Windows services",
                Command = "services.msc",
                Arguments = "",
                Icon = "⚙️",
                Category = "System Management"
            });

            SystemShortcuts.Add(new SystemShortcut
            {
                Name = "Task Manager",
                Description = "Monitor system performance and processes",
                Command = "taskmgr",
                Arguments = "",
                Icon = "📊",
                Category = "Performance"
            });

            SystemShortcuts.Add(new SystemShortcut
            {
                Name = "Resource Monitor",
                Description = "Detailed system resource monitoring",
                Command = "resmon",
                Arguments = "",
                Icon = "📈",
                Category = "Performance"
            });

            SystemShortcuts.Add(new SystemShortcut
            {
                Name = "Power Options",
                Description = "Configure power and sleep settings",
                Command = "powercfg.cpl",
                Arguments = "",
                Icon = "🔋",
                Category = "Power Management"
            });

            SystemShortcuts.Add(new SystemShortcut
            {
                Name = "Network Connections",
                Description = "Manage network adapters and connections",
                Command = "ncpa.cpl",
                Arguments = "",
                Icon = "🌐",
                Category = "Network"
            });

            SystemShortcuts.Add(new SystemShortcut
            {
                Name = "Sound Settings",
                Description = "Configure audio devices and settings",
                Command = "mmsys.cpl",
                Arguments = "",
                Icon = "🔊",
                Category = "Audio"
            });

            SystemShortcuts.Add(new SystemShortcut
            {
                Name = "Display Settings",
                Description = "Configure display and graphics settings",
                Command = "desk.cpl",
                Arguments = "",
                Icon = "🖥️",
                Category = "Display"
            });

            SystemShortcuts.Add(new SystemShortcut
            {
                Name = "Windows Features",
                Description = "Enable or disable Windows features",
                Command = "optionalfeatures",
                Arguments = "",
                Icon = "📦",
                Category = "System Management"
            });

            SystemShortcuts.Add(new SystemShortcut
            {
                Name = "Disk Cleanup",
                Description = "Free up disk space",
                Command = "cleanmgr",
                Arguments = "",
                Icon = "🧹",
                Category = "Maintenance"
            });
        }

        private void LoadExternalTools()
        {
            ExternalTools.Clear();

            LoadDLSSAndGraphicsTools();
            LoadSystemOptimizationTools();
            LoadMonitoringAndLatencyTools();
            LoadOverclockingTools();
            LoadHardwareMonitoringTools();
            LoadBenchmarkingTools();
            LoadStorageAndMemoryTools();
            LoadDriverAndGPUTools();
            LoadDisplayAndAudioTools();
            LoadRuntimeLibraries();
        }

        private void LoadDLSSAndGraphicsTools()
        {
            // DLSS and Graphics Tools
            ExternalTools.Add(new ExternalTool
            {
                Name = "DLSS Swapper",
                Description = "Swap DLSS versions for better performance in games",
                Category = "DLSS and Graphics Tools",
                DownloadUrl = "https://github.com/beeradmoore/dlss-swapper/releases",
                Icon = "🎮",
                Version = "Latest",
                IsDirectDownload = false
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "NVIDIA Profile Inspector",
                Description = "Advanced NVIDIA driver settings editor",
                Category = "DLSS and Graphics Tools",
                DownloadUrl = "https://github.com/Orbmu2k/nvidiaProfileInspector/releases",
                Icon = "🔧",
                Version = "Latest",
                IsDirectDownload = false
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "OptiScaler",
                Description = "Universal upscaling solution for games",
                Category = "DLSS and Graphics Tools",
                DownloadUrl = "https://github.com/emoose/OptiScaler",
                Icon = "📈",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Radeon Software Slimmer",
                Description = "Remove unnecessary components from AMD drivers",
                Category = "DLSS and Graphics Tools",
                DownloadUrl = "https://github.com/GSDragoon/RadeonSoftwareSlimmer/releases",
                Icon = "🔴",
                Version = "Latest"
            });
        }

        private void LoadSystemOptimizationTools()
        {
            // System and Optimization Tools
            ExternalTools.Add(new ExternalTool
            {
                Name = "PowerSettingsExplorer",
                Description = "Advanced Windows power plan settings explorer",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://forums.guru3d.com/threads/windows-power-plan-settings-explorer-utility.416058/",
                Icon = "⚡",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "DNS Jumper",
                Description = "Fast DNS changer and tester",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://dnsjumper.com/",
                Icon = "🌐",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Bullcrap Installer",
                Description = "Automated Windows optimization and debloating tool",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://github.com/Bullsh1t/Bullcrap-Installer",
                Icon = "🧹",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Driver Store Explorer (RAPR)",
                Description = "Manage Windows driver store",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://github.com/lostindark/DriverStoreExplorer/releases",
                Icon = "💾",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "TCP Optimizer",
                Description = "Network settings optimization tool",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://www.speedguide.net/downloads.php",
                Icon = "🌐",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "AutoStart (Sysinternals Autoruns)",
                Description = "Manage Windows startup programs and services",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://learn.microsoft.com/en-us/sysinternals/downloads/autoruns",
                Icon = "🚀",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "RAMMap",
                Description = "Advanced memory usage analyzer",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://learn.microsoft.com/en-us/sysinternals/downloads/rammap",
                Icon = "🧠",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "MSI Utility v3",
                Description = "Enable MSI mode for devices",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://forums.guru3d.com/threads/msi-utility-v3.378044/",
                Icon = "⚙️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "WinHags (WinHags Controller)",
                Description = "Hardware-accelerated GPU scheduling controller",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://github.com/Chuyu-Team/WinHAGS/releases",
                Icon = "🎮",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Chris Titus Tech Tool (WinUtil)",
                Description = "Comprehensive Windows optimization utility",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://christitus.com/winutil/",
                Icon = "🔧",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Winaero Tweaker",
                Description = "Advanced Windows customization tool",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://winaerotweaker.com/",
                Icon = "🎨",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Microsoft PowerToys",
                Description = "Official Microsoft utilities for power users",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://docs.microsoft.com/en-us/powertoys/",
                Icon = "⚡",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Wintoys",
                Description = "Modern Windows optimization and tweaking tool",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://apps.microsoft.com/store/detail/wintoys/9PGCV4V3BK4W",
                Icon = "🎯",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "jv16 PowerTools",
                Description = "Comprehensive system optimization suite",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://jv16powertools.com/",
                Icon = "🔧",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Tweaking.com Windows Repair Toolbox",
                Description = "Comprehensive Windows repair and optimization",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://www.tweaking.com/",
                Icon = "🛠️",
                Version = "Latest"
            });
        }

        private void LoadMonitoringAndLatencyTools()
        {
            // Monitoring and Latency Tools
            ExternalTools.Add(new ExternalTool
            {
                Name = "LatencyMon",
                Description = "Monitor system latency and DPC/ISR activity",
                Category = "Monitoring and Latency Tools",
                DownloadUrl = "https://www.resplendence.com/latencymon",
                Icon = "⏱️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Latency Checker",
                Description = "USB and system latency measurement tool",
                Category = "Monitoring and Latency Tools",
                DownloadUrl = "https://www.thesycon.de/eng/latency_check.shtml",
                Icon = "📊",
                Version = "Latest"
            });
        }

        private void LoadOverclockingTools()
        {
            // Overclocking and Hardware Control Tools
            ExternalTools.Add(new ExternalTool
            {
                Name = "MSI Afterburner",
                Description = "GPU overclocking and monitoring tool",
                Category = "Overclocking and Hardware Control",
                DownloadUrl = "https://www.msi.com/Landing/afterburner",
                Icon = "🎮",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "RivaTuner / RTSS",
                Description = "Advanced graphics card tuning and statistics",
                Category = "Overclocking and Hardware Control",
                DownloadUrl = "https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html",
                Icon = "📈",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Intel Extreme Tuning Utility (XTU)",
                Description = "Intel CPU overclocking and monitoring",
                Category = "Overclocking and Hardware Control",
                DownloadUrl = "https://www.intel.com/content/www/us/en/download/17881/intel-extreme-tuning-utility-intel-xtu.html",
                Icon = "💻",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "AMD Ryzen Master",
                Description = "AMD CPU overclocking and monitoring",
                Category = "Overclocking and Hardware Control",
                DownloadUrl = "https://www.amd.com/en/technologies/ryzen-master",
                Icon = "🔴",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "ThrottleStop",
                Description = "CPU throttling monitoring and control",
                Category = "Overclocking and Hardware Control",
                DownloadUrl = "https://www.techpowerup.com/download/techpowerup-throttlestop/",
                Icon = "🌡️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "DRAM Calculator for Ryzen",
                Description = "Memory overclocking calculator for AMD systems",
                Category = "Overclocking and Hardware Control",
                DownloadUrl = "https://www.techpowerup.com/download/ryzen-dram-calculator/",
                Icon = "🧠",
                Version = "Latest"
            });
        }

        private void LoadHardwareMonitoringTools()
        {
            // Hardware Monitoring Tools
            ExternalTools.Add(new ExternalTool
            {
                Name = "HWMonitor",
                Description = "Hardware monitoring program for temperatures, voltages, and fan speeds",
                Category = "Hardware Monitoring",
                DownloadUrl = "https://www.cpuid.com/softwares/hwmonitor.html",
                Icon = "🌡️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "HWiNFO",
                Description = "Comprehensive hardware analysis and monitoring",
                Category = "Hardware Monitoring",
                DownloadUrl = "https://www.hwinfo.com/download/",
                Icon = "📊",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Open Hardware Monitor",
                Description = "Free open source hardware monitoring",
                Category = "Hardware Monitoring",
                DownloadUrl = "https://openhardwaremonitor.org/",
                Icon = "🔍",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Core Temp",
                Description = "CPU temperature monitoring tool",
                Category = "Hardware Monitoring",
                DownloadUrl = "https://www.alcpu.com/CoreTemp/",
                Icon = "🌡️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Real Temp",
                Description = "CPU temperature monitoring for Intel processors",
                Category = "Hardware Monitoring",
                DownloadUrl = "https://www.techpowerup.com/realtemp/",
                Icon = "🌡️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "GPU-Z",
                Description = "Graphics card information and monitoring",
                Category = "Hardware Monitoring",
                DownloadUrl = "https://www.techpowerup.com/gpu-specs/gpu-z",
                Icon = "🎮",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "CPU-Z",
                Description = "CPU and system information utility",
                Category = "Hardware Monitoring",
                DownloadUrl = "https://www.cpuid.com/softwares/cpu-z.html",
                Icon = "💻",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "AIDA64 Extreme",
                Description = "System information, diagnostics and benchmarking",
                Category = "Hardware Monitoring",
                DownloadUrl = "https://www.aida64.com/downloads",
                Icon = "📈",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Fan Control",
                Description = "Advanced fan curve control software",
                Category = "Hardware Monitoring",
                DownloadUrl = "https://fancontrol.io",
                Icon = "🌪️",
                Version = "Latest"
            });
        }

        private void LoadBenchmarkingTools()
        {
            // Benchmarking and Testing Tools
            ExternalTools.Add(new ExternalTool
            {
                Name = "OCCT",
                Description = "CPU, GPU and power supply testing tool",
                Category = "Benchmarking and Testing",
                DownloadUrl = "https://www.ocbase.com/",
                Icon = "🔥",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Prime95",
                Description = "CPU stress testing and benchmarking",
                Category = "Benchmarking and Testing",
                DownloadUrl = "https://www.mersenne.org/download/",
                Icon = "💪",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "FurMark",
                Description = "GPU stress testing and benchmarking",
                Category = "Benchmarking and Testing",
                DownloadUrl = "https://geeks3d.com/furmark/",
                Icon = "🔥",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Cinebench",
                Description = "CPU rendering performance benchmark",
                Category = "Benchmarking and Testing",
                DownloadUrl = "https://www.maxon.net/en/cinebench",
                Icon = "🎬",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "3DMark",
                Description = "GPU and gaming performance benchmark",
                Category = "Benchmarking and Testing",
                DownloadUrl = "https://www.ul.com/benchmarks/3dmark",
                Icon = "🎮",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "UserBenchmark",
                Description = "Quick system performance benchmark",
                Category = "Benchmarking and Testing",
                DownloadUrl = "https://www.userbenchmark.com/",
                Icon = "⚡",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Unigine Heaven/Valley/Superposition",
                Description = "GPU benchmarking and stress testing suite",
                Category = "Benchmarking and Testing",
                DownloadUrl = "https://benchmark.unigine.com/",
                Icon = "🌄",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "PassMark PerformanceTest",
                Description = "Comprehensive system benchmarking suite",
                Category = "Benchmarking and Testing",
                DownloadUrl = "https://www.passmark.com/products/performancetest/index.php",
                Icon = "📊",
                Version = "Latest"
            });
        }

        private void LoadStorageAndMemoryTools()
        {
            // Storage and Memory Tools
            ExternalTools.Add(new ExternalTool
            {
                Name = "CrystalDiskMark",
                Description = "Storage device benchmark and performance testing",
                Category = "Storage and Memory",
                DownloadUrl = "https://crystalmark.info/en/software/crystaldiskmark/",
                Icon = "💾",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "CrystalDiskInfo",
                Description = "Storage device health monitoring and S.M.A.R.T. analysis",
                Category = "Storage and Memory",
                DownloadUrl = "https://crystalmark.info/en/software/crystaldiskinfo/",
                Icon = "💿",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Samsung Magician",
                Description = "Samsung SSD management and optimization tool",
                Category = "Storage and Memory",
                DownloadUrl = "https://semiconductor.samsung.com/consumer-storage/support/tools/",
                Icon = "💾",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "MemTest86",
                Description = "Memory testing and diagnostics tool",
                Category = "Storage and Memory",
                DownloadUrl = "https://www.memtest86.com/",
                Icon = "🧠",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "TestMem5",
                Description = "Advanced memory testing utility",
                Category = "Storage and Memory",
                DownloadUrl = "https://testmem.tz.ru/",
                Icon = "🧠",
                Version = "Latest"
            });
        }

        private void LoadDriverAndGPUTools()
        {
            // Driver and GPU Utilities
            ExternalTools.Add(new ExternalTool
            {
                Name = "nvcleaninstall (NVCleanstall)",
                Description = "Clean NVIDIA driver installation utility",
                Category = "Driver and GPU Utilities",
                DownloadUrl = "https://www.techpowerup.com/download/techpowerup-nvcleanstall/",
                Icon = "🧹",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Display Driver Uninstaller (DDU)",
                Description = "Complete graphics driver removal tool",
                Category = "Driver and GPU Utilities",
                DownloadUrl = "https://www.guru3d.com/files-details/display-driver-uninstaller-download.html",
                Icon = "🗑️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "NVIDIA NVFlash",
                Description = "NVIDIA graphics card BIOS flashing utility",
                Category = "Driver and GPU Utilities",
                DownloadUrl = "https://www.techpowerup.com/download/nvidia-nvflash/",
                Icon = "⚡",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "AMDVbFlash / ATI ATIFlash",
                Description = "AMD graphics card BIOS flashing utility",
                Category = "Driver and GPU Utilities",
                DownloadUrl = "https://www.techpowerup.com/download/ati-atiflash/",
                Icon = "🔴",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "NVIDIA GeForce Graphics Drivers",
                Description = "Latest NVIDIA graphics drivers",
                Category = "Driver and GPU Utilities",
                DownloadUrl = "https://www.nvidia.com/Download/index.aspx",
                Icon = "🎮",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "AMD Radeon Graphics Drivers",
                Description = "Latest AMD graphics drivers",
                Category = "Driver and GPU Utilities",
                DownloadUrl = "https://www.amd.com/en/support",
                Icon = "🔴",
                Version = "Latest"
            });
        }

        private void LoadDisplayAndAudioTools()
        {
            // Display and Audio Utilities
            ExternalTools.Add(new ExternalTool
            {
                Name = "CRU (Custom Resolution Utility)",
                Description = "Create custom display resolutions and refresh rates",
                Category = "Display and Audio Utilities",
                DownloadUrl = "https://www.monitortests.com/forum/Thread-Custom-Resolution-Utility-CRU",
                Icon = "🖥️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "FxSound",
                Description = "Audio enhancement and sound improvement software",
                Category = "Display and Audio Utilities",
                DownloadUrl = "https://fxsound.com",
                Icon = "🔊",
                Version = "Latest"
            });
        }

        private void LoadRuntimeLibraries()
        {
            // Runtime and Libraries
            ExternalTools.Add(new ExternalTool
            {
                Name = "Visual C++ Redistributable Runtimes All-in-One",
                Description = "Complete package of all Visual C++ redistributable runtimes",
                Category = "Runtime and Libraries",
                DownloadUrl = "https://www.techpowerup.com/download/visual-c-redistributable-runtime-package-all-in-one/",
                Icon = "📦",
                Version = "Latest"
            });
        }

        public bool ExecuteShortcut(SystemShortcut shortcut)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = shortcut.Command,
                    Arguments = shortcut.Arguments,
                    UseShellExecute = true,
                    Verb = "runas" // Run as administrator when needed
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing shortcut {shortcut.Name}: {ex.Message}");
                return false;
            }
        }

        public async void OpenExternalToolUrl(ExternalTool tool)
        {
            try
            {
                var downloadService = ToolDownloadService.Instance;
                await downloadService.DownloadOrOpenTool(tool);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing tool {tool.Name}: {ex.Message}");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
