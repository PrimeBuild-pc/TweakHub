using System.Collections.ObjectModel;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TweakHub.Localization;
using TweakHub.Models;

namespace TweakHub.Services
{
    public class TweakService : INotifyPropertyChanged
    {
        public static TweakService Instance { get; } = new();

        public ObservableCollection<TweakCategory> TweakCategories { get; } = new();

        private bool _hasAppliedTweaksThisSession;
        public bool HasAppliedTweaksThisSession
        {
            get => _hasAppliedTweaksThisSession;
            private set { _hasAppliedTweaksThisSession = value; OnPropertyChanged(); }
        }

        public bool RestartNoticeShownThisSession { get; set; }
        public bool RegistryDisclaimerShown { get; set; }

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
            LoadMemoryManagementTweaks();
            LoadPrivacyTweaks();
            LoadAdvancedTweaks();
            LoadWindowsUpdateTweaks();
            LoadVisualEffectsPerformanceTweaks();
            var pendingRestarts = RestartVerificationService.Instance.CurrentBootPendingTweakIds();
            foreach (var tweak in TweakCategories.SelectMany(category => category.Tweaks))
                tweak.IsRestartPending = pendingRestarts.Contains(tweak.Id);
            RefreshBackupState();
        }

        private void LoadCpuProcessorOptimizationTweaks()
        {
            var category = new TweakCategory
            {
                Name = L.Get("Tweaks:CategoryOrTweakNameCPUProcessorOptimization"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionOptimizeCPUSchedulingPriorityAndProcessor"),
                Icon = "\uE945"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "cpu_priority_separation",
                Name = L.Get("Tweaks:CategoryOrTweakNamePrioritizeForegroundPrograms"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionUsesTheWindowsForegroundProgramScheduling"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                RegistryKey = "Win32PrioritySeparation",
                EnabledValue = 0x26,
                RiskLevel = 3
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_cpu_throttling",
                Name = L.Get("Tweaks:CategoryOrTweakNameMaximumCPUPerformanceOnAC"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionSetsTheActivePowerPlanMinimum"),
                Type = TweakType.Power,
                RiskLevel = 3
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_core_parking",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableCPUCoreParking"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionSetsTheActivePowerPlanMinimum2"),
                Type = TweakType.Power,
                RiskLevel = 3
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "high_performance_power_plan",
                Name = L.Get("Tweaks:CategoryOrTweakNameForceHighPerformancePowerPlan"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionActivatesTheWindowsHighPerformancePlan"),
                Type = TweakType.Power,
                RiskLevel = 3
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_power_throttling",
                Name = L.Get("Maintenance:DisablePowerThrottlingName"),
                Description = L.Get("Maintenance:DisablePowerThrottlingDescription"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling",
                RegistryKey = "PowerThrottlingOff", EnabledValue = 1, RiskLevel = 3, RequiresRestart = true
            });
            TweakCategories.Add(category);
        }

        private void LoadNetworkLatencyReductionTweaks()
        {
            var category = new TweakCategory
            {
                Name = L.Get("Tweaks:CategoryOrTweakNameNetworkLatencyReduction"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionOptimizeNetworkStackForMinimalLatency"),
                Icon = "\uE968"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "optimize_network_throttling",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableNetworkThrottlingIndex"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionUsesTheDisabledDWORDValueAnd"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                RegistryKey = "NetworkThrottlingIndex",
                EnabledValue = -1,
                RiskLevel = 3
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "optimize_network_for_gaming",
                Name = L.Get("Tweaks:CategoryOrTweakNameOptimizeNetworkForGaming"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionOptimizeNetworkForGaming"),
                Type = TweakType.NetworkAdapter,
                RiskLevel = 3,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "limit_background_updates",
                Name = L.Get("Tweaks:CategoryOrTweakNameLimitBackgroundUpdates"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionLimitBackgroundUpdates"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization",
                RegistryKey = "DOPercentageMaxBackgroundBandwidth",
                // Background Windows Update downloads that fill the line are the usual cause of in-game bufferbloat on a PC.
                EnabledValue = 30,
                RiskLevel = 2
            });

            TweakCategories.Add(category);
        }

        private void LoadGamingPerformanceTweaks()
        {
            var category = new TweakCategory
            {
                Name = L.Get("Tweaks:CategoryOrTweakNameGamingPerformance"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionOptimizeSystemForGamingPerformanceAnd"),
                Icon = "\uE7FC"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_mouse_acceleration",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableMouseAcceleration"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionChangesMouseSpeedAndBothThresholdValues"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Control Panel\Mouse",
                RegistryKey = "MouseSpeed",
                EnabledValue = "0", // Disabled
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_fullscreen_optimizations",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableFullscreenOptimizations"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisablesWindowsFullscreenOptimizationsForBetter"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\System\GameConfigStore",
                RegistryKey = "GameDVR_FSEBehaviorMode",
                EnabledValue = 2, // Disabled
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "optimize_flip_model",
                Name = L.Get("Tweaks:CategoryOrTweakNameOptimizeFlipModel"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionOptimizeFlipModel"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                RegistryKey = "HwSchMode",
                EnabledValue = 2,
                RiskLevel = 2,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_game_bar",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableXboxGameBar"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisablesXboxGameBarToReduce"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR",
                RegistryKey = "AppCaptureEnabled",
                EnabledValue = 0, // Disabled
                RiskLevel = 1
            });

            TweakCategories.Add(category);
        }

        private void LoadSystemResponsivenessTweaks()
        {
            var category = new TweakCategory
            {
                Name = L.Get("Tweaks:CategoryOrTweakNameSystemResponsiveness"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionImproveOverallSystemResponsivenessAndUI"),
                Icon = "\uE945"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "reduce_menu_delay",
                Name = L.Get("Tweaks:CategoryOrTweakNameReduceMenuShowDelay"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionMakesMenusAppearInstantlyForBetter"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Control Panel\Desktop",
                RegistryKey = "MenuShowDelay",
                EnabledValue = "0", // Instant
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_startup_delay",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableStartupApplicationDelay"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionRemovesArtificialDelayForStartupApplications"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                RegistryKey = "StartupDelayInMSec",
                EnabledValue = 0, // No delay
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "enable_taskbar_end_task",
                Name = L.Get("Tweaks:CategoryOrTweakNameEnableTaskbarEndTask"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionEnableTaskbarEndTask"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings",
                RegistryKey = "TaskbarEndTask",
                EnabledValue = 1,
                RiskLevel = 1
            });

            TweakCategories.Add(category);
        }

        private void LoadMemoryManagementTweaks()
        {
            var category = new TweakCategory
            {
                Name = L.Get("Tweaks:CategoryOrTweakNameMemoryManagement"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionWindowsNormallyUsesSpareRAMAs"),
                Icon = "\uE950"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_sysmain",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableSysMainSuperfetch"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionStopsAndDisablesSysMainUsuallyLeave"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SysMain",
                RegistryKey = "Start",
                EnabledValue = 4,
                RiskLevel = 3,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_prefetch",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisablePrefetch"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisablesApplicationAndBootPrefetchingUsually"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters",
                RegistryKey = "EnablePrefetcher",
                EnabledValue = 0,
                RiskLevel = 3,
                RequiresRestart = true
            });

            TweakCategories.Add(category);
        }

        private void LoadPrivacyTweaks()
        {
            var category = new TweakCategory
            {
                Name = L.Get("Tweaks:CategoryOrTweakNamePrivacyDeviceControl"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionReversiblePrivacyAndDeviceInstallationPolicies"),
                Icon = "\uE72E"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_windows_ai_policies",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableWindowsAIPolicies"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionHidesTheWindowsAIComponentsSettings"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                RegistryKey = "SettingsPageVisibility",
                EnabledValue = "hide:aicomponents",
                RiskLevel = 4,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_wpbt",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableWindowsPlatformBinaryTableWPBT"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionPreventsFirmwareProvidedWPBTSoftwareFrom"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager",
                RegistryKey = "DisableWpbtExecution",
                EnabledValue = 1,
                RiskLevel = 4,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "prevent_device_metadata",
                Name = L.Get("Tweaks:CategoryOrTweakNamePreventDeviceCompanionApps"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionPreventsAutomaticDownloadOfApplicationsAnd"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Device Metadata",
                RegistryKey = "PreventDeviceMetadataFromNetwork",
                EnabledValue = 1,
                RiskLevel = 3
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_device_coinstallers",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableAutomaticDriverSearchAndCo"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisablesAutomaticDriverSearchingAndAll"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching",
                RegistryKey = "SearchOrderConfig",
                EnabledValue = 0,
                RiskLevel = 5,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_notifications_calendar",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableNotificationsAndCalendar"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisablesNotificationCenterToastNotificationsAnd"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer",
                RegistryKey = "DisableNotificationCenter",
                EnabledValue = 1,
                RiskLevel = 3,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_advertising_id",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableAdvertisingId"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisableAdvertisingId"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                RegistryKey = "Enabled",
                EnabledValue = 0,
                RiskLevel = 2
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_tailored_experiences",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableTailoredExperiences"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisableTailoredExperiences"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Privacy",
                RegistryKey = "TailoredExperiencesWithDiagnosticDataEnabled",
                EnabledValue = 0,
                RiskLevel = 2
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_activity_history",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableActivityHistory"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisableActivityHistory"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",
                RegistryKey = "EnableActivityFeed",
                EnabledValue = 0,
                RiskLevel = 3,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_telemetry",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableTelemetry"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisableTelemetry"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                RegistryKey = "AllowTelemetry",
                EnabledValue = 0,
                RiskLevel = 3,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_consumer_content",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableConsumerContent"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisableConsumerContent"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                RegistryKey = "DisableWindowsConsumerFeatures",
                EnabledValue = 1,
                RiskLevel = 2
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_bing_search",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableBingSearch"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisableBingSearch"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer",
                RegistryKey = "DisableSearchBoxSuggestions",
                EnabledValue = 1,
                RiskLevel = 1,
                RequiresRestart = true
            });

            TweakCategories.Add(category);
        }

        private void LoadAdvancedTweaks()
        {
            var category = new TweakCategory
            {
                Name = L.Get("Tweaks:CategoryOrTweakNameAdvanced"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionHigherImpactChangesThatTradeWindows"),
                Icon = "\uE83D"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_windows_search",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableWindowsSearchIndexing"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisablesWindowsSearchIndexingAndRestores"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch",
                RegistryKey = "Start",
                EnabledValue = 4, // Disabled
                RiskLevel = 3,
                RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_fast_startup",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableFastStartup"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisableFastStartup"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Power",
                RegistryKey = "HiberbootEnabled",
                EnabledValue = 0,
                RiskLevel = 2
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_hibernation",
                Name = L.Get("Maintenance:DisableHibernationName"),
                Description = L.Get("Maintenance:DisableHibernationDescription"),
                Type = TweakType.Power, RiskLevel = 3
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "power_policy_no_auto_sleep",
                Name = L.Get("Maintenance:NoAutoSleepName"),
                Description = L.Get("Maintenance:NoAutoSleepDescription"),
                // Windows reads power policies at boot: a policy refresh (gpupdate) does not apply them (checked in a VM).
                Type = TweakType.Registry, RiskLevel = 2, RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "power_policy_disable_sleep",
                Name = L.Get("Maintenance:DisableSleepName"),
                Description = L.Get("Maintenance:DisableSleepDescription"),
                Type = TweakType.Registry, RiskLevel = 3, RequiresRestart = true
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "detailed_bsod",
                Name = L.Get("Tweaks:CategoryOrTweakNameDetailedBsod"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDetailedBsod"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\CrashControl",
                RegistryKey = "DisplayParameters",
                EnabledValue = 1,
                RiskLevel = 1,
                RequiresRestart = true
            });

            TweakCategories.Add(category);
        }

        private void LoadWindowsUpdateTweaks()
        {
            var category = new TweakCategory
            {
                Name = L.Get("Tweaks:CategoryOrTweakNameWindowsUpdateControl"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionControlDriverDeliveryAndUseA"),
                Icon = "\uE895"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_automatic_driver_updates",
                Name = L.Get("Tweaks:CategoryOrTweakNameExcludeDriversFromWindowsUpdate"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionPreventsWindowsUpdateQualityUpdatesFrom"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
                RegistryKey = "ExcludeWUDriversInQualityUpdate",
                EnabledValue = 1,
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

        private static readonly RegistryValueChange[] WindowsAiPolicies =
        [
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "SettingsPageVisibility", "hide:aicomponents", Microsoft.Win32.RegistryValueKind.String),
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\WindowsNotepad", "DisableAIFeatures", 1),
            new(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1),
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 1),
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "AllowRecallEnablement", 0),
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableClickToDo", 1)
        ];

        private static readonly RegistryValueChange[] TelemetryPolicies =
        [
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0),
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DoNotShowFeedbackNotifications", 1),
            new(@"HKEY_CURRENT_USER\Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0),
            new(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DiagTrack", "Start", 4)
        ];

        private const string ContentDelivery = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";

        private static readonly RegistryValueChange[] ConsumerContentPolicies =
        [
            // Honoured only by Enterprise and Education; the per-user values below cover Home and Pro.
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1),
            new(ContentDelivery, "SilentInstalledAppsEnabled", 0),
            new(ContentDelivery, "SystemPaneSuggestionsEnabled", 0),
            new(ContentDelivery, "SoftLandingEnabled", 0),
            new(ContentDelivery, "SubscribedContent-338388Enabled", 0),
            new(ContentDelivery, "SubscribedContent-338389Enabled", 0),
            new(ContentDelivery, "SubscribedContent-353694Enabled", 0),
            new(ContentDelivery, "SubscribedContent-353696Enabled", 0),
            new(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement", "ScoobeSystemSettingEnabled", 0),
            new(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_IrisRecommendations", 0)
        ];

        private static readonly RegistryValueChange[] BingSearchPolicies =
        [
            new(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1),
            new(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0),
            new(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", 0)
        ];

        private const string UserGpuPreferences = @"HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences";
        private const string GameConfigStore = @"HKEY_CURRENT_USER\System\GameConfigStore";

        /// <summary>The settings windowed and borderless games need to reach independent flip.</summary>
        private static RegistryValueChange[] FlipModelChanges() =>
        [
            new(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2),
            new(UserGpuPreferences, "DirectXUserGlobalSettings", WithDirectXSetting(
                RegistryService.Instance.GetRegistryValue(UserGpuPreferences, "DirectXUserGlobalSettings") as string,
                "SwapEffectUpgradeEnable", "1"), Microsoft.Win32.RegistryValueKind.String),
            new(GameConfigStore, "GameDVR_FSEBehaviorMode", 0),
            new(GameConfigStore, "GameDVR_HonorUserFSEBehaviorMode", 0),
            new(GameConfigStore, "GameDVR_DXGIHonorFSEWindowsCompatible", 0),
            // Multiplane overlay (OverlayTestMode) is left to Tweak check "Mpo": a shared backup let undoing one feature undo the other.
            new(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "AutoGameModeEnabled", 1)
        ];

        /// <summary>Sets one "Name=Value;" entry and keeps the other settings Windows stores in the same string, in place.</summary>
        internal static string WithDirectXSetting(string? settings, string name, string value)
        {
            var entries = (settings ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            var index = entries.FindIndex(entry => entry.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase));
            if (index >= 0) entries[index] = $"{name}={value}";
            else entries.Add($"{name}={value}");
            return string.Concat(entries.Select(entry => entry + ";"));
        }

        private static readonly RegistryValueChange[] DeviceCoInstallerPolicies =
        [
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", "SearchOrderConfig", 0),
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Device Installer", "DisableCoInstallers", 1)
        ];

        private static readonly RegistryValueChange[] NotificationPolicies =
        [
            new(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter", 1),
            new(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\PushNotifications", "ToastEnabled", 0)
        ];

        private static readonly RegistryValueChange[] ActivityHistoryPolicies =
        [
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0),
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", 0),
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", 0)
        ];

        private static readonly RegistryValueChange[] MouseAccelerationOff =
        [
            new(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseSpeed", "0", Microsoft.Win32.RegistryValueKind.String),
            new(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold1", "0", Microsoft.Win32.RegistryValueKind.String),
            new(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold2", "0", Microsoft.Win32.RegistryValueKind.String)
        ];

        // Group Policy → Power Management, from Windows' Power.admx. AC (plugged in) only: battery keeps the Windows behaviour.
        private const string PowerPolicies = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Power\PowerSettings\";

        private static readonly RegistryValueChange[] NoAutoSleepPolicies =
        [
            new(PowerPolicies + "29F6C1DB-86DA-48C5-9FDB-F2B67B1F44DA", "ACSettingIndex", 0), // system sleep timeout: never
            new(PowerPolicies + "9D7815A6-7EE4-497E-8888-515A05F02364", "ACSettingIndex", 0), // system hibernate timeout: never
            new(PowerPolicies + "94ac6d29-73ce-41a6-809f-6363ba21b47e", "ACSettingIndex", 0), // turn off hybrid sleep
            new(PowerPolicies + "6738E2C4-E8A5-4A42-B16A-E040E769756E", "ACSettingIndex", 0), // turn off the hard disk: never
            new(PowerPolicies + "E69653CA-CF7F-4F05-AA73-CB833FA90AD4", "ACSettingIndex", 0), // Energy Saver threshold: never automatic
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Power\EnergySaver", "EnableEnergySaver", 0) // Energy Saver not forced always on
        ];

        private static readonly RegistryValueChange[] DisableSleepPolicies =
        [
            new(PowerPolicies + "abfc2519-3608-4c2a-94ea-171b0ed546ab", "ACSettingIndex", 0), // standby states S1-S3 not allowed
            new(PowerPolicies + "96996BC0-AD50-47EC-923B-6F41874DD9EB", "ACSettingIndex", 0), // sleep button: do nothing
            new(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings", "ShowSleepOption", 0)
        ];

        internal static IReadOnlyList<RegistryValueChange> GetCompositeRegistryChanges(string id) => id switch
        {
            "power_policy_no_auto_sleep" => NoAutoSleepPolicies,
            "power_policy_disable_sleep" => DisableSleepPolicies,
            "disable_mouse_acceleration" => MouseAccelerationOff,
            "windows_update_security_preset" => SecurityUpdatePreset,
            "disable_windows_ai_policies" => WindowsAiPolicies,
            "disable_device_coinstallers" => DeviceCoInstallerPolicies,
            "disable_notifications_calendar" => NotificationPolicies,
            "disable_activity_history" => ActivityHistoryPolicies,
            "disable_telemetry" => TelemetryPolicies,
            "disable_consumer_content" => ConsumerContentPolicies,
            "disable_bing_search" => BingSearchPolicies,
            "optimize_flip_model" => FlipModelChanges(),
            _ => []
        };

        private void LoadVisualEffectsPerformanceTweaks()
        {
            var category = new TweakCategory
            {
                Name = L.Get("Tweaks:CategoryOrTweakNameVisualEffectsPerformance"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionOptimizeVisualEffectsForBetterPerformance"),
                Icon = "\uE790"
            };

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_animations",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableWindowAnimations"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisablesWindowAnimationsForFasterUI"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics",
                RegistryKey = "MinAnimate",
                EnabledValue = "0", // Disabled
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "disable_transparency",
                Name = L.Get("Tweaks:CategoryOrTweakNameDisableWindowTransparency"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionDisablesWindowTransparencyEffectsToImprove"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                RegistryKey = "EnableTransparency",
                EnabledValue = 0, // Disabled
                RiskLevel = 1
            });

            category.Tweaks.Add(new PerformanceTweak
            {
                Id = "optimize_visual_effects",
                Name = L.Get("Tweaks:CategoryOrTweakNameOptimizeForPerformance"),
                Description = L.Get("Tweaks:CategoryOrTweakDescriptionSetsVisualEffectsToAdjustFor"),
                Type = TweakType.Registry,
                RegistryPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                RegistryKey = "VisualFXSetting",
                EnabledValue = 2, // Best performance
                RiskLevel = 1
            });

            TweakCategories.Add(category);
        }

        public async Task<bool> ApplyTweakAsync(PerformanceTweak tweak, bool targetEnabled)
        {
            var result = await ApplyCoreAsync(tweak, targetEnabled);
            RefreshBackupState(); // Failed or cancelled mutations can still leave a recovery backup.
            if (result)
            {
                MarkApplied(tweak.Id, targetEnabled);
                tweak.IsEnabled = targetEnabled;
                tweak.IsPartiallyApplied = false;
                if (tweak.RequiresRestart)
                {
                    tweak.IsRestartPending = true;
                    RestartVerificationService.Instance.TrackTweak(tweak.Id, targetEnabled);
                }
            }
            return result;
        }

        private static Task<bool> ApplyCoreAsync(PerformanceTweak tweak, bool enable)
        {
            var changes = GetCompositeRegistryChanges(tweak.Id);
            if (changes.Count > 0)
                return enable
                    ? RegistryService.Instance.ApplyValuesWithBackupAsync(changes)
                    // Values added to a tweak after it was applied, or adapters added later, were never changed: leave them.
                    : RegistryService.Instance.RestoreValuesAsync(changes
                        .Where(change => RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName)).ToList());

            return tweak.Id switch
            {
                "disable_cpu_throttling" => enable
                    ? PowerService.Instance.ApplyProcessorSettingAsync(tweak.Id, "PROCTHROTTLEMIN", 100)
                    : PowerService.Instance.RestoreProcessorSettingAsync(tweak.Id),
                "disable_core_parking" => enable
                    ? PowerService.Instance.ApplyProcessorSettingAsync(tweak.Id, "CPMINCORES", 100)
                    : PowerService.Instance.RestoreProcessorSettingAsync(tweak.Id),
                "high_performance_power_plan" => enable
                    ? PowerService.Instance.ApplyPerformancePlanAsync(null)
                    : PowerService.Instance.RestorePowerPlanAsync(),
                "disable_hibernation" => HibernationService.Instance.SetDisabledAsync(enable),
                "disable_sysmain" => ApplyServiceTweakAsync(tweak, "SysMain", enable),
                "disable_windows_search" => ApplyServiceTweakAsync(tweak, "WSearch", enable),
                "optimize_network_for_gaming" => enable
                    ? ApplyNetworkPresetAsync()
                    : RegistryService.Instance.RestoreValuesAsync(NetworkAdapterService.BackedUpChanges()),
                _ => Task.Run(() => RegistryService.Instance.ApplyTweak(tweak, enable))
            };
        }

        private static Task<bool> ApplyNetworkPresetAsync()
        {
            var changes = NetworkAdapterService.PresetChanges();
            return changes.Count == 0 ? Task.FromResult(false) : RegistryService.Instance.ApplyValuesWithBackupAsync(changes);
        }

        private static async Task<bool> ApplyServiceTweakAsync(PerformanceTweak tweak, string serviceName, bool disable)
        {
            var registry = RegistryService.Instance;
            if (disable)
            {
                registry.CreateBackup([tweak]);
                var result = await PowerShellService.Instance.ExecuteScriptAsync($$"""
                    $ErrorActionPreference = 'Stop'
                    Set-Service -Name '{{serviceName}}' -StartupType Disabled
                    Stop-Service -Name '{{serviceName}}' -Force -ErrorAction SilentlyContinue
                    """, requireAdministrator: true, timeout: TimeSpan.FromMinutes(2));
                var success = result.Success
                    && Equals(registry.GetRegistryValue(tweak.RegistryPath, tweak.RegistryKey), 4)
                    && await IsServiceStoppedAsync(serviceName);
                if (!success) registry.RestoreRegistryValue(tweak.RegistryPath, tweak.RegistryKey);
                return success;
            }

            if (!registry.RestoreRegistryValue(tweak.RegistryPath, tweak.RegistryKey)) return false;
            var start = Convert.ToInt32(registry.GetRegistryValue(tweak.RegistryPath, tweak.RegistryKey) ?? 4);
            if (start != 4)
                await PowerShellService.Instance.ExecuteScriptAsync(
                    $"Start-Service -Name '{serviceName}' -ErrorAction SilentlyContinue",
                    requireAdministrator: true,
                    timeout: TimeSpan.FromMinutes(2));
            return true;
        }

        internal static async Task<bool> IsServiceStoppedAsync(string serviceName) =>
            (await GetStoppedServicesAsync(serviceName)).Contains(serviceName);

        private static async Task<HashSet<string>> GetStoppedServicesAsync(params string[] serviceNames)
        {
            var result = await PowerShellService.Instance.ExecuteScriptAsync(
                $"Get-Service -Name {string.Join(",", serviceNames.Select(PowerShellService.Literal))} -ErrorAction SilentlyContinue | Where-Object Status -eq 'Stopped' | ForEach-Object Name",
                timeout: TimeSpan.FromSeconds(10));
            return result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<(int restored, int failed)> RestoreAllTweaksAsync()
        {
            var restored = 0;
            var failed = 0;
            // The preset first: it shares values with single tweaks (driver updates, device metadata), and restoring
            // those first would drop the shared backups the preset needs.
            var windowsUpdate = WindowsUpdatePresetService.Instance;
            var windowsUpdateRestored = false;
            if (windowsUpdate.HasCompleteBackup)
            {
                var result = await windowsUpdate.RestoreAsync();
                if (result.Success) { restored++; windowsUpdateRestored = true; } else failed++;
            }
            else
            {
                var legacyPreset = GetCompositeRegistryChanges("windows_update_security_preset");
                if (legacyPreset.Any(change => RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName)))
                {
                    if (await RegistryService.Instance.RestoreValuesAsync(legacyPreset)) restored++; else failed++;
                }
            }

            foreach (var tweak in TweakCategories.SelectMany(category => category.Tweaks).Where(HasBackup).ToList())
            {
                if (await ApplyCoreAsync(tweak, false))
                {
                    MarkApplied(tweak.Id, false);
                    restored++;
                    if (tweak.RequiresRestart)
                    {
                        tweak.IsRestartPending = true;
                        RestartVerificationService.Instance.TrackTweak(tweak.Id, false);
                    }
                }
                else failed++;
            }

            if (windowsUpdateRestored)
            {
                var preset = await windowsUpdate.DetectAsync();
                if (preset is WindowsUpdatePreset.Default or WindowsUpdatePreset.Disabled)
                    RestartVerificationService.Instance.TrackWindowsUpdate(preset);
            }

            await RefreshTweakStatesAsync();
            RefreshBackupState();
            return (restored, failed);
        }

        public static bool HasBackup(PerformanceTweak tweak)
        {
            var changes = GetCompositeRegistryChanges(tweak.Id);
            if (changes.Count > 0)
                return changes.Any(change => RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName));

            return tweak.Id switch
            {
                "disable_cpu_throttling" or "disable_core_parking" or "high_performance_power_plan" =>
                    PowerService.Instance.HasBackup(tweak.Id),
                "disable_hibernation" => HibernationService.Instance.HasBackup,
                "optimize_network_for_gaming" => NetworkAdapterService.BackedUpChanges().Count > 0,
                _ => RegistryService.Instance.HasBackup(tweak.RegistryPath, tweak.RegistryKey)
            };
        }

        /// <summary>
        /// Tweaks TweakHub applied (their undo backup is still there) that are no longer in effect: a Windows update or
        /// another tool put the values back. Reapplying keeps the first backup, so restoring still returns the original.
        /// </summary>
        public List<PerformanceTweak> RevertedTweaks()
        {
            var applied = JsonStore.Load<HashSet<string>>(AppliedTweaksPath);
            return TweakCategories.SelectMany(category => category.Tweaks)
                .Where(tweak => IsReverted(tweak, applied.Contains(tweak.Id), HasBackup)).ToList();
        }

        // A backup alone is not proof: the Backup button and cancelled applies create one too, so the user's own
        // successful apply is recorded. Half-applied groups already show their own warning.
        internal static bool IsReverted(PerformanceTweak tweak, bool appliedByUser, Func<PerformanceTweak, bool> hasBackup) =>
            appliedByUser && tweak.IsAvailable && !tweak.IsEnabled && !tweak.IsPartiallyApplied && hasBackup(tweak);

        internal static string AppliedTweaksPath { get; set; } = Path.Combine(AppDataPath.BasePath, "applied-tweaks.json");

        internal static void MarkApplied(string id, bool applied)
        {
            try
            {
                var ids = JsonStore.Load<HashSet<string>>(AppliedTweaksPath);
                if (applied ? ids.Add(id) : ids.Remove(id)) JsonStore.Save(AppliedTweaksPath, ids);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Only the reverted-tweaks banner depends on this file; the change itself already succeeded.
            }
        }

        public async Task LoadWithStatesAsync()
        {
            LoadTweaks();
            await RefreshTweakStatesAsync();
        }

        public async Task RefreshTweakStatesAsync()
        {
            var power = PowerService.Instance.GetTweakStates();
            var stopped = await GetStoppedServicesAsync("SysMain", "WSearch");
            foreach (var tweak in TweakCategories.SelectMany(category => category.Tweaks))
            {
                if (tweak.Id == "disable_hibernation")
                {
                    tweak.UnavailableReason = HibernationService.Instance.UnavailableReason;
                    tweak.IsEnabled = HibernationService.Instance.IsDisabled;
                    tweak.IsPartiallyApplied = false;
                    continue;
                }
                if (tweak.Type == TweakType.NetworkAdapter)
                {
                    RefreshNetworkPreset(tweak);
                    continue;
                }

                var changes = GetCompositeRegistryChanges(tweak.Id);
                if (changes.Count > 0)
                {
                    var matches = changes.Count(change =>
                        Equals(RegistryService.Instance.GetRegistryValue(change.KeyPath, change.ValueName), change.Value));
                    tweak.IsEnabled = matches == changes.Count;
                    // Some targets are also Windows defaults (the GameDVR values of the flip tweak are 0 out of the box):
                    // only a group TweakHub changed and still has a backup for can be half applied.
                    tweak.IsPartiallyApplied = matches > 0 && matches < changes.Count && HasBackup(tweak);
                    continue;
                }

                tweak.IsPartiallyApplied = false;
                tweak.IsEnabled = tweak.Id switch
                {
                    "disable_cpu_throttling" => power.MaximumProcessorState,
                    "disable_core_parking" => power.NoCoreParking,
                    "high_performance_power_plan" => power.PerformancePlan,
                    "disable_sysmain" => RegistryService.Instance.CheckTweakStatus(tweak) && stopped.Contains("SysMain"),
                    "disable_windows_search" => RegistryService.Instance.CheckTweakStatus(tweak) && stopped.Contains("WSearch"),
                    _ => RegistryService.Instance.CheckTweakStatus(tweak)
                };
            }
        }

        private static void RefreshNetworkPreset(PerformanceTweak tweak)
        {
            var adapters = NetworkAdapterService.GetAdapters();
            var changes = NetworkAdapterService.Selected(adapters).SelectMany(adapter => adapter.Changes).ToList();
            var matches = changes.Count(change =>
                Equals(RegistryService.Instance.GetRegistryValue(change.KeyPath, change.ValueName), change.Value));
            tweak.IsEnabled = changes.Count > 0 && matches == changes.Count;
            tweak.IsPartiallyApplied = matches > 0 && matches < changes.Count && NetworkAdapterService.BackedUpChanges().Count > 0;
            // Values changed on an adapter that is gone or no longer supported must stay restorable.
            tweak.UnavailableReason = changes.Count > 0 || NetworkAdapterService.BackedUpChanges().Count > 0
                ? string.Empty
                : L.Format("Tweaks:NetworkPresetUnavailable", adapters.Count == 0
                    ? L.Get("Tweaks:NetworkPresetNoAdapters")
                    : string.Join(", ", adapters.Select(adapter => adapter.Model)));
        }

        internal static WindowsUpdatePreset RecommendedWindowsUpdatePreset => WindowsUpdatePreset.Security;

        // Disabling fullscreen optimizations blocks independent flip on Windows 11; the network preset trades CPU and power for latency.
        public static bool IsRecommended(PerformanceTweak tweak) =>
            tweak.Id is not ("disable_sysmain" or "disable_prefetch" or "disable_fullscreen_optimizations" or "optimize_network_for_gaming"
                or "disable_hibernation" or "disable_power_throttling" or "power_policy_disable_sleep");

        public void RefreshBackupState() => HasAppliedTweaksThisSession =
            RegistryService.Instance.BackupCount > 0 || PowerService.Instance.HasAnyBackup || HibernationService.Instance.HasBackup || WindowsUpdatePresetService.Instance.HasBackup;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
