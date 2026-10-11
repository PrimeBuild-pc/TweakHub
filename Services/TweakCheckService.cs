using System.Text.Json;
using TweakHub.Localization;

namespace TweakHub.Services;

public enum TweakCheckStatus { Ok, Flagged, NeedsAdministrator }

public sealed record TweakCheck(string Id, TweakCheckStatus Status, string Current, IReadOnlyList<RegistryValueChange> Repair,
    string? RepairScript = null, bool RequiresRestart = false)
{
    public string Title => L.Get($"Devices:Check{Id}");
    public string Explanation => L.Get($"Devices:Check{Id}Explain");
    public string StatusText => L.Get($"Devices:CheckStatus{Status}");
    public bool CanRepair => Status == TweakCheckStatus.Flagged && (Repair.Count > 0 || RepairScript != null);
    /// <summary>Every registry value this check can change, so a repair can be undone even after a rescan shows it as fixed.</summary>
    public IReadOnlyList<RegistryValueChange> Managed { get; init; } = [];
    /// <summary>Registry repairs keep a backup, so they can be undone; command repairs (bcdedit, netsh) cannot.</summary>
    public bool CanUndo => Managed.Any(change => RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName));
}

/// <summary>Finds popular tweaks that hurt stability or security (the RepairBadTweaks list) and puts Windows defaults back.</summary>
public sealed class TweakCheckService
{
    public static TweakCheckService Instance { get; } = new();
    private TweakCheckService() { }

    private const string Control = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control";
    private const string Kernel = Control + @"\Session Manager\kernel";
    private const string MemoryManagement = Control + @"\Session Manager\Memory Management";
    private const string Wer = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting";
    private const string KeyboardClass = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\kbdclass\Parameters";
    private const string MouseClass = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\mouclass\Parameters";
    private const string Csrss = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\csrss.exe\PerfOptions";
    private const string Dwm = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Dwm";
    private const string Mouse = @"HKEY_CURRENT_USER\Control Panel\Mouse";
    internal const int DefaultSvcHostSplitThreshold = 0x380000;
    internal static readonly string[] BcdTimerValues = ["useplatformclock", "disabledynamictick", "useplatformtick", "tscsyncpolicy"];
    private static readonly string[] RawMouseThrottleValues = ["RawMouseThrottleEnabled", "RawMouseThrottleForced", "RawMouseThrottleDuration", "RawMouseThrottleLeeway"];

    // bcdedit needs administrator rights; without them the timer check reports NeedsAdministrator.
    private const string ScanScript = """
        $ErrorActionPreference = 'SilentlyContinue'
        $bcd = $null
        $output = & bcdedit.exe /enum '{current}' 2>$null
        if ($LASTEXITCODE -eq 0) { $bcd = @('useplatformclock', 'disabledynamictick', 'useplatformtick', 'tscsyncpolicy' | Where-Object { $output -match "^$_\s" }) }
        [pscustomobject]@{ Bcd = $bcd; BcdRead = $null -ne $bcd; AutoTuning = [string](Get-NetTCPSetting -SettingName Internet).AutoTuningLevelLocal } | ConvertTo-Json -Compress
        """;

    public async Task<IReadOnlyList<TweakCheck>> ScanAsync(bool elevated)
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(ScanScript, elevated, TimeSpan.FromMinutes(1));
        if (!result.Success) throw new InvalidOperationException(result.Error);
        using var document = JsonDocument.Parse(result.Output);
        var root = document.RootElement;
        var bcd = JsonRead.Bool(root, "BcdRead") ? JsonRead.Texts(root, "Bcd") : null;
        return Evaluate(name => RegistryService.Instance.GetRegistryValue(name.Key, name.Value), bcd, JsonRead.Text(root, "AutoTuning"));
    }

    /// <summary>Pure evaluation so the rules can be tested without touching the registry.</summary>
    internal static List<TweakCheck> Evaluate(Func<KeyValuePair<string, string>, object?> read, IReadOnlyList<string>? bcd, string autoTuning)
    {
        object? Value(string key, string name) => read(new(key, name));
        bool Exists(string key, string name) => Value(key, name) is not null;
        RegistryValueChange Delete(string key, string name) => new(key, name, null);
        List<RegistryValueChange> DeleteExisting(string key, params string[] names) =>
            names.Where(name => Exists(key, name)).Select(name => Delete(key, name)).ToList();
        TweakCheck Check(string id, IReadOnlyList<RegistryValueChange> repair, string current, bool restart = false) =>
            new(id, repair.Count > 0 ? TweakCheckStatus.Flagged : TweakCheckStatus.Ok, current, repair, RequiresRestart: restart);
        string Show(string key, params string[] names) => string.Join(", ",
            names.Where(name => Exists(key, name)).Select(name => $"{name}={Value(key, name)}"));

        var checks = new List<TweakCheck>
        {
            bcd is null
                ? new("BcdTimers", TweakCheckStatus.NeedsAdministrator, "", [])
                : new("BcdTimers", bcd.Count > 0 ? TweakCheckStatus.Flagged : TweakCheckStatus.Ok, string.Join(", ", bcd), [],
                    string.Join("\n", bcd.Where(BcdTimerValues.Contains).Select(value => $"& bcdedit.exe /deletevalue '{{current}}' {value}")),
                    RequiresRestart: true),
            Check("Mitigations", DeleteExisting(MemoryManagement, "FeatureSettingsOverride", "FeatureSettingsOverrideMask"),
                Show(MemoryManagement, "FeatureSettingsOverride", "FeatureSettingsOverrideMask"), restart: true),
            autoTuning.Length == 0 || autoTuning.Equals("Normal", StringComparison.OrdinalIgnoreCase)
                ? new("AutoTuning", TweakCheckStatus.Ok, autoTuning, [])
                : new("AutoTuning", TweakCheckStatus.Flagged, autoTuning, [], "& netsh.exe interface tcp set global autotuninglevel=normal"),
            Check("SvcHostSplit",
                Value(Control, "SvcHostSplitThresholdInKB") is int split && split != DefaultSvcHostSplitThreshold
                    ? [new(Control, "SvcHostSplitThresholdInKB", DefaultSvcHostSplitThreshold)] : [],
                Show(Control, "SvcHostSplitThresholdInKB"), restart: true),
            Check("ThreadDpc", DeleteExisting(Kernel, "ThreadDpcEnable"), Show(Kernel, "ThreadDpcEnable"), restart: true),
            Check("ErrorReporting", Value(Wer, "Disabled") is int and 1 ? [Delete(Wer, "Disabled")] : [], Show(Wer, "Disabled")),
            Check("InputQueues", [.. DeleteExisting(KeyboardClass, "KeyboardDataQueueSize"), .. DeleteExisting(MouseClass, "MouseDataQueueSize")],
                string.Join(", ", new[] { Show(KeyboardClass, "KeyboardDataQueueSize"), Show(MouseClass, "MouseDataQueueSize") }.Where(text => text.Length > 0)),
                restart: true),
            Check("CsrssPriority", DeleteExisting(Csrss, "CpuPriorityClass", "IoPriority"), Show(Csrss, "CpuPriorityClass", "IoPriority"), restart: true),
            Check("Mpo", DeleteExisting(Dwm, "OverlayTestMode"), Show(Dwm, "OverlayTestMode"), restart: true),
            Check("MemoryManagement",
                [
                    .. Value(MemoryManagement, "DisablePagingExecutive") is int and 1 ? [new RegistryValueChange(MemoryManagement, "DisablePagingExecutive", 0)] : Array.Empty<RegistryValueChange>(),
                    .. Value(MemoryManagement, "LargeSystemCache") is int and 1 ? [new RegistryValueChange(MemoryManagement, "LargeSystemCache", 0)] : Array.Empty<RegistryValueChange>(),
                    .. DeleteExisting(MemoryManagement, "IoPageLockLimit", "CacheUnmapBehindLengthInMB")
                ],
                Show(MemoryManagement, "DisablePagingExecutive", "LargeSystemCache", "IoPageLockLimit", "CacheUnmapBehindLengthInMB"), restart: true),
            Check("RawMouseThrottle", DeleteExisting(Mouse, RawMouseThrottleValues), Show(Mouse, RawMouseThrottleValues)),
            Check("GlobalTimer", DeleteExisting(Kernel, "GlobalTimerResolutionRequests"), Show(Kernel, "GlobalTimerResolutionRequests"), restart: true)
        };
        return checks.Select(check => check with { Managed = ManagedValues(check.Id) }).ToList();
    }

    private static RegistryValueChange[] ManagedValues(string id)
    {
        static RegistryValueChange[] Values(string key, params string[] names) => names.Select(name => new RegistryValueChange(key, name, null)).ToArray();
        return id switch
        {
            "Mitigations" => Values(MemoryManagement, "FeatureSettingsOverride", "FeatureSettingsOverrideMask"),
            "SvcHostSplit" => Values(Control, "SvcHostSplitThresholdInKB"),
            "ThreadDpc" => Values(Kernel, "ThreadDpcEnable"),
            "ErrorReporting" => Values(Wer, "Disabled"),
            "InputQueues" => [.. Values(KeyboardClass, "KeyboardDataQueueSize"), .. Values(MouseClass, "MouseDataQueueSize")],
            "CsrssPriority" => Values(Csrss, "CpuPriorityClass", "IoPriority"),
            "Mpo" => Values(Dwm, "OverlayTestMode"),
            "MemoryManagement" => Values(MemoryManagement, "DisablePagingExecutive", "LargeSystemCache", "IoPageLockLimit", "CacheUnmapBehindLengthInMB"),
            "RawMouseThrottle" => Values(Mouse, RawMouseThrottleValues),
            "GlobalTimer" => Values(Kernel, "GlobalTimerResolutionRequests"),
            _ => []
        };
    }

    public async Task<PowerShellResult> RepairAsync(TweakCheck check)
    {
        if (check.Repair.Count > 0)
        {
            var success = await RegistryService.Instance.ApplyValuesWithBackupAsync(check.Repair.ToList());
            return new PowerShellResult { Success = success, Error = success ? string.Empty : L.Get("Devices:RepairVerifyFailed") };
        }
        return await PowerShellService.Instance.ExecuteScriptAsync(check.RepairScript ?? string.Empty, requireAdministrator: true, TimeSpan.FromMinutes(2));
    }

    public Task<bool> UndoAsync(TweakCheck check) =>
        RegistryService.Instance.RestoreValuesAsync(check.Managed
            .Where(change => RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName)).ToList());
}
