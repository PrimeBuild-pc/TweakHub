using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TweakHub.Models;

namespace TweakHub.Services
{
    public class TweakService : INotifyPropertyChanged
    {
        private static TweakService? _instance;

        public static TweakService Instance => _instance ??= new TweakService();

        public ObservableCollection<TweakCategory> TweakCategories { get; } = new();

        // Session state flags
        private bool _hasAppliedTweaksThisSession;
        public bool HasAppliedTweaksThisSession
        {
            get => _hasAppliedTweaksThisSession;
            private set { _hasAppliedTweaksThisSession = value; OnPropertyChanged(); }
        }

        private bool _registryDisclaimerShown;
        public bool RegistryDisclaimerShown
        {
            get => _registryDisclaimerShown;
            set { _registryDisclaimerShown = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private TweakService() { }

        public void LoadTweaks()
        {
            TweakCategories.Clear();

            // Load performance-focused tweak categories
            LoadCpuProcessorOptimizationTweaks();
            LoadMemoryManagementTweaks();
            LoadNetworkLatencyReductionTweaks();
            LoadGamingPerformanceTweaks();
            LoadSystemResponsivenessTweaks();
            LoadStorageFileSystemTweaks();
            LoadVisualEffectsPerformanceTweaks();
            LoadBackgroundProcessOptimizationTweaks();
        }

        private void LoadCpuProcessorOptimizationTweaks()
        {
            var category = new TweakCategory
            {
                Name = "CPU & Processor Optimization",
                Description = "Optimize CPU scheduling, priority, and processor performance",
                Icon = "🔥"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "cpu_priority_separation",
                Name = "Optimize CPU Priority Separation",
                Description = "Improves foreground application responsiveness by adjusting CPU time allocation",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                RegistryKey = "Win32PrioritySeparation",
                EnabledValue = 38, // Optimized for desktop performance
                DisabledValue = 2,  // Default Windows value
                Category = "CPU & Processor Optimization",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_cpu_throttling",
                Name = "Disable CPU Throttling",
                Description = "Prevents CPU from throttling under load for maximum performance",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\893dee8e-2bef-41e0-89c6-b55d0929964c",
                RegistryKey = "ValueMax",
                EnabledValue = 0,   // Disable throttling
                DisabledValue = 100, // Default throttling
                Category = "CPU & Processor Optimization",
                RiskLevel = 2,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_core_parking",
                Name = "Disable CPU Core Parking",
                Description = "Keeps all CPU cores active for better performance and lower latency",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583",
                RegistryKey = "ValueMax",
                EnabledValue = 0,   // Disable core parking
                DisabledValue = 100, // Default core parking
                Category = "CPU & Processor Optimization",
                RiskLevel = 2,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "high_performance_power_plan",
                Name = "Force High Performance Power Plan",
                Description = "Sets system to use high performance power plan for maximum CPU performance",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes",
                RegistryKey = "ActivePowerScheme",
                EnabledValue = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", // High Performance GUID
                DisabledValue = "381b4222-f694-41f0-9685-ff5bb260df2e", // Balanced GUID
                Category = "CPU & Processor Optimization",
                RiskLevel = 1
            });

            TweakCategories.Add(category);
        }

        private void LoadMemoryManagementTweaks()
        {
            var category = new TweakCategory
            {
                Name = "Memory Management",
                Description = "Optimize memory allocation, paging, and RAM usage",
                Icon = "🧠"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_superfetch",
                Name = "Disable Superfetch/SysMain",
                Description = "Prevents aggressive memory caching that can cause stutters and high memory usage",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SysMain",
                RegistryKey = "Start",
                EnabledValue = 4, // Disabled
                DisabledValue = 2, // Automatic
                Category = "Memory Management",
                RiskLevel = 2,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "optimize_paging_executive",
                Name = "Keep System in Memory",
                Description = "Prevents system executive from being paged to disk for better responsiveness",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                RegistryKey = "DisablePagingExecutive",
                EnabledValue = 1, // Keep in memory
                DisabledValue = 0, // Allow paging
                Category = "Memory Management",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "optimize_large_system_cache",
                Name = "Optimize System Cache",
                Description = "Optimizes system cache for better memory management and file access",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                RegistryKey = "LargeSystemCache",
                EnabledValue = 1, // Optimize for applications
                DisabledValue = 0, // Default
                Category = "Memory Management",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "clear_pagefile_shutdown",
                Name = "Disable Pagefile Clearing on Shutdown",
                Description = "Speeds up shutdown by not clearing pagefile (safe for most users)",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                RegistryKey = "ClearPageFileAtShutdown",
                EnabledValue = 0, // Don't clear
                DisabledValue = 1, // Clear (slower shutdown)
                Category = "Memory Management",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_prefetch",
                Name = "Disable Prefetch",
                Description = "Disables prefetch to reduce disk I/O and improve SSD performance",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters",
                RegistryKey = "EnablePrefetcher",
                EnabledValue = 0, // Disabled
                DisabledValue = 3, // Enabled for both
                Category = "Memory Management",
                RiskLevel = 2
            });

            TweakCategories.Add(category);
        }

        private void LoadNetworkLatencyReductionTweaks()
        {
            var category = new TweakCategory
            {
                Name = "Network Latency Reduction",
                Description = "Optimize network stack for minimal latency and maximum responsiveness",
                Icon = "🌐"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_nagle_algorithm",
                Name = "Disable Nagle Algorithm",
                Description = "Reduces network latency by disabling packet coalescing for immediate transmission",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces",
                RegistryKey = "TcpAckFrequency",
                EnabledValue = 1, // Disable Nagle
                DisabledValue = 2, // Default
                Category = "Network Latency Reduction",
                RiskLevel = 2
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "tcp_no_delay",
                Name = "Enable TCP No Delay",
                Description = "Forces immediate transmission of TCP packets for lower latency",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces",
                RegistryKey = "TcpNoDelay",
                EnabledValue = 1, // Enable no delay
                DisabledValue = 0, // Default
                Category = "Network Latency Reduction",
                RiskLevel = 2
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_bandwidth_throttling",
                Name = "Disable Network Bandwidth Throttling",
                Description = "Removes Windows bandwidth limitations for maximum network performance",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Psched",
                RegistryKey = "NonBestEffortLimit",
                EnabledValue = 0, // No throttling
                DisabledValue = 80, // Default 20% reserved
                Category = "Network Latency Reduction",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "optimize_network_throttling",
                Name = "Disable Network Throttling Index",
                Description = "Disables Windows network throttling for consistent performance",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                RegistryKey = "NetworkThrottlingIndex",
                EnabledValue = 0xffffffff, // Disable throttling
                DisabledValue = 10, // Default
                Category = "Network Latency Reduction",
                RiskLevel = 2
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_tcp_chimney",
                Name = "Disable TCP Chimney Offload",
                Description = "Prevents TCP processing offload that can introduce latency",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                RegistryKey = "EnableTCPChimney",
                EnabledValue = 0, // Disabled
                DisabledValue = 1, // Enabled
                Category = "Network Latency Reduction",
                RiskLevel = 2
            });

            TweakCategories.Add(category);
        }

        private void LoadGamingPerformanceTweaks()
        {
            var category = new TweakCategory
            {
                Name = "Gaming Performance",
                Description = "Optimize system for gaming performance and input responsiveness",
                Icon = "🎮"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_mouse_acceleration",
                Name = "Disable Mouse Acceleration",
                Description = "Provides consistent mouse movement for precise gaming control",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Control Panel\Mouse",
                RegistryKey = "MouseSpeed",
                EnabledValue = "0", // Disabled
                DisabledValue = "1", // Enabled
                Category = "Gaming Performance",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "gaming_mode_priority",
                Name = "Enable Gaming Mode Priority",
                Description = "Prioritizes gaming applications for better performance",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                RegistryKey = "Priority",
                EnabledValue = 6, // High priority
                DisabledValue = 2, // Normal priority
                Category = "Gaming Performance",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_fullscreen_optimizations",
                Name = "Disable Fullscreen Optimizations",
                Description = "Disables Windows fullscreen optimizations for better gaming performance",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\System\GameConfigStore",
                RegistryKey = "GameDVR_FSEBehaviorMode",
                EnabledValue = 2, // Disabled
                DisabledValue = 0, // Enabled
                Category = "Gaming Performance",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_game_bar",
                Name = "Disable Xbox Game Bar",
                Description = "Disables Xbox Game Bar to reduce gaming overhead and improve performance",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR",
                RegistryKey = "AppCaptureEnabled",
                EnabledValue = 0, // Disabled
                DisabledValue = 1, // Enabled
                Category = "Gaming Performance",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "reduce_mouse_threshold",
                Name = "Optimize Mouse Precision",
                Description = "Reduces mouse threshold for improved precision and responsiveness",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Control Panel\Mouse",
                RegistryKey = "MouseThreshold1",
                EnabledValue = "0", // No threshold
                DisabledValue = "6", // Default threshold
                Category = "Gaming Performance",
                RiskLevel = 1
            });

            TweakCategories.Add(category);
        }

        private void LoadSystemResponsivenessTweaks()
        {
            var category = new TweakCategory
            {
                Name = "System Responsiveness",
                Description = "Improve overall system responsiveness and UI performance",
                Icon = "⚡"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "reduce_menu_delay",
                Name = "Reduce Menu Show Delay",
                Description = "Makes menus appear instantly for better responsiveness",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Control Panel\Desktop",
                RegistryKey = "MenuShowDelay",
                EnabledValue = "0", // Instant
                DisabledValue = "400", // Default 400ms
                Category = "System Responsiveness",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_startup_delay",
                Name = "Disable Startup Application Delay",
                Description = "Removes artificial delay for startup applications",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                RegistryKey = "StartupDelayInMSec",
                EnabledValue = 0, // No delay
                DisabledValue = 10000, // Default 10 seconds
                Category = "System Responsiveness",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "optimize_foreground_lock_timeout",
                Name = "Optimize Foreground Lock Timeout",
                Description = "Reduces time for applications to steal focus, improving responsiveness",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Control Panel\Desktop",
                RegistryKey = "ForegroundLockTimeout",
                EnabledValue = 0, // Immediate
                DisabledValue = 200000, // Default
                Category = "System Responsiveness",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_windows_search",
                Name = "Disable Windows Search Indexing",
                Description = "Reduces CPU and disk usage by disabling search indexing service",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch",
                RegistryKey = "Start",
                EnabledValue = 4, // Disabled
                DisabledValue = 2, // Automatic
                Category = "System Responsiveness",
                RiskLevel = 3,
                RequiresRestart = true
            });

            TweakCategories.Add(category);
        }

        private void LoadStorageFileSystemTweaks()
        {
            var category = new TweakCategory
            {
                Name = "Storage & File System",
                Description = "Optimize disk performance and file system operations",
                Icon = "💾"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_ntfs_last_access",
                Name = "Disable NTFS Last Access Time",
                Description = "Improves file system performance by not updating last access timestamps",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem",
                RegistryKey = "NtfsDisableLastAccessUpdate",
                EnabledValue = 1, // Disabled
                DisabledValue = 0, // Enabled
                Category = "Storage & File System",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "optimize_ntfs_memory_usage",
                Name = "Optimize NTFS Memory Usage",
                Description = "Increases NTFS memory usage for better file system performance",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem",
                RegistryKey = "NtfsMemoryUsage",
                EnabledValue = 2, // Optimized
                DisabledValue = 1, // Default
                Category = "Storage & File System",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_8dot3_names",
                Name = "Disable 8.3 Short File Names",
                Description = "Improves file system performance by disabling legacy 8.3 filename generation",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem",
                RegistryKey = "NtfsDisable8dot3NameCreation",
                EnabledValue = 1, // Disabled
                DisabledValue = 0, // Enabled
                Category = "Storage & File System",
                RiskLevel = 2
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "optimize_disk_timeout",
                Name = "Optimize Disk Timeout Values",
                Description = "Reduces disk timeout for faster error recovery and better responsiveness",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Disk",
                RegistryKey = "TimeOutValue",
                EnabledValue = 30, // 30 seconds
                DisabledValue = 60, // Default 60 seconds
                Category = "Storage & File System",
                RiskLevel = 2
            });

            TweakCategories.Add(category);
        }

        private void LoadVisualEffectsPerformanceTweaks()
        {
            var category = new TweakCategory
            {
                Name = "Visual Effects & Performance",
                Description = "Optimize visual effects for better performance and responsiveness",
                Icon = "🎨"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_animations",
                Name = "Disable Window Animations",
                Description = "Disables window animations for faster UI response and lower resource usage",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics",
                RegistryKey = "MinAnimate",
                EnabledValue = "0", // Disabled
                DisabledValue = "1", // Enabled
                Category = "Visual Effects & Performance",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_transparency",
                Name = "Disable Window Transparency",
                Description = "Disables window transparency effects to improve performance",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                RegistryKey = "EnableTransparency",
                EnabledValue = 0, // Disabled
                DisabledValue = 1, // Enabled
                Category = "Visual Effects & Performance",
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "optimize_visual_effects",
                Name = "Optimize for Performance",
                Description = "Sets visual effects to 'Adjust for best performance' mode",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                RegistryKey = "VisualFXSetting",
                EnabledValue = 2, // Best performance
                DisabledValue = 0, // Let Windows choose
                Category = "Visual Effects & Performance",
                RiskLevel = 1
            });

            TweakCategories.Add(category);
        }

        private void LoadBackgroundProcessOptimizationTweaks()
        {
            var category = new TweakCategory
            {
                Name = "Background Process Optimization",
                Description = "Optimize background processes and services for better performance",
                Icon = "🔧"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_background_apps",
                Name = "Disable Background Apps",
                Description = "Prevents apps from running in background to save resources",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications",
                RegistryKey = "GlobalUserDisabled",
                EnabledValue = 1, // Disabled
                DisabledValue = 0, // Enabled
                Category = "Background Process Optimization",
                RiskLevel = 2
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_telemetry",
                Name = "Disable Windows Telemetry",
                Description = "Reduces background telemetry data collection for better performance",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                RegistryKey = "AllowTelemetry",
                EnabledValue = 0, // Disabled
                DisabledValue = 3, // Full
                Category = "Background Process Optimization",
                RiskLevel = 2
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_cortana",
                Name = "Disable Cortana",
                Description = "Disables Cortana to reduce background resource usage",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                RegistryKey = "AllowCortana",
                EnabledValue = 0, // Disabled
                DisabledValue = 1, // Enabled
                Category = "Background Process Optimization",
                RiskLevel = 2
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_windows_defender_realtime",
                Name = "Disable Windows Defender Real-time Protection",
                Description = "Disables real-time protection for maximum performance (use with caution)",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection",
                RegistryKey = "DisableRealtimeMonitoring",
                EnabledValue = 1, // Disabled
                DisabledValue = 0, // Enabled
                Category = "Background Process Optimization",
                RiskLevel = 4, // High risk - security implications
                RequiresRestart = true
            });

            TweakCategories.Add(category);
        }

        public async Task<bool> ApplyTweakAsync(PerformanceTweak tweak)
        {
            return await Task.Run(() =>
            {
                var registryService = RegistryService.Instance;
                var result = registryService.ApplyTweak(tweak);

                if (result)
                {
                    tweak.IsEnabled = !tweak.IsEnabled;
                    HasAppliedTweaksThisSession = true;
                    OnPropertyChanged(nameof(TweakCategories));
                }

                return result;
            });
        }

        public async Task<(int restored, int failed)> RestoreAllTweaksAsync()
        {
            return await Task.Run(() =>
            {
                int restored = 0;
                int failed = 0;
                var registryService = RegistryService.Instance;

                foreach (var category in TweakCategories)
                {
                    foreach (var tweak in category.Tweaks)
                    {
                        try
                        {
                            if (registryService.RestoreTweak(tweak))
                            {
                                restored++;
                            }
                            else
                            {
                                failed++;
                            }
                        }
                        catch
                        {
                            failed++;
                        }
                    }
                }

                // After restore, refresh states
                RefreshTweakStates();
                HasAppliedTweaksThisSession = false;
                return (restored, failed);
            });
        }

        public void RefreshTweakStates()
        {
            var registryService = RegistryService.Instance;
            
            foreach (var category in TweakCategories)
            {
                foreach (var tweak in category.Tweaks)
                {
                    if (tweak.Type == TweakType.Registry)
                    {
                        tweak.IsEnabled = registryService.CheckTweakStatus(tweak);
                    }
                }
            }
            
            OnPropertyChanged(nameof(TweakCategories));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
