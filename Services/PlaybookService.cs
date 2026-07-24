using System.IO;
using TweakHub.Localization;
using TweakHub.Models;

namespace TweakHub.Services;

public sealed class PlaybookService
{
    public static PlaybookService Instance { get; } = new();

    private PlaybookService() { }

    public PlaybookPreflight Preflight(Playbook playbook)
    {
        var lines = new List<string>();
        var errors = new List<string>();
        var builtIn = BuiltInTweaks();
        var custom = UserDataService.Instance.LoadCustomTweaks().Where(tweak => !string.IsNullOrWhiteSpace(tweak.Id))
            .GroupBy(tweak => tweak.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var scripts = UserDataService.Instance.LoadCustomScripts().Where(script => !string.IsNullOrWhiteSpace(script.Id))
            .GroupBy(script => script.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < playbook.Steps.Count; index++)
        {
            var step = playbook.Steps[index];
            var prefix = $"{index + 1}. ";
            switch (step.Type)
            {
                case PlaybookStepType.Tweak:
                    var builtInTweak = step.ReferenceId.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase)
                        ? builtIn.GetValueOrDefault(step.ReferenceId[8..])
                        : null;
                    var customTweak = step.ReferenceId.StartsWith("custom:", StringComparison.OrdinalIgnoreCase)
                        ? custom.GetValueOrDefault(step.ReferenceId[7..])
                        : null;
                    var tweakExists = builtInTweak != null || customTweak != null;
                    var tweakNotes = new List<string>();
                    if (builtInTweak?.RiskLevel >= 3 || customTweak?.RegistryPath.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase) == true
                        || customTweak?.RegistryPath.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase) == true)
                        tweakNotes.Add(L.Get("Scripts:Administrator"));
                    if (builtInTweak?.RequiresRestart == true) tweakNotes.Add(L.Get("UI:RestartRequired"));
                    lines.Add(prefix + step.Summary + (tweakNotes.Count > 0 ? $" [{string.Join(", ", tweakNotes)}]" : string.Empty));
                    if (!tweakExists) errors.Add(L.Format("Scripts:PlaybookMissingReference", step.Name));
                    break;
                case PlaybookStepType.Winget:
                    lines.Add(prefix + step.Summary);
                    try { UserDataService.ValidateWingetId(step.WingetId); }
                    catch { errors.Add(L.Format("Scripts:PlaybookInvalidWinget", step.WingetId)); }
                    break;
                case PlaybookStepType.Script:
                    var script = scripts.GetValueOrDefault(step.ReferenceId);
                    lines.Add(prefix + step.Summary + (script?.RequiresAdministrator == true ? $" [{L.Get("Scripts:Administrator")}]" : string.Empty));
                    if (script == null) errors.Add(L.Format("Scripts:PlaybookMissingReference", step.Name));
                    break;
            }
        }
        if (playbook.Steps.Count == 0) errors.Add(L.Get("Scripts:PlaybookEmpty"));
        return new(lines, errors);
    }

    public async Task<PlaybookRunResult> ExecuteAsync(
        Playbook playbook,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var preflight = Preflight(playbook);
        if (!preflight.CanRun) return new(false, 0, string.Join(Environment.NewLine, preflight.Errors), string.Empty);

        var builtIn = BuiltInTweaks();
        var custom = UserDataService.Instance.LoadCustomTweaks().Where(tweak => !string.IsNullOrWhiteSpace(tweak.Id))
            .GroupBy(tweak => tweak.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var scripts = UserDataService.Instance.LoadCustomScripts().Where(script => !string.IsNullOrWhiteSpace(script.Id))
            .GroupBy(script => script.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var lines = new List<string> { $"{DateTimeOffset.Now:O} — {playbook.Name}" };
        var completed = await RunSequentiallyAsync(playbook.Steps, async step =>
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                    PlaybookStepType.Script => await ExecuteScript(step, scripts, value => details = value, cancellationToken),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                success = false;
                details = ex.Message;
            }
            lines.Add($"[{(success ? "OK" : "FAILED")}] {step.Summary}{(details.Length > 0 ? Environment.NewLine + details : string.Empty)}");
            return success;
        });

        var logDirectory = Path.Combine(UserDataService.Instance.DataDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);
        var logPath = Path.Combine(logDirectory, $"playbook-{SafeFileName(playbook.Name)}-last.log");
        await File.WriteAllLinesAsync(logPath, lines, cancellationToken);
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
            return RegistryService.Instance.RestoreRegistryValue(tweak.RegistryPath, tweak.RegistryKey);
        var value = RegistryService.ParseData(tweak.ValueType, tweak.Data, out var kind);
        return RegistryService.Instance.ApplyValueWithBackup(tweak.RegistryPath, tweak.RegistryKey, value, kind);
    }

    private static async Task<bool> ExecuteScript(
        PlaybookStep step,
        IReadOnlyDictionary<string, CustomScript> scripts,
        Action<string> setDetails,
        CancellationToken cancellationToken)
    {
        var result = await PowerShellService.Instance.ExecuteCustomScriptAsync(scripts[step.ReferenceId], cancellationToken);
        setDetails(string.Join(Environment.NewLine, new[] { result.Output.Trim(), result.Error.Trim() }.Where(value => value.Length > 0)));
        return result.Success;
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray()).Trim();
        return safe.Length == 0 ? "playbook" : safe;
    }
}
