using System.Collections.ObjectModel;

namespace TweakHub.Models
{
    public enum TweakType
    {
        Registry,
        Service,
        Script,
        FileSystem
    }

    public class TweakCategory
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public ObservableCollection<PerformanceTweak> Tweaks { get; set; } = new();
    }

    public class PerformanceTweak
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TweakType Type { get; set; }
        public bool IsEnabled { get; set; }
        public bool RequiresRestart { get; set; }
        public string RegistryPath { get; set; } = string.Empty;
        public string RegistryKey { get; set; } = string.Empty;
        public object? EnabledValue { get; set; }
        public object? DisabledValue { get; set; }
        public string? ServiceName { get; set; }
        public string? ScriptPath { get; set; }
        public string Category { get; set; } = string.Empty;
        public int RiskLevel { get; set; } = 1; // 1-5 scale
    }

    public class ExternalTool
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        // For website/GitHub links
        public string DownloadUrl { get; set; } = string.Empty;
        public string DirectDownloadUrl { get; set; } = string.Empty;
        // For Winget installs (single package)
        public string WingetId { get; set; } = string.Empty;
        // For custom Winget command (e.g., multi-package installs)
        public string WingetCommand { get; set; } = string.Empty;
        // For running a PowerShell command/script directly (admin when required)
        public string PowerShellCommand { get; set; } = string.Empty;
        public bool RequiresAdmin { get; set; } = false;
        // Optional post-install info to show (e.g., Autoruns message)
        public string PostInstallMessage { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsDirectDownload { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    public class SystemShortcut
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class SystemMetric
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double NumericValue { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
