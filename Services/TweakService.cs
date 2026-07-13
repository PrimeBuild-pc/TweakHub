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
            LoadAdvancedTweaks();
            LoadWindowsUpdateTweaks();
            LoadVisualEffectsPerformanceTweaks();
            var pendingRestarts = UserDataService.Instance.LoadPendingRestartIds();
            foreach (var tweak in TweakCategories.SelectMany(category => category.Tweaks))
                tweak.IsRestartPending = pendingRestarts.Contains(tweak.Id);
            HasAppliedTweaksThisSession = RegistryService.Instance.BackupCount > 0 || PowerService.Instance.HasAnyBackup;
        }

        private void LoadCpuProcessorOptimizationTweaks()
        {
            var category = new TweakCategory
            {
                Name = "CPU & Processor Optimization",
                Description = "Optimize CPU scheduling, priority, and processor performance",
                Icon = "\uE945"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "cpu_priority_separation",
                Name = "Prioritize Foreground Programs",
                Description = "Uses the Windows foreground-program scheduling profile and restores the captured original value.",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                RegistryKey = "Win32PrioritySeparation",
                EnabledValue = 0x26,
                DisabledValue = null,
                Category = "CPU & Processor Optimization",
                RiskLevel = 3
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_cpu_throttling",
                Name = "Maximum CPU Performance on AC",
                Description = "Sets the active power plan minimum processor state to 100% while plugged in. Increases power use and heat.",
                Type = TweakType.Power,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\893dee8e-2bef-41e0-89c6-b55d0929964c",
                RegistryKey = "ValueMax",
                EnabledValue = 0,   // Disable throttling
                DisabledValue = 100, // Default throttling
                Category = "CPU & Processor Optimization",
                RiskLevel = 3
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_core_parking",
                Name = "Disable CPU Core Parking",
                Description = "Sets the active power plan minimum parked cores to 100% while plugged in. Increases power use and heat.",
                Type = TweakType.Power,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583",
                RegistryKey = "ValueMax",
                EnabledValue = 0,   // Disable core parking
                DisabledValue = 100, // Default core parking
                Category = "CPU & Processor Optimization",
                RiskLevel = 3
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "high_performance_power_plan",
                Name = "Force High Performance Power Plan",
                Description = "Activates the Windows High Performance plan and restores the previously active plan when disabled.",
                Type = TweakType.Power,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes",
                RegistryKey = "ActivePowerScheme",
                EnabledValue = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", // High Performance GUID
                DisabledValue = "381b4222-f694-41f0-9685-ff5bb260df2e", // Balanced GUID
                Category = "CPU & Processor Optimization",
                RiskLevel = 3
            });

            TweakCategories.Add(category);
        }

        private void LoadNetworkLatencyReductionTweaks()
        {
            var category = new TweakCategory
            {
                Name = "Network Latency Reduction",
                Description = "Optimize network stack for minimal latency and maximum responsiveness",
                Icon = "\uE968"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "optimize_network_throttling",
                Name = "Disable Network Throttling Index",
                Description = "Uses the disabled DWORD value and restores the captured original setting.",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                RegistryKey = "NetworkThrottlingIndex",
                EnabledValue = -1,
                DisabledValue = null,
                Category = "Network Latency Reduction",
                RiskLevel = 3
            });

            TweakCategories.Add(category);
        }

        private void LoadGamingPerformanceTweaks()
        {
            var category = new TweakCategory
            {
                Name = "Gaming Performance",
                Description = "Optimize system for gaming performance and input responsiveness",
                Icon = "\uE7FC"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_mouse_acceleration",
                Name = "Disable Mouse Acceleration",
                Description = "Changes MouseSpeed and both threshold values as one restorable operation.",
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
                Icon = "\uE945"
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

            TweakCategories.Add(category);
        }

        private void LoadAdvancedTweaks()
        {
            var category = new TweakCategory
            {
                Name = "Advanced",
                Description = "Higher-impact changes that trade Windows functionality for lower background activity",
                Icon = "\uE83D"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_windows_search",
                Name = "Disable Windows Search Indexing",
                Description = "Disables Windows Search indexing and restores the captured service configuration when re-enabled.",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch",
                RegistryKey = "Start",
                EnabledValue = 4, // Disabled
                DisabledValue = 2, // Automatic
                Category = "Advanced",
                RiskLevel = 3,
                RequiresRestart = true
            });

            TweakCategories.Add(category);
        }

        private void LoadWindowsUpdateTweaks()
        {
            var category = new TweakCategory
            {
                Name = "Windows Update Control",
                Description = "Control driver delivery and use a conservative manual update policy",
                Icon = "\uE895"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_automatic_driver_updates",
                Name = "Exclude Drivers from Windows Update",
                Description = "Prevents Windows Update quality updates from automatically delivering device drivers.",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
                RegistryKey = "ExcludeWUDriversInQualityUpdate",
                EnabledValue = 1,
                DisabledValue = null,
                Category = category.Name,
                RiskLevel = 3,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "windows_update_security_preset",
                Name = "Manual Security Update Preset",
                Description = "Notifies before downloads and defers feature updates by 90 days. Security and quality updates remain available for manual installation.",
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
                RegistryKey = "AUOptions",
                EnabledValue = 2,
                DisabledValue = null,
                Category = category.Name,
                RiskLevel = 3,
                RequiresRestart = true
            });

            TweakCategories.Add(category);
        }

        private static readonly RegistryValueChange[] SecurityUpdatePreset =
        [
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUOptions", 2),
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 0),
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdates", 1),
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdatesPeriodInDays", 90)
        ];

        private void LoadVisualEffectsPerformanceTweaks()
        {
            var category = new TweakCategory
            {
                Name = "Visual Effects & Performance",
                Description = "Optimize visual effects for better performance and responsiveness",
                Icon = "\uE790"
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
            var result = await ApplyCoreAsync(tweak, targetEnabled);
            if (result)
            {
                tweak.IsEnabled = targetEnabled;
                if (tweak.RequiresRestart)
                {
                    tweak.IsRestartPending = true;
                    UserDataService.Instance.MarkRestartPending(tweak.Id);
                }
                HasAppliedTweaksThisSession = RegistryService.Instance.BackupCount > 0 || PowerService.Instance.HasAnyBackup;
                OnPropertyChanged(nameof(TweakCategories));
            }
            return result;
        }

        private static Task<bool> ApplyCoreAsync(PerformanceTweak tweak, bool enable) => tweak.Id switch
        {
            "disable_cpu_throttling" => enable
                ? PowerService.Instance.ApplyProcessorSettingAsync(tweak.Id, "PROCTHROTTLEMIN", 100)
                : PowerService.Instance.RestoreProcessorSettingAsync(tweak.Id),
            "disable_core_parking" => enable
                ? PowerService.Instance.ApplyProcessorSettingAsync(tweak.Id, "CPMINCORES", 100)
                : PowerService.Instance.RestoreProcessorSettingAsync(tweak.Id),
            "high_performance_power_plan" => enable
                ? PowerService.Instance.ApplyHighPerformancePlanAsync()
                : PowerService.Instance.RestorePowerPlanAsync(),
            "disable_mouse_acceleration" => Task.Run(() => ApplyMouseAcceleration(enable)),
            "windows_update_security_preset" => enable
                ? RegistryService.Instance.ApplyValuesWithBackupAsync(SecurityUpdatePreset)
                : RegistryService.Instance.RestoreValuesAsync(SecurityUpdatePreset),
            _ => Task.Run(() => RegistryService.Instance.ApplyTweak(tweak, enable))
        };

        private static bool ApplyMouseAcceleration(bool disable)
        {
            const string path = @"HKEY_CURRENT_USER\Control Panel\Mouse";
            var registry = RegistryService.Instance;
            var values = new[] { "MouseSpeed", "MouseThreshold1", "MouseThreshold2" };

            if (!disable)
                return values.All(name => !registry.HasBackup(path, name) || registry.RestoreRegistryValue(path, name));

            foreach (var name in values)
            {
                if (registry.ApplyValueWithBackup(path, name, "0", Microsoft.Win32.RegistryValueKind.String)) continue;
                foreach (var applied in values) registry.RestoreRegistryValue(path, applied);
                return false;
            }
            return true;
        }

        public async Task<(int restored, int failed)> RestoreAllTweaksAsync()
        {
            var tweaks = TweakCategories.SelectMany(category => category.Tweaks).Where(HasBackup).ToList();
            var restored = 0;
            var failed = 0;
            foreach (var tweak in tweaks)
            {
                if (await ApplyCoreAsync(tweak, false))
                {
                    restored++;
                    if (tweak.RequiresRestart)
                    {
                        tweak.IsRestartPending = true;
                        UserDataService.Instance.MarkRestartPending(tweak.Id);
                    }
                }
                else failed++;
            }

            await RefreshTweakStatesAsync();
            HasAppliedTweaksThisSession = RegistryService.Instance.BackupCount > 0 || PowerService.Instance.HasAnyBackup;
            return (restored, failed);
        }

        private static bool HasBackup(PerformanceTweak tweak) => tweak.Id switch
        {
            "disable_cpu_throttling" or "disable_core_parking" or "high_performance_power_plan" =>
                PowerService.Instance.HasBackup(tweak.Id),
            "disable_mouse_acceleration" => new[] { "MouseSpeed", "MouseThreshold1", "MouseThreshold2" }
                .Any(name => RegistryService.Instance.HasBackup(@"HKEY_CURRENT_USER\Control Panel\Mouse", name)),
            "windows_update_security_preset" => SecurityUpdatePreset.Any(change =>
                RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName)),
            _ => RegistryService.Instance.HasBackup(tweak.RegistryPath, tweak.RegistryKey)
        };

        public async Task RefreshTweakStatesAsync()
        {
            foreach (var tweak in TweakCategories.SelectMany(category => category.Tweaks))
            {
                tweak.IsEnabled = tweak.Id switch
                {
                    "disable_cpu_throttling" => await PowerService.Instance
                        .IsProcessorSettingActiveAsync("PROCTHROTTLEMIN", 100),
                    "disable_core_parking" => await PowerService.Instance
                        .IsProcessorSettingActiveAsync("CPMINCORES", 100),
                    "high_performance_power_plan" => await PowerService.Instance.IsHighPerformancePlanActiveAsync(),
                    "disable_mouse_acceleration" => new[] { "MouseSpeed", "MouseThreshold1", "MouseThreshold2" }
                        .All(name => Equals(RegistryService.Instance.GetRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Mouse", name), "0")),
                    "windows_update_security_preset" => SecurityUpdatePreset.All(change =>
                        Equals(RegistryService.Instance.GetRegistryValue(change.KeyPath, change.ValueName), change.Value)),
                    _ => RegistryService.Instance.CheckTweakStatus(tweak)
                };
            }
            OnPropertyChanged(nameof(TweakCategories));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
