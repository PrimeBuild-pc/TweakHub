using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using TweakHub.Localization;
using TweakHub.Models;

namespace TweakHub.Services;

/// <summary>A registry value of a Registry step; Binary values are base64.</summary>
public sealed record PlaybookRegistryValue(string Key, string Name, string Kind, string Value);

/// <summary>A shareable playbook with the custom tweaks and scripts it references.</summary>
public sealed class PlaybookPackage
{
    public int Version { get; set; } = 1;
    public Playbook Playbook { get; set; } = new();
    public List<CustomRegistryTweak> CustomTweaks { get; set; } = [];
    public List<CustomScript> CustomScripts { get; set; } = [];
}

public sealed record PlaybookImportResult(Playbook Playbook, int CustomTweaks, int CustomScripts);

public sealed class PlaybookService
{
    public static PlaybookService Instance { get; } = new();

    private PlaybookService() { }

    private const string SkippedMarker = "SKIPPED";
    private const string AlreadyDoneMarker = "ALREADY";

    /// <summary>Hash of everything a playbook will do, to check after the elevated restart that it is what the user confirmed.</summary>
    internal static string Fingerprint(Playbook playbook, UserDataService? data = null)
    {
        data ??= UserDataService.Instance;
        var references = playbook.Steps.Select(step => step.ReferenceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // The custom scripts and tweaks it points to are part of what runs.
        var content = new
        {
            playbook,
            scripts = data.LoadCustomScripts().Where(script => references.Contains(script.Id))
                .Select(script => new { script.Id, script.Language, script.RequiresAdministrator, script.Content }),
            tweaks = data.LoadCustomTweaks().Where(tweak => references.Contains($"custom:{tweak.Id}"))
                .Select(tweak => new { tweak.Id, tweak.RegistryPath, tweak.RegistryKey, tweak.ValueType, tweak.Data })
        };
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(content)));
    }

    internal static string BuiltInScriptName(string id) => L.Get($"Scripts:{id.Replace('-', '_')}_Name");

    private static bool IsBuiltInScript(PlaybookStep step) => step.ReferenceId.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase);

    internal static readonly string[] WindowsUpdatePresets =
        [nameof(WindowsUpdatePreset.Default), nameof(WindowsUpdatePreset.Disabled), nameof(WindowsUpdatePreset.Security)];

    // Registry steps may only carry the per-device values TweakHub itself writes (interrupt policy and USB power saving).
    private static readonly Regex AllowedRegistryKey = new(
        @"^HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Enum\\(?<id>[^\\]+\\[^\\]+\\[^\\]+)\\Device Parameters(?<sub>\\Interrupt Management\\(MessageSignaledInterruptProperties|Affinity Policy))?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    internal static bool IsAllowedRegistryValue(string key, string name)
    {
        var match = AllowedRegistryKey.Match(key);
        if (!match.Success) return false;
        var sub = match.Groups["sub"].Value;
        return sub.EndsWith("MessageSignaledInterruptProperties", StringComparison.OrdinalIgnoreCase) ? name == "MSISupported"
            : sub.EndsWith("Affinity Policy", StringComparison.OrdinalIgnoreCase) ? name is "DevicePriority" or "DevicePolicy" or "AssignmentSetOverride"
            : name is "SelectiveSuspendEnabled" or "EnhancedPowerManagementEnabled";
    }

    internal static string DeviceIdOf(string key) => AllowedRegistryKey.Match(key).Groups["id"].Value;

    public PlaybookPreflight Preflight(Playbook playbook)
    {
        var lines = new List<string>();
        var errors = new List<string>();
        var administratorSteps = 0;
        var (builtIn, custom, scripts) = LoadReferences();

        for (var index = 0; index < playbook.Steps.Count; index++)
        {
            var step = playbook.Steps[index];
            var prefix = $"{index + 1}. ";
            var notes = new List<string>();
            switch (step.Type)
            {
                case PlaybookStepType.Tweak:
                    var builtInTweak = step.ReferenceId.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase)
                        ? builtIn.GetValueOrDefault(step.ReferenceId[8..])
                        : null;
                    var customTweak = step.ReferenceId.StartsWith("custom:", StringComparison.OrdinalIgnoreCase)
                        ? custom.GetValueOrDefault(step.ReferenceId[7..])
                        : null;
                    if (builtInTweak?.RiskLevel >= 3 || customTweak?.RegistryPath.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase) == true
                        || customTweak?.RegistryPath.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase) == true)
                        notes.Add(L.Get("Scripts:Administrator"));
                    if (builtInTweak?.RequiresRestart == true) notes.Add(L.Get("UI:RestartRequired"));
                    if (builtInTweak == null && customTweak == null) errors.Add(L.Format("Scripts:PlaybookMissingReference", step.Name));
                    break;
                case PlaybookStepType.Winget:
                    try { UserDataService.ValidateWingetId(step.WingetId); }
                    catch { errors.Add(L.Format("Scripts:PlaybookInvalidWinget", step.WingetId)); }
                    break;
                case PlaybookStepType.Script when IsBuiltInScript(step):
                    if (BuiltInScripts.All.GetValueOrDefault(step.ReferenceId[8..]).Administrator) notes.Add(L.Get("Scripts:Administrator"));
                    break;
                case PlaybookStepType.Script:
                    var script = scripts.GetValueOrDefault(step.ReferenceId);
                    if (script?.RequiresAdministrator == true) notes.Add(L.Get("Scripts:Administrator"));
                    if (script == null) errors.Add(L.Format("Scripts:PlaybookMissingReference", step.Name));
                    break;
                case PlaybookStepType.Device or PlaybookStepType.Registry or PlaybookStepType.TweakCheck or PlaybookStepType.SecurityFix
                    or PlaybookStepType.WindowsUpdate or PlaybookStepType.Service or PlaybookStepType.Dns:
                    notes.Add(L.Get("Scripts:Administrator"));
                    break;
            }
            try { ValidateStepData(step); }
            catch (InvalidDataException) { errors.Add(L.Format("Scripts:PlaybookInvalidData", step.Name)); }
            if (notes.Contains(L.Get("Scripts:Administrator"))) administratorSteps++;
            lines.Add(prefix + step.Summary + (notes.Count > 0 ? $" [{string.Join(", ", notes)}]" : string.Empty));
        }
        if (playbook.Steps.Count == 0) errors.Add(L.Get("Scripts:PlaybookEmpty"));
        return new(lines, errors, administratorSteps);
    }

    /// <summary>Checks the payload of the step types that carry one; imported playbooks pass through here too.</summary>
    public static void ValidateStepData(PlaybookStep step)
    {
        static InvalidDataException Invalid() => new(L.Get("Scripts:PlaybookInvalid"));
        if (step.Data.Length > 256 * 1024) throw Invalid();
        switch (step.Type)
        {
            case PlaybookStepType.Device:
                if (step.ReferenceId.Length > 400 || step.ReferenceId.Contains('\n') || step.Data.Length > 100) throw Invalid();
                break;
            case PlaybookStepType.Registry:
                var values = ParseRegistryData(step.Data);
                if (values.Count is 0 or > 16 || values.Any(value => DeviceIdOf(value.Key) != step.ReferenceId)) throw Invalid();
                break;
            case PlaybookStepType.PowerPlan:
                if (!Guid.TryParse(step.ReferenceId, out _) || ParsePowerData(step.Data).Count is 0 or > 2000) throw Invalid();
                break;
            case PlaybookStepType.Script when IsBuiltInScript(step):
                if (!BuiltInScripts.All.GetValueOrDefault(step.ReferenceId[8..]).Playbook) throw Invalid();
                break;
            case PlaybookStepType.TweakCheck:
                if (!TweakCheckService.Ids.Contains(step.ReferenceId, StringComparer.Ordinal)) throw Invalid();
                break;
            case PlaybookStepType.SecurityFix:
                if (!SecurityService.FixableIds.Contains(step.ReferenceId, StringComparer.Ordinal)) throw Invalid();
                break;
            case PlaybookStepType.WindowsUpdate:
                if (!WindowsUpdatePresets.Contains(step.ReferenceId, StringComparer.Ordinal)) throw Invalid();
                break;
            case PlaybookStepType.Service:
                if (!OptionalServicesService.CanManage(step.ReferenceId) || step.Data is not ("3" or "4")) throw Invalid();
                break;
            case PlaybookStepType.Dns:
                if (step.Data.Length > 400 || !PrivacyService.TryParseServers(step.Data, out _, max: 8)) throw Invalid();
                break;
        }
    }

    internal static List<PlaybookRegistryValue> ParseRegistryData(string data)
    {
        List<PlaybookRegistryValue>? values;
        try { values = JsonSerializer.Deserialize<List<PlaybookRegistryValue>>(data); }
        catch (JsonException) { throw new InvalidDataException(L.Get("Scripts:PlaybookInvalid")); }
        if (values is null || values.Any(value => value is null || !IsAllowedRegistryValue(value.Key, value.Name) || ToChange(value) is null))
            throw new InvalidDataException(L.Get("Scripts:PlaybookInvalid"));
        return values;
    }

    private static List<PowerSettingValue> ParsePowerData(string data)
    {
        try { return JsonSerializer.Deserialize<List<PowerSettingValue>>(data) ?? []; }
        catch (JsonException) { throw new InvalidDataException(L.Get("Scripts:PlaybookInvalid")); }
    }

    private static RegistryValueChange? ToChange(PlaybookRegistryValue value)
    {
        try
        {
            return value.Kind switch
            {
                "DWord" => new RegistryValueChange(value.Key, value.Name, int.Parse(value.Value, System.Globalization.CultureInfo.InvariantCulture)),
                // Affinity masks are 8 bytes; anything longer is not something TweakHub wrote.
                "Binary" when Convert.FromBase64String(value.Value) is { Length: <= 16 } bytes =>
                    new RegistryValueChange(value.Key, value.Name, bytes, RegistryValueKind.Binary),
                _ => null
            };
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            return null;
        }
    }

    // Capture

    /// <summary>
    /// Turns this PC's configuration into playbook steps: power plan, active tweaks, interrupt and USB power settings,
    /// disabled devices, then favorite apps (unchecked: installing is a bigger decision).
    /// </summary>
    public async Task<List<PlaybookStep>> CaptureAsync()
    {
        var steps = new List<PlaybookStep>();
        if (PowerPlanService.ActiveScheme() is { } active && PowerPlanService.GetPlans().FirstOrDefault(plan => plan.Id == active) is { } plan)
        {
            var values = await Task.Run(() => PowerPlanService.ReadValues(active));
            steps.Add(new PlaybookStep { Type = PlaybookStepType.PowerPlan, ReferenceId = plan.Id.ToString(), Name = Trim(plan.Name), Data = JsonSerializer.Serialize(values) });
        }

        await Preload.Get(Preload.TweakStates, TweakService.Instance.LoadWithStatesAsync);
        // Power tweaks change the active plan: after the PowerPlan step they would overwrite the captured values
        // (or switch to High performance), and the step already carries every setting they touch.
        var hasPlan = steps.Count > 0;
        steps.AddRange(TweakService.Instance.TweakCategories.SelectMany(category => category.Tweaks)
            .Where(tweak => tweak.IsEnabled && !(hasPlan && tweak.Type == TweakType.Power && tweak.Id != "disable_hibernation"))
            .Select(tweak => new PlaybookStep { Type = PlaybookStepType.Tweak, ReferenceId = $"builtin:{tweak.Id}", Name = Trim(tweak.Name) }));
        steps.AddRange(UserDataService.Instance.LoadCustomTweaks().Where(IsApplied)
            .Select(tweak => new PlaybookStep { Type = PlaybookStepType.Tweak, ReferenceId = $"custom:{tweak.Id}", Name = Trim(tweak.Name) }));

        IReadOnlyList<PnpDeviceInfo> devices;
        try { devices = await Preload.Get(Preload.Devices, DeviceService.Instance.GetDevicesAsync); }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException) { devices = []; }
        var names = devices.GroupBy(device => device.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().DisplayName, StringComparer.OrdinalIgnoreCase);

        foreach (var group in RegistryService.Instance.BackedUpValues.Where(value => IsAllowedRegistryValue(value.KeyPath, value.ValueName))
                     .GroupBy(value => DeviceIdOf(value.KeyPath), StringComparer.OrdinalIgnoreCase))
        {
            var values = group.Select(value => Capture(value.KeyPath, value.ValueName)).OfType<PlaybookRegistryValue>().ToList();
            if (values.Count > 0)
                steps.Add(new PlaybookStep { Type = PlaybookStepType.Registry, ReferenceId = group.Key, Name = Trim(names.GetValueOrDefault(group.Key, group.Key)), Data = JsonSerializer.Serialize(values) });
        }

        // Network adapters and devices the rules say to keep are proposed unchecked: on another PC they may be the only way online.
        steps.AddRange(devices.Where(device => device.IsDisabled)
            .Select(device => new PlaybookStep
            {
                Type = PlaybookStepType.Device, ReferenceId = device.Id, Name = Trim(device.DisplayName), Data = device.Class,
                TargetEnabled = false, IsIncluded = device.Class != "Net" && device.Rule?.Advice != DeviceAdvice.Avoid
            }));

        steps.AddRange(await CaptureSystemStepsAsync());

        steps.AddRange(ShortcutService.Instance.ExternalTools.Where(tool => tool.IsFavorite && tool.WingetId.Length > 0)
            .Select(tool => new PlaybookStep { Type = PlaybookStepType.Winget, Name = Trim(tool.Name), WingetId = tool.WingetId, IsIncluded = false }));
        return steps;
    }

    private static string Trim(string name) => name.Length > 100 ? name[..100] : name;

    /// <summary>
    /// Settings TweakHub changed on this PC outside the registry tweaks: Tweak Check repairs it can undo, optional services
    /// it switched, the Windows Update preset and DNS servers. A source that cannot be read is left out, not failed.
    /// </summary>
    private static async Task<List<PlaybookStep>> CaptureSystemStepsAsync()
    {
        var steps = new List<PlaybookStep>();
        static async Task Try(Func<Task> capture)
        {
            try { await capture(); }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or JsonException) { }
        }

        await Try(async () => steps.AddRange((await TweakCheckService.Instance.ScanAsync(elevated: false)).Where(check => check.CanUndo)
            .Select(check => new PlaybookStep { Type = PlaybookStepType.TweakCheck, ReferenceId = check.Id, Name = Trim(check.Title) })));
        await Try(async () => steps.AddRange((await OptionalServicesService.Instance.ScanAsync())
            .Where(service => service.Managed && service.CanChange && service.Start is 3 or 4)
            .Select(service => new PlaybookStep
            {
                Type = PlaybookStepType.Service, ReferenceId = service.Name, Name = service.Name,
                Data = service.Start.ToString(System.Globalization.CultureInfo.InvariantCulture)
            })));
        // Default is what Windows does anyway; only a chosen restriction is worth carrying to another PC.
        if (await WindowsUpdatePresetService.Instance.DetectAsync() is var preset and (WindowsUpdatePreset.Disabled or WindowsUpdatePreset.Security))
            steps.Add(new PlaybookStep { Type = PlaybookStepType.WindowsUpdate, ReferenceId = preset.ToString(), Name = Trim(L.Get($"Tweaks:WindowsUpdatePreset{preset}")) });
        if (PrivacyService.HasDnsBackup)
            await Try(async () =>
            {
                var adapter = (await PrivacyService.Instance.GetDnsAdaptersAsync()).FirstOrDefault(adapter => adapter.StaticServers.Length > 0);
                if (adapter is not null && PrivacyService.TryParseServers($"{adapter.StaticServers},{adapter.StaticServers6}", out var servers, max: 8))
                    steps.Add(DnsStep(servers));
            });
        return steps;
    }

    /// <summary>A DNS step named after the preset it matches, or after the servers themselves.</summary>
    internal static PlaybookStep DnsStep(string[] servers)
    {
        var preset = PrivacyService.DnsPresets.FirstOrDefault(preset => preset.Servers.ToHashSet().SetEquals(servers))?.Id ?? "Custom";
        return new PlaybookStep
        {
            Type = PlaybookStepType.Dns, ReferenceId = preset, Name = Trim(L.Get($"Security:Dns{preset}")), Data = string.Join(",", servers)
        };
    }

    private static bool IsApplied(CustomRegistryTweak tweak)
    {
        try { return RegistryService.Instance.IsValueSet(tweak.RegistryPath, tweak.RegistryKey, RegistryService.ParseData(tweak.ValueType, tweak.Data, out _)); }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidDataException) { return false; }
    }

    private static PlaybookRegistryValue? Capture(string key, string name) => RegistryService.Instance.GetRegistryValue(key, name) switch
    {
        int number => new PlaybookRegistryValue(key, name, "DWord", number.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        byte[] bytes => new PlaybookRegistryValue(key, name, "Binary", Convert.ToBase64String(bytes)),
        _ => null
    };

    // Import and export

    public void Export(Playbook playbook, string path, UserDataService? data = null)
    {
        data ??= UserDataService.Instance;
        var tweakIds = playbook.Steps.Where(step => step.ReferenceId.StartsWith("custom:", StringComparison.OrdinalIgnoreCase))
            .Select(step => step.ReferenceId[7..]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var scriptIds = playbook.Steps.Where(step => step.Type == PlaybookStepType.Script).Select(step => step.ReferenceId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        JsonStore.Save(path, new PlaybookPackage
        {
            Playbook = playbook,
            CustomTweaks = data.LoadCustomTweaks().Where(tweak => tweakIds.Contains(tweak.Id)).ToList(),
            CustomScripts = data.LoadCustomScripts().Where(script => scriptIds.Contains(script.Id)).ToList()
        });
    }

    /// <summary>Reads and validates a package without saving anything, so its code can be reviewed before importing.</summary>
    public PlaybookPackage Inspect(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists || file.Length > 10 * 1024 * 1024) throw new InvalidDataException(L.Get("Scripts:PlaybookInvalid"));
        PlaybookPackage? package;
        try { package = JsonSerializer.Deserialize<PlaybookPackage>(File.ReadAllText(path)); }
        catch (JsonException) { throw new InvalidDataException(L.Get("Scripts:PlaybookInvalid")); }
        if (package?.Playbook is null || package.Version != 1) throw new InvalidDataException(L.Get("Scripts:PlaybookInvalid"));
        package.CustomTweaks ??= [];
        package.CustomScripts ??= [];
        UserDataService.ValidatePlaybook(package.Playbook);
        foreach (var tweak in package.CustomTweaks) UserDataService.ValidateCustomTweak(tweak);
        foreach (var script in package.CustomScripts) UserDataService.ValidateCustomScript(script);
        return package;
    }

    /// <summary>
    /// The code a package would add to this PC (custom scripts and registry tweaks it does not have yet), as text to show
    /// before importing; empty when the package adds none. Imported scripts run with the playbook, so they must be seen.
    /// </summary>
    public static string DescribeNewCode(PlaybookPackage package, UserDataService? data = null)
    {
        data ??= UserDataService.Instance;
        var tweaks = data.LoadCustomTweaks().Select(tweak => tweak.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var scripts = data.LoadCustomScripts().Select(script => script.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var text = new System.Text.StringBuilder();
        foreach (var script in package.CustomScripts.Where(script => !scripts.Contains(script.Id)))
        {
            text.AppendLine($"■ {script.Name} ({script.Language}{(script.RequiresAdministrator ? ", " + L.Get("Scripts:Administrator") : string.Empty)})");
            foreach (var line in script.Content.Split('\n')) text.AppendLine("    " + line.TrimEnd('\r'));
        }
        foreach (var tweak in package.CustomTweaks.Where(tweak => !tweaks.Contains(tweak.Id)))
            text.AppendLine($"■ {tweak.Name}: {tweak.RegistryPath}\\{tweak.RegistryKey} = {tweak.Data} ({tweak.ValueType})");
        return text.ToString().TrimEnd();
    }

    /// <summary>Validates everything first, then adds the playbook and only the custom tweaks and scripts this PC does not have.</summary>
    public PlaybookImportResult Import(string path, UserDataService? data = null) => Import(Inspect(path), data);

    /// <summary>Imports the package the user reviewed, not the file again: it can change on disk while the review is open.</summary>
    public PlaybookImportResult Import(PlaybookPackage package, UserDataService? data = null)
    {
        data ??= UserDataService.Instance;
        var tweaks = data.LoadCustomTweaks();
        var newTweaks = package.CustomTweaks.Where(tweak => tweaks.All(existing => !existing.Id.Equals(tweak.Id, StringComparison.OrdinalIgnoreCase))).ToList();
        var scripts = data.LoadCustomScripts().ToList();
        var newScripts = package.CustomScripts.Where(script => scripts.All(existing => !existing.Id.Equals(script.Id, StringComparison.OrdinalIgnoreCase))).ToList();
        var playbooks = data.LoadPlaybooks();
        if (playbooks.Any(existing => existing.Id.Equals(package.Playbook.Id, StringComparison.OrdinalIgnoreCase)))
            package.Playbook.Id = Guid.NewGuid().ToString("N");

        if (newTweaks.Count > 0) data.SaveCustomTweaks(tweaks.Concat(newTweaks));
        if (newScripts.Count > 0) data.SaveCustomScripts(scripts.Concat(newScripts));
        data.SavePlaybooks(playbooks.Append(package.Playbook));
        return new(package.Playbook, newTweaks.Count, newScripts.Count);
    }

    // Execution

    public async Task<PlaybookRunResult> ExecuteAsync(
        Playbook playbook,
        IProgress<string>? progress = null)
    {
        var preflight = Preflight(playbook);
        if (!preflight.CanRun) return new(false, 0, string.Join(Environment.NewLine, preflight.Errors), string.Empty);

        var (builtIn, custom, scripts) = LoadReferences();
        Task<IReadOnlyList<TweakCheck>>? tweakChecks = null;
        Task<SecuritySnapshot>? security = null;
        var lines = new List<string> { $"{DateTimeOffset.Now:O} — {playbook.Name}" };
        var completed = await RunSequentiallyAsync(playbook.Steps, async step =>
        {
            progress?.Report(step.Summary);
            bool success;
            string details = string.Empty;
            try
            {
                success = step.Type switch
                {
                    PlaybookStepType.Tweak => await ExecuteTweak(step, builtIn, custom),
                    PlaybookStepType.Winget => await ToolDownloadService.Instance.InstallWithWinget(new ExternalTool
                    {
                        Name = step.Name,
                        WingetId = step.WingetId
                    }),
                    PlaybookStepType.Script when IsBuiltInScript(step) => await ExecuteBuiltInScript(step, value => details = value),
                    PlaybookStepType.Script => await ExecuteScript(step, scripts, value => details = value),
                    PlaybookStepType.Device => await ExecuteDevice(step, value => details = value),
                    PlaybookStepType.Registry => await ExecuteRegistry(step, value => details = value),
                    PlaybookStepType.PowerPlan => await ExecutePowerPlan(step, value => details = value),
                    PlaybookStepType.TweakCheck => await ExecuteTweakCheck(step, tweakChecks ??= TweakCheckService.Instance.ScanAsync(elevated: true), value => details = value),
                    PlaybookStepType.SecurityFix => await ExecuteSecurityFix(step, security ??= SecurityService.Instance.GetSnapshotAsync(), value => details = value),
                    PlaybookStepType.WindowsUpdate => await ExecuteWindowsUpdate(step, value => details = value),
                    PlaybookStepType.Service => await ExecuteService(step, value => details = value),
                    PlaybookStepType.Dns => await ExecuteDns(step, value => details = value),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                success = false;
                details = ex.Message;
            }
            var status = !success ? "FAILED" : details is SkippedMarker or AlreadyDoneMarker ? "SKIPPED" : "OK";
            if (details == SkippedMarker) details = L.Get("Scripts:PlaybookSkipped");
            else if (details == AlreadyDoneMarker) details = L.Get("Scripts:PlaybookAlreadyDone");
            lines.Add($"[{status}] {step.Summary}{(details.Length > 0 ? Environment.NewLine + details : string.Empty)}");
            return success;
        });

        var logDirectory = Path.Combine(UserDataService.Instance.DataDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);
        var logPath = Path.Combine(logDirectory, $"playbook-{SafeFileName(playbook.Name)}-last.log");
        await File.WriteAllLinesAsync(logPath, lines);
        return new(completed == playbook.Steps.Count, completed, string.Join(Environment.NewLine, lines), logPath);
    }

    internal static async Task<int> RunSequentiallyAsync(
        IReadOnlyList<PlaybookStep> steps,
        Func<PlaybookStep, Task<bool>> execute)
    {
        var completed = 0;
        foreach (var step in steps)
        {
            if (!await execute(step)) break;
            completed++;
        }
        return completed;
    }

    private static (Dictionary<string, PerformanceTweak> BuiltIn, Dictionary<string, CustomRegistryTweak> Custom, Dictionary<string, CustomScript> Scripts) LoadReferences()
    {
        static Dictionary<string, T> ById<T>(IEnumerable<T> items, Func<T, string> id) => items
            .Where(item => !string.IsNullOrWhiteSpace(id(item)))
            .GroupBy(id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        return (BuiltInTweaks(),
            ById(UserDataService.Instance.LoadCustomTweaks(), tweak => tweak.Id),
            ById(UserDataService.Instance.LoadCustomScripts(), script => script.Id));
    }

    private static Dictionary<string, PerformanceTweak> BuiltInTweaks()
    {
        if (TweakService.Instance.TweakCategories.Count == 0) TweakService.Instance.LoadTweaks();
        return TweakService.Instance.TweakCategories.SelectMany(category => category.Tweaks)
            .ToDictionary(tweak => tweak.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<bool> ExecuteTweak(
        PlaybookStep step,
        IReadOnlyDictionary<string, PerformanceTweak> builtIn,
        IReadOnlyDictionary<string, CustomRegistryTweak> custom)
    {
        if (step.ReferenceId.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase))
            return await TweakService.Instance.ApplyTweakAsync(builtIn[step.ReferenceId[8..]], step.TargetEnabled);

        var tweak = custom[step.ReferenceId[7..]];
        if (!step.TargetEnabled)
            return await Task.Run(() => RegistryService.Instance.RestoreRegistryValue(tweak.RegistryPath, tweak.RegistryKey));
        var value = RegistryService.ParseData(tweak.ValueType, tweak.Data, out var kind);
        return await Task.Run(() => RegistryService.Instance.ApplyValueWithBackup(tweak.RegistryPath, tweak.RegistryKey, value, kind));
    }

    private static async Task<bool> ExecuteScript(
        PlaybookStep step,
        IReadOnlyDictionary<string, CustomScript> scripts,
        Action<string> setDetails)
    {
        var result = await PowerShellService.Instance.ExecuteCustomScriptAsync(scripts[step.ReferenceId]);
        setDetails(string.Join(Environment.NewLine, new[] { result.Output.Trim(), result.Error.Trim() }.Where(value => value.Length > 0)));
        return result.Success;
    }

    private static async Task<bool> ExecuteBuiltInScript(PlaybookStep step, Action<string> setDetails)
    {
        var id = step.ReferenceId[8..];
        var (administrator, minutes, _) = BuiltInScripts.All[id];
        var result = await PowerShellService.Instance.ExecuteScriptAsync(BuiltInScripts.Body(id), administrator, TimeSpan.FromMinutes(minutes));
        setDetails(string.Join(Environment.NewLine, new[] { result.Output.Trim(), result.Error.Trim() }.Where(value => value.Length > 0)));
        return result.Success;
    }

    /// <summary>Repairs only what this PC actually has: a check already at the Windows default is skipped.</summary>
    private static async Task<bool> ExecuteTweakCheck(PlaybookStep step, Task<IReadOnlyList<TweakCheck>> scan, Action<string> setDetails)
    {
        var check = (await scan).Single(check => check.Id == step.ReferenceId);
        if (check.Status == TweakCheckStatus.NeedsAdministrator)
        {
            setDetails(check.StatusText);
            return false;
        }
        if (!check.CanRepair)
        {
            setDetails(AlreadyDoneMarker);
            return true;
        }
        var result = await TweakCheckService.Instance.RepairAsync(check);
        setDetails(result.Error.Trim());
        return result.Success;
    }

    private static async Task<bool> ExecuteSecurityFix(PlaybookStep step, Task<SecuritySnapshot> snapshot, Action<string> setDetails)
    {
        var check = (await snapshot).Checks.Single(check => check.Id == step.ReferenceId);
        if (!check.CanFix)
        {
            // A failed check without a fix (e.g. a third-party antivirus TweakHub cannot switch on) is reported, not called correct.
            setDetails(check.Status == CheckStatus.Passed ? AlreadyDoneMarker : check.Detail);
            return check.Status == CheckStatus.Passed;
        }
        var result = await SecurityService.Instance.RunElevatedAsync(check.FixScript!);
        setDetails(result.Error.Trim());
        return result.Success;
    }

    private static async Task<bool> ExecuteWindowsUpdate(PlaybookStep step, Action<string> setDetails)
    {
        var result = await WindowsUpdatePresetService.Instance.ApplyAsync(Enum.Parse<WindowsUpdatePreset>(step.ReferenceId));
        setDetails(result.Error.Trim());
        return result.Success;
    }

    private static async Task<bool> ExecuteService(PlaybookStep step, Action<string> setDetails)
    {
        var service = (await OptionalServicesService.Instance.ScanAsync()).FirstOrDefault(service => service.Name == step.ReferenceId);
        if (service is not { Exists: true })
        {
            setDetails(SkippedMarker);
            return true;
        }
        var success = await OptionalServicesService.Instance.SetModeAsync(service, int.Parse(step.Data, System.Globalization.CultureInfo.InvariantCulture));
        if (!success) setDetails(service.Explanation);
        return success;
    }

    private static async Task<bool> ExecuteDns(PlaybookStep step, Action<string> setDetails)
    {
        var adapters = await PrivacyService.Instance.GetDnsAdaptersAsync();
        if (adapters.Count == 0 || !PrivacyService.TryParseServers(step.Data, out var servers, max: 8))
        {
            setDetails(SkippedMarker);
            return true;
        }
        var result = await PrivacyService.Instance.ApplyDnsAsync(adapters, servers);
        setDetails(result.Error.Trim());
        return result.Success;
    }

    // A device that is not on this PC is skipped, not failed: playbooks move between machines.
    private static async Task<bool> ExecuteDevice(PlaybookStep step, Action<string> setDetails)
    {
        var (success, skipped, error) = await DeviceService.Instance.SetEnabledMatchingAsync(step.ReferenceId, step.Name, step.Data, step.TargetEnabled);
        setDetails(skipped ? SkippedMarker : error);
        if (success && !skipped) Preload.Invalidate(Preload.Devices);
        return success;
    }

    private static async Task<bool> ExecuteRegistry(PlaybookStep step, Action<string> setDetails)
    {
        using var device = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{step.ReferenceId}");
        if (device is null)
        {
            setDetails(SkippedMarker);
            return true;
        }
        var changes = ParseRegistryData(step.Data).Select(value => ToChange(value)!).ToList();
        foreach (var change in changes.Where(change => change.ValueName == "AssignmentSetOverride"))
        {
            var bytes = ((byte[])change.Value!).ToArray();
            Array.Resize(ref bytes, 8);
            if (!InterruptService.CanAssign(BitConverter.ToUInt64(bytes)))
            {
                setDetails(L.Get("Maintenance:AffinitySelectionRequired"));
                return false;
            }
        }
        return await RegistryService.Instance.ApplyValuesWithBackupAsync(changes);
    }

    /// <summary>
    /// Writes the saved values into the plan with the same id, or into a new copy of Balanced (or the active plan).
    /// Never matched by name: another PC's plan with the same name may be a different plan. The copy keeps the saved id,
    /// so running the playbook again updates it instead of adding another copy.
    /// </summary>
    private static async Task<bool> ExecutePowerPlan(PlaybookStep step, Action<string> setDetails)
    {
        var plans = PowerPlanService.GetPlans();
        var savedId = Guid.Parse(step.ReferenceId);
        var target = plans.FirstOrDefault(plan => plan.Id == savedId)?.Id;
        if (target is null && (plans.FirstOrDefault(plan => plan.Id == PowerPlanService.Balanced) ?? plans.FirstOrDefault(plan => plan.Active)) is { } source)
            target = PowerPlanService.Duplicate(source.Id, step.Name, savedId) ?? PowerPlanService.Duplicate(source.Id, step.Name);
        if (target is not { } scheme) return false;

        var known = PowerPlanService.LoadDefinitions().Select(definition => (definition.Subgroup, definition.Id)).ToHashSet();
        var values = ParsePowerData(step.Data).Where(value => known.Contains((value.Subgroup, value.Setting))).ToList();
        var failed = await Task.Run(() => PowerPlanService.WriteValues(scheme, values));
        if (failed.Count > 0)
        {
            var result = await PowerShellService.Instance.ExecuteScriptAsync(PowerPlanService.BuildElevatedWriteScript(scheme, failed), requireAdministrator: true);
            if (!result.Success)
            {
                setDetails(result.Error);
                return false;
            }
        }
        PowerPlanService.Activate(scheme);
        setDetails(L.Format("Scripts:PlaybookPowerApplied", values.Count, ParsePowerData(step.Data).Count - values.Count));
        return true;
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray()).Trim();
        return safe.Length == 0 ? "playbook" : safe;
    }
}
