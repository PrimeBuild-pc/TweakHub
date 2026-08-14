using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using TweakHub.Localization;
using TweakHub.Models;

namespace TweakHub.Services;

public sealed class ShortcutService
{
    public static ShortcutService Instance { get; } = new();

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
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameDeviceManager"), Description = L.Get("Tools:DescriptionManageHardwareDevicesAndDrivers"), Command = "devmgmt.msc", Category = "System Management" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameSystemInformation"), Description = L.Get("Tools:DescriptionViewDetailedSystemInformation"), Command = "msinfo32", Category = "System Information" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameRegistryEditor"), Description = L.Get("Tools:DescriptionEditTheWindowsRegistry"), Command = "regedit", Category = "Advanced Tools" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameServices"), Description = L.Get("Tools:DescriptionManageWindowsServices"), Command = "services.msc", Category = "System Management" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameEventViewer"), Description = L.Get("Tools:DescriptionReviewWindowsSystemAndApplicationEvents"), Command = "eventvwr.msc", Category = "System Management" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameDiskManagement"), Description = L.Get("Tools:DescriptionManageDisksPartitionsAndDriveLetters"), Command = "diskmgmt.msc", Category = "System Management" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameTaskManager"), Description = L.Get("Tools:DescriptionMonitorProcessesAndPerformance"), Command = "taskmgr", Category = "Performance" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameResourceMonitor"), Description = L.Get("Tools:DescriptionInspectDetailedResourceUsage"), Command = "resmon", Category = "Performance" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNamePowerOptions"), Description = L.Get("Tools:DescriptionConfigurePowerAndSleepSettings"), Command = "powercfg.cpl", Category = "Power Management" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameNetworkConnections"), Description = L.Get("Tools:DescriptionManageNetworkAdapters"), Command = "ncpa.cpl", Category = "Network" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameSoundSettings"), Description = L.Get("Tools:DescriptionConfigureAudioDevices"), Command = "mmsys.cpl", Category = "Audio" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameDisplaySettings"), Description = L.Get("Tools:DescriptionConfigureDisplays"), Command = "ms-settings:display", Category = "Display" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameWindowsFeatures"), Description = L.Get("Tools:DescriptionEnableOrDisableWindowsFeatures"), Command = "optionalfeatures", Category = "System Management" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameDiskCleanup"), Description = L.Get("Tools:DescriptionFreeDiskSpaceWithTheWindowsUtility"), Command = "cleanmgr", Category = "Maintenance" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameGroupPolicyEditor"), Description = L.Get("Tools:DescriptionEditLocalWindowsGroupPoliciesProAnd"), Command = "gpedit.msc", Category = "Advanced Tools" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameTaskScheduler"), Description = L.Get("Tools:DescriptionCreateAndManageScheduledTasks"), Command = "taskschd.msc", Category = "System Management" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameComputerManagement"), Description = L.Get("Tools:DescriptionOpenComputerManagementConsole"), Command = "compmgmt.msc", Category = "System Management" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNamePerformanceMonitor"), Description = L.Get("Tools:DescriptionRecordAndInspectPerformanceCounters"), Command = "perfmon.msc", Category = "Performance" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameReliabilityMonitor"), Description = L.Get("Tools:DescriptionReviewApplicationAndSystemReliabilityHistory"), Command = "perfmon.exe", Arguments = "/rel", Category = "Performance" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameAdvancedFirewall"), Description = L.Get("Tools:DescriptionManageAdvancedWindowsFirewallRules"), Command = "wf.msc", Category = "Advanced Tools" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameLocalSecurityPolicy"), Description = L.Get("Tools:DescriptionManageLocalSecurityPolicies"), Command = "secpol.msc", Category = "Advanced Tools" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameLocalUsersAndGroups"), Description = L.Get("Tools:DescriptionManageLocalUsersAndGroups"), Command = "lusrmgr.msc", Category = "System Management" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameUserCertificates"), Description = L.Get("Tools:DescriptionManageCurrentUserCertificates"), Command = "certmgr.msc", Category = "Advanced Tools" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameComputerCertificates"), Description = L.Get("Tools:DescriptionManageLocalComputerCertificates"), Command = "certlm.msc", Category = "Advanced Tools" },
            new SystemShortcut { Name = L.Get("Tools:ShortcutNameIndexingOptions"), Description = L.Get("Tools:DescriptionConfigureWindowsSearchIndexing"), Command = "control.exe", Arguments = "/name Microsoft.IndexingOptions", Category = "System Management" }
        }) SystemShortcuts.Add(shortcut);
    }

    private void LoadExternalTools()
    {
        ExternalTools.Clear();
        foreach (var tool in new[]
        {
            // System utilities
            new ExternalTool { Name = "PowerToys", Description = L.Get("Tools:DescriptionOfficialMicrosoftUtilitiesForWindowsPowerUsers"), Category = "System Utilities", WingetId = "Microsoft.PowerToys" },
            new ExternalTool { Name = "Flow Launcher", Description = L.Get("Tools:DescriptionOpenSourceQuickLauncherForAppsFiles"), Category = "System Utilities", DownloadUrl = "https://github.com/Flow-Launcher/Flow.Launcher" },
            new ExternalTool { Name = "FancyWM", Description = L.Get("Tools:DescriptionDynamicTilingWindowManagerForWindows"), Category = "System Utilities", DownloadUrl = "https://github.com/FancyWM/fancywm" },
            new ExternalTool { Name = "Autoruns", Description = L.Get("Tools:DescriptionInspectAndManageEveryWindowsStartupLocation"), Category = "System Utilities", WingetId = "Microsoft.Sysinternals.Autoruns" },
            new ExternalTool { Name = "Bulk Crap Uninstaller", Description = L.Get("Tools:DescriptionOpenSourceBulkApplicationUninstaller"), Category = "System Utilities", WingetId = "Klocman.BulkCrapUninstaller" },
            new ExternalTool { Name = "Cleanmgr+", Description = L.Get("Tools:DescriptionExtendedReplacementForTheClassicWindowsDisk"), Category = "System Utilities", DownloadUrl = "https://github.com/builtbybel/CleanmgrPlus" },
            new ExternalTool { Name = "Device Cleanup Tool", Description = L.Get("Tools:DescriptionRemoveRecordsForNonPresentDevicesOpens"), Category = "System Utilities", DownloadUrl = "https://www.majorgeeks.com/files/details/device_cleanup_tool.html" },
            new ExternalTool { Name = "DISM++", Description = L.Get("Tools:DescriptionGraphicalWindowsImageServicingAndCleanupUtility"), Category = "System Utilities", WingetId = "ChuyuTeam.DISM++" },
            new ExternalTool { Name = "Driver Store Explorer", Description = L.Get("Tools:DescriptionInspectAndRemovePackagesFromTheWindows"), Category = "System Utilities", WingetId = "lostindark.DriverStoreExplorer" },
            new ExternalTool { Name = "Snappy Driver Installer Origin", Description = L.Get("Tools:DescriptionOpenSourceOfflineDriverInstallerAndIndex"), Category = "System Utilities", WingetId = "GlennDelahoy.SnappyDriverInstallerOrigin" },
            new ExternalTool { Name = "UniGetUI", Description = L.Get("Tools:DescriptionGraphicalInterfaceForWinGetAndOtherPackage"), Category = "System Utilities", DownloadUrl = "https://github.com/marticliment/UniGetUI" },
            new ExternalTool { Name = "ViVeTool GUI", Description = L.Get("Tools:DescriptionGraphicalInterfaceForWindowsFeatureConfigurationIDs"), Category = "System Utilities", WingetId = "PeterStrick.ViVeTool-GUI" },
            new ExternalTool { Name = "Winhance", Description = L.Get("Tools:DescriptionConfigureAndCustomizeWindowsFromATransparent"), Category = "System Utilities", WingetId = "memstechtips.Winhance" },
            new ExternalTool { Name = "Wintoys", Description = L.Get("Tools:DescriptionWindowsMaintenanceAndCustomizationDashboardFromMicrosoft"), Category = "System Utilities", DownloadUrl = "https://apps.microsoft.com/detail/9P8LTPGCBZXD" },
            new ExternalTool { Name = "WinUtil", Description = L.Get("Tools:DescriptionChrisTitusTechWindowsUtilityOpensThe"), Category = "System Utilities", DownloadUrl = "https://github.com/ChrisTitusTech/winutil" },
            new ExternalTool { Name = "ZapTweaks", Description = L.Get("Tools:DescriptionConfigureAndCustomizeWindowsFromATransparent"), Category = "System Utilities", DownloadUrl = "https://github.com/PrimeBuild-pc/ZapTweaks" },
            new ExternalTool { Name = "ShareX", Description = L.Get("Tools:DescriptionCaptureAnnotateAndShareScreenshots"), Category = "System Utilities", WingetId = "ShareX.ShareX" },
            new ExternalTool { Name = "AME Wizard", Description = L.Get("Tools:DescriptionConfigureAndCustomizeWindowsFromATransparent"), Category = "System Utilities", DownloadUrl = "https://amelabs.net/" },
            new ExternalTool { Name = "Everything", Description = L.Get("Tools:DescriptionFastLocalFileAndFolderSearch"), Category = "System Utilities", WingetId = "voidtools.Everything" },
            new ExternalTool { Name = "WizTree", Description = L.Get("Tools:DescriptionFastVisualDiskSpaceAnalyzer"), Category = "System Utilities", WingetId = "AntibodySoftware.WizTree" },

            // CPU and memory
            new ExternalTool { Name = "CPU-Z", Description = L.Get("Tools:DescriptionCPUMemoryAndPlatformInformation"), Category = "CPU & Memory", WingetId = "CPUID.CPU-Z" },
            new ExternalTool { Name = "AMD Ryzen Master", Description = L.Get("Tools:DescriptionOfficialAMDRyzenMonitoringAndOverclockingUtility"), Category = "CPU & Memory", DownloadUrl = "https://www.amd.com/en/products/software/ryzen-master.html" },
            new ExternalTool { Name = "ZenTimings", Description = L.Get("Tools:DescriptionInspectAMDRyzenMemoryTimingsAndInfinity"), Category = "CPU & Memory", DownloadUrl = "https://zentimings.com/" },
            new ExternalTool { Name = "ThrottleStop", Description = L.Get("Tools:DescriptionMonitorAndTuneIntelCPUVoltageAndThrottling"), Category = "CPU & Memory", DownloadUrl = "https://www.techpowerup.com/download/techpowerup-throttlestop/" },
            new ExternalTool { Name = "DRAM Calculator for Ryzen", Description = L.Get("Tools:DescriptionInspectAMDRyzenMemoryTimingsAndInfinity"), Category = "CPU & Memory", DownloadUrl = "https://www.techpowerup.com/download/ryzen-dram-calculator/" },
            new ExternalTool { Name = "AMD Chipset Drivers", Description = L.Get("Tools:DescriptionOpenOfficialHardwareDriverDownloadPage"), Category = "CPU & Memory", DownloadUrl = "https://www.amd.com/en/support/download/drivers.html" },
            new ExternalTool { Name = "CPU Set Setter", Description = L.Get("Tools:DescriptionManageWindowsCPUSets"), Category = "CPU & Memory", DownloadUrl = "https://github.com/SimonvBez/CPUSetSetter" },
            new ExternalTool { Name = "ThreadPilot", Description = L.Get("Tools:DescriptionInspectAndManageCPUThreadAffinity"), Category = "CPU & Memory", DownloadUrl = "https://github.com/PrimeBuild-pc/ThreadPilot" },
            new ExternalTool { Name = "Process Lasso", Description = L.Get("Tools:DescriptionProcessPriorityAffinityAndResponsivenessControls"), Category = "CPU & Memory", WingetId = "BitSum.ProcessLasso" },
            new ExternalTool { Name = "ParkControl", Description = L.Get("Tools:DescriptionInspectAndConfigureCPUCoreParkingAnd"), Category = "CPU & Memory", WingetId = "BitSum.ParkControl" },
            new ExternalTool { Name = "CoreCycler", Description = L.Get("Tools:DescriptionCycleStressTestsAcrossIndividualCPUCores"), Category = "CPU & Memory", DownloadUrl = "https://github.com/sp00n/corecycler" },
            new ExternalTool { Name = "Ryzen SMU Debug Tool", Description = L.Get("Tools:DescriptionAdvancedAMDSMUMSRAndPowerTable"), Category = "CPU & Memory", DownloadUrl = "https://github.com/irusanov/SMUDebugTool" },
            new ExternalTool { Name = "Intel Application Optimization", Description = L.Get("Tools:DescriptionOfficialIntelAPOInterfaceForSupportedProcessors"), Category = "CPU & Memory", DownloadUrl = "https://www.intel.com/content/www/us/en/download/870620/intel-application-optimization-user-interface.html" },
            new ExternalTool { Name = "KGuiX", Description = L.Get("Tools:DescriptionAdvancedGraphicalLauncherForKarhuRAMTest"), Category = "CPU & Memory", DownloadUrl = "https://github.com/jjgraphix/KGuiX" },
            new ExternalTool { Name = "TestMem5", Description = L.Get("Tools:DescriptionCommunityMemoryStabilityTestDownloadsTheUser"), Category = "CPU & Memory", DownloadUrl = "https://github.com/CoolCmd/TestMem5/releases/download/v0.13.1/TestMem5.7z" },
            new ExternalTool { Name = "MemTest86", Description = L.Get("Tools:DescriptionBootableMemoryDiagnostics"), Category = "CPU & Memory", DownloadUrl = "https://www.memtest86.com/" },

            // Monitoring and diagnostics
            new ExternalTool { Name = "HWiNFO", Description = L.Get("Tools:DescriptionDetailedHardwareInformationAndSensorMonitoring"), Category = "Monitoring & Diagnostics", DownloadUrl = "https://www.hwinfo.com/download/" },
            new ExternalTool { Name = "HWMonitor", Description = L.Get("Tools:DescriptionCPUIDHardwareTemperatureVoltageAndFanMonitoring"), Category = "Monitoring & Diagnostics", WingetId = "CPUID.HWMonitor" },
            new ExternalTool { Name = "RAMMap", Description = L.Get("Tools:DescriptionMicrosoftSysinternalsPhysicalMemoryAnalyzer"), Category = "Monitoring & Diagnostics", WingetId = "Microsoft.Sysinternals.RAMMap" },
            new ExternalTool { Name = "LatencyMon", Description = L.Get("Tools:DescriptionAnalyzeRealTimeAudioAndDPCLatency"), Category = "Monitoring & Diagnostics", WingetId = "Resplendence.LatencyMon" },
            new ExternalTool { Name = "Fan Control", Description = L.Get("Tools:DescriptionOpenSourceFanCurveAndSensorControl"), Category = "Monitoring & Diagnostics", WingetId = "Rem0o.FanControl" },
            new ExternalTool { Name = "PresentMon", Description = L.Get("Tools:DescriptionIntelFramePresentationAndPerformanceMonitoring"), Category = "Monitoring & Diagnostics", WingetId = "Intel.PresentMon" },
            new ExternalTool { Name = "GPUView", Description = L.Get("Tools:DescriptionMicrosoftGPUAndCPUPerformanceTraceVisualization"), Category = "Monitoring & Diagnostics", DownloadUrl = "https://learn.microsoft.com/windows-hardware/drivers/display/using-gpuview" },
            new ExternalTool { Name = "PeStudio", Description = L.Get("Tools:DescriptionStaticInspectionOfWindowsExecutablesWithoutRunning"), Category = "Monitoring & Diagnostics", DownloadUrl = "https://www.winitor.com/download" },
            new ExternalTool { Name = "Process Explorer", Description = L.Get("Tools:DescriptionSysinternalsProcessHandleAndDLLDiagnostics"), Category = "Monitoring & Diagnostics", WingetId = "Microsoft.Sysinternals.ProcessExplorer" },
            new ExternalTool { Name = "Process Monitor", Description = L.Get("Tools:DescriptionRealTimeFilesystemRegistryAndProcessTracing"), Category = "Monitoring & Diagnostics", WingetId = "Microsoft.Sysinternals.ProcessMonitor" },
            new ExternalTool { Name = "Windows Performance Toolkit (WPR, WPA & Xperf)", Description = L.Get("Tools:DescriptionAdvancedETWRecordingAndAnalysisWithWPR"), Category = "Monitoring & Diagnostics", DownloadUrl = "https://learn.microsoft.com/windows-hardware/get-started/adk-install" },
            new ExternalTool { Name = "NVIDIA FrameView", Description = L.Get("Tools:DescriptionFrameRateFrameTimePowerAndPerformance"), Category = "Monitoring & Diagnostics", WingetId = "Nvidia.FrameView" },
            new ExternalTool { Name = "GPU Shark 2", Description = L.Get("Tools:DescriptionLightweightDetailedGPUMonitoringFromGeeks3D"), Category = "Monitoring & Diagnostics", DownloadUrl = "https://www.geeks3d.com/gpushark/" },
            new ExternalTool { Name = "NVIDIA Nsight Systems", Description = L.Get("Tools:DescriptionAdvancedCPUAndGPUWorkloadProfilingAnd"), Category = "Monitoring & Diagnostics", DownloadUrl = "https://developer.nvidia.com/nsight-systems" },
            new ExternalTool { Name = "AMD Radeon GPU Profiler", Description = L.Get("Tools:DescriptionLowLevelRadeonGPUPerformanceAndWorkload"), Category = "Monitoring & Diagnostics", DownloadUrl = "https://gpuopen.com/rgp/" },

            // GPU and display
            new ExternalTool { Name = "GPU-Z", Description = L.Get("Tools:DescriptionGPUInformationSensorsAndValidation"), Category = "GPU & Display", WingetId = "TechPowerUp.GPU-Z" },
            new ExternalTool { Name = "MSI Afterburner", Description = L.Get("Tools:DescriptionGPUMonitoringFanCurvesAndOverclockingControls"), Category = "GPU & Display", WingetId = "Guru3D.Afterburner" },
            new ExternalTool { Name = "RivaTuner Statistics Server", Description = L.Get("Tools:DescriptionFrameRateLimitingAndInGamePerformance"), Category = "GPU & Display", WingetId = "Guru3D.RTSS" },
            new ExternalTool { Name = "MoreClockTool 2", Description = L.Get("Tools:DescriptionPaidAMDGPUTuningUtilityFromMicrosoft"), Category = "GPU & Display", DownloadUrl = "https://apps.microsoft.com/detail/9N08X8C1QDQP" },
            new ExternalTool { Name = "MoreClockTool (Free)", Description = L.Get("Tools:DescriptionLegacyFreeAMDGPUTuningUtilityFrom"), Category = "GPU & Display", DownloadUrl = "https://www.igorslab.de/en/download-area-new-version-of-morepowertool-mpt-and-final-release-of-redbioseditor-rbe/" },
            new ExternalTool { Name = "MorePowerTool", Description = L.Get("Tools:DescriptionAdvancedAMDRadeonPowerLimitEditor"), Category = "GPU & Display", DownloadUrl = "https://www.igorslab.de/en/morepowertool-mpt-beta-program-new-features-the-community-tests/" },
            new ExternalTool { Name = "RadeonTuner", Description = L.Get("Tools:DescriptionLightweightOpenSourceAMDRadeonTuningInterface"), Category = "GPU & Display", DownloadUrl = "https://github.com/dumbie/RadeonTuner" },
            new ExternalTool { Name = "NVIDIA Drivers", Description = L.Get("Tools:DescriptionOpenOfficialHardwareDriverDownloadPage"), Category = "GPU & Display", DownloadUrl = "https://www.nvidia.com/en-us/drivers/" },
            new ExternalTool { Name = "NVIDIA App", Description = L.Get("Tools:DescriptionOfficialNVIDIAAppForDriversOptimizationAndOverlay"), Category = "GPU & Display", DownloadUrl = "https://www.nvidia.com/en-us/software/nvidia-app/" },
            new ExternalTool { Name = "AMD Graphics Drivers", Description = L.Get("Tools:DescriptionOpenOfficialHardwareDriverDownloadPage"), Category = "GPU & Display", DownloadUrl = "https://www.amd.com/en/support/download/drivers.html" },
            new ExternalTool { Name = "AMD Cleanup Utility", Description = L.Get("Tools:DescriptionCompletelyRemoveGraphicsDriversBeforeReinstalling"), Category = "GPU & Display", DownloadUrl = "https://drivers.amd.com/drivers/amdcleanuputility.exe" },
            new ExternalTool { Name = "NVIDIA Profile Inspector", Description = L.Get("Tools:DescriptionInspectAdvancedNVIDIADriverProfiles"), Category = "GPU & Display", DownloadUrl = "https://github.com/Orbmu2k/nvidiaProfileInspector" },
            new ExternalTool { Name = "NVCleanstall", Description = L.Get("Tools:DescriptionCustomizeNVIDIADriverInstallations"), Category = "GPU & Display", WingetId = "TechPowerUp.NVCleanstall" },
            new ExternalTool { Name = "Display Driver Uninstaller", Description = L.Get("Tools:DescriptionCompletelyRemoveGraphicsDriversBeforeReinstalling"), Category = "GPU & Display", WingetId = "Wagnardsoft.DisplayDriverUninstaller" },
            new ExternalTool { Name = "NVFlash", Description = L.Get("Tools:DescriptionAdvancedNVIDIAGraphicsCardFirmwareUtility"), Category = "GPU & Display", DownloadUrl = "https://www.techpowerup.com/download/nvidia-nvflash/" },
            new ExternalTool { Name = "AMD VBFlash", Description = L.Get("Tools:DescriptionAdvancedAMDGraphicsCardFirmwareUtilityFormerly"), Category = "GPU & Display", DownloadUrl = "https://www.techpowerup.com/download/ati-atiflash/" },
            new ExternalTool { Name = "MPO GPU Fix", Description = L.Get("Tools:DescriptionCommunityTroubleshootingUtilityForMultiplaneOverlayIssues"), Category = "GPU & Display", DownloadUrl = "https://github.com/RedDot-3ND7355/MPO-GPU-FIX" },
            new ExternalTool { Name = "Custom Resolution Utility", Description = L.Get("Tools:DescriptionInspectAndEditDisplayResolutionsAndEDID"), Category = "GPU & Display", DownloadUrl = "https://www.monitortests.com/forum/Thread-Custom-Resolution-Utility-CRU" },
            new ExternalTool { Name = "VibranceGUI", Description = L.Get("Tools:DescriptionAutomateNVIDIADigitalVibranceAndAMDSaturation"), Category = "GPU & Display", DownloadUrl = "https://vibrancegui.com/" },
            new ExternalTool { Name = "OpenRGB", Description = L.Get("Tools:DescriptionOpenSourceRGBDeviceControl"), Category = "GPU & Display", WingetId = "OpenRGB.OpenRGB" },
            new ExternalTool { Name = "SignalRGB", Description = L.Get("Tools:DescriptionUnifiedRGBLightingAndDeviceEffects"), Category = "GPU & Display", WingetId = "WhirlwindFX.SignalRgb" },

            // Firmware and power
            new ExternalTool { Name = "SCEWIN GUI Better", Description = L.Get("Tools:DescriptionUnofficialGUIForAdvancedAMIFirmwareVariable"), Category = "Firmware & Power", DownloadUrl = "https://github.com/loko8002/SCEWIN-GUI-BETTER1" },
            new ExternalTool { Name = "SCEHUB", Description = L.Get("Tools:DescriptionCommunitySCEWINBinariesAndTroubleshootingFirmwareChanges"), Category = "Firmware & Power", DownloadUrl = "https://github.com/ab3lkaizen/SCEHUB" },
            new ExternalTool { Name = "Power Settings Explorer", Description = L.Get("Tools:DescriptionInspectHiddenWindowsPowerPlanSettingsOpens"), Category = "Firmware & Power", DownloadUrl = "https://www.mediafire.com/file/wt37sbsejk7iepm/PowerSettingsExplorer.zip/file" },
            new ExternalTool { Name = "UEFITool", Description = L.Get("Tools:DescriptionUEFIFirmwareImageViewerAndEditor"), Category = "Firmware & Power", DownloadUrl = "https://github.com/LongSoft/UEFITool" },

            // Gaming and input
            new ExternalTool { Name = "DLSS Swapper", Description = L.Get("Tools:DescriptionManageDLSSVersionsInSupportedGames"), Category = "Gaming & Input", DownloadUrl = "https://github.com/beeradmoore/dlss-swapper" },
            new ExternalTool { Name = "OptiScaler", Description = L.Get("Tools:DescriptionUpscalerInjectionAndCompatibilityLayerForSupported"), Category = "Gaming & Input", DownloadUrl = "https://github.com/optiscaler/OptiScaler" },
            new ExternalTool { Name = "OptiScaler Client", Description = L.Get("Tools:DescriptionUpscalerInjectionAndCompatibilityLayerForSupported"), Category = "Gaming & Input", DownloadUrl = "https://github.com/Agustinm28/Optiscaler-Client" },
            new ExternalTool { Name = "DLSS Enabler", Description = L.Get("Tools:DescriptionCommunityDirectX12UpscalerAndFrameGeneration"), Category = "Gaming & Input", DownloadUrl = "https://github.com/artur-graniszewski/DLSS-Enabler" },
            new ExternalTool { Name = "Special K", Description = L.Get("Tools:DescriptionFrameRateLimitingAndInGamePerformance"), Category = "Gaming & Input", WingetId = "SpecialK.SpecialK" },
            new ExternalTool { Name = "Upscale It", Description = L.Get("Tools:DescriptionUpscalerInjectionAndCompatibilityLayerForSupported"), Category = "Gaming & Input", DownloadUrl = "https://github.com/NODIX-TECH/UPSCALE-IT" },
            new ExternalTool { Name = "Visual C++ Redistributable Runtimes All-in-One", Description = L.Get("Tools:DescriptionInstallLegacyAndCurrentVisualCRuntimes"), Category = "Gaming & Input", DownloadUrl = "https://www.techpowerup.com/download/visual-c-redistributable-runtime-package-all-in-one/" },
            new ExternalTool { Name = "LightCrosshair", Description = L.Get("Tools:DescriptionLightweightOpenSourceConfigurableCrosshairOverlay"), Category = "Gaming & Input", WingetId = "PrimeBuild.LightCrosshair" },
            new ExternalTool { Name = "Raw Accel", Description = L.Get("Tools:DescriptionOpenSourceMouseAccelerationDriver"), Category = "Gaming & Input", DownloadUrl = "https://github.com/RawAccelOfficial/rawaccel" },
            new ExternalTool { Name = "HIDUSBF", Description = L.Get("Tools:DescriptionAdvancedUSBHIDPollingRateFilterDriver"), Category = "Gaming & Input", DownloadUrl = "https://github.com/LordOfMice/hidusbf" },
            new ExternalTool { Name = "DualShock Calibration", Description = L.Get("Tools:DescriptionBrowserBasedCalibrationForSupportedPlayStationControllers"), Category = "Gaming & Input", DownloadUrl = "https://dualshock-tools.github.io/" },
            new ExternalTool { Name = "Razer Polling Rate Tester", Description = L.Get("Tools:DescriptionOfficialBrowserBasedMousePollingRateTester"), Category = "Gaming & Input", DownloadUrl = "https://rzr.to/pollingrate" },

            // Storage and USB
            new ExternalTool { Name = "CrystalDiskMark", Description = L.Get("Tools:DescriptionStoragePerformanceBenchmark"), Category = "Storage & USB", WingetId = "CrystalDewWorld.CrystalDiskMark" },
            new ExternalTool { Name = "CrystalDiskInfo", Description = L.Get("Tools:DescriptionStorageHealthAndSMARTInformation"), Category = "Storage & USB", WingetId = "CrystalDewWorld.CrystalDiskInfo" },
            new ExternalTool { Name = "USBDeview", Description = L.Get("Tools:DescriptionNirSoftInventoryAndManagementOfCurrentAnd"), Category = "Storage & USB", WingetId = "NirSoft.USBDeview" },
            new ExternalTool { Name = "USB Latency Analyzer", Description = L.Get("Tools:DescriptionMariusHeierUSBLatencyToolsOpensThe"), Category = "Storage & USB", DownloadUrl = "https://tools.mariusheier.com/" },
            new ExternalTool { Name = "DiskSpd", Description = L.Get("Tools:DescriptionMicrosoftCommandLineStorageLoadGeneratorAnd"), Category = "Storage & USB", WingetId = "Microsoft.DiskSpd" },
            new ExternalTool { Name = "CompactGUI", Description = L.Get("Tools:DescriptionCompressGamesAndProgramsWithNativeWindowsAPIs"), Category = "Storage & USB", WingetId = "IridiumIO.CompactGUI" },
            new ExternalTool { Name = "Rufus", Description = L.Get("Tools:DescriptionCreateBootableUSBDrives"), Category = "Storage & USB", WingetId = "Rufus.Rufus" },
            new ExternalTool { Name = "Ventoy", Description = L.Get("Tools:DescriptionBootMultipleISOImagesFromOneUSBDrive"), Category = "Storage & USB", WingetId = "Ventoy.Ventoy" },

            // Network
            new ExternalTool { Name = "qBittorrent", Description = L.Get("Tools:DescriptionOpenSourceBitTorrentClient"), Category = "Network", WingetId = "qBittorrent.qBittorrent" },
            new ExternalTool { Name = "TCP Optimizer", Description = L.Get("Tools:DescriptionInspectAndTuneWindowsTCPIPParameters"), Category = "Network", DownloadUrl = "https://www.speedguide.net/downloads.php" },
            new ExternalTool { Name = "TCPView", Description = L.Get("Tools:DescriptionMicrosoftSysinternalsLiveTCPAndUDPEndpoint"), Category = "Network", WingetId = "Microsoft.Sysinternals.TCPView" },
            new ExternalTool { Name = "DNS Jumper", Description = L.Get("Tools:DescriptionTestAndSwitchDNSResolvers"), Category = "Network", WingetId = "sordum.DnsJumper" },
            new ExternalTool { Name = "iPerf3", Description = L.Get("Tools:DescriptionMeasureMaximumTCPAndUDPNetworkThroughput"), Category = "Network", WingetId = "ar51an.iPerf3" },
            new ExternalTool { Name = "Wireshark", Description = L.Get("Tools:DescriptionCaptureAndInspectNetworkTrafficAndProtocols"), Category = "Network", WingetId = "WiresharkFoundation.Wireshark" },
            new ExternalTool { Name = "Nmap", Description = L.Get("Tools:DescriptionNetworkDiscoveryAndSecurityAuditing"), Category = "Network", DownloadUrl = "https://nmap.org/download.html" },
            new ExternalTool { Name = "WinSCP", Description = L.Get("Tools:DescriptionSecureFileTransferAndRemoteFileManagement"), Category = "Network", WingetId = "WinSCP.WinSCP" },

            // Audio
            new ExternalTool { Name = "FxSound", Description = L.Get("Tools:DescriptionAudioEqualizerAndEnhancement"), Category = "Audio", WingetId = "FxSound.FxSound" },
            new ExternalTool { Name = "Equalizer APO", Description = L.Get("Tools:DescriptionSystemWideParametricAudioEqualizer"), Category = "Audio", DownloadUrl = "https://sourceforge.net/projects/equalizerapo/" },
            new ExternalTool { Name = "Peace Equalizer", Description = L.Get("Tools:DescriptionGraphicalInterfaceForEqualizerAPO"), Category = "Audio", DownloadUrl = "https://sourceforge.net/projects/peace-equalizer-apo-extension/" },
            new ExternalTool { Name = "SteelSeries GG", Description = L.Get("Tools:DescriptionSteelSeriesDeviceControlSonarAudioAndGame"), Category = "Audio", WingetId = "SteelSeries.GG" },

            // Benchmarks and stability
            new ExternalTool { Name = "AIDA64 Extreme", Description = L.Get("Tools:DescriptionHardwareDiagnosticsMonitoringAndSystemBenchmarks"), Category = "Benchmarks & Stability", WingetId = "FinalWire.AIDA64.Extreme" },
            new ExternalTool { Name = "CapFrameX", Description = L.Get("Tools:DescriptionFrameTimeCaptureAndPerformanceAnalysis"), Category = "Benchmarks & Stability", WingetId = "CXWorld.CapFrameX" },
            new ExternalTool { Name = "OCCT", Description = L.Get("Tools:DescriptionCPUGPUMemoryAndPowerStabilityTesting"), Category = "Benchmarks & Stability", WingetId = "OCBase.OCCT.Personal" },
            new ExternalTool { Name = "Prime95", Description = L.Get("Tools:DescriptionCPUAndMemoryStressTesting"), Category = "Benchmarks & Stability", WingetId = "mersenne.prime95" },
            new ExternalTool { Name = "Heaven Benchmark", Description = L.Get("Tools:DescriptionUnigineGPUBenchmarkAndStabilityTest"), Category = "Benchmarks & Stability", WingetId = "Unigine.HeavenBenchmark" },
            new ExternalTool { Name = "BenchMate", Description = L.Get("Tools:DescriptionBenchmarkLauncherValidationAndResultManagement"), Category = "Benchmarks & Stability", WingetId = "MatthiasZronek.BenchMate" },
            new ExternalTool { Name = "Cinebench 2024", Description = L.Get("Tools:DescriptionCurrentOfficialMaxonCPUAndGPURendering"), Category = "Benchmarks & Stability", DownloadUrl = "https://www.maxon.net/en/downloads/cinebench-downloads" },
            new ExternalTool { Name = "Cinebench R23", Description = L.Get("Tools:DescriptionLegacyMaxonCPURenderingBenchmark"), Category = "Benchmarks & Stability", WingetId = "Maxon.CinebenchR23" },
            new ExternalTool { Name = "Cinebench Legacy Downloads", Description = L.Get("Tools:DescriptionTechPowerUpArchiveForOlderCinebenchReleasesThe"), Category = "Benchmarks & Stability", DownloadUrl = "https://www.techpowerup.com/download/maxon-cinebench/" },
            new ExternalTool { Name = "Linpack Xtreme", Description = L.Get("Tools:DescriptionHighLoadCPUAndMemoryStabilityTest"), Category = "Benchmarks & Stability", DownloadUrl = "https://www.techpowerup.com/download/linpack-xtreme/" },
            new ExternalTool { Name = "FurMark 2", Description = L.Get("Tools:DescriptionGPUStressTestAndOpenGLVulkanBenchmark"), Category = "Benchmarks & Stability", WingetId = "Geeks3D.FurMark.2" },
            new ExternalTool { Name = "MSI Kombustor", Description = L.Get("Tools:DescriptionMSIGPUStressTestAndOpenGLVulkan"), Category = "Benchmarks & Stability", DownloadUrl = "https://msikombustor.com/#download" },
            new ExternalTool { Name = "y-cruncher", Description = L.Get("Tools:DescriptionHeavyCPURAMAndMemoryControllerBenchmark"), Category = "Benchmarks & Stability", DownloadUrl = "https://www.numberworld.org/y-cruncher/" },
            new ExternalTool { Name = "memtest_vulkan", Description = L.Get("Tools:DescriptionVulkanBasedGPUMemoryStabilityAndError"), Category = "Benchmarks & Stability", DownloadUrl = "https://github.com/GpuZelenograd/memtest_vulkan" },
            new ExternalTool { Name = "Superposition Benchmark", Description = L.Get("Tools:DescriptionModernUnigineGPUBenchmarkAndStabilityTest"), Category = "Benchmarks & Stability", WingetId = "Unigine.SuperpositionBenchmark" },
            new ExternalTool { Name = "3DMark", Description = L.Get("Tools:DescriptionIndustryStandardGamingCPUAndGPUBenchmark"), Category = "Benchmarks & Stability", DownloadUrl = "https://benchmarks.ul.com/3dmark" },
            new ExternalTool { Name = "Blender Benchmark", Description = L.Get("Tools:DescriptionRealWorldCPUAndGPURenderingBenchmark"), Category = "Benchmarks & Stability", DownloadUrl = "https://opendata.blender.org/" },
            new ExternalTool { Name = "Geekbench 6", Description = L.Get("Tools:DescriptionCrossPlatformCPUAndGPUComputeBenchmark"), Category = "Benchmarks & Stability", WingetId = "PrimateLabs.Geekbench.6" },

            // AI tools
            new ExternalTool { Name = "Google Antigravity", Description = L.Get("Tools:DescriptionGoogleSAgenticDevelopmentEnvironment"), Category = "AI Tools", DownloadUrl = "https://antigravity.google/" },
            new ExternalTool { Name = "Cursor", Description = L.Get("Tools:DescriptionAIFirstCodeEditor"), Category = "AI Tools", WingetId = "Anysphere.Cursor" },
            new ExternalTool { Name = "Ollama", Description = L.Get("Tools:DescriptionRunAndManageLocalLanguageModels"), Category = "AI Tools", WingetId = "Ollama.Ollama" },
            new ExternalTool { Name = "OpenCode", Description = L.Get("Tools:DescriptionOpenSourceAICodingAgentForThe"), Category = "AI Tools", DownloadUrl = "https://opencode.ai/" },
            new ExternalTool { Name = "Pi Coding Agent", Description = L.Get("Tools:DescriptionMinimalExtensibleTerminalCodingAgentFramework"), Category = "AI Tools", DownloadUrl = "https://github.com/badlogic/pi-mono" },
            new ExternalTool { Name = "Warp", Description = L.Get("Tools:DescriptionAgenticTerminalAndDevelopmentEnvironment"), Category = "AI Tools", WingetId = "Warp.Warp" },
            new ExternalTool { Name = "Unsloth Studio", Description = L.Get("Tools:DescriptionLocalInterfaceForTrainingAndRunningOpen"), Category = "AI Tools", DownloadUrl = "https://unsloth.ai/docs/new/studio" },
            new ExternalTool { Name = "LM Studio", Description = L.Get("Tools:DescriptionDiscoverDownloadAndRunLocalLanguageModels"), Category = "AI Tools", WingetId = "ElementLabs.LMStudio" },
            new ExternalTool { Name = "Jan", Description = L.Get("Tools:DescriptionOpenSourceLocalAIDesktopApplication"), Category = "AI Tools", WingetId = "Jan.Jan" },

            // Development and DevOps
            new ExternalTool { Name = "Arduino IDE", Description = L.Get("Tools:DescriptionOfficialOpenSourceArduinoDevelopmentEnvironment"), Category = "Development & DevOps", WingetId = "ArduinoSA.IDE.stable" },
            new ExternalTool { Name = "Docker Desktop", Description = L.Get("Tools:DescriptionContainerDevelopmentWithDockerAndWSL2"), Category = "Development & DevOps", WingetId = "Docker.DockerDesktop" },
            new ExternalTool { Name = "Podman Desktop", Description = L.Get("Tools:DescriptionOpenSourceContainerAndKubernetesDesktop"), Category = "Development & DevOps", WingetId = "RedHat.Podman-Desktop" },

            // Recovery and forensics
            new ExternalTool { Name = "Windows File Recovery", Description = L.Get("Tools:DescriptionOfficialMicrosoftCommandLineFileRecovery"), Category = "Recovery & Forensics", WingetId = "9N26S50LN705" },
            new ExternalTool { Name = "TestDisk & PhotoRec", Description = L.Get("Tools:DescriptionRecoverPartitionsAndFilesFromDamagedMedia"), Category = "Recovery & Forensics", WingetId = "CGSecurity.TestDisk" },
            new ExternalTool { Name = "Autopsy", Description = L.Get("Tools:DescriptionOpenSourceDigitalForensicsPlatform"), Category = "Recovery & Forensics", DownloadUrl = "https://www.autopsy.com/download/" },

            // OSINT and security
            new ExternalTool { Name = "ExifTool", Description = L.Get("Tools:DescriptionInspectAndEditFileMetadata"), Category = "OSINT & Security", WingetId = "OliverBetz.ExifTool" },
            new ExternalTool { Name = "SpiderFoot", Description = L.Get("Tools:DescriptionAutomatedOSINTAndAttackSurfaceMapping"), Category = "OSINT & Security", DownloadUrl = "https://github.com/smicallef/spiderfoot" },
            new ExternalTool { Name = "Sherlock", Description = L.Get("Tools:DescriptionFindUsernamesAcrossSocialNetworks"), Category = "OSINT & Security", DownloadUrl = "https://github.com/sherlock-project/sherlock" },
            new ExternalTool { Name = "OWASP Amass", Description = L.Get("Tools:DescriptionAttackSurfaceMappingAndAssetDiscovery"), Category = "OSINT & Security", DownloadUrl = "https://github.com/owasp-amass/amass" }
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

    private static readonly IReadOnlyDictionary<string, (string Resource, string Icon, int Order)> Categories =
        new Dictionary<string, (string, string, int)>(StringComparer.OrdinalIgnoreCase)
        {
            ["System Utilities"] = ("Tools:CategorySystemUtilities", "\uE713", 1),
            ["CPU & Memory"] = ("Tools:CategoryCPUMemory", "\uE950", 2),
            ["Firmware & Power"] = ("Tools:CategoryFirmwarePower", "\uE945", 3),
            ["Monitoring & Diagnostics"] = ("Tools:CategoryMonitoringDiagnostics", "\uE9D9", 4),
            ["GPU & Display"] = ("Tools:CategoryGPUDisplay", "\uE7F4", 5),
            ["Gaming & Input"] = ("Tools:CategoryGamingInput", "\uE7FC", 6),
            ["Storage & USB"] = ("Tools:CategoryStorageUSB", "\uEDA2", 7),
            ["System Management"] = ("Tools:CategorySystemManagement", "\uE713", 8),
            ["Performance"] = ("Tools:CategoryPerformance", "\uE9D9", 9),
            ["System Information"] = ("Tools:CategorySystemInformation", "\uE946", 10),
            ["Network"] = ("Tools:CategoryNetwork", "\uE968", 11),
            ["Audio"] = ("Tools:CategoryAudio", "\uE767", 12),
            ["Display"] = ("Tools:CategoryDisplay", "\uE7F4", 13),
            ["Power Management"] = ("Tools:CategoryPowerManagement", "\uE945", 14),
            ["Maintenance"] = ("Tools:CategoryMaintenance", "\uE74D", 15),
            ["Advanced Tools"] = ("Tools:CategoryAdvancedTools", "\uE90F", 16),
            ["Benchmarks & Stability"] = ("Tools:CategoryBenchmarksStability", "\uE9D2", 17),
            ["AI Tools"] = ("Tools:CategoryAITools", "\uE99A", 18),
            ["Development & DevOps"] = ("Tools:CategoryDevelopmentDevOps", "\uE943", 19),
            ["Recovery & Forensics"] = ("Tools:CategoryRecoveryForensics", "\uE8C8", 20),
            ["OSINT & Security"] = ("Tools:CategoryOSINTSecurity", "\uE72E", 21),
            ["Custom"] = ("Tools:CategoryCustom", "\uE90F", 22)
        };

    public static string LocalizeCategory(string category) =>
        Categories.TryGetValue(category, out var metadata) ? L.Get(metadata.Resource) : category;

    public static string CategoryKey(string display) => Categories.Keys.FirstOrDefault(category =>
        LocalizeCategory(category).Equals(display, StringComparison.CurrentCultureIgnoreCase)) ?? display;

    public static string CategoryIcon(string category) =>
        Categories.TryGetValue(category, out var metadata) ? metadata.Icon : "\uE8F1";

    public static int CategoryOrder(string category) =>
        Categories.TryGetValue(category, out var metadata) ? metadata.Order : int.MaxValue;

    public IEnumerable<string> GetToolCategories() => ExternalTools.Select(tool => tool.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name);

    public void SaveCustomTool(ExternalTool tool)
    {
        UserDataService.ValidateCustomTool(tool);
        if (ExternalTools.Any(existing => existing.Id != tool.Id && existing.Name.Equals(tool.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException(L.Get("Tools:DuplicateToolName"));
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
