using System.IO;
using Microsoft.Win32;
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
    private const string MachineBackupKey = @"SOFTWARE\TweakHub\WindowsUpdate";
    private const string MachineBackupName = "ServiceTaskBackup";
    private readonly string _scriptPath;
    private readonly string _legacyBackupPath;
    private readonly Func<bool> _hasMachineBackup;

    public static WindowsUpdatePresetService Instance { get; } = new(
        Path.Combine(AppContext.BaseDirectory, "Scripts", "WindowsUpdatePreset.ps1"),
        Path.Combine(AppDataPath.BasePath, "windows-update-backup.json"));

    internal static readonly RegistryValueChange[] PresetRegistryValues =
    [
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUOptions", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUPowerManagement", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "SetUpdateNotificationLevel", null),
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "UpdateNotificationLevel", null),
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
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config", "DODownloadMode", null),
        // Security sets this and every preset clears it. SettingsPageVisibility is left out on purpose: the script only
        // removes 'hide:windowsupdate', and backing it up here would let a preset restore undo the Windows AI tweak.
        new(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\Device Metadata", "PreventDeviceMetadataFromNetwork", null)
    ];

    // Home editions report EditionID Core, CoreN, CoreSingleLanguage or CoreCountrySpecific.
    internal static bool IsHomeEditionId(string? editionId) => editionId?.StartsWith("Core", StringComparison.OrdinalIgnoreCase) == true;

    public static bool IsHomeEdition => IsHomeEditionId(
        Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "EditionID", null) as string);

    internal WindowsUpdatePresetService(string scriptPath, string legacyBackupPath, Func<bool>? hasMachineBackup = null)
    {
        _scriptPath = scriptPath;
        _legacyBackupPath = legacyBackupPath;
        _hasMachineBackup = hasMachineBackup ?? HasMachineBackup;
    }

    internal bool HasLegacyBackup => File.Exists(_legacyBackupPath);

    public bool HasBackup => HasLegacyBackup || _hasMachineBackup() || PresetRegistryValues.Any(change =>
        RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName));

    private static bool HasRegistryBackup => PresetRegistryValues.All(change =>
        RegistryService.Instance.HasBackup(change.KeyPath, change.ValueName));

    internal bool HasCompleteBackup => HasRegistryBackup && (_hasMachineBackup() || !HasLegacyBackup);

    internal bool HasAnySnapshot => _hasMachineBackup() || HasLegacyBackup || HasRegistryBackup;

    public async Task<PowerShellResult> ApplyAsync(WindowsUpdatePreset preset)
    {
        if (preset == WindowsUpdatePreset.Custom) throw new ArgumentOutOfRangeException(nameof(preset));
        if (HasLegacyBackup && !_hasMachineBackup()) return LegacyBackupNeedsReview();
        RegistryService.Instance.CreateBackupValues(PresetRegistryValues);
        return await RunAsync(preset.ToString(), requireAdministrator: true, TimeSpan.FromMinutes(10));
    }

    public async Task<PowerShellResult> RestoreAsync()
    {
        var hasMachineBackup = _hasMachineBackup();
        if (!hasMachineBackup && HasLegacyBackup) return LegacyBackupNeedsReview();
        if (!HasRegistryBackup) return RegistryRestoreFailed();

        var result = hasMachineBackup
            ? await RunAsync("Restore", requireAdministrator: true, TimeSpan.FromMinutes(10))
            : new PowerShellResult { Success = true };
        if (!result.Success) return result;

        return await RegistryService.Instance.RestoreValuesAsync(PresetRegistryValues) ? result : RegistryRestoreFailed();
    }

    private static bool HasMachineBackup()
    {
        using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = machine.OpenSubKey(MachineBackupKey);
        return key?.GetValue(MachineBackupName) is string;
    }

    private static PowerShellResult LegacyBackupNeedsReview() =>
        new() { Error = L.Get("Tweaks:WindowsUpdateLegacyBackup"), ExitCode = -1 };

    private static PowerShellResult RegistryRestoreFailed() =>
        new() { Error = L.Get("Tweaks:RestoreFailed"), ExitCode = -1 };

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

    public string GetPreview(WindowsUpdatePreset preset) => preset == WindowsUpdatePreset.Custom
        ? string.Empty
        : $"& {PowerShellService.Literal(_scriptPath)} -Preset '{preset}'";

    private Task<PowerShellResult> RunAsync(string preset, bool requireAdministrator, TimeSpan timeout)
    {
        EnsureScriptExists();
        // Include the bundled code in the bytes checked by the elevated runner.
        var script = File.ReadAllText(_scriptPath);
        return PowerShellService.Instance.ExecuteScriptAsync(
            $"& {{\n{script}\n}} -Preset '{preset}'", requireAdministrator, timeout);
    }

    private void EnsureScriptExists()
    {
        if (!File.Exists(_scriptPath)) throw new FileNotFoundException(L.Get("Tweaks:WindowsUpdateScriptMissing"), _scriptPath);
    }
}
