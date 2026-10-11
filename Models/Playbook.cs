using System.Text.Json.Serialization;
using TweakHub.Localization;

namespace TweakHub.Models;

public enum PlaybookStepType
{
    Tweak,
    Winget,
    Script,
    Device,
    Registry,
    PowerPlan
}

public sealed class Playbook
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PlaybookStep> Steps { get; set; } = [];
    public string StepSummary => L.Format("Scripts:PlaybookStepCount", Steps.Count);
}

public sealed class PlaybookStep
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public PlaybookStepType Type { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WingetId { get; set; } = string.Empty;
    public bool TargetEnabled { get; set; } = true;
    /// <summary>Step payload: device class, registry values or power settings as JSON.</summary>
    public string Data { get; set; } = string.Empty;
    /// <summary>Editor checkbox: unchecked steps are not saved.</summary>
    [JsonIgnore] public bool IsIncluded { get; set; } = true;
    [JsonIgnore] public string TypeText => L.Get($"Scripts:PlaybookType{Type}");
    [JsonIgnore] public string Icon => Type switch
    {
        PlaybookStepType.Tweak => "",
        PlaybookStepType.Winget => "",
        PlaybookStepType.Script => "",
        PlaybookStepType.Device => "",
        PlaybookStepType.Registry => "",
        _ => ""
    };

    public string Summary => Type switch
    {
        PlaybookStepType.Tweak => L.Format("Scripts:PlaybookTweakSummary", Name,
            L.Get(TargetEnabled ? "Scripts:PlaybookEnable" : "Scripts:PlaybookRestore")),
        PlaybookStepType.Winget => $"{Name} ({WingetId})",
        PlaybookStepType.Device => L.Format(TargetEnabled ? "Scripts:PlaybookEnableDevice" : "Scripts:PlaybookDisableDevice", Name),
        PlaybookStepType.Registry => L.Format("Scripts:PlaybookRegistrySummary", Name),
        PlaybookStepType.PowerPlan => L.Format("Scripts:PlaybookPowerSummary", Name),
        _ => Name
    };
}

public sealed record PlaybookPreflight(IReadOnlyList<string> Lines, IReadOnlyList<string> Errors, int AdministratorSteps = 0)
{
    public bool CanRun => Errors.Count == 0;
}

public sealed record PlaybookRunResult(bool Success, int Completed, string Details, string LogPath);
