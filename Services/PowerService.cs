using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TweakHub.Services;

public class PowerService
{
    private const string HighPerformanceScheme = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private static PowerService? _instance;
    private readonly string _dataFile;
    private readonly string _logFile;
    private PowerBackup _backup = new();

    public static PowerService Instance => _instance ??= new PowerService();

    private PowerService() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TweakHub")) { }

    internal PowerService(string dataPath)
    {
        Directory.CreateDirectory(dataPath);
        _dataFile = Path.Combine(dataPath, "power-backup.json");
        _logFile = Path.Combine(dataPath, "operations.jsonl");
        Load();
    }

    public async Task<bool> ApplyProcessorSettingAsync(string id, string settingAlias, int targetAcValue)
    {
        var scheme = await GetActiveSchemeAsync();
        if (scheme is null) return false;

        if (!_backup.Settings.ContainsKey(id))
        {
            var original = await GetAcValueAsync(scheme, settingAlias);
            if (original is null) return false;
            _backup.Settings[id] = new PowerSettingBackup { Scheme = scheme, Alias = settingAlias, AcValue = original.Value };
            Save();
        }

        var result = await RunMutationAsync("/setacvalueindex", scheme, "SUB_PROCESSOR", settingAlias, targetAcValue.ToString(CultureInfo.InvariantCulture));
        if (result.Success) result = await RunMutationAsync("/setactive", scheme);
        var verified = result.Success && await GetAcValueAsync(scheme, settingAlias) == targetAcValue;
        Log("power-apply", id, verified, verified ? null : result.Error);
        return verified;
    }

    public async Task<bool> RestoreProcessorSettingAsync(string id)
    {
        if (!_backup.Settings.TryGetValue(id, out var backup)) return false;

        var result = await RunMutationAsync(
            "/setacvalueindex",
            backup.Scheme,
            "SUB_PROCESSOR",
            backup.Alias,
            backup.AcValue.ToString(CultureInfo.InvariantCulture));
        if (result.Success) result = await RunMutationAsync("/setactive", backup.Scheme);
        var verified = result.Success && await GetAcValueAsync(backup.Scheme, backup.Alias) == backup.AcValue;
        if (verified)
        {
            _backup.Settings.Remove(id);
            Save();
        }
        Log("power-restore", id, verified, verified ? null : result.Error);
        return verified;
    }

    public async Task<bool> IsProcessorSettingActiveAsync(string settingAlias, int expectedAcValue)
    {
        var scheme = await GetActiveSchemeAsync();
        return scheme is not null && await GetAcValueAsync(scheme, settingAlias) == expectedAcValue;
    }

    public async Task<bool> ApplyHighPerformancePlanAsync()
    {
        if (string.IsNullOrWhiteSpace(_backup.ActiveScheme))
        {
            _backup.ActiveScheme = await GetActiveSchemeAsync();
            if (_backup.ActiveScheme is null) return false;
            Save();
        }

        var result = await RunMutationAsync("/setactive", HighPerformanceScheme);
        var verified = result.Success &&
                       string.Equals(await GetActiveSchemeAsync(), HighPerformanceScheme, StringComparison.OrdinalIgnoreCase);
        Log("power-plan-apply", HighPerformanceScheme, verified, verified ? null : result.Error);
        return verified;
    }

    public async Task<bool> RestorePowerPlanAsync()
    {
        if (string.IsNullOrWhiteSpace(_backup.ActiveScheme)) return false;
        var original = _backup.ActiveScheme;
        var result = await RunMutationAsync("/setactive", original);
        var verified = result.Success &&
                       string.Equals(await GetActiveSchemeAsync(), original, StringComparison.OrdinalIgnoreCase);
        if (verified)
        {
            _backup.ActiveScheme = null;
            Save();
        }
        Log("power-plan-restore", original, verified, verified ? null : result.Error);
        return verified;
    }

    public async Task<bool> IsHighPerformancePlanActiveAsync() =>
        string.Equals(await GetActiveSchemeAsync(), HighPerformanceScheme, StringComparison.OrdinalIgnoreCase);

    public bool HasAnyBackup => !string.IsNullOrWhiteSpace(_backup.ActiveScheme) || _backup.Settings.Count > 0;

    public bool HasBackup(string id) => id == "high_performance_power_plan"
        ? !string.IsNullOrWhiteSpace(_backup.ActiveScheme)
        : _backup.Settings.ContainsKey(id);

    private async Task<string?> GetActiveSchemeAsync()
    {
        var result = await RunAsync("/getactivescheme");
        if (!result.Success) return null;
        return Regex.Match(result.Output, @"[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}").Value.ToLowerInvariant() is { Length: > 0 } guid
            ? guid
            : null;
    }

    private async Task<int?> GetAcValueAsync(string scheme, string settingAlias)
    {
        var result = await RunAsync("/query", scheme, "SUB_PROCESSOR");
        return result.Success ? ParseAcSetting(result.Output, settingAlias) : null;
    }

    internal static int? ParseAcSetting(string output, string settingAlias)
    {
        var aliasIndex = output.IndexOf(settingAlias, StringComparison.OrdinalIgnoreCase);
        if (aliasIndex < 0) return null;
        var nextSetting = output.IndexOf("GUID Alias:", aliasIndex + settingAlias.Length, StringComparison.OrdinalIgnoreCase);
        var block = output[aliasIndex..(nextSetting < 0 ? output.Length : nextSetting)];
        var values = Regex.Matches(block, @"0x([0-9a-fA-F]{1,8})");
        return values.Count >= 2
            ? unchecked((int)uint.Parse(values[^2].Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            : null;
    }

    private static async Task<ProcessResult> RunMutationAsync(params string[] arguments)
    {
        if (Elevation.IsAdministrator) return await RunAsync(arguments);
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                UseShellExecute = true,
                Verb = "runas"
            };
            foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
            using var process = Process.Start(startInfo);
            if (process is null) return new ProcessResult(false, string.Empty, "Failed to start powercfg.");
            await process.WaitForExitAsync();
            return new ProcessResult(process.ExitCode == 0, string.Empty, process.ExitCode == 0 ? string.Empty : $"powercfg exited with {process.ExitCode}.");
        }
        catch (Exception ex)
        {
            return new ProcessResult(false, string.Empty, ex.Message);
        }
    }

    private static async Task<ProcessResult> RunAsync(params string[] arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
            process.Start();
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return new ProcessResult(process.ExitCode == 0, await output, await error);
        }
        catch (Exception ex)
        {
            return new ProcessResult(false, string.Empty, ex.Message);
        }
    }

    private void Load()
    {
        if (!File.Exists(_dataFile)) return;
        try
        {
            _backup = JsonSerializer.Deserialize<PowerBackup>(File.ReadAllText(_dataFile)) ?? new();
        }
        catch
        {
            File.Move(_dataFile, _dataFile + $".corrupt-{DateTime.Now:yyyyMMddHHmmss}", true);
            _backup = new();
        }
    }

    private void Save()
    {
        var temp = _dataFile + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_backup, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, _dataFile, true);
    }

    private void Log(string action, string target, bool success, string? error)
    {
        try
        {
            File.AppendAllText(_logFile, JsonSerializer.Serialize(new
            {
                timestamp = DateTimeOffset.Now,
                action,
                target,
                success,
                error
            }) + Environment.NewLine);
        }
        catch { }
    }

    private sealed record ProcessResult(bool Success, string Output, string Error);
    private sealed class PowerBackup
    {
        public string? ActiveScheme { get; set; }
        public Dictionary<string, PowerSettingBackup> Settings { get; set; } = new();
    }
    private sealed class PowerSettingBackup
    {
        public string Scheme { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public int AcValue { get; set; }
    }
}
