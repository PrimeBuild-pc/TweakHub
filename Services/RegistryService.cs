using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TweakHub.Models;

namespace TweakHub.Services
{
    public class RegistryService : INotifyPropertyChanged
    {
        private static RegistryService? _instance;
        private readonly Dictionary<string, object?> _backupValues = new();

        private DateTime? _lastBackupCreatedAt;

        public static RegistryService Instance => _instance ??= new RegistryService();

        public event PropertyChangedEventHandler? PropertyChanged;

        public DateTime? LastBackupCreatedAt
        {
            get => _lastBackupCreatedAt;
            private set
            {
                if (_lastBackupCreatedAt == value) return;
                _lastBackupCreatedAt = value;
                OnPropertyChanged(nameof(LastBackupCreatedAt));
                OnPropertyChanged(nameof(LastBackupStatusText));
            }
        }

        public string LastBackupStatusText
        {
            get
            {
                if (LastBackupCreatedAt is null) return string.Empty;
                var delta = DateTime.Now - LastBackupCreatedAt.Value;
                return $"Backup created: {FormatRelativeTime(delta)}";
            }
        }

        private static string FormatRelativeTime(TimeSpan delta)
        {
            if (delta.TotalSeconds < 10) return "just now";
            if (delta.TotalMinutes < 1) return "a few moments ago";
            if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
            if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
            return $"{(int)delta.TotalDays}d ago";
        }

        private RegistryService() { }

        public void Initialize()
        {
            // Initialize registry service
            CreateBackupDirectory();
        }

        private void CreateBackupDirectory()
        {
            try
            {
                var backupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TweakHub", "Backups");
                Directory.CreateDirectory(backupPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating backup directory: {ex.Message}");
            }
        }

        public bool ApplyTweak(PerformanceTweak tweak)
        {
            try
            {
                if (tweak.Type != TweakType.Registry)
                    return false;

                return ApplyTweak(tweak, tweak.IsEnabled);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying tweak {tweak.Name}: {ex.Message}");
                return false;
            }
        }

        public bool ApplyTweak(PerformanceTweak tweak, bool enable)
        {
            try
            {
                if (tweak.Type != TweakType.Registry)
                    return false;

                var keyPath = tweak.RegistryPath;
                var valueName = tweak.RegistryKey;
                var newValue = enable ? tweak.EnabledValue : tweak.DisabledValue;

                // Backup current value
                BackupRegistryValue(keyPath, valueName);

                // Apply the tweak
                return SetRegistryValue(keyPath, valueName, newValue);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying tweak {tweak.Name}: {ex.Message}");
                return false;
            }
        }

        public bool RestoreTweak(PerformanceTweak tweak)
        {
            try
            {
                var backupKey = $"{tweak.RegistryPath}\\{tweak.RegistryKey}";
                if (_backupValues.TryGetValue(backupKey, out var backupValue))
                {
                    return SetRegistryValue(tweak.RegistryPath, tweak.RegistryKey, backupValue);
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error restoring tweak {tweak.Name}: {ex.Message}");
                return false;
            }
        }

        public object? GetRegistryValue(string keyPath, string valueName)
        {
            try
            {
                var rootKey = GetRootKey(keyPath);
                var subKeyPath = GetSubKeyPath(keyPath);

                using var key = rootKey.OpenSubKey(subKeyPath);
                return key?.GetValue(valueName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading registry value {keyPath}\\{valueName}: {ex.Message}");
                return null;
            }
        }

        public bool SetRegistryValue(string keyPath, string valueName, object? value)
        {
            return SetRegistryValue(keyPath, valueName, value, null);
        }

        public bool SetRegistryValue(string keyPath, string valueName, object? value, RegistryValueKind? explicitKind)
        {
            try
            {
                var rootKey = GetRootKey(keyPath);
                var subKeyPath = GetSubKeyPath(keyPath);

                using var key = rootKey.CreateSubKey(subKeyPath, true);
                if (key == null) return false;

                if (value == null)
                {
                    key.DeleteValue(valueName, false);
                }
                else
                {
                    // Determine registry value type
                    var valueKind = explicitKind ?? GetRegistryValueKind(value);
                    key.SetValue(valueName, value, valueKind);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting registry value {keyPath}\\{valueName}: {ex.Message}");
                return false;
            }
        }

        private void BackupRegistryValue(string keyPath, string valueName)
        {
            try
            {
                var backupKey = $"{keyPath}\\{valueName}";
                if (!_backupValues.ContainsKey(backupKey))
                {
                    var currentValue = GetRegistryValue(keyPath, valueName);
                    _backupValues[backupKey] = currentValue;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error backing up registry value: {ex.Message}");
            }
        }

        private RegistryKey GetRootKey(string keyPath)
        {
            if (keyPath.StartsWith("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase) || keyPath.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase))
                return Registry.CurrentUser;
            if (keyPath.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase) || keyPath.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase))
                return Registry.LocalMachine;
            if (keyPath.StartsWith("HKEY_CLASSES_ROOT", StringComparison.OrdinalIgnoreCase) || keyPath.StartsWith("HKCR", StringComparison.OrdinalIgnoreCase))
                return Registry.ClassesRoot;
            if (keyPath.StartsWith("HKEY_USERS", StringComparison.OrdinalIgnoreCase) || keyPath.StartsWith("HKU", StringComparison.OrdinalIgnoreCase))
                return Registry.Users;
            if (keyPath.StartsWith("HKEY_CURRENT_CONFIG", StringComparison.OrdinalIgnoreCase) || keyPath.StartsWith("HKCC", StringComparison.OrdinalIgnoreCase))
                return Registry.CurrentConfig;

            // Default to HKEY_CURRENT_USER
            return Registry.CurrentUser;
        }

        private string GetSubKeyPath(string keyPath)
        {
            var firstSlash = keyPath.IndexOf('\\');
            if (firstSlash < 0 || firstSlash == keyPath.Length - 1)
                return keyPath;

            return keyPath[(firstSlash + 1)..];
        }

        private RegistryValueKind GetRegistryValueKind(object value)
        {
            return value switch
            {
                int => RegistryValueKind.DWord,
                long => RegistryValueKind.QWord,
                string => RegistryValueKind.String,
                byte[] => RegistryValueKind.Binary,
                string[] => RegistryValueKind.MultiString,
                _ => RegistryValueKind.String
            };
        }

        public bool CheckTweakStatus(PerformanceTweak tweak)
        {
            try
            {
                var currentValue = GetRegistryValue(tweak.RegistryPath, tweak.RegistryKey);
                return currentValue?.Equals(tweak.EnabledValue) == true;
            }
            catch
            {
                return false;
            }
        }

        public void SaveBackupsToFile()
        {
            try
            {
                var backupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TweakHub", "Backups", $"registry_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(_backupValues, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(backupPath, json);

                LastBackupCreatedAt = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving backup file: {ex.Message}");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
