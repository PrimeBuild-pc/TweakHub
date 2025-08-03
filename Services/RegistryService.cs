using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TweakHub.Models;

namespace TweakHub.Services
{
    public class RegistryService : INotifyPropertyChanged
    {
        private static RegistryService? _instance;
        private readonly Dictionary<string, object?> _backupValues = new();

        public static RegistryService Instance => _instance ??= new RegistryService();

        public event PropertyChangedEventHandler? PropertyChanged;

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
                var backupPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TweakHub", "Backups");
                System.IO.Directory.CreateDirectory(backupPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating backup directory: {ex.Message}");
            }
        }

        public bool ApplyTweak(PerformanceTweak tweak)
        {
            try
            {
                if (tweak.Type != TweakType.Registry)
                    return false;

                var keyPath = tweak.RegistryPath;
                var valueName = tweak.RegistryKey;
                var newValue = tweak.IsEnabled ? tweak.EnabledValue : tweak.DisabledValue;

                // Backup current value
                BackupRegistryValue(keyPath, valueName);

                // Apply the tweak
                return SetRegistryValue(keyPath, valueName, newValue);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying tweak {tweak.Name}: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error restoring tweak {tweak.Name}: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error reading registry value {keyPath}\\{valueName}: {ex.Message}");
                return null;
            }
        }

        public bool SetRegistryValue(string keyPath, string valueName, object? value)
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
                    var valueKind = GetRegistryValueKind(value);
                    key.SetValue(valueName, value, valueKind);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting registry value {keyPath}\\{valueName}: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error backing up registry value: {ex.Message}");
            }
        }

        private RegistryKey GetRootKey(string keyPath)
        {
            if (keyPath.StartsWith("HKEY_CURRENT_USER") || keyPath.StartsWith("HKCU"))
                return Registry.CurrentUser;
            if (keyPath.StartsWith("HKEY_LOCAL_MACHINE") || keyPath.StartsWith("HKLM"))
                return Registry.LocalMachine;
            if (keyPath.StartsWith("HKEY_CLASSES_ROOT") || keyPath.StartsWith("HKCR"))
                return Registry.ClassesRoot;
            if (keyPath.StartsWith("HKEY_USERS") || keyPath.StartsWith("HKU"))
                return Registry.Users;
            if (keyPath.StartsWith("HKEY_CURRENT_CONFIG") || keyPath.StartsWith("HKCC"))
                return Registry.CurrentConfig;

            // Default to HKEY_CURRENT_USER
            return Registry.CurrentUser;
        }

        private string GetSubKeyPath(string keyPath)
        {
            var parts = keyPath.Split('\\');
            if (parts.Length > 1)
            {
                return string.Join("\\", parts.Skip(1));
            }
            return keyPath;
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
                var backupPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TweakHub", "Backups", $"registry_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(_backupValues, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(backupPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving backup file: {ex.Message}");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
