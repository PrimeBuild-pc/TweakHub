namespace TweakHub.Services;

/// <summary>
/// Startup reads shared by the splash screen and the sections: each section's first load reuses the splash result,
/// every later refresh reads fresh data.
/// </summary>
public static class Preload
{
    public const string TweakStates = "tweaks";
    public const string PowerSchemes = "power";
    public const string WindowsUpdate = "windows-update";
    public const string Security = "security";
    public const string Devices = "devices";
    public const string Interrupts = "interrupts";
    public const string TweakChecks = "checks";

    private static readonly Dictionary<string, Task> Tasks = new();

    public static Task Start(string key, Func<Task> load) => Tasks[key] = load();

    /// <summary>The splash task when one was started, otherwise a fresh read.</summary>
    public static Task<T> Get<T>(string key, Func<Task<T>> load) =>
        Tasks.TryGetValue(key, out var task) && task is Task<T> typed ? typed : load();

    public static Task Get(string key, Func<Task> load) => Tasks.TryGetValue(key, out var task) ? task : load();

    /// <summary>Called after a change: the next first-load must read the new state.</summary>
    public static void Invalidate(string key) => Tasks.Remove(key);
}
