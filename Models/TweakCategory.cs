using System.Collections.ObjectModel;

namespace TweakHub.Models
{
    public enum TweakType
    {
        Registry,
        Power
    }

    public class TweakCategory
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public ObservableCollection<PerformanceTweak> Tweaks { get; set; } = new();
    }

    public class PerformanceTweak : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        private bool _isEnabled;
        private bool _isPreviewVisible;
        private string _previewContent = string.Empty;

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TweakType Type { get; set; }
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }
        public bool RequiresRestart { get; set; }
        public string RegistryPath { get; set; } = string.Empty;
        public string RegistryKey { get; set; } = string.Empty;
        public object? EnabledValue { get; set; }
        public object? DisabledValue { get; set; }
        public string? ServiceName { get; set; }
        public string? ScriptPath { get; set; }
        public string Category { get; set; } = string.Empty;
        public int RiskLevel { get; set; } = 1; // 1-5 scale
        public bool IsAvailable { get; set; } = true;

        public bool IsPreviewVisible
        {
            get => _isPreviewVisible;
            set { _isPreviewVisible = value; OnPropertyChanged(nameof(IsPreviewVisible)); }
        }

        public string PreviewContent
        {
            get => _previewContent;
            set { _previewContent = value; OnPropertyChanged(nameof(PreviewContent)); }
        }
    }

    public class ExternalTool
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        // For website/GitHub links
        public string DownloadUrl { get; set; } = string.Empty;
        // For Winget installs (single package)
        public string WingetId { get; set; } = string.Empty;
        // For custom Winget command (e.g., multi-package installs)
        public string WingetCommand { get; set; } = string.Empty;
        // Optional post-install info to show (e.g., Autoruns message)
        public string PostInstallMessage { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        // Favorites
        public bool IsFavorite { get; set; }
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

    public enum ScriptLanguage
    {
        PowerShell,
        Cmd
    }

    public class CustomScript
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public ScriptLanguage Language { get; set; } = ScriptLanguage.PowerShell;
        public string Content { get; set; } = string.Empty;
    }

    public class CustomRegistryTweak
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RegistryPath { get; set; } = string.Empty;
        public string RegistryKey { get; set; } = string.Empty;
        public string ValueType { get; set; } = "REG_SZ"; // REG_DWORD, REG_QWORD, REG_SZ
        public string Data { get; set; } = string.Empty;
        public int RiskLevel { get; set; } = 1;
    }
}
