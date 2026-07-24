using System.IO;
using System.Text.Json;
using TweakHub.Localization;
using TweakHub.Models;

namespace TweakHub.Services;

public enum RestartOperationKind
{
    Tweak,
    WindowsUpdate
}

public enum RestartVerificationStatus
{
    Verified,
    Failed,
    Partial,
    Unavailable
}

public sealed class PendingRestartOperation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public RestartOperationKind Kind { get; set; }
    public string TargetId { get; set; } = string.Empty;
    public bool TargetEnabled { get; set; }
    public string TargetPreset { get; set; } = string.Empty;
    public DateTimeOffset BootTimeUtc { get; set; }
}

public sealed class RestartVerificationResult
{
    public string OperationId { get; set; } = string.Empty;
    public RestartOperationKind Kind { get; set; }
    public string TargetId { get; set; } = string.Empty;
    public bool TargetEnabled { get; set; }
    public string TargetPreset { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public RestartVerificationStatus Status { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
}

public sealed class RestartVerificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _statePath;
    private readonly string? _legacyPath;
    private readonly Func<DateTimeOffset> _bootTime;
    private RestartState _state;

    public static RestartVerificationService Instance { get; } = new(
        Path.Combine(AppDataPath.MachinePath, "pending-restarts.json"),
        CurrentBootTimeUtc,
        UserDataService.Instance.LegacyPendingRestartsFile);

    internal RestartVerificationService(string statePath, Func<DateTimeOffset> bootTime, string? legacyPath = null)
    {
        _statePath = statePath;
        _bootTime = bootTime;
        _legacyPath = legacyPath;
        _state = LoadState();
    }

    public IReadOnlyList<RestartVerificationResult> Results => _state.Results;

    public HashSet<string> CurrentBootPendingTweakIds() => _state.Pending
        .Where(operation => operation.Kind == RestartOperationKind.Tweak && SameBoot(operation.BootTimeUtc, _bootTime()))
        .Select(operation => operation.TargetId)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public void TrackTweak(string tweakId, bool targetEnabled)
    {
        Upsert(new PendingRestartOperation
        {
            Kind = RestartOperationKind.Tweak,
            TargetId = tweakId,
            TargetEnabled = targetEnabled,
            BootTimeUtc = _bootTime()
        });
    }

    internal void TrackWindowsUpdate(WindowsUpdatePreset preset)
    {
        Upsert(new PendingRestartOperation
        {
            Kind = RestartOperationKind.WindowsUpdate,
            TargetId = "windows-update",
            TargetPreset = preset.ToString(),
            BootTimeUtc = _bootTime()
        });
    }

    public void MigrateLegacyCurrentBoot(IEnumerable<PerformanceTweak> tweaks)
    {
        if (string.IsNullOrWhiteSpace(_legacyPath) || !File.Exists(_legacyPath)) return;
        try
        {
            var legacy = JsonSerializer.Deserialize<LegacyState>(File.ReadAllText(_legacyPath));
            if (legacy != null && SameBoot(legacy.BootTimeUtc, _bootTime()))
            {
                var byId = tweaks.ToDictionary(tweak => tweak.Id, StringComparer.OrdinalIgnoreCase);
                foreach (var id in legacy.TweakIds)
                    if (byId.TryGetValue(id, out var tweak)) TrackTweak(id, tweak.IsEnabled);
            }
        }
        catch { }
        finally
        {
            try { File.Delete(_legacyPath); } catch { }
        }
    }

    public async Task<IReadOnlyList<RestartVerificationResult>> VerifyAfterRebootAsync()
    {
        var currentBoot = _bootTime();
        var due = _state.Pending.Where(operation => !SameBoot(operation.BootTimeUtc, currentBoot)).ToList();
        if (due.Count == 0) return _state.Results;

        var tweaks = TweakService.Instance.TweakCategories.SelectMany(category => category.Tweaks)
            .ToDictionary(tweak => tweak.Id, StringComparer.OrdinalIgnoreCase);
        var newResults = new List<RestartVerificationResult>();
        foreach (var operation in due)
        {
            var result = new RestartVerificationResult
            {
                OperationId = operation.Id,
                Kind = operation.Kind,
                TargetId = operation.TargetId,
                TargetEnabled = operation.TargetEnabled,
                TargetPreset = operation.TargetPreset,
                VerifiedAt = DateTimeOffset.Now
            };

            if (operation.Kind == RestartOperationKind.Tweak)
            {
                if (!tweaks.TryGetValue(operation.TargetId, out var tweak))
                {
                    result.Name = operation.TargetId;
                    result.Status = RestartVerificationStatus.Unavailable;
                }
                else
                {
                    result.Name = tweak.Name;
                    result.Status = tweak.IsPartiallyApplied
                        ? RestartVerificationStatus.Partial
                        : tweak.IsEnabled == operation.TargetEnabled
                            ? RestartVerificationStatus.Verified
                            : RestartVerificationStatus.Failed;
                }
            }
            else
            {
                result.Name = L.Get("Tweaks:WindowsUpdatePresets");
                var current = await WindowsUpdatePresetService.Instance.DetectAsync();
                result.Status = current.ToString().Equals(operation.TargetPreset, StringComparison.OrdinalIgnoreCase)
                    ? RestartVerificationStatus.Verified
                    : RestartVerificationStatus.Failed;
            }
            newResults.Add(result);
        }

        _state.Results.RemoveAll(result => due.Any(operation => operation.Id == result.OperationId));
        _state.Results.AddRange(newResults);
        _state.Pending.RemoveAll(operation => due.Any(item => item.Id == operation.Id));
        SaveState();
        return _state.Results;
    }

    public async Task<bool> RetryAsync(RestartVerificationResult result)
    {
        if (result.Kind == RestartOperationKind.WindowsUpdate)
        {
            if (!Enum.TryParse<WindowsUpdatePreset>(result.TargetPreset, out var preset)) return false;
            var applied = await WindowsUpdatePresetService.Instance.ApplyAsync(preset);
            if (applied.Success)
            {
                TrackWindowsUpdate(preset);
                RemoveResult(result.OperationId);
            }
            return applied.Success;
        }

        var tweak = TweakService.Instance.TweakCategories.SelectMany(category => category.Tweaks)
            .FirstOrDefault(item => item.Id.Equals(result.TargetId, StringComparison.OrdinalIgnoreCase));
        var success = tweak != null && await TweakService.Instance.ApplyTweakAsync(tweak, result.TargetEnabled);
        if (success) RemoveResult(result.OperationId);
        return success;
    }

    public async Task<bool> RestoreOriginalAsync(RestartVerificationResult result)
    {
        if (result.Kind == RestartOperationKind.WindowsUpdate)
        {
            if (!WindowsUpdatePresetService.Instance.HasCompleteBackup) return false;
            var restored = await WindowsUpdatePresetService.Instance.RestoreAsync();
            if (restored.Success) RemoveResult(result.OperationId);
            return restored.Success;
        }

        var tweak = TweakService.Instance.TweakCategories.SelectMany(category => category.Tweaks)
            .FirstOrDefault(item => item.Id.Equals(result.TargetId, StringComparison.OrdinalIgnoreCase));
        var success = tweak != null && TweakService.Instance.HasBackupFor(tweak)
            && await TweakService.Instance.ApplyTweakAsync(tweak, false);
        if (success) RemoveResult(result.OperationId);
        return success;
    }

    public bool CanRestore(RestartVerificationResult result)
    {
        if (result.Kind == RestartOperationKind.WindowsUpdate) return WindowsUpdatePresetService.Instance.HasCompleteBackup;
        var tweak = TweakService.Instance.TweakCategories.SelectMany(category => category.Tweaks)
            .FirstOrDefault(item => item.Id.Equals(result.TargetId, StringComparison.OrdinalIgnoreCase));
        return tweak != null && TweakService.Instance.HasBackupFor(tweak);
    }

    public void AcknowledgeResults()
    {
        _state.Results.Clear();
        SaveState();
    }

    private void RemoveResult(string operationId)
    {
        _state.Results.RemoveAll(result => result.OperationId == operationId);
        SaveState();
    }

    private void Upsert(PendingRestartOperation operation)
    {
        _state.Pending.RemoveAll(item => item.Kind == operation.Kind
            && item.TargetId.Equals(operation.TargetId, StringComparison.OrdinalIgnoreCase));
        _state.Pending.Add(operation);
        SaveState();
    }

    private RestartState LoadState()
    {
        try
        {
            var state = File.Exists(_statePath)
                ? JsonSerializer.Deserialize<RestartState>(File.ReadAllText(_statePath)) ?? new()
                : new();
            state.Pending ??= [];
            state.Results ??= [];
            return state;
        }
        catch
        {
            try { File.Move(_statePath, _statePath + $".corrupt-{DateTime.Now:yyyyMMddHHmmss}", true); } catch { }
            return new();
        }
    }

    private void SaveState()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
        var temp = _statePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_state, JsonOptions));
        File.Move(temp, _statePath, true);
    }

    private static bool SameBoot(DateTimeOffset left, DateTimeOffset right) => Math.Abs((left - right).TotalMinutes) <= 2;
    private static DateTimeOffset CurrentBootTimeUtc() => DateTimeOffset.UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount64);

    private sealed class RestartState
    {
        public List<PendingRestartOperation> Pending { get; set; } = [];
        public List<RestartVerificationResult> Results { get; set; } = [];
    }

    private sealed class LegacyState
    {
        public DateTimeOffset BootTimeUtc { get; set; }
        public HashSet<string> TweakIds { get; set; } = [];
    }
}
