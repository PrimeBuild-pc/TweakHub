using System.Collections.ObjectModel;
using System.Diagnostics;
using TweakHub.Models;

namespace TweakHub.Services
{
    public class ShortcutService
    {
        private static ShortcutService? _instance;
        public static ShortcutService Instance => _instance ??= new ShortcutService();

        public ObservableCollection<SystemShortcut> SystemShortcuts { get; } = new();
        public ObservableCollection<ExternalTool> ExternalTools { get; } = new();

        private ShortcutService() { }

        public void Initialize()
        {
            LoadSystemShortcuts();
            LoadExternalTools();
        }

        private void LoadSystemShortcuts()
        {
            SystemShortcuts.Clear();
            foreach (var shortcut in new[]
            {
                new SystemShortcut { Name = "Device Manager", Description = "Manage hardware devices and drivers", Command = "devmgmt.msc", Icon = "🔧", Category = "System Management" },
                new SystemShortcut { Name = "System Information", Description = "View detailed system information", Command = "msinfo32", Icon = "ℹ️", Category = "System Information" },
                new SystemShortcut { Name = "Registry Editor", Description = "Edit the Windows registry", Command = "regedit", Icon = "📝", Category = "Advanced Tools" },
                new SystemShortcut { Name = "Services", Description = "Manage Windows services", Command = "services.msc", Icon = "⚙️", Category = "System Management" },
                new SystemShortcut { Name = "Task Manager", Description = "Monitor processes and performance", Command = "taskmgr", Icon = "📊", Category = "Performance" },
                new SystemShortcut { Name = "Resource Monitor", Description = "Inspect detailed resource usage", Command = "resmon", Icon = "📈", Category = "Performance" },
                new SystemShortcut { Name = "Power Options", Description = "Configure power and sleep settings", Command = "powercfg.cpl", Icon = "🔋", Category = "Power Management" },
                new SystemShortcut { Name = "Network Connections", Description = "Manage network adapters", Command = "ncpa.cpl", Icon = "🌐", Category = "Network" },
                new SystemShortcut { Name = "Sound Settings", Description = "Configure audio devices", Command = "mmsys.cpl", Icon = "🔊", Category = "Audio" },
                new SystemShortcut { Name = "Display Settings", Description = "Configure displays", Command = "ms-settings:display", Icon = "🖥️", Category = "Display" },
                new SystemShortcut { Name = "Windows Features", Description = "Enable or disable Windows features", Command = "optionalfeatures", Icon = "📦", Category = "System Management" },
                new SystemShortcut { Name = "Disk Cleanup", Description = "Free disk space with the Windows utility", Command = "cleanmgr", Icon = "🧹", Category = "Maintenance" }
            }) SystemShortcuts.Add(shortcut);
        }

        private void LoadExternalTools()
        {
            ExternalTools.Clear();
            foreach (var tool in new[]
            {
                new ExternalTool { Name = "PowerToys", Description = "Official Microsoft utilities for power users", Category = "System Tools", WingetId = "Microsoft.PowerToys", Icon = "⚡" },
                new ExternalTool { Name = "Autoruns", Description = "Microsoft Sysinternals startup manager", Category = "System Tools", WingetId = "Microsoft.Sysinternals.Autoruns", Icon = "🚀" },
                new ExternalTool { Name = "RAMMap", Description = "Microsoft Sysinternals memory analyzer", Category = "System Tools", WingetId = "Microsoft.Sysinternals.RAMMap", Icon = "🧠" },
                new ExternalTool { Name = "Bulk Crap Uninstaller", Description = "Open-source bulk application uninstaller", Category = "System Tools", WingetId = "Klocman.BulkCrapUninstaller", Icon = "🧹" },
                new ExternalTool { Name = "CPU Set Setter", Description = "Manage Windows CPU Sets", Category = "System Tools", DownloadUrl = "https://github.com/SimonvBez/CPUSetSetter", Icon = "⚙️" },
                new ExternalTool { Name = "ThreadPilot", Description = "Inspect and manage CPU thread affinity", Category = "System Tools", DownloadUrl = "https://github.com/PrimeBuild-pc/ThreadPilot", Icon = "🧭" },

                new ExternalTool { Name = "LatencyMon", Description = "Analyze real-time audio and DPC latency", Category = "Monitoring", WingetId = "Resplendence.LatencyMon", Icon = "⏱️" },
                new ExternalTool { Name = "HWiNFO", Description = "Hardware information and sensor monitoring", Category = "Monitoring", DownloadUrl = "https://www.hwinfo.com/download/", Icon = "📊" },
                new ExternalTool { Name = "Fan Control", Description = "Open-source fan curve control", Category = "Monitoring", WingetId = "Rem0o.FanControl", Icon = "🌪️" },
                new ExternalTool { Name = "GPU-Z", Description = "GPU information and monitoring", Category = "Monitoring", WingetId = "TechPowerUp.GPU-Z", Icon = "🎮" },
                new ExternalTool { Name = "CPU-Z", Description = "CPU and platform information", Category = "Monitoring", WingetId = "CPUID.CPU-Z", Icon = "💻" },

                new ExternalTool { Name = "CapFrameX", Description = "Frame-time capture and analysis", Category = "Benchmarking", WingetId = "CXWorld.CapFrameX", Icon = "📈" },
                new ExternalTool { Name = "OCCT", Description = "CPU, GPU and power stability testing", Category = "Benchmarking", DownloadUrl = "https://www.ocbase.com/download", Icon = "🔥" },
                new ExternalTool { Name = "Prime95", Description = "CPU and memory stress testing", Category = "Benchmarking", DownloadUrl = "https://www.mersenne.org/download/", Icon = "💪" },
                new ExternalTool { Name = "CrystalDiskMark", Description = "Storage performance benchmark", Category = "Storage", WingetId = "CrystalDewWorld.CrystalDiskMark", Icon = "💾" },
                new ExternalTool { Name = "CrystalDiskInfo", Description = "Storage health and SMART information", Category = "Storage", WingetId = "CrystalDewWorld.CrystalDiskInfo", Icon = "💿" },
                new ExternalTool { Name = "MemTest86", Description = "Bootable memory diagnostics", Category = "Storage", DownloadUrl = "https://www.memtest86.com/", Icon = "🧠" },

                new ExternalTool { Name = "DLSS Swapper", Description = "Manage DLSS versions in supported games", Category = "Graphics", DownloadUrl = "https://github.com/beeradmoore/dlss-swapper", Icon = "🎮" },
                new ExternalTool { Name = "NVIDIA Profile Inspector", Description = "Inspect advanced NVIDIA driver profiles", Category = "Graphics", DownloadUrl = "https://github.com/Orbmu2k/nvidiaProfileInspector", Icon = "🔧" },
                new ExternalTool { Name = "NVCleanstall", Description = "Customize NVIDIA driver installation", Category = "Graphics", DownloadUrl = "https://www.techpowerup.com/download/techpowerup-nvcleanstall/", Icon = "🧹" },
                new ExternalTool { Name = "Display Driver Uninstaller", Description = "Completely remove graphics drivers", Category = "Graphics", DownloadUrl = "https://www.wagnardsoft.com/display-driver-uninstaller-DDU-", Icon = "🗑️" },
                new ExternalTool { Name = "Raw Accel", Description = "Open-source mouse acceleration driver", Category = "Input & Display", DownloadUrl = "https://github.com/RawAccelOfficial/rawaccel", Icon = "🖱️" },
                new ExternalTool { Name = "OpenRGB", Description = "Open-source RGB device control", Category = "Input & Display", WingetId = "OpenRGB.OpenRGB", Icon = "🌈" },
                new ExternalTool { Name = "FxSound", Description = "Audio equalizer and enhancement", Category = "Input & Display", WingetId = "FxSound.FxSound", Icon = "🔊" },
                new ExternalTool { Name = "Custom Resolution Utility", Description = "Inspect and edit display resolutions", Category = "Input & Display", DownloadUrl = "https://www.monitortests.com/forum/Thread-Custom-Resolution-Utility-CRU", Icon = "🖥️" }
            }) ExternalTools.Add(tool);

            var favorites = UserDataService.Instance.LoadFavoriteTools();
            foreach (var tool in ExternalTools) tool.IsFavorite = favorites.Contains(tool.Name);
        }

        public bool ExecuteShortcut(SystemShortcut shortcut)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = shortcut.Command,
                    Arguments = shortcut.Arguments,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error executing shortcut {shortcut.Name}: {ex.Message}");
                return false;
            }
        }
    }
}
