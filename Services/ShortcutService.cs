using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using TweakHub.Models;

namespace TweakHub.Services;

public sealed class ShortcutService
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
            new SystemShortcut { Name = "Device Manager", Description = "Manage hardware devices and drivers", Command = "devmgmt.msc", Category = "System Management" },
            new SystemShortcut { Name = "System Information", Description = "View detailed system information", Command = "msinfo32", Category = "System Information" },
            new SystemShortcut { Name = "Registry Editor", Description = "Edit the Windows registry", Command = "regedit", Category = "Advanced Tools" },
            new SystemShortcut { Name = "Services", Description = "Manage Windows services", Command = "services.msc", Category = "System Management" },
            new SystemShortcut { Name = "Task Manager", Description = "Monitor processes and performance", Command = "taskmgr", Category = "Performance" },
            new SystemShortcut { Name = "Resource Monitor", Description = "Inspect detailed resource usage", Command = "resmon", Category = "Performance" },
            new SystemShortcut { Name = "Power Options", Description = "Configure power and sleep settings", Command = "powercfg.cpl", Category = "Power Management" },
            new SystemShortcut { Name = "Network Connections", Description = "Manage network adapters", Command = "ncpa.cpl", Category = "Network" },
            new SystemShortcut { Name = "Sound Settings", Description = "Configure audio devices", Command = "mmsys.cpl", Category = "Audio" },
            new SystemShortcut { Name = "Display Settings", Description = "Configure displays", Command = "ms-settings:display", Category = "Display" },
            new SystemShortcut { Name = "Windows Features", Description = "Enable or disable Windows features", Command = "optionalfeatures", Category = "System Management" },
            new SystemShortcut { Name = "Disk Cleanup", Description = "Free disk space with the Windows utility", Command = "cleanmgr", Category = "Maintenance" }
        }) SystemShortcuts.Add(shortcut);
    }

    private void LoadExternalTools()
    {
        ExternalTools.Clear();
        foreach (var tool in new[]
        {
            // System utilities
            new ExternalTool { Name = "PowerToys", Description = "Official Microsoft utilities for Windows power users", Category = "System Utilities", WingetId = "Microsoft.PowerToys" },
            new ExternalTool { Name = "Autoruns", Description = "Inspect and manage every Windows startup location", Category = "System Utilities", WingetId = "Microsoft.Sysinternals.Autoruns" },
            new ExternalTool { Name = "Bulk Crap Uninstaller", Description = "Open-source bulk application uninstaller", Category = "System Utilities", WingetId = "Klocman.BulkCrapUninstaller" },
            new ExternalTool { Name = "Cleanmgr+", Description = "Extended replacement for the classic Windows Disk Cleanup", Category = "System Utilities", DownloadUrl = "https://github.com/builtbybel/CleanmgrPlus" },
            new ExternalTool { Name = "Device Cleanup Tool", Description = "Remove records for non-present devices; opens the MajorGeeks information page", Category = "System Utilities", DownloadUrl = "https://www.majorgeeks.com/files/details/device_cleanup_tool.html" },
            new ExternalTool { Name = "DISM++", Description = "Graphical Windows image servicing and cleanup utility", Category = "System Utilities", WingetId = "ChuyuTeam.DISM++" },
            new ExternalTool { Name = "Driver Store Explorer", Description = "Inspect and remove packages from the Windows driver store", Category = "System Utilities", WingetId = "lostindark.DriverStoreExplorer" },
            new ExternalTool { Name = "Snappy Driver Installer Origin", Description = "Open-source offline driver installer and index", Category = "System Utilities", WingetId = "GlennDelahoy.SnappyDriverInstallerOrigin" },
            new ExternalTool { Name = "UniGetUI", Description = "Graphical interface for WinGet and other package managers", Category = "System Utilities", DownloadUrl = "https://github.com/marticliment/UniGetUI" },
            new ExternalTool { Name = "ViVeTool GUI", Description = "Graphical interface for Windows feature configuration IDs", Category = "System Utilities", WingetId = "PeterStrick.ViVeTool-GUI" },
            new ExternalTool { Name = "Winhance", Description = "Configure and customize Windows from a transparent open-source UI", Category = "System Utilities", WingetId = "memstechtips.Winhance" },
            new ExternalTool { Name = "Wintoys", Description = "Windows maintenance and customization dashboard from Microsoft Store", Category = "System Utilities", DownloadUrl = "https://apps.microsoft.com/detail/9P8LTPGCBZXD" },
            new ExternalTool { Name = "WinUtil", Description = "Chris Titus Tech Windows utility; opens the official project, never a remote script", Category = "System Utilities", DownloadUrl = "https://github.com/ChrisTitusTech/winutil" },
            new ExternalTool { Name = "Everything", Description = "Fast local file and folder search", Category = "System Utilities", WingetId = "voidtools.Everything" },
            new ExternalTool { Name = "WizTree", Description = "Fast visual disk-space analyzer", Category = "System Utilities", WingetId = "AntibodySoftware.WizTree" },

            // CPU and memory
            new ExternalTool { Name = "CPU-Z", Description = "CPU, memory and platform information", Category = "CPU & Memory", WingetId = "CPUID.CPU-Z" },
            new ExternalTool { Name = "AMD Ryzen Master", Description = "Official AMD Ryzen monitoring and overclocking utility", Category = "CPU & Memory", DownloadUrl = "https://www.amd.com/en/products/software/ryzen-master.html" },
            new ExternalTool { Name = "ZenTimings", Description = "Inspect AMD Ryzen memory timings and Infinity Fabric settings", Category = "CPU & Memory", DownloadUrl = "https://zentimings.com/" },
            new ExternalTool { Name = "CPU Set Setter", Description = "Manage Windows CPU Sets", Category = "CPU & Memory", DownloadUrl = "https://github.com/SimonvBez/CPUSetSetter" },
            new ExternalTool { Name = "ThreadPilot", Description = "Inspect and manage CPU thread affinity", Category = "CPU & Memory", DownloadUrl = "https://github.com/PrimeBuild-pc/ThreadPilot" },
            new ExternalTool { Name = "Process Lasso", Description = "Process priority, affinity and responsiveness controls", Category = "CPU & Memory", WingetId = "BitSum.ProcessLasso" },
            new ExternalTool { Name = "ParkControl", Description = "Inspect and configure CPU core parking and frequency scaling", Category = "CPU & Memory", WingetId = "BitSum.ParkControl" },
            new ExternalTool { Name = "CoreCycler", Description = "Cycle stress tests across individual CPU cores", Category = "CPU & Memory", DownloadUrl = "https://github.com/sp00n/corecycler" },
            new ExternalTool { Name = "Ryzen SMU Debug Tool", Description = "Advanced AMD SMU, MSR and power-table inspection", Category = "CPU & Memory", DownloadUrl = "https://github.com/irusanov/SMUDebugTool" },
            new ExternalTool { Name = "Intel Application Optimization", Description = "Official Intel APO interface for supported processors and games", Category = "CPU & Memory", DownloadUrl = "https://www.intel.com/content/www/us/en/download/870620/intel-application-optimization-user-interface.html" },
            new ExternalTool { Name = "KGuiX", Description = "Advanced graphical launcher for Karhu RAM Test", Category = "CPU & Memory", DownloadUrl = "https://github.com/jjgraphix/KGuiX" },
            new ExternalTool { Name = "TestMem5", Description = "Community memory stability test; downloads the user-provided GitHub release archive", Category = "CPU & Memory", DownloadUrl = "https://github.com/CoolCmd/TestMem5/releases/download/v0.13.1/TestMem5.7z" },
            new ExternalTool { Name = "MemTest86", Description = "Bootable memory diagnostics", Category = "CPU & Memory", DownloadUrl = "https://www.memtest86.com/" },

            // Monitoring and diagnostics
            new ExternalTool { Name = "HWiNFO", Description = "Detailed hardware information and sensor monitoring", Category = "Monitoring & Diagnostics", DownloadUrl = "https://www.hwinfo.com/download/" },
            new ExternalTool { Name = "HWMonitor", Description = "CPUID hardware temperature, voltage and fan monitoring", Category = "Monitoring & Diagnostics", WingetId = "CPUID.HWMonitor" },
            new ExternalTool { Name = "RAMMap", Description = "Microsoft Sysinternals physical-memory analyzer", Category = "Monitoring & Diagnostics", WingetId = "Microsoft.Sysinternals.RAMMap" },
            new ExternalTool { Name = "LatencyMon", Description = "Analyze real-time audio and DPC latency", Category = "Monitoring & Diagnostics", WingetId = "Resplendence.LatencyMon" },
            new ExternalTool { Name = "Fan Control", Description = "Open-source fan curve and sensor control", Category = "Monitoring & Diagnostics", WingetId = "Rem0o.FanControl" },
            new ExternalTool { Name = "PresentMon", Description = "Intel frame-presentation and performance monitoring", Category = "Monitoring & Diagnostics", WingetId = "Intel.PresentMon" },
            new ExternalTool { Name = "GPUView", Description = "Microsoft GPU and CPU performance trace visualization", Category = "Monitoring & Diagnostics", DownloadUrl = "https://learn.microsoft.com/windows-hardware/drivers/display/using-gpuview" },
            new ExternalTool { Name = "PeStudio", Description = "Static inspection of Windows executables without running them", Category = "Monitoring & Diagnostics", DownloadUrl = "https://www.winitor.com/download" },
            new ExternalTool { Name = "Process Explorer", Description = "Sysinternals process, handle and DLL diagnostics", Category = "Monitoring & Diagnostics", WingetId = "Microsoft.Sysinternals.ProcessExplorer" },
            new ExternalTool { Name = "Process Monitor", Description = "Real-time filesystem, Registry and process tracing", Category = "Monitoring & Diagnostics", WingetId = "Microsoft.Sysinternals.ProcessMonitor" },
            new ExternalTool { Name = "Windows Performance Toolkit", Description = "Advanced ETW recording and analysis with WPR and WPA", Category = "Monitoring & Diagnostics", DownloadUrl = "https://learn.microsoft.com/windows-hardware/test/wpt/" },
            new ExternalTool { Name = "NVIDIA FrameView", Description = "Frame-rate, frame-time, power and performance-per-watt monitoring", Category = "Monitoring & Diagnostics", WingetId = "Nvidia.FrameView" },
            new ExternalTool { Name = "GPU Shark 2", Description = "Lightweight detailed GPU monitoring from Geeks3D", Category = "Monitoring & Diagnostics", DownloadUrl = "https://www.geeks3d.com/gpushark/" },
            new ExternalTool { Name = "NVIDIA Nsight Systems", Description = "Advanced CPU and GPU workload profiling and timeline analysis", Category = "Monitoring & Diagnostics", DownloadUrl = "https://developer.nvidia.com/nsight-systems" },
            new ExternalTool { Name = "AMD Radeon GPU Profiler", Description = "Low-level Radeon GPU performance and workload analysis", Category = "Monitoring & Diagnostics", DownloadUrl = "https://gpuopen.com/rgp/" },

            // GPU and display
            new ExternalTool { Name = "GPU-Z", Description = "GPU information, sensors and validation", Category = "GPU & Display", WingetId = "TechPowerUp.GPU-Z" },
            new ExternalTool { Name = "MSI Afterburner", Description = "GPU monitoring, fan curves and overclocking controls", Category = "GPU & Display", WingetId = "Guru3D.Afterburner" },
            new ExternalTool { Name = "RivaTuner Statistics Server", Description = "Frame-rate limiting and in-game performance overlay", Category = "GPU & Display", WingetId = "Guru3D.RTSS" },
            new ExternalTool { Name = "MoreClockTool 2", Description = "Paid AMD GPU tuning utility from Microsoft Store", Category = "GPU & Display", DownloadUrl = "https://apps.microsoft.com/detail/9N08X8C1QDQP" },
            new ExternalTool { Name = "MoreClockTool (Free)", Description = "Legacy free AMD GPU tuning utility from Igor's Lab", Category = "GPU & Display", DownloadUrl = "https://www.igorslab.de/en/download-area-new-version-of-morepowertool-mpt-and-final-release-of-redbioseditor-rbe/" },
            new ExternalTool { Name = "MorePowerTool", Description = "Advanced AMD Radeon power-limit editor", Category = "GPU & Display", DownloadUrl = "https://www.igorslab.de/en/morepowertool-mpt-beta-program-new-features-the-community-tests/" },
            new ExternalTool { Name = "RadeonTuner", Description = "Lightweight open-source AMD Radeon tuning interface", Category = "GPU & Display", DownloadUrl = "https://github.com/dumbie/RadeonTuner" },
            new ExternalTool { Name = "NVIDIA Profile Inspector", Description = "Inspect advanced NVIDIA driver profiles", Category = "GPU & Display", DownloadUrl = "https://github.com/Orbmu2k/nvidiaProfileInspector" },
            new ExternalTool { Name = "NVCleanstall", Description = "Customize NVIDIA driver installations", Category = "GPU & Display", WingetId = "TechPowerUp.NVCleanstall" },
            new ExternalTool { Name = "Display Driver Uninstaller", Description = "Completely remove graphics drivers before reinstalling", Category = "GPU & Display", WingetId = "Wagnardsoft.DisplayDriverUninstaller" },
            new ExternalTool { Name = "NVFlash", Description = "Advanced NVIDIA graphics-card firmware utility", Category = "GPU & Display", DownloadUrl = "https://www.techpowerup.com/download/nvidia-nvflash/" },
            new ExternalTool { Name = "AMD VBFlash", Description = "Advanced AMD graphics-card firmware utility formerly ATIFlash", Category = "GPU & Display", DownloadUrl = "https://www.techpowerup.com/download/ati-atiflash/" },
            new ExternalTool { Name = "MPO GPU Fix", Description = "Community troubleshooting utility for Multiplane Overlay issues", Category = "GPU & Display", DownloadUrl = "https://github.com/RedDot-3ND7355/MPO-GPU-FIX" },
            new ExternalTool { Name = "Custom Resolution Utility", Description = "Inspect and edit display resolutions and EDID data", Category = "GPU & Display", DownloadUrl = "https://www.monitortests.com/forum/Thread-Custom-Resolution-Utility-CRU" },
            new ExternalTool { Name = "VibranceGUI", Description = "Automate NVIDIA Digital Vibrance and AMD saturation per game", Category = "GPU & Display", DownloadUrl = "https://vibrancegui.com/" },
            new ExternalTool { Name = "OpenRGB", Description = "Open-source RGB device control", Category = "GPU & Display", WingetId = "OpenRGB.OpenRGB" },
            new ExternalTool { Name = "SignalRGB", Description = "Unified RGB lighting and device effects", Category = "GPU & Display", WingetId = "WhirlwindFX.SignalRgb" },

            // Firmware and power
            new ExternalTool { Name = "SCEWIN GUI Better", Description = "Unofficial GUI for advanced AMI firmware-variable editing; use only with a recovery plan", Category = "Firmware & Power", DownloadUrl = "https://github.com/loko8002/SCEWIN-GUI-BETTER1" },
            new ExternalTool { Name = "SCEHUB", Description = "Community SCEWIN binaries and troubleshooting; firmware changes can make a system unbootable", Category = "Firmware & Power", DownloadUrl = "https://github.com/ab3lkaizen/SCEHUB" },

            // Gaming and input
            new ExternalTool { Name = "DLSS Swapper", Description = "Manage DLSS versions in supported games", Category = "Gaming & Input", DownloadUrl = "https://github.com/beeradmoore/dlss-swapper" },
            new ExternalTool { Name = "OptiScaler", Description = "Upscaler injection and compatibility layer for supported games", Category = "Gaming & Input", DownloadUrl = "https://github.com/optiscaler/OptiScaler" },
            new ExternalTool { Name = "DLSS Enabler", Description = "Community DirectX 12 upscaler and frame-generation compatibility mod", Category = "Gaming & Input", DownloadUrl = "https://github.com/artur-graniszewski/DLSS-Enabler" },
            new ExternalTool { Name = "LightCrosshair", Description = "Lightweight open-source configurable crosshair overlay", Category = "Gaming & Input", WingetId = "PrimeBuild.LightCrosshair" },
            new ExternalTool { Name = "Raw Accel", Description = "Open-source mouse acceleration driver", Category = "Gaming & Input", DownloadUrl = "https://github.com/RawAccelOfficial/rawaccel" },
            new ExternalTool { Name = "HIDUSBF", Description = "Advanced USB HID polling-rate filter driver", Category = "Gaming & Input", DownloadUrl = "https://github.com/LordOfMice/hidusbf" },
            new ExternalTool { Name = "DualShock Calibration", Description = "Browser-based calibration for supported PlayStation controllers", Category = "Gaming & Input", DownloadUrl = "https://dualshock-tools.github.io/" },
            new ExternalTool { Name = "Razer Polling Rate Tester", Description = "Official browser-based mouse polling-rate tester", Category = "Gaming & Input", DownloadUrl = "https://rzr.to/pollingrate" },

            // Storage and USB
            new ExternalTool { Name = "CrystalDiskMark", Description = "Storage performance benchmark", Category = "Storage & USB", WingetId = "CrystalDewWorld.CrystalDiskMark" },
            new ExternalTool { Name = "CrystalDiskInfo", Description = "Storage health and SMART information", Category = "Storage & USB", WingetId = "CrystalDewWorld.CrystalDiskInfo" },
            new ExternalTool { Name = "USBDeview", Description = "NirSoft inventory and management of current and historical USB devices", Category = "Storage & USB", WingetId = "NirSoft.USBDeview" },
            new ExternalTool { Name = "USB Latency Analyzer", Description = "Marius Heier USB latency tools; opens the official site without executing scripts", Category = "Storage & USB", DownloadUrl = "https://tools.mariusheier.com/" },
            new ExternalTool { Name = "DiskSpd", Description = "Microsoft command-line storage load generator and benchmark", Category = "Storage & USB", WingetId = "Microsoft.DiskSpd" },

            // Network
            new ExternalTool { Name = "qBittorrent", Description = "Open-source BitTorrent client", Category = "Network", WingetId = "qBittorrent.qBittorrent" },
            new ExternalTool { Name = "TCP Optimizer", Description = "Inspect and tune Windows TCP/IP parameters", Category = "Network", DownloadUrl = "https://www.speedguide.net/downloads.php" },
            new ExternalTool { Name = "TCPView", Description = "Microsoft Sysinternals live TCP and UDP endpoint viewer", Category = "Network", WingetId = "Microsoft.Sysinternals.TCPView" },
            new ExternalTool { Name = "DNS Jumper", Description = "Test and switch DNS resolvers", Category = "Network", WingetId = "sordum.DnsJumper" },
            new ExternalTool { Name = "iPerf3", Description = "Measure maximum TCP and UDP network throughput", Category = "Network", WingetId = "ar51an.iPerf3" },
            new ExternalTool { Name = "Wireshark", Description = "Capture and inspect network traffic and protocols", Category = "Network", WingetId = "WiresharkFoundation.Wireshark" },

            // Audio
            new ExternalTool { Name = "FxSound", Description = "Audio equalizer and enhancement", Category = "Audio", WingetId = "FxSound.FxSound" },
            new ExternalTool { Name = "Equalizer APO", Description = "System-wide parametric audio equalizer", Category = "Audio", DownloadUrl = "https://sourceforge.net/projects/equalizerapo/" },
            new ExternalTool { Name = "Peace Equalizer", Description = "Graphical interface for Equalizer APO", Category = "Audio", DownloadUrl = "https://sourceforge.net/projects/peace-equalizer-apo-extension/" },
            new ExternalTool { Name = "SteelSeries GG", Description = "SteelSeries device control, Sonar audio and game capture", Category = "Audio", WingetId = "SteelSeries.GG" },

            // Benchmarks and stability
            new ExternalTool { Name = "AIDA64 Extreme", Description = "Hardware diagnostics, monitoring and system benchmarks", Category = "Benchmarks & Stability", WingetId = "FinalWire.AIDA64.Extreme" },
            new ExternalTool { Name = "CapFrameX", Description = "Frame-time capture and performance analysis", Category = "Benchmarks & Stability", WingetId = "CXWorld.CapFrameX" },
            new ExternalTool { Name = "OCCT", Description = "CPU, GPU, memory and power stability testing", Category = "Benchmarks & Stability", WingetId = "OCBase.OCCT.Personal" },
            new ExternalTool { Name = "Prime95", Description = "CPU and memory stress testing", Category = "Benchmarks & Stability", WingetId = "mersenne.prime95" },
            new ExternalTool { Name = "Heaven Benchmark", Description = "Unigine GPU benchmark and stability test", Category = "Benchmarks & Stability", WingetId = "Unigine.HeavenBenchmark" },
            new ExternalTool { Name = "BenchMate", Description = "Benchmark launcher, validation and result management", Category = "Benchmarks & Stability", WingetId = "MatthiasZronek.BenchMate" },
            new ExternalTool { Name = "Cinebench 2024", Description = "Current official Maxon CPU and GPU rendering benchmark", Category = "Benchmarks & Stability", DownloadUrl = "https://www.maxon.net/en/downloads/cinebench-downloads" },
            new ExternalTool { Name = "Cinebench R23", Description = "Legacy Maxon CPU rendering benchmark", Category = "Benchmarks & Stability", WingetId = "Maxon.CinebenchR23" },
            new ExternalTool { Name = "Cinebench Legacy Downloads", Description = "TechPowerUp archive for older Cinebench releases; the user chooses the version", Category = "Benchmarks & Stability", DownloadUrl = "https://www.techpowerup.com/download/maxon-cinebench/" },
            new ExternalTool { Name = "Linpack Xtreme", Description = "High-load CPU and memory stability test", Category = "Benchmarks & Stability", DownloadUrl = "https://www.techpowerup.com/download/linpack-xtreme/" },
            new ExternalTool { Name = "FurMark 2", Description = "GPU stress test and OpenGL/Vulkan benchmark", Category = "Benchmarks & Stability", WingetId = "Geeks3D.FurMark.2" },
            new ExternalTool { Name = "MSI Kombustor", Description = "MSI GPU stress test and OpenGL/Vulkan benchmark", Category = "Benchmarks & Stability", DownloadUrl = "https://msikombustor.com/#download" },
            new ExternalTool { Name = "y-cruncher", Description = "Heavy CPU, RAM and memory-controller benchmark and stress test", Category = "Benchmarks & Stability", DownloadUrl = "https://www.numberworld.org/y-cruncher/" },
            new ExternalTool { Name = "memtest_vulkan", Description = "Vulkan-based GPU memory stability and error test", Category = "Benchmarks & Stability", DownloadUrl = "https://github.com/GpuZelenograd/memtest_vulkan" },
            new ExternalTool { Name = "Superposition Benchmark", Description = "Modern Unigine GPU benchmark and stability test", Category = "Benchmarks & Stability", WingetId = "Unigine.SuperpositionBenchmark" },
            new ExternalTool { Name = "3DMark", Description = "Industry-standard gaming CPU and GPU benchmark suite", Category = "Benchmarks & Stability", DownloadUrl = "https://benchmarks.ul.com/3dmark" },
            new ExternalTool { Name = "Blender Benchmark", Description = "Real-world CPU and GPU rendering benchmark", Category = "Benchmarks & Stability", DownloadUrl = "https://opendata.blender.org/" },
            new ExternalTool { Name = "Geekbench 6", Description = "Cross-platform CPU and GPU compute benchmark", Category = "Benchmarks & Stability", WingetId = "PrimateLabs.Geekbench.6" },

            // AI tools
            new ExternalTool { Name = "Google Antigravity", Description = "Google's agentic development environment", Category = "AI Tools", DownloadUrl = "https://antigravity.google/" },
            new ExternalTool { Name = "Cursor", Description = "AI-first code editor", Category = "AI Tools", WingetId = "Anysphere.Cursor" },
            new ExternalTool { Name = "Ollama", Description = "Run and manage local language models", Category = "AI Tools", WingetId = "Ollama.Ollama" },
            new ExternalTool { Name = "OpenCode", Description = "Open-source AI coding agent for the terminal", Category = "AI Tools", DownloadUrl = "https://opencode.ai/" },
            new ExternalTool { Name = "Pi Coding Agent", Description = "Minimal extensible terminal coding-agent framework", Category = "AI Tools", DownloadUrl = "https://github.com/badlogic/pi-mono" },
            new ExternalTool { Name = "Warp", Description = "Agentic terminal and development environment", Category = "AI Tools", WingetId = "Warp.Warp" },
            new ExternalTool { Name = "Unsloth Studio", Description = "Local interface for training and running open models", Category = "AI Tools", DownloadUrl = "https://unsloth.ai/docs/new/studio" },
            new ExternalTool { Name = "LM Studio", Description = "Discover, download and run local language models", Category = "AI Tools", WingetId = "ElementLabs.LMStudio" },
            new ExternalTool { Name = "Jan", Description = "Open-source local AI desktop application", Category = "AI Tools", WingetId = "Jan.Jan" }
        }) ExternalTools.Add(tool);

        foreach (var tool in UserDataService.Instance.LoadCustomTools())
        {
            try
            {
                UserDataService.ValidateCustomTool(tool);
                if (ExternalTools.All(existing => !existing.Name.Equals(tool.Name, StringComparison.OrdinalIgnoreCase)))
                    ExternalTools.Add(tool);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Skipped invalid custom tool: {ex.Message}");
            }
        }

        var favorites = UserDataService.Instance.LoadFavoriteTools();
        foreach (var tool in ExternalTools)
            tool.IsFavorite = favorites.Contains(FavoriteKey(tool)) || favorites.Contains(tool.Name); // migrate old name-based favorites
    }

    public IEnumerable<string> GetToolCategories() => ExternalTools.Select(tool => tool.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name);

    public void SaveCustomTool(ExternalTool tool)
    {
        UserDataService.ValidateCustomTool(tool);
        if (ExternalTools.Any(existing => existing.Id != tool.Id && existing.Name.Equals(tool.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("A tool with this name already exists.");
        var tools = UserDataService.Instance.LoadCustomTools();
        var index = tools.FindIndex(existing => existing.Id == tool.Id);
        if (index >= 0) tools[index] = tool; else tools.Add(tool);
        UserDataService.Instance.SaveCustomTools(tools);
        Initialize();
    }

    public void DeleteCustomTool(ExternalTool tool)
    {
        UserDataService.Instance.SaveCustomTools(UserDataService.Instance.LoadCustomTools().Where(existing => existing.Id != tool.Id));
        Initialize();
    }

    public static string FavoriteKey(ExternalTool tool) => tool.IsCustom ? $"custom:{tool.Id}" : $"builtin:{tool.Name}";

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
