using System.IO;
using TweakHub.Localization;

namespace TweakHub.Services;

internal enum WindowsUpdatePreset
{
    Default,
    Disabled,
    Security,
    Custom
}

internal sealed class WindowsUpdatePresetService
{
    private readonly string _scriptPath;
    private readonly string _backupPath;

    public static WindowsUpdatePresetService Instance { get; } = new(
        Path.Combine(AppContext.BaseDirectory, "Scripts", "WindowsUpdatePreset.ps1"),
        Path.Combine(AppDataPath.BasePath, "windows-update-backup.json"));

    private static readonly RegistryValueChange[] PresetRegistryValues =
    [
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUOptions", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUPowerManagement", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdates", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdatesPeriodInDays", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferQualityUpdates", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferQualityUpdatesPeriodInDays", null),
        new(@"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "BranchReadinessLevel", null),
        new(@"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "DeferFeatureUpdatesPeriodInDays", null),
        new(@"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "DeferQualityUpdatesPeriodInDays", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DriverSearching", "DontPromptForWindowsUpdate", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DriverSearching", "DontSearchWindowsUpdate", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DriverSearching", "DriverUpdateWizardWuSearchEnabled", null),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config", "DODownloadMode", null)
    ];

    private static readonly RegistryValueChange[] RelatedTweakRegistryValues =
    [
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "SettingsPageVisibility", null, Microsoft.Win32.RegistryValueKind.String),
        new(@"HKLM\SOFTWARE\Policies\WindowsNotepad", "DisableAIFeatures", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\Device Metadata", "PreventDeviceMetadataFromNetwork", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", null)
    ];

    private static readonly RegistryValueChange[] AllRegistryValues = [.. PresetRegistryValues, .. RelatedTweakRegistryValues];

    internal WindowsUpdatePresetService(string scriptPath, string backupPath)
    {
        _scriptPath = scriptPath;
        _backupPath = backupPath;
    }

    public bool HasBackup => File.Exists(_backupPath) || AllRegistryValues.Any(change =>
        RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName));

    internal bool HasCompleteBackup => File.Exists(_backupPath) || PresetRegistryValues.All(change =>
        RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName));

    public async Task<PowerShellResult> ApplyAsync(WindowsUpdatePreset preset)
    {
        if (preset == WindowsUpdatePreset.Custom) throw new ArgumentOutOfRangeException(nameof(preset));
        RegistryService.Instance.CreateBackupValues(AllRegistryValues);
        return await RunAsync(preset.ToString(), requireAdministrator: true, TimeSpan.FromMinutes(10));
    }

    public async Task<PowerShellResult> RestoreAsync()
    {
        var result = File.Exists(_backupPath)
            ? await RunAsync("Restore", requireAdministrator: true, TimeSpan.FromMinutes(10))
            : new PowerShellResult { Success = true };
        if (!result.Success) return result;

        var hasAllRegistryBackups = PresetRegistryValues.All(change =>
            RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName));
        if (hasAllRegistryBackups && await RegistryService.Instance.RestoreValuesAsync(PresetRegistryValues)) return result;
        return new PowerShellResult { Success = false, Error = L.Get("Tweaks:RestoreFailed"), ExitCode = -1 };
    }

    public async Task<WindowsUpdatePreset> DetectAsync()
    {
        try
        {
            var result = await RunAsync("Status", requireAdministrator: false, TimeSpan.FromSeconds(30));
            if (result.Success)
            {
                var marker = result.Output.Split('\n', StringSplitOptions.TrimEntries)
                    .LastOrDefault(line => line.StartsWith("PRESET:", StringComparison.OrdinalIgnoreCase));
                if (marker != null && Enum.TryParse<WindowsUpdatePreset>(marker[7..], true, out var preset)) return preset;
            }
        }
        catch { }
        return WindowsUpdatePreset.Custom;
    }

    public string GetPreview(WindowsUpdatePreset preset)
    {
        EnsureScriptExists();
        return $"# {L.Get("Tweaks:WindowsUpdateBundledScript")}\n# {L.Format("Tweaks:WindowsUpdateSelectedMode", L.Get($"Tweaks:WindowsUpdatePreset{preset}"))}\n\n" +
               File.ReadAllText(_scriptPath) + $"\n\n# {BuildInvocation(preset.ToString())}";
    }

    private Task<PowerShellResult> RunAsync(string preset, bool requireAdministrator, TimeSpan timeout)
    {
        EnsureScriptExists();
        return PowerShellService.Instance.ExecuteScriptAsync(
            BuildInvocation(preset), requireAdministrator, timeout);
    }

    private string BuildInvocation(string preset)
    {
        static string Quote(string value) => value.Replace("'", "''");
        return $"& '{Quote(_scriptPath)}' -Preset '{preset}' -BackupPath '{Quote(_backupPath)}'";
    }

    private void EnsureScriptExists()
    {
        if (!File.Exists(_scriptPath)) throw new FileNotFoundException(L.Get("Tweaks:WindowsUpdateScriptMissing"), _scriptPath);
    }
}
