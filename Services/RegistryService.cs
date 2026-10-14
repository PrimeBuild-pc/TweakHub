using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using TweakHub.Localization;
using TweakHub.Models;

namespace TweakHub.Services
{
    public class RegistryService : INotifyPropertyChanged
    {
        // Single values change backups on a worker thread while groups change them on the UI thread.
        private readonly Lock _lock = new();
        private readonly Dictionary<string, RegistryBackup> _backups = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _dataPath;
        private readonly string _backupFile;
        private readonly string _logFile;
        private DateTime? _lastBackupCreatedAt;

        public static RegistryService Instance { get; } = new();
        public event PropertyChangedEventHandler? PropertyChanged;
        public int BackupCount { get { lock (_lock) return _backups.Count; } }

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
            : L.Format("UI:BackupCreatedStatus", FormatRelativeTime(DateTime.Now - LastBackupCreatedAt.Value));

        public void Initialize()
        {
            Directory.CreateDirectory(_dataPath);
            lock (_lock) LoadBackups();
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
                lock (_lock)
                {
                    if (!_backups.ContainsKey(backupKey))
                    {
                        _backups[backupKey] = ReadBackup(keyPath, valueName);
                        added = true;
                        SaveBackups(); // Persist before changing Windows.
                    }
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
                    lock (_lock)
                    {
                        _backups.Remove(backupKey);
                        TrySaveBackups();
                    }
                }
                Log("apply", keyPath, valueName, false, ex.Message);
                return false;
            }
        }

        public async Task<bool> ApplyValuesWithBackupAsync(IReadOnlyCollection<RegistryValueChange> changes)
        {
            try
            {
                lock (_lock)
                {
                    foreach (var change in changes)
                    {
                        ValidateLocation(change.KeyPath, change.ValueName);
                        var key = BackupKey(change.KeyPath, change.ValueName);
                        if (!_backups.ContainsKey(key)) _backups[key] = ReadBackup(change.KeyPath, change.ValueName);
                    }
                    SaveBackups();
                }

                var result = await PowerShellService.Instance.ExecuteScriptAsync(
                    BuildRegScript(changes), requireAdministrator: changes.Any(change => RequiresElevation(change.KeyPath)));
                var success = result.Success && changes.All(change => change.Value is null
                    ? !RegistryValueExists(change.KeyPath, change.ValueName)
                    : ValuesEqual(GetRegistryValue(change.KeyPath, change.ValueName), Normalize(change.Value)));
                foreach (var change in changes)
                    Log("apply", change.KeyPath, change.ValueName, success, success ? null : result.Error);
                if (!success) await RestoreValuesAsync(changes);
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
            List<RegistryBackup?> backups;
            lock (_lock) backups = changes.Select(change => _backups.GetValueOrDefault(BackupKey(change.KeyPath, change.ValueName))).ToList();
            if (backups.Any(backup => backup is null)) return false;
            // Values already at their original (for example when UAC was declined on apply) need no elevated script.
            var restoreChanges = backups.Where(backup => !IsOriginal(backup!)).Select(backup => new RegistryValueChange(
                backup!.KeyPath, backup.ValueName, backup.Existed ? Decode(backup) : null, backup.Kind)).ToList();
            var result = restoreChanges.Count == 0
                ? new PowerShellResult { Success = true }
                : await PowerShellService.Instance.ExecuteScriptAsync(
                    BuildRegScript(restoreChanges), requireAdministrator: restoreChanges.Any(change => RequiresElevation(change.KeyPath)));
            var success = result.Success && backups.All(backup => IsOriginal(backup!));
            foreach (var backup in backups)
                Log("restore", backup!.KeyPath, backup.ValueName, success, success ? null : result.Error);
            if (success)
            {
                lock (_lock)
                {
                    foreach (var backup in backups) _backups.Remove(BackupKey(backup!.KeyPath, backup.ValueName));
                    SaveBackups();
                }
            }
            return success;
        }

        private bool IsOriginal(RegistryBackup backup) => backup.Existed
            ? ValuesEqual(GetRegistryValue(backup.KeyPath, backup.ValueName), Decode(backup))
            : !RegistryValueExists(backup.KeyPath, backup.ValueName);

        private static string BuildRegScript(IEnumerable<RegistryValueChange> changes)
        {
            static string Quote(string value) => PowerShellService.Literal(value);
            // A skipped delete leaves $LASTEXITCODE untouched, so start from a known success value.
            var script = new System.Text.StringBuilder("$ErrorActionPreference = 'Stop'\n$global:LASTEXITCODE = 0\n");
            foreach (var change in changes)
            {
                var keyPath = ForScript(change.KeyPath);
                // reg.exe fails when the value is already gone, which is the state we want, so delete only what exists.
                if (change.Value is null)
                    script.AppendLine($"if ($null -ne (Get-ItemProperty -LiteralPath {Quote("Registry::" + keyPath)} -Name {Quote(change.ValueName)} -ErrorAction SilentlyContinue)) {{ & reg.exe delete {Quote(keyPath)} /v {Quote(change.ValueName)} /f }}");
                else
                    script.AppendLine($"& reg.exe add {Quote(keyPath)} /v {Quote(change.ValueName)} /t {RegType(change.Kind)} /d {Quote(FormatRegData(change.Value, change.Kind))} /f");
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

        public int CreateBackup(IEnumerable<PerformanceTweak> tweaks) => CreateBackupValues(
            tweaks.Where(tweak => tweak.Type == TweakType.Registry)
                .Select(tweak => new RegistryValueChange(tweak.RegistryPath, tweak.RegistryKey, tweak.EnabledValue)));

        public int CreateBackupValues(IEnumerable<RegistryValueChange> changes)
        {
            var addedKeys = new List<string>();
            lock (_lock)
            try
            {
                foreach (var change in changes)
                {
                    var key = BackupKey(change.KeyPath, change.ValueName);
                    if (_backups.ContainsKey(key)) continue;
                    _backups[key] = ReadBackup(change.KeyPath, change.ValueName);
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

        /// <summary>Every value TweakHub has changed and can restore.</summary>
        public IReadOnlyList<(string KeyPath, string ValueName)> BackedUpValues
        {
            get { lock (_lock) return _backups.Values.Select(backup => (backup.KeyPath, backup.ValueName)).ToList(); }
        }

        public bool HasBackup(string keyPath, string valueName)
        {
            lock (_lock) return _backups.ContainsKey(BackupKey(keyPath, valueName));
        }

        public bool RestoreRegistryValue(string keyPath, string valueName)
        {
            var key = BackupKey(keyPath, valueName);
            RegistryBackup? backup;
            lock (_lock)
                if (!_backups.TryGetValue(key, out backup)) return false;

            try
            {
                var original = backup.Existed ? Decode(backup) : null;
                var success = backup.Existed
                    ? SetRegistryValue(keyPath, valueName, original, backup.Kind) &&
                      ValuesEqual(GetRegistryValue(keyPath, valueName), original)
                    : DeleteRegistryValue(keyPath, valueName) && !RegistryValueExists(keyPath, valueName);

                if (success)
                {
                    lock (_lock)
                    {
                        _backups.Remove(key);
                        SaveBackups();
                    }
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

        public bool CheckTweakStatus(PerformanceTweak tweak) =>
            IsValueSet(tweak.RegistryPath, tweak.RegistryKey, tweak.EnabledValue);

        public bool IsValueSet(string keyPath, string valueName, object? expected) =>
            expected is not null && ValuesEqual(GetRegistryValue(keyPath, valueName), Normalize(expected));

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

        // HKCU\Software\Policies is read-only for the user, like the machine hives.
        internal static bool RequiresElevation(string keyPath) => UserSubKey(keyPath) is not { } subKey
            || subKey.StartsWith(@"Software\Policies\", StringComparison.OrdinalIgnoreCase);

        private static string? UserSubKey(string keyPath) =>
            keyPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase) ? keyPath[5..]
            : keyPath.StartsWith("HKEY_CURRENT_USER\\", StringComparison.OrdinalIgnoreCase) ? keyPath[18..]
            : null;

        /// <summary>
        /// HKCU of this user as a HKEY_USERS path. An elevated script can run under another account
        /// (a standard user types an administrator's password), where HKCU would be that account's hive.
        /// </summary>
        internal static string ForScript(string keyPath) => UserSubKey(keyPath) is { } subKey
            ? $@"HKEY_USERS\{System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value}\{subKey}"
            : keyPath;

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
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                startInfo.ArgumentList.Add(action);
                startInfo.ArgumentList.Add(ForScript(keyPath));
                startInfo.ArgumentList.Add("/v");
                startInfo.ArgumentList.Add(valueName);
                if (action == "add")
                {
                    startInfo.ArgumentList.Add("/t");
                    startInfo.ArgumentList.Add(RegType(kind));
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
            var corrupt = false;
            var backups = JsonStore.Load<List<RegistryBackup>>(_backupFile, message =>
            {
                corrupt = true;
                Log("load-backup", string.Empty, string.Empty, false, message);
            });
            foreach (var backup in backups)
                _backups[BackupKey(backup.KeyPath, backup.ValueName)] = backup;
            if (!corrupt) LastBackupCreatedAt = File.GetLastWriteTime(_backupFile);
        }

        private void SaveBackups()
        {
            JsonStore.Save(_backupFile, _backups.Values.ToList());
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
                throw new ArgumentException(L.Get("UI:RegistryValueNameRequired"), nameof(valueName));
        }

        public static object ParseData(string valueType, string data, out RegistryValueKind kind)
        {
            switch (valueType.ToUpperInvariant())
            {
                case "REG_DWORD":
                    kind = RegistryValueKind.DWord;
                    return data.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? Convert.ToInt32(data[2..], 16)
                        : int.Parse(data, CultureInfo.InvariantCulture);
                case "REG_QWORD":
                    kind = RegistryValueKind.QWord;
                    return data.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? Convert.ToInt64(data[2..], 16)
                        : long.Parse(data, CultureInfo.InvariantCulture);
                case "REG_BINARY":
                    kind = RegistryValueKind.Binary;
                    var cleaned = new string(data.Where(character => !char.IsWhiteSpace(character) && character is not ',' and not '-').ToArray());
                    if (cleaned.Length == 0 || cleaned.Length % 2 != 0 || cleaned.Any(character => !Uri.IsHexDigit(character)))
                        throw new FormatException(L.Get("Tweaks:BinaryInvalid"));
                    return Convert.FromHexString(cleaned);
                case "REG_MULTI_SZ":
                    kind = RegistryValueKind.MultiString;
                    return data.Split(['\n', ';', '|'], StringSplitOptions.RemoveEmptyEntries).Select(value => value.Trim()).ToArray();
                case "REG_EXPAND_SZ":
                    kind = RegistryValueKind.ExpandString;
                    return data;
                case "REG_SZ":
                    kind = RegistryValueKind.String;
                    return data;
                default:
                    throw new FormatException(L.Get("UI:RegistryValueTypeUnsupported"));
            }
        }

        private static (RegistryKey Root, string SubKeyPath) ParsePath(string keyPath)
        {
            if (string.IsNullOrWhiteSpace(keyPath))
                throw new ArgumentException(L.Get("UI:RegistryPathRequired"), nameof(keyPath));

            var parts = keyPath.Split('\\', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
                throw new ArgumentException(L.Get("UI:RegistryPathInvalid"), nameof(keyPath));

            var root = parts[0].ToUpperInvariant() switch
            {
                "HKEY_CURRENT_USER" or "HKCU" => Registry.CurrentUser,
                "HKEY_LOCAL_MACHINE" or "HKLM" => Registry.LocalMachine,
                "HKEY_CLASSES_ROOT" or "HKCR" => Registry.ClassesRoot,
                "HKEY_USERS" or "HKU" => Registry.Users,
                "HKEY_CURRENT_CONFIG" or "HKCC" => Registry.CurrentConfig,
                _ => throw new ArgumentException(L.Format("UI:RegistryRootUnsupported", parts[0]), nameof(keyPath))
            };
            return (root, parts[1]);
        }

        // HKLM\ and HKEY_LOCAL_MACHINE\ name the same value, so they must share one backup.
        private static string BackupKey(string keyPath, string valueName)
        {
            var path = keyPath.Trim();
            var separator = path.IndexOf('\\');
            var root = separator < 0 ? path : path[..separator];
            var full = root.ToUpperInvariant() switch
            {
                "HKLM" => "HKEY_LOCAL_MACHINE",
                "HKCU" => "HKEY_CURRENT_USER",
                "HKCR" => "HKEY_CLASSES_ROOT",
                "HKU" => "HKEY_USERS",
                "HKCC" => "HKEY_CURRENT_CONFIG",
                _ => root
            };
            return $"{full}{path[root.Length..]}\\{valueName.Trim()}";
        }

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
            if (delta.TotalSeconds < 10) return L.Get("UI:JustNow");
            if (delta.TotalMinutes < 1) return L.Get("UI:FewMomentsAgo");
            if (delta.TotalMinutes < 60) return L.Format("UI:MinutesAgo", (int)delta.TotalMinutes);
            if (delta.TotalHours < 24) return L.Format("UI:HoursAgo", (int)delta.TotalHours);
            return L.Format("UI:DaysAgo", (int)delta.TotalDays);
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
