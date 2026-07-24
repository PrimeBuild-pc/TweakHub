using TweakHub.Localization;

namespace TweakHub.Models;

public enum PlaybookStepType
{
    Tweak,
    Winget,
    Script
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

    public string Summary => Type switch
    {
        PlaybookStepType.Tweak => L.Format("Scripts:PlaybookTweakSummary", Name,
            L.Get(TargetEnabled ? "Scripts:PlaybookEnable" : "Scripts:PlaybookRestore")),
        PlaybookStepType.Winget => $"{Name} ({WingetId})",
        _ => Name
    };
}

public sealed record PlaybookPreflight(IReadOnlyList<string> Lines, IReadOnlyList<string> Errors)
{
    public bool CanRun => Errors.Count == 0;
}

public sealed record PlaybookRunResult(bool Success, int Completed, string Details, string LogPath);
