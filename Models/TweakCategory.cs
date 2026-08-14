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
        private bool _isFavorite;
        private bool _isPreviewVisible;
        private bool _isRestartPending;
        private bool _isPartiallyApplied;
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
        public bool IsFavorite
        {
            get => _isFavorite;
            set { _isFavorite = value; OnPropertyChanged(nameof(IsFavorite)); }
        }
        public bool IsPartiallyApplied
        {
            get => _isPartiallyApplied;
            set { _isPartiallyApplied = value; OnPropertyChanged(nameof(IsPartiallyApplied)); }
        }
        public bool RequiresRestart { get; set; }
        public bool IsRestartPending
        {
            get => _isRestartPending;
            set { _isRestartPending = value; OnPropertyChanged(nameof(IsRestartPending)); }
        }
        public string RegistryPath { get; set; } = string.Empty;
        public string RegistryKey { get; set; } = string.Empty;
        public object? EnabledValue { get; set; }
        public int RiskLevel { get; set; } = 1; // 1-5 scale

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
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string WingetId { get; set; } = string.Empty;
        public string PowerShellCommand { get; set; } = string.Empty;
        public bool RequiresAdministrator { get; set; }
        public bool IsCustom { get; set; }
        public bool IsFavorite { get; set; }
    }

    public class AppearanceSettings
    {
        public string Theme { get; set; } = "System";
        public string AccentColor { get; set; } = string.Empty;
        public bool Transparency { get; set; } = true;
        public string Language { get; set; } = "System";
    }

    public class SystemShortcut
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
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
        public bool RequiresAdministrator { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsFavorite { get; set; }
    }

    public class CustomRegistryTweak : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private bool _isFavorite;
        private bool _isApplied;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RegistryPath { get; set; } = string.Empty;
        public string RegistryKey { get; set; } = string.Empty;
        public string ValueType { get; set; } = "REG_SZ"; // REG_DWORD, REG_QWORD, REG_SZ
        public string Data { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsApplied
        {
            get => _isApplied;
            set
            {
                if (_isApplied == value) return;
                _isApplied = value;
                PropertyChanged?.Invoke(this, new(nameof(IsApplied)));
            }
        }
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite == value) return;
                _isFavorite = value;
                PropertyChanged?.Invoke(this, new(nameof(IsFavorite)));
            }
        }
    }
}
