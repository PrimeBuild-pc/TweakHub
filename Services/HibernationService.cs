using System.IO;
using System.Runtime.InteropServices;
using TweakHub.Localization;

namespace TweakHub.Services;

/// <summary>Changes hibernation through powercfg, never by writing HibernateEnabled directly.</summary>
public sealed class HibernationService
{
    internal const string PowerKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power";
    public static HibernationService Instance { get; } = new(AppDataPath.BasePath);
    private readonly string _path;
    private readonly Func<State?> _read;
    private readonly Func<string, Task<PowerShellResult>> _run;
    internal HibernationService(string dataPath, Func<State?>? read = null, Func<string, Task<PowerShellResult>>? run = null)
    {
        _path = Path.Combine(dataPath, "hibernation-backup.json");
        _read = read ?? ReadState;
        _run = run ?? (script => PowerShellService.Instance.ExecuteScriptAsync(script, requireAdministrator: true, timeout: TimeSpan.FromMinutes(2)));
    }
    public bool HasBackup => Load().Original is not null;
    private Backup Load() => JsonStore.Load<Backup>(_path);

    internal sealed record State(int Enabled, int? Type, int? Size);
    private sealed class Backup { public State? Original { get; set; } }

    [DllImport("powrprof.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool GetPwrCapabilities([Out] byte[] capabilities);

    internal static State? ReadState()
    {
        var registry = RegistryService.Instance;
        if (registry.GetRegistryValue(PowerKey, "HibernateEnabled") is not int enabled) return null;
        // SYSTEM_POWER_CAPABILITIES.HiberFileType at byte 22 reports the actual file type, including Windows defaults.
        var capabilities = new byte[76];
        int? type = enabled == 1 && GetPwrCapabilities(capabilities) ? capabilities[22] : null;
        return new(enabled, type, registry.GetRegistryValue(PowerKey, "HiberFileSizePercent") as int?);
    }

    internal static bool CanRestore(State state) => state.Enabled == 0
        || state.Enabled == 1 && state.Type is 1 or 2 && state.Size is >= 0 and <= 100
            && (state.Type != 1 || state.Size == 0);

    public string UnavailableReason => HasBackup || _read() is { Enabled: 1 } state && CanRestore(state)
        ? string.Empty : L.Get("Maintenance:HibernationUnavailable");
    public bool IsDisabled => _read()?.Enabled == 0;

    internal static string BuildScript(State? restore)
    {
        if (restore is not null && !CanRestore(restore)) throw new ArgumentException(nameof(restore));
        var commands = restore is null || restore.Enabled == 0
            ? new[] { "powercfg.exe /hibernate off" }
            : restore.Type == 1
                ? new[] { "powercfg.exe /hibernate on", "powercfg.exe /hibernate /size 0", "powercfg.exe /hibernate /type reduced" }
                : new[] { "powercfg.exe /hibernate on", "powercfg.exe /hibernate /type full", $"powercfg.exe /hibernate /size {restore.Size}" };
        return "$ErrorActionPreference = 'Stop'\n" + string.Join("\n", commands.Select(command =>
            $"& {command}\nif ($LASTEXITCODE -ne 0) {{ throw 'powercfg failed; the original configuration remains backed up.' }}"));
    }

    public async Task<bool> SetDisabledAsync(bool disable)
    {
        var backup = Load();
        if (disable && backup.Original is null)
        {
            var original = _read();
            if (original is null || !CanRestore(original)) return false;
            if (original.Enabled == 0) return true;
            backup.Original = original;
            JsonStore.Save(_path, backup); // Save before powercfg, including a failed or cancelled elevation.
        }
        if (!disable && backup.Original is null) return false;
        var result = await _run(BuildScript(disable ? null : backup.Original));
        var current = _read();
        var verified = result.Success && (disable ? current?.Enabled == 0 : current == backup.Original);
        if (verified && !disable) JsonStore.Save(_path, new Backup());
        return verified;
    }
}
