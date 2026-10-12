using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace TweakHub.Services;

public sealed record PowerScheme(string Guid, string Name, bool Active);

public sealed record PowerTweakStates(bool MaximumProcessorState, bool NoCoreParking, bool PerformancePlan);

public class PowerService
{
    internal const string HighPerformanceScheme = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private const string UltimatePerformanceScheme = "e9a42b02-d5df-448d-aa00-03f14749eb61";
    private readonly string _dataFile;
    private readonly string _logFile;
    private PowerBackup _backup = new();

    public static PowerService Instance { get; } = new();

    private PowerService() : this(AppDataPath.BasePath) { }

    internal PowerService(string dataPath)
    {
        Directory.CreateDirectory(dataPath);
        _dataFile = Path.Combine(dataPath, "power-backup.json");
        _logFile = Path.Combine(dataPath, "operations.jsonl");
        Load();
    }

    public Task<bool> ApplyProcessorSettingAsync(string id, string settingAlias, int targetAcValue) =>
        ApplySettingAsync(id, "SUB_PROCESSOR", settingAlias, targetAcValue);

    public Task<bool> RestoreProcessorSettingAsync(string id) => RestoreSettingAsync(id);

    /// <summary>Sets the AC value of a power setting in the active plan, keeping the original for <see cref="RestoreSettingAsync"/>.</summary>
    public async Task<bool> ApplySettingAsync(string id, string subgroup, string settingAlias, int targetAcValue)
    {
        var scheme = ActiveScheme();
        if (scheme is null) return false;

        if (!_backup.Settings.ContainsKey(id))
        {
            var original = GetAcValue(scheme, subgroup, settingAlias);
            if (original is null) return false;
            _backup.Settings[id] = new PowerSettingBackup { Scheme = scheme, Subgroup = subgroup, Alias = settingAlias, AcValue = original.Value };
            Save();
        }

        var result = await RunMutationAsync("/setacvalueindex", scheme, subgroup, settingAlias, targetAcValue.ToString(CultureInfo.InvariantCulture));
        if (result.Success) result = await RunMutationAsync("/setactive", scheme);
        var verified = result.Success && GetAcValue(scheme, subgroup, settingAlias) == targetAcValue;
        Log("power-apply", id, verified, verified ? null : result.Error);
        return verified;
    }

    public int? GetActiveSetting(string subgroup, string settingAlias) =>
        ActiveScheme() is { } scheme ? GetAcValue(scheme, subgroup, settingAlias) : null;

    public async Task<bool> RestoreSettingAsync(string id)
    {
        if (!_backup.Settings.TryGetValue(id, out var backup)) return false;

        var result = await RunMutationAsync(
            "/setacvalueindex",
            backup.Scheme,
            backup.Subgroup,
            backup.Alias,
            backup.AcValue.ToString(CultureInfo.InvariantCulture));
        if (result.Success) result = await RunMutationAsync("/setactive", backup.Scheme);
        var verified = result.Success && GetAcValue(backup.Scheme, backup.Subgroup, backup.Alias) == backup.AcValue;
        if (verified)
        {
            _backup.Settings.Remove(id);
            Save();
        }
        Log("power-restore", id, verified, verified ? null : result.Error);
        return verified;
    }

    public PowerTweakStates GetTweakStates()
    {
        var scheme = ActiveScheme();
        if (scheme is null) return new(false, false, false);
        return new(
            GetAcValue(scheme, "SUB_PROCESSOR", "PROCTHROTTLEMIN") == 100,
            GetAcValue(scheme, "SUB_PROCESSOR", "CPMINCORES") == 100,
            IsPerformancePlan(scheme));
    }

    /// <summary>The plan the user picked; otherwise any Windows high or ultimate performance plan counts.</summary>
    private bool IsPerformancePlan(string scheme) => _backup.PreferredScheme is { } preferred
        ? scheme.Equals(preferred, StringComparison.OrdinalIgnoreCase)
        : scheme is HighPerformanceScheme or UltimatePerformanceScheme;

    public string? PreferredScheme => _backup.PreferredScheme;

    public static IReadOnlyList<PowerScheme> GetSchemes() =>
        PowerPlanService.GetPlans().Select(plan => new PowerScheme(plan.Id.ToString(), plan.Name, plan.Active)).ToList();

    public Task<bool> ApplyHighPerformancePlanAsync() => ApplyPerformancePlanAsync(null);

    /// <summary>
    /// Activates <paramref name="schemeGuid"/> (or the saved choice, or Windows High performance) and remembers it.
    /// Debloaters often delete the High performance plan, so it is recreated from the Windows template when missing.
    /// </summary>
    public async Task<bool> ApplyPerformancePlanAsync(string? schemeGuid)
    {
        var target = (schemeGuid ?? _backup.PreferredScheme ?? HighPerformanceScheme).ToLowerInvariant();
        var installed = GetSchemes().Select(scheme => scheme.Guid).ToHashSet();
        if (!installed.Contains(target))
        {
            if (target != HighPerformanceScheme) return false;
            target = await RecreateHighPerformancePlanAsync() ?? string.Empty;
            if (target.Length == 0) return false;
        }

        if (string.IsNullOrWhiteSpace(_backup.ActiveScheme))
        {
            _backup.ActiveScheme = ActiveScheme();
            if (_backup.ActiveScheme is null) return false;
        }
        _backup.PreferredScheme = target;
        Save();

        var result = await RunMutationAsync("/setactive", target);
        var verified = result.Success &&
                       string.Equals(ActiveScheme(), target, StringComparison.OrdinalIgnoreCase);
        Log("power-plan-apply", target, verified, verified ? null : result.Error);
        return verified;
    }

    private async Task<string?> RecreateHighPerformancePlanAsync()
    {
        // Windows keeps the default plans as templates; Ultimate is the fallback when the High performance template is gone too.
        foreach (var template in new[] { HighPerformanceScheme, UltimatePerformanceScheme })
        {
            var copy = Guid.NewGuid().ToString();
            var result = await RunAsync("/duplicatescheme", template, copy);
            if (!result.Success) result = await RunMutationAsync("/duplicatescheme", template, copy);
            if (result.Success && GetSchemes().Any(scheme => scheme.Guid == copy))
            {
                Log("power-plan-recreate", template, true, null);
                return copy;
            }
        }
        Log("power-plan-recreate", HighPerformanceScheme, false, "No power plan template available");
        return null;
    }

    public async Task<bool> RestorePowerPlanAsync()
    {
        if (string.IsNullOrWhiteSpace(_backup.ActiveScheme)) return false;
        var original = _backup.ActiveScheme;
        var result = await RunMutationAsync("/setactive", original);
        var verified = result.Success &&
                       string.Equals(ActiveScheme(), original, StringComparison.OrdinalIgnoreCase);
        if (verified)
        {
            _backup.ActiveScheme = null;
            Save();
        }
        Log("power-plan-restore", original, verified, verified ? null : result.Error);
        return verified;
    }

    public bool HasAnyBackup => !string.IsNullOrWhiteSpace(_backup.ActiveScheme) || _backup.Settings.Count > 0;

    public bool HasBackup(string id) => id == "high_performance_power_plan"
        ? !string.IsNullOrWhiteSpace(_backup.ActiveScheme)
        : _backup.Settings.ContainsKey(id);

    private static string? ActiveScheme() => PowerPlanService.ActiveScheme()?.ToString();

    // powrprof wants GUIDs; saved backups still name processor settings by their powercfg alias.
    private static readonly Dictionary<string, Guid> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SUB_PROCESSOR"] = new("54533251-82be-4824-96c1-47b60b740d00"),
        ["PROCTHROTTLEMIN"] = new("893dee8e-2bef-41e0-89c6-b55d0929964c"),
        ["CPMINCORES"] = new("0cc5b647-c1df-4637-891a-dec35c318583")
    };

    /// <summary>Read through powrprof: parsing <c>powercfg /query</c> breaks on Windows languages other than English.</summary>
    internal static int? GetAcValue(string scheme, string subgroup, string setting) =>
        Guid.TryParse(scheme, out var schemeId) && ToGuid(subgroup) is { } subgroupId && ToGuid(setting) is { } settingId
        && PowerPlanService.ReadValue(schemeId, subgroupId, settingId) is { } value
            ? unchecked((int)value.Ac)
            : null;

    private static Guid? ToGuid(string value) =>
        Aliases.TryGetValue(value, out var id) ? id : Guid.TryParse(value, out id) ? id : null;

    internal static async Task<ProcessResult> RunMutationAsync(params string[] arguments)
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

    private void Load() => _backup = JsonStore.Load<PowerBackup>(_dataFile);

    private void Save() => JsonStore.Save(_dataFile, _backup);

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

    internal sealed record ProcessResult(bool Success, string Output, string Error);
    private sealed class PowerBackup
    {
        public string? ActiveScheme { get; set; }
        public string? PreferredScheme { get; set; }
        public Dictionary<string, PowerSettingBackup> Settings { get; set; } = new();
    }
    private sealed class PowerSettingBackup
    {
        public string Scheme { get; set; } = string.Empty;
        // Older backups only stored processor settings.
        public string Subgroup { get; set; } = "SUB_PROCESSOR";
        public string Alias { get; set; } = string.Empty;
        public int AcValue { get; set; }
    }
}
