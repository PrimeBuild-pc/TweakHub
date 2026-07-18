using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using TweakHub.Models;

namespace TweakHub.Services
{
    public class RegistryService : INotifyPropertyChanged
    {
        private readonly Dictionary<string, RegistryBackup> _backups = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _dataPath;
        private readonly string _backupFile;
        private readonly string _logFile;
        private DateTime? _lastBackupCreatedAt;

        public static RegistryService Instance { get; } = new();
        public event PropertyChangedEventHandler? PropertyChanged;
        public int BackupCount => _backups.Count;

        private RegistryService() : this(AppDataPath.BasePath) { }

        internal RegistryService(string dataPath)
        {
            _dataPath = dataPath;
            _backupFile = Path.Combine(dataPath, "registry-backup.json");
            _logFile = Path.Combine(dataPath, "operations.jsonl");
        }

        public DateTime? LastBackupCreatedAt
        {
            get => _lastBackupCreatedAt;
            private set
            {
                if (_lastBackupCreatedAt == value) return;
                _lastBackupCreatedAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastBackupStatusText));
            }
        }

        public string LastBackupStatusText => LastBackupCreatedAt is null
            ? string.Empty
            : $"Backup created: {FormatRelativeTime(DateTime.Now - LastBackupCreatedAt.Value)}";

        public void Initialize()
        {
            Directory.CreateDirectory(_dataPath);
            LoadBackups();
        }

        public bool ApplyTweak(PerformanceTweak tweak, bool enable)
        {
            if (tweak.Type != TweakType.Registry) return false;
            return enable
                ? ApplyValueWithBackup(tweak.RegistryPath, tweak.RegistryKey, tweak.EnabledValue)
                : RestoreRegistryValue(tweak.RegistryPath, tweak.RegistryKey);
        }

        public bool ApplyValueWithBackup(
            string keyPath,
            string valueName,
            object? value,
            RegistryValueKind? explicitKind = null)
        {
            var backupKey = BackupKey(keyPath, valueName);
            var added = false;

            try
            {
                ValidateLocation(keyPath, valueName);
                if (!_backups.ContainsKey(backupKey))
                {
                    _backups[backupKey] = ReadBackup(keyPath, valueName);
                    added = true;
                    SaveBackups(); // Persist before changing Windows.
                }

                var success = SetRegistryValue(keyPath, valueName, value, explicitKind) &&
                              (value is null
                                  ? !RegistryValueExists(keyPath, valueName)
                                  : ValuesEqual(GetRegistryValue(keyPath, valueName), Normalize(value)));
                Log("apply", keyPath, valueName, success, success ? null : "Registry write verification failed");
                return success;
            }
            catch (Exception ex)
            {
                if (added)
                {
                    _backups.Remove(backupKey);
                    TrySaveBackups();
                }
                Log("apply", keyPath, valueName, false, ex.Message);
                return false;
            }
        }

        public async Task<bool> ApplyValuesWithBackupAsync(IReadOnlyCollection<RegistryValueChange> changes)
        {
            try
            {
                foreach (var change in changes)
                {
                    ValidateLocation(change.KeyPath, change.ValueName);
                    var key = BackupKey(change.KeyPath, change.ValueName);
                    if (!_backups.ContainsKey(key)) _backups[key] = ReadBackup(change.KeyPath, change.ValueName);
                }
                SaveBackups();

                var result = await PowerShellService.Instance.ExecuteScriptAsync(
                    BuildRegScript(changes), requireAdministrator: changes.Any(change => RequiresElevation(change.KeyPath)));
                var success = result.Success && changes.All(change => change.Value is not null &&
                    ValuesEqual(GetRegistryValue(change.KeyPath, change.ValueName), Normalize(change.Value)));
                foreach (var change in changes)
                    Log("apply", change.KeyPath, change.ValueName, success, success ? null : result.Error);
                return success;
            }
            catch (Exception ex)
            {
                foreach (var change in changes) Log("apply", change.KeyPath, change.ValueName, false, ex.Message);
                return false;
            }
        }

        public async Task<bool> RestoreValuesAsync(IReadOnlyCollection<RegistryValueChange> changes)
        {
            var backups = changes.Select(change => _backups.GetValueOrDefault(BackupKey(change.KeyPath, change.ValueName))).ToList();
            if (backups.Any(backup => backup is null)) return false;
            var restoreChanges = backups.Select(backup => new RegistryValueChange(
                backup!.KeyPath, backup.ValueName, backup.Existed ? Decode(backup) : null, backup.Kind)).ToList();
            var result = await PowerShellService.Instance.ExecuteScriptAsync(
                BuildRegScript(restoreChanges), requireAdministrator: restoreChanges.Any(change => RequiresElevation(change.KeyPath)));
            var success = result.Success && backups.All(backup => backup!.Existed
                ? ValuesEqual(GetRegistryValue(backup.KeyPath, backup.ValueName), Decode(backup))
                : !RegistryValueExists(backup.KeyPath, backup.ValueName));
            foreach (var backup in backups)
                Log("restore", backup!.KeyPath, backup.ValueName, success, success ? null : result.Error);
            if (success)
            {
                foreach (var backup in backups) _backups.Remove(BackupKey(backup!.KeyPath, backup.ValueName));
                SaveBackups();
            }
            return success;
        }

        private static string BuildRegScript(IEnumerable<RegistryValueChange> changes)
        {
            static string Quote(string value) => $"'{value.Replace("'", "''")}'";
            var script = new System.Text.StringBuilder("$ErrorActionPreference = 'Stop'\n");
            foreach (var change in changes)
            {
                if (change.Value is null)
                    script.AppendLine($"& reg.exe delete {Quote(change.KeyPath)} /v {Quote(change.ValueName)} /f");
                else
                    script.AppendLine($"& reg.exe add {Quote(change.KeyPath)} /v {Quote(change.ValueName)} /t {RegType(change.Kind)} /d {Quote(FormatRegData(change.Value, change.Kind))} /f");
                script.AppendLine("if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }");
            }
            return script.ToString();
        }

        private static string RegType(RegistryValueKind kind) => kind switch
        {
            RegistryValueKind.DWord => "REG_DWORD",
            RegistryValueKind.QWord => "REG_QWORD",
            RegistryValueKind.Binary => "REG_BINARY",
            RegistryValueKind.MultiString => "REG_MULTI_SZ",
            RegistryValueKind.ExpandString => "REG_EXPAND_SZ",
            _ => "REG_SZ"
        };

        public int CreateBackup(IEnumerable<PerformanceTweak> tweaks)
        {
            var addedKeys = new List<string>();
            try
            {
                foreach (var tweak in tweaks.Where(t => t.Type == TweakType.Registry))
                {
                    var key = BackupKey(tweak.RegistryPath, tweak.RegistryKey);
                    if (_backups.ContainsKey(key)) continue;
                    _backups[key] = ReadBackup(tweak.RegistryPath, tweak.RegistryKey);
                    addedKeys.Add(key);
                }

                SaveBackups();
                Log("backup", string.Empty, string.Empty, true, null);
                return addedKeys.Count;
            }
            catch (Exception ex)
            {
                foreach (var key in addedKeys) _backups.Remove(key);
                Log("backup", string.Empty, string.Empty, false, ex.Message);
                throw;
            }
        }

        public bool HasBackup(string keyPath, string valueName) =>
            _backups.ContainsKey(BackupKey(keyPath, valueName));


        public bool RestoreRegistryValue(string keyPath, string valueName)
        {
            var key = BackupKey(keyPath, valueName);
            if (!_backups.TryGetValue(key, out var backup)) return false;

            try
            {
                var original = backup.Existed ? Decode(backup) : null;
                var success = backup.Existed
                    ? SetRegistryValue(keyPath, valueName, original, backup.Kind) &&
                      ValuesEqual(GetRegistryValue(keyPath, valueName), original)
                    : DeleteRegistryValue(keyPath, valueName) && !RegistryValueExists(keyPath, valueName);

                if (success)
                {
                    _backups.Remove(key);
                    SaveBackups();
                }

                Log("restore", keyPath, valueName, success, success ? null : "Registry restore failed");
                return success;
            }
            catch (Exception ex)
            {
                Log("restore", keyPath, valueName, false, ex.Message);
                return false;
            }
        }

        public object? GetRegistryValue(string keyPath, string valueName)
        {
            try
            {
                var (root, subKeyPath) = ParsePath(keyPath);
                using var key = root.OpenSubKey(subKeyPath);
                return key?.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading registry value {keyPath}\\{valueName}: {ex.Message}");
                return null;
            }
        }

        public bool SetRegistryValue(
            string keyPath,
            string valueName,
            object? value,
            RegistryValueKind? explicitKind = null)
        {
            try
            {
                ValidateLocation(keyPath, valueName);
                var kind = explicitKind ?? (value is null ? RegistryValueKind.String : GetValueKind(value));
                if (RequiresElevation(keyPath) && !Elevation.IsAdministrator)
                    return RunElevatedReg(value is null ? "delete" : "add", keyPath, valueName, value, kind);

                var (root, subKeyPath) = ParsePath(keyPath);
                using var key = root.CreateSubKey(subKeyPath, true);
                if (key is null) return false;

                if (value is null)
                    key.DeleteValue(valueName, false);
                else
                    key.SetValue(valueName, Normalize(value), kind);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting registry value {keyPath}\\{valueName}: {ex.Message}");
                return false;
            }
        }

        public bool CheckTweakStatus(PerformanceTweak tweak)
        {
            var current = GetRegistryValue(tweak.RegistryPath, tweak.RegistryKey);
            return ValuesEqual(current, tweak.EnabledValue);
        }

        private RegistryBackup ReadBackup(string keyPath, string valueName)
        {
            ValidateLocation(keyPath, valueName);
            var (root, subKeyPath) = ParsePath(keyPath);
            using var key = root.OpenSubKey(subKeyPath);
            var existed = key?.GetValueNames().Contains(valueName, StringComparer.OrdinalIgnoreCase) == true;
            if (!existed)
                return new RegistryBackup { KeyPath = keyPath, ValueName = valueName, Existed = false };

            var kind = key!.GetValueKind(valueName);
            var value = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            return new RegistryBackup
            {
                KeyPath = keyPath,
                ValueName = valueName,
                Existed = true,
                Kind = kind,
                Data = Encode(value, kind)
            };
        }

        private static bool RegistryValueExists(string keyPath, string valueName)
        {
            var (root, subKeyPath) = ParsePath(keyPath);
            using var key = root.OpenSubKey(subKeyPath);
            return key?.GetValueNames().Contains(valueName, StringComparer.OrdinalIgnoreCase) == true;
        }

        private bool DeleteRegistryValue(string keyPath, string valueName)
        {
            if (RequiresElevation(keyPath) && !Elevation.IsAdministrator)
                return RunElevatedReg("delete", keyPath, valueName, null, RegistryValueKind.String);

            var (root, subKeyPath) = ParsePath(keyPath);
            using var key = root.OpenSubKey(subKeyPath, writable: true);
            key?.DeleteValue(valueName, false);
            return true;
        }

        private static bool RequiresElevation(string keyPath) =>
            !keyPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase) &&
            !keyPath.StartsWith("HKEY_CURRENT_USER\\", StringComparison.OrdinalIgnoreCase);

        private static bool RunElevatedReg(
            string action,
            string keyPath,
            string valueName,
            object? value,
            RegistryValueKind kind)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                startInfo.ArgumentList.Add(action);
                startInfo.ArgumentList.Add(keyPath);
                startInfo.ArgumentList.Add("/v");
                startInfo.ArgumentList.Add(valueName);
                if (action == "add")
                {
                    startInfo.ArgumentList.Add("/t");
                    startInfo.ArgumentList.Add(kind switch
                    {
                        RegistryValueKind.DWord => "REG_DWORD",
                        RegistryValueKind.QWord => "REG_QWORD",
                        RegistryValueKind.Binary => "REG_BINARY",
                        RegistryValueKind.MultiString => "REG_MULTI_SZ",
                        RegistryValueKind.ExpandString => "REG_EXPAND_SZ",
                        _ => "REG_SZ"
                    });
                    startInfo.ArgumentList.Add("/d");
                    startInfo.ArgumentList.Add(FormatRegData(value, kind));
                }
                startInfo.ArgumentList.Add("/f");
                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static string FormatRegData(object? value, RegistryValueKind kind) => kind switch
        {
            RegistryValueKind.DWord => unchecked((uint)Convert.ToInt32(value, CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture),
            RegistryValueKind.QWord => Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            RegistryValueKind.Binary => Convert.ToHexString((byte[])(value ?? Array.Empty<byte>())),
            RegistryValueKind.MultiString => string.Join("\\0", (string[])(value ?? Array.Empty<string>())),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };

        private void LoadBackups()
        {
            _backups.Clear();
            if (!File.Exists(_backupFile)) return;

            try
            {
                var backups = JsonSerializer.Deserialize<List<RegistryBackup>>(File.ReadAllText(_backupFile)) ?? [];
                foreach (var backup in backups)
                    _backups[BackupKey(backup.KeyPath, backup.ValueName)] = backup;
                LastBackupCreatedAt = File.GetLastWriteTime(_backupFile);
            }
            catch (Exception ex)
            {
                var corrupt = _backupFile + $".corrupt-{DateTime.Now:yyyyMMddHHmmss}";
                File.Move(_backupFile, corrupt, true);
                Log("load-backup", string.Empty, string.Empty, false, $"Corrupt backup moved to {corrupt}: {ex.Message}");
            }
        }

        private void SaveBackups()
        {
            Directory.CreateDirectory(_dataPath);
            var temp = _backupFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(_backups.Values, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, _backupFile, true);
            LastBackupCreatedAt = DateTime.Now;
        }

        private void TrySaveBackups()
        {
            try { SaveBackups(); } catch { }
        }

        private void Log(string action, string keyPath, string valueName, bool success, string? error)
        {
            try
            {
                Directory.CreateDirectory(_dataPath);
                File.AppendAllText(_logFile, JsonSerializer.Serialize(new
                {
                    timestamp = DateTimeOffset.Now,
                    action,
                    keyPath,
                    valueName,
                    success,
                    error
                }) + Environment.NewLine);
            }
            catch { }
        }

        public static void ValidateLocation(string keyPath, string valueName)
        {
            _ = ParsePath(keyPath);
            if (string.IsNullOrWhiteSpace(valueName))
                throw new ArgumentException("Registry value name is required.", nameof(valueName));
        }

        private static (RegistryKey Root, string SubKeyPath) ParsePath(string keyPath)
        {
            if (string.IsNullOrWhiteSpace(keyPath))
                throw new ArgumentException("Registry path is required.", nameof(keyPath));

            var parts = keyPath.Split('\\', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
                throw new ArgumentException("Registry path must include a supported root and subkey.", nameof(keyPath));

            var root = parts[0].ToUpperInvariant() switch
            {
                "HKEY_CURRENT_USER" or "HKCU" => Registry.CurrentUser,
                "HKEY_LOCAL_MACHINE" or "HKLM" => Registry.LocalMachine,
                "HKEY_CLASSES_ROOT" or "HKCR" => Registry.ClassesRoot,
                "HKEY_USERS" or "HKU" => Registry.Users,
                "HKEY_CURRENT_CONFIG" or "HKCC" => Registry.CurrentConfig,
                _ => throw new ArgumentException($"Unsupported registry root '{parts[0]}'.", nameof(keyPath))
            };
            return (root, parts[1]);
        }

        private static string BackupKey(string keyPath, string valueName) =>
            $"{keyPath.Trim()}\\{valueName.Trim()}";

        private static object Normalize(object value) => value switch
        {
            uint number => unchecked((int)number),
            _ => value
        };

        private static RegistryValueKind GetValueKind(object value) => value switch
        {
            int or uint => RegistryValueKind.DWord,
            long => RegistryValueKind.QWord,
            byte[] => RegistryValueKind.Binary,
            string[] => RegistryValueKind.MultiString,
            _ => RegistryValueKind.String
        };

        private static bool ValuesEqual(object? left, object? right)
        {
            if (left is null || right is null) return left is null && right is null;
            if (left is byte[] leftBytes && right is byte[] rightBytes) return leftBytes.SequenceEqual(rightBytes);
            if (left is string[] leftStrings && right is string[] rightStrings) return leftStrings.SequenceEqual(rightStrings);
            if (IsNumber(left) && IsNumber(right))
                return Convert.ToInt64(left, CultureInfo.InvariantCulture) == Convert.ToInt64(right, CultureInfo.InvariantCulture);
            return left.Equals(right);
        }

        private static bool IsNumber(object value) =>
            value is byte or sbyte or short or ushort or int or uint or long;

        private static string? Encode(object? value, RegistryValueKind kind) => kind switch
        {
            RegistryValueKind.Binary => Convert.ToBase64String((byte[])(value ?? Array.Empty<byte>())),
            RegistryValueKind.MultiString => JsonSerializer.Serialize((string[])(value ?? Array.Empty<string>())),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };

        private static object Decode(RegistryBackup backup) => backup.Kind switch
        {
            RegistryValueKind.DWord => int.Parse(backup.Data ?? "0", CultureInfo.InvariantCulture),
            RegistryValueKind.QWord => long.Parse(backup.Data ?? "0", CultureInfo.InvariantCulture),
            RegistryValueKind.Binary => Convert.FromBase64String(backup.Data ?? string.Empty),
            RegistryValueKind.MultiString => JsonSerializer.Deserialize<string[]>(backup.Data ?? "[]") ?? [],
            _ => backup.Data ?? string.Empty
        };

        private static string FormatRelativeTime(TimeSpan delta)
        {
            if (delta.TotalSeconds < 10) return "just now";
            if (delta.TotalMinutes < 1) return "a few moments ago";
            if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
            if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
            return $"{(int)delta.TotalDays}d ago";
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed record RegistryValueChange(string KeyPath, string ValueName, object? Value, RegistryValueKind Kind = RegistryValueKind.DWord);

    internal sealed class RegistryBackup
    {
        public string KeyPath { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
        public bool Existed { get; set; }
        public RegistryValueKind Kind { get; set; }
        public string? Data { get; set; }
    }
}
