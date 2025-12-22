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
            try
            {
                // Use dispatcher for UI thread safety
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    LoadSystemShortcuts();
                    LoadExternalTools();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShortcutService initialization failed: {ex}");
                throw;
            }
        }

        private void LoadSystemShortcuts()
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSystemShortcuts failed: {ex}");
                throw;
            }
        }

        private void LoadExternalTools()
        {
            try
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
                LoadBrandUtilities();
                LoadDependenciesPack();

                LoadRuntimeLibraries();

                // Append ThreadPilot tool
                ExternalTools.Add(new ExternalTool
                {
                    Name = "ThreadPilot",
                    Description = "Monitor and pin CPU thread affinities to improve performance.",
                    Category = "System and Optimization Tools",
                    DownloadUrl = "https://github.com/PrimeBuild-pc/ThreadPilot",
                    Icon = "🧭",
                    Version = "Latest",
                    IsDirectDownload = false
                });

                // Apply favorites
                var favorites = UserDataService.Instance.LoadFavoriteTools();
                foreach (var t in ExternalTools)
                {
                    t.IsFavorite = favorites.Contains(t.Name);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadExternalTools failed: {ex}");
                throw;
            }
        }

        private void LoadDLSSAndGraphicsTools()
        {
            // DLSS and Graphics Tools
            ExternalTools.Add(new ExternalTool
            {
                Name = "DLSS Swapper",
                Description = "Swap DLSS versions for better performance in games",
                Category = "DLSS and Graphics Tools",
                DownloadUrl = "https://github.com/beeradmoore/dlss-swapper",
                Icon = "🎮",
                Version = "Latest",
                IsDirectDownload = false
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "NVIDIA Profile Inspector",
                Description = "Advanced NVIDIA driver settings editor",
                Category = "DLSS and Graphics Tools",
                DownloadUrl = "https://github.com/Orbmu2k/nvidiaProfileInspector",
                Icon = "🔧",
                Version = "Latest",
                IsDirectDownload = false
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "OptiScaler",
                Description = "Universal upscaling solution for games",
                Category = "DLSS and Graphics Tools",
                DownloadUrl = "https://github.com/optiscaler/OptiScaler",
                Icon = "📈",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Radeon Software Slimmer",
                Description = "Remove unnecessary components from AMD drivers",
                Category = "DLSS and Graphics Tools",
                DownloadUrl = "https://github.com/GSDragoon/RadeonSoftwareSlimmer",
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
                DownloadUrl = "https://dnsjumper.net/",
                Icon = "🌐",
                Version = "Latest"
            });

            // Removed: "Bullcrap Installer" (invalid/nonexistent repository)

            ExternalTools.Add(new ExternalTool
            {
                Name = "Driver Store Explorer (RAPR)",
                Description = "Manage Windows driver store",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://github.com/lostindark/DriverStoreExplorer",
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
                Name = "Autoruns",
                Description = "Manage Windows startup programs and services",
                Category = "System and Optimization Tools",
                WingetId = "Microsoft.Sysinternals.Autoruns",
                Icon = "🚀",
                Version = "Latest",
                PostInstallMessage = "To use Autoruns, open Command Prompt as Administrator and type: `autoruns`"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "RAMMap",
                Description = "Advanced memory usage analyzer",
                Category = "System and Optimization Tools",
                WingetId = "Microsoft.Sysinternals.RAMMap",
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
                Name = "Chris Titus Tech Tool",
                Description = "Execute Chris Titus Tech PowerShell tool (admin)",
                Category = "System and Optimization Tools",
                PowerShellCommand = "iwr -useb https://christitus.com/win | iex",
                RequiresAdmin = true,
                Icon = "🔧",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Winaero Tweaker",
                Description = "Advanced Windows customization tool",
                Category = "System and Optimization Tools",
                WingetId = "winaero.tweaker",
                Icon = "🎨",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "PowerToys",
                Description = "Official Microsoft utilities for power users",
                Category = "System and Optimization Tools",
                WingetId = "Microsoft.PowerToys",
                Icon = "⚡",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Wintoys",
                Description = "Modern Windows optimization and tweaking tool",
                Category = "System and Optimization Tools",
                WingetId = "9P8LTPGCBZXD",
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

            // New tools (System and Optimization Tools)
            ExternalTools.Add(new ExternalTool
            {
                Name = "Geek Uninstaller",
                Description = "Lightweight uninstaller for Windows",
                Category = "System and Optimization Tools",
                WingetId = "GeekUninstaller.GeekUninstaller",
                Icon = "🧹",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Bulk Crap Uninstaller",
                Description = "Bulk uninstall and cleanup utility",
                Category = "System and Optimization Tools",
                WingetId = "Klocman.BulkCrapUninstaller",
                Icon = "🧹",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Process Lasso",
                Description = "Real-time CPU optimization and process automation",
                Category = "System and Optimization Tools",
                WingetId = "BitSum.ProcessLasso",
                Icon = "⚙️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "ParkControl",
                Description = "CPU core parking and frequency scaling control",
                Category = "System and Optimization Tools",
                WingetId = "BitSum.ParkControl",
                Icon = "⚙️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Quick CPU",
                Description = "CPU performance tuning utility",
                Category = "System and Optimization Tools",
                WingetId = "CoderBag.QuickCPUx64",
                Icon = "⚙️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Wise Registry Cleaner",
                Description = "Windows registry cleanup and optimization",
                Category = "System and Optimization Tools",
                WingetId = "WiseCleaner.WiseRegistryCleaner",
                Icon = "🧹",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "ISLC",
                Description = "Intelligent Standby List Cleaner",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://www.wagnardsoft.com/forums/viewtopic.php?t=1256",
                Icon = "🧠",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "WinHance",
                Description = "Windows enhancement toolkit",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://winhance.net/",
                Icon = "✨",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Windows KeyFinder",
                Description = "Retrieve your Windows product key",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://cdn.discordapp.com/attachments/1208935867729580043/1355867378722013297/windowskeyfinder.zip",
                Icon = "🔑",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Windows & Office Activation (MAS)",
                Description = "Microsoft Activation Scripts (open-source)",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://github.com/massgravel/Microsoft-Activation-Scripts",
                Icon = "🪟",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Vortex Mod Manager",
                Description = "Nexus Mods mod manager",
                Category = "System and Optimization Tools",
                WingetId = "NexusMods.Vortex",
                Icon = "🧩",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "winscript",
                Description = "Windows optimization scripts (website)",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://winscript.cc/",
                Icon = "🔧",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "CPU Set Setter",
                Description = "Advanced CPU thread affinity and core assignment tool",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://github.com/SimonvBez/CPUSetSetter",
                Icon = "⚙️",
                Version = "Latest",
                IsDirectDownload = false
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "ZapTweaks",
                Description = "Lightning-fast Windows optimization and tweaking utility",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://github.com/PrimeBuild-pc/ZapTweaks",
                Icon = "⚡",
                Version = "Latest",
                IsDirectDownload = false
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "G-Helper",
                Description = "Hardware optimization utility for ASUS laptops and handheld consoles",
                Category = "System and Optimization Tools",
                DownloadUrl = "https://github.com/seerge/g-helper",
                Icon = "🎮",
                Version = "Latest",
                IsDirectDownload = false
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Network Tweaker",
                Description = "PowerShell script with UI for network optimization tweaks",
                Category = "System and Optimization Tools",
                PowerShellCommand = "{APP_DIR}\\Scripts\\Network_Tweaker.ps1",
                RequiresAdmin = true,
                Icon = "💻",
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
                WingetId = "Resplendence.LatencyMon",
                Icon = "⏱️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Latency Checker",
                Description = "USB and system latency measurement tool",
                Category = "Monitoring and Latency Tools",
                DownloadUrl = "https://www.majorgeeks.com/files/details/dpc_latency_checker.html",
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
                WingetId = "Guru3D.Afterburner",
                Icon = "🎮",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "RTSS",
                Description = "RivaTuner Statistics Server (OSD/Frametime)",
                Category = "Overclocking and Hardware Control",
                WingetId = "Guru3D.RTSS",
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
                DownloadUrl = "https://www.amd.com/en/products/software/ryzen-master.html#download",
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
                WingetId = "gpu-z",
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
                WingetId = "Rem0o.FanControl",
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
                DownloadUrl = "https://www.ocbase.com/download",
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
                Name = "FurMark 2",
                Description = "GPU stress testing and benchmarking",
                Category = "Benchmarking and Testing",
                WingetId = "Geeks3D.FurMark.2",
                Icon = "🔥",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Cinebench R24",
                Description = "CPU rendering performance benchmark",
                Category = "Benchmarking and Testing",
                DownloadUrl = "https://www.guru3d.com/download/download-maxon-cinebench-2024/",
                Icon = "🎬",
                Version = "Latest"
            });
            ExternalTools.Add(new ExternalTool
            {
                Name = "CapFrameX",
                Description = "Frame-time capture and analysis",
                Category = "Benchmarking and Testing",
                WingetId = "CXWorld.CapFrameX",
                Icon = "📊",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "BenchMate",
                Description = "Automated benchmarking harness",
                Category = "Benchmarking and Testing",
                WingetId = "MatthiasZronek.BenchMate",
                Icon = "🧪",
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
                WingetId = "CrystalDewWorld.CrystalDiskMark",
                Icon = "💾",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "CrystalDiskInfo",
                Description = "Storage device health monitoring and S.M.A.R.T. analysis",
                Category = "Storage and Memory",
                WingetId = "CrystalDewWorld.CrystalDiskInfo",
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
                Name = "TM5 Pack",
                Description = "TestMem5 presets pack for memory stability testing",
                Category = "Storage and Memory",
                DownloadUrl = "https://mega.nz/folder/xPFBzQoR#v2TV5vnruNPunqt-I4Gpmw",
                Icon = "🧠",
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

            ExternalTools.Add(new ExternalTool
            {
                Name = "DiskAlizer",
                Description = "Analyze disk allocation and optimize space",
                Category = "Storage and Memory",
                DownloadUrl = "https://www.gentiluomodigitale.it/?s=DiskAlizer",
                Icon = "🧮",
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
            ExternalTools.Add(new ExternalTool
            {
                Name = "AMD GPU Profile Manager",
                Description = "Manage AMD GPU profiles via OpenSystemTelemetry",
                Category = "Driver and GPU Utilities",
                DownloadUrl = "https://github.com/OpenSystemTelemetry/AMD.GPU.ProfileManager",
                Icon = "🔧",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Snappy Driver Installer Origin",
                Description = "Open-source driver installer",
                Category = "Driver and GPU Utilities",
                WingetId = "GlennDelahoy.SnappyDriverInstallerOrigin",
                Icon = "📦",
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
                WingetId = "FxSound.FxSound",
                Icon = "🔊",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "SignalRGB",
                Description = "Control addressable RGB devices",
                Category = "Display and Audio Utilities",
                WingetId = "WhirlwindFX.SignalRgb",
                Icon = "🌈",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "OpenRGB",
                Description = "Open-source RGB lighting control",
                Category = "Display and Audio Utilities",
                WingetId = "OpenRGB.OpenRGB",
                Icon = "🌈",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Raw Accel",
                Description = "Low-latency mouse acceleration driver",
                Category = "Display and Audio Utilities",
                DownloadUrl = "https://github.com/RawAccelOfficial/rawaccel",
                Icon = "🖱️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Razer Polling Rate Tester",
                Description = "Test mouse USB polling rate",
                Category = "Display and Audio Utilities",
                DownloadUrl = "https://rzr.to/pollingrate",
                Icon = "🖱️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Keyboard Inspector",
                Description = "Keyboard key scan and diagnostics",
                Category = "Display and Audio Utilities",
                DownloadUrl = "https://github.com/mat1jaczyyy/Keyboard-Inspector",
                Icon = "⌨️",
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

        // Brand Utilities
        private void LoadBrandUtilities()
        {
            ExternalTools.Add(new ExternalTool
            {
                Name = "MSI Center",
                Description = "MSI device and system management",
                Category = "Brand Utilities",
                WingetId = "9NVMNJCR03XV",
                Icon = "🏷️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Armoury Crate",
                Description = "ASUS system management and device control",
                Category = "Brand Utilities",
                WingetId = "Asus.ArmouryCrate",
                Icon = "🏷️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Gigabyte Control Center",
                Description = "Gigabyte system and device control",
                Category = "Brand Utilities",
                WingetId = "GIGABYTE.ControlCenter",
                Icon = "🏷️",
                Version = "Latest"
            });





            ExternalTools.Add(new ExternalTool
            {
                Name = "Logitech G HUB",
                Description = "Logitech gaming device manager",
                Category = "Brand Utilities",
                WingetId = "Logitech.GHUB",
                Icon = "🏷️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "Logitech Onboard Memory Manager",
                Description = "Configure onboard profiles of Logitech devices",
                Category = "Brand Utilities",
                WingetId = "Logitech.OnboardMemoryManager",
                Icon = "🏷️",
                Version = "Latest"
            });

            ExternalTools.Add(new ExternalTool
            {
                Name = "SteelSeries Sonar",
                Description = "SteelSeries GG - Sonar audio suite",
                Category = "Brand Utilities",
                DownloadUrl = "https://steelseries.com/gg/sonar",
                Icon = "🏷️",
                Version = "Latest"
            });


        }

        // Dependencies Pack
        private void LoadDependenciesPack()
        {
            ExternalTools.Add(new ExternalTool
            {
                Name = "Dependencies Pack",
                Description = "Install common gaming/runtime dependencies (one click)",
                Category = "Dependencies Pack",
                WingetCommand = "install Microsoft.DotNet.SDK.9 Microsoft.DotNet.DesktopRuntime.9 Microsoft.DirectX Microsoft.DotNet.Framework.DeveloperPack_4 CreativeTechnology.OpenAL Microsoft.XNARedist --accept-source-agreements --accept-package-agreements",
                Icon = "📦",
                Version = "Bundle"
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
