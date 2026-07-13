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
        private bool _restartNoticeShownThisSession;
        public bool HasAppliedTweaksThisSession
        {
            get => _hasAppliedTweaksThisSession;
            private set { _hasAppliedTweaksThisSession = value; OnPropertyChanged(); }
        }

        public bool RestartNoticeShownThisSession
        {
            get => _restartNoticeShownThisSession;
            set { _restartNoticeShownThisSession = value; OnPropertyChanged(); }
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
            LoadNetworkLatencyReductionTweaks();
            LoadGamingPerformanceTweaks();
            LoadSystemResponsivenessTweaks();
            LoadVisualEffectsPerformanceTweaks();
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
                Description = "Temporarily unavailable while foreground scheduling and rollback are implemented safely.",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                RegistryKey = "Win32PrioritySeparation",
                EnabledValue = 26, // Optimized for desktop performance
                DisabledValue = 2,  // Default Windows value
                Category = "CPU & Processor Optimization",
                IsAvailable = false,
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_cpu_throttling",
                Name = "Disable CPU Throttling",
                Description = "Temporarily unavailable: this must use the active Windows power plan, not Registry metadata.",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\893dee8e-2bef-41e0-89c6-b55d0929964c",
                RegistryKey = "ValueMax",
                EnabledValue = 0,   // Disable throttling
                DisabledValue = 100, // Default throttling
                Category = "CPU & Processor Optimization",
                IsAvailable = false,
                RiskLevel = 2,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_core_parking",
                Name = "Disable CPU Core Parking",
                Description = "Temporarily unavailable: this must use the active Windows power plan, not Registry metadata.",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583",
                RegistryKey = "ValueMax",
                EnabledValue = 0,   // Disable core parking
                DisabledValue = 100, // Default core parking
                Category = "CPU & Processor Optimization",
                IsAvailable = false,
                RiskLevel = 2,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "high_performance_power_plan",
                Name = "Force High Performance Power Plan",
                Description = "Temporarily unavailable until the active power plan can be captured and restored safely.",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes",
                RegistryKey = "ActivePowerScheme",
                EnabledValue = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", // High Performance GUID
                DisabledValue = "381b4222-f694-41f0-9685-ff5bb260df2e", // Balanced GUID
                Category = "CPU & Processor Optimization",
                IsAvailable = false,
                RiskLevel = 1
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
                Id = "optimize_network_throttling",
                Name = "Disable Network Throttling Index",
                Description = "Temporarily unavailable while DWORD handling and rollback are corrected.",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                RegistryKey = "NetworkThrottlingIndex",
                EnabledValue = 0xffffffff, // Disable throttling
                DisabledValue = 10, // Default
                Category = "Network Latency Reduction",
                IsAvailable = false,
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
                Description = "Temporarily unavailable until all related mouse values can be applied and restored atomically.",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Control Panel\Mouse",
                RegistryKey = "MouseSpeed",
                EnabledValue = "0", // Disabled
                DisabledValue = "1", // Enabled
                Category = "Gaming Performance",
                IsAvailable = false,
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
                Id = "disable_windows_search",
                Name = "Disable Windows Search Indexing",
                Description = "Temporarily unavailable until the service state and original configuration can be restored safely.",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch",
                RegistryKey = "Start",
                EnabledValue = 4, // Disabled
                DisabledValue = 2, // Automatic
                Category = "System Responsiveness",
                IsAvailable = false,
                RiskLevel = 3,
                RequiresRestart = true
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

        public async Task<bool> ApplyTweakAsync(PerformanceTweak tweak)
        {
            // Back-compat overload: apply the opposite of current state.
            return await ApplyTweakAsync(tweak, !tweak.IsEnabled);
        }

        public async Task<bool> ApplyTweakAsync(PerformanceTweak tweak, bool targetEnabled)
        {
            return await Task.Run(() =>
            {
                var registryService = RegistryService.Instance;
                var result = registryService.ApplyTweak(tweak, targetEnabled);

                if (result)
                {
                    tweak.IsEnabled = targetEnabled;
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
