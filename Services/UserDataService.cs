using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using TweakHub.Models;

namespace TweakHub.Services;

public sealed class UserDataService
{
    private const int ProfileVersion = 1;
    private readonly string _favoritesFile;
    private readonly string _customScriptsFile;
    private readonly string _customTweaksFile;
    private readonly string _customToolsFile;
    private readonly string _appearanceFile;
    private readonly string _pendingRestartsFile;

    public static UserDataService Instance { get; } = new();
    public string DataDirectory { get; }

    private UserDataService() : this(AppDataPath.BasePath) { }

    internal UserDataService(string dataDirectory)
    {
        DataDirectory = dataDirectory;
        Directory.CreateDirectory(dataDirectory);
        _favoritesFile = Path.Combine(dataDirectory, "favorites.json");
        _customScriptsFile = Path.Combine(dataDirectory, "custom-scripts.json");
        _customTweaksFile = Path.Combine(dataDirectory, "custom-tweaks.json");
        _customToolsFile = Path.Combine(dataDirectory, "custom-tools.json");
        _appearanceFile = Path.Combine(dataDirectory, "appearance.json");
        _pendingRestartsFile = Path.Combine(dataDirectory, "pending-restarts.json");
    }

    public HashSet<string> LoadFavoriteTools() => Load<HashSet<string>>(_favoritesFile);
    public void SaveFavoriteTools(IEnumerable<string> favorites) =>
        Save(_favoritesFile, favorites.ToHashSet(StringComparer.OrdinalIgnoreCase));

    public ObservableCollection<CustomScript> LoadCustomScripts() => new(Load<List<CustomScript>>(_customScriptsFile));
    public void SaveCustomScripts(IEnumerable<CustomScript> scripts) => Save(_customScriptsFile, scripts.ToList());

    public List<CustomRegistryTweak> LoadCustomTweaks() => Load<List<CustomRegistryTweak>>(_customTweaksFile);
    public void SaveCustomTweaks(IEnumerable<CustomRegistryTweak> tweaks) => Save(_customTweaksFile, tweaks.ToList());

    public List<ExternalTool> LoadCustomTools() => Load<List<ExternalTool>>(_customToolsFile);
    public void SaveCustomTools(IEnumerable<ExternalTool> tools) => Save(_customToolsFile, tools.ToList());

    public AppearanceSettings LoadAppearance() => Load<AppearanceSettings>(_appearanceFile);
    public void SaveAppearance(AppearanceSettings appearance) => Save(_appearanceFile, appearance);

    public HashSet<string> LoadPendingRestartIds()
    {
        var state = Load<PendingRestartState>(_pendingRestartsFile);
        var currentBoot = CurrentBootTimeUtc();
        if (state.BootTimeUtc == default || Math.Abs((state.BootTimeUtc - currentBoot).TotalMinutes) > 2)
        {
            Save(_pendingRestartsFile, new PendingRestartState { BootTimeUtc = currentBoot });
            return [];
        }
        return state.TweakIds;
    }

    public void MarkRestartPending(string tweakId)
    {
        var ids = LoadPendingRestartIds();
        ids.Add(tweakId);
        Save(_pendingRestartsFile, new PendingRestartState { BootTimeUtc = CurrentBootTimeUtc(), TweakIds = ids });
    }

    public void ExportProfile(string path)
    {
        var profile = new UserProfile
        {
            Version = ProfileVersion,
            CustomScripts = Load<List<CustomScript>>(_customScriptsFile),
            CustomTweaks = Load<List<CustomRegistryTweak>>(_customTweaksFile),
            CustomTools = Load<List<ExternalTool>>(_customToolsFile),
            FavoriteTools = LoadFavoriteTools(),
            Appearance = LoadAppearance()
        };
        Save(path, profile);
    }

    public AppearanceSettings ImportProfile(string path)
    {
        if (new FileInfo(path).Length > 5 * 1024 * 1024) throw new InvalidDataException("Profile files are limited to 5 MB.");
        var profile = JsonSerializer.Deserialize<UserProfile>(File.ReadAllText(path))
            ?? throw new InvalidDataException("The profile is empty or invalid.");
        if (profile.Version != ProfileVersion) throw new InvalidDataException($"Unsupported profile version: {profile.Version}.");
        if (profile.CustomScripts.Count > 500 || profile.CustomTweaks.Count > 500 || profile.CustomTools.Count > 500)
            throw new InvalidDataException("The profile contains too many custom entries.");
        foreach (var tool in profile.CustomTools) ValidateCustomTool(tool);

        SaveCustomScripts(profile.CustomScripts);
        SaveCustomTweaks(profile.CustomTweaks);
        SaveCustomTools(profile.CustomTools);
        SaveFavoriteTools(profile.FavoriteTools);
        SaveAppearance(profile.Appearance);
        return profile.Appearance;
    }

    public static void ValidateCustomTool(ExternalTool tool)
    {
        tool.Name = tool.Name.Trim();
        tool.Description = tool.Description.Trim();
        tool.Category = tool.Category.Trim();
        tool.WingetId = tool.WingetId.Trim();
        tool.DownloadUrl = tool.DownloadUrl.Trim();
        tool.PowerShellCommand = tool.PowerShellCommand.Trim();
        if (tool.Name.Length is < 1 or > 100 || tool.Category.Length is < 1 or > 80 || tool.Description.Length > 500)
            throw new InvalidDataException("Name, category or description is invalid.");
        var actions = new[] { tool.WingetId, tool.DownloadUrl, tool.PowerShellCommand }.Count(value => value.Length > 0);
        if (actions != 1) throw new InvalidDataException("Choose exactly one action: Winget, HTTPS link or PowerShell.");
        if (tool.WingetId.Length > 0 && !Regex.IsMatch(tool.WingetId, "^[A-Za-z0-9][A-Za-z0-9._-]{1,127}$"))
            throw new InvalidDataException("Enter a Winget package ID, not a command line.");
        if (tool.DownloadUrl.Length > 0 && (!Uri.TryCreate(tool.DownloadUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidDataException("Links must use HTTPS.");
        if (tool.PowerShellCommand.Length > 8192) throw new InvalidDataException("PowerShell commands are limited to 8192 characters.");
        tool.Id = string.IsNullOrWhiteSpace(tool.Id) ? Guid.NewGuid().ToString("N") : tool.Id;
        tool.IsCustom = true;
    }

    private static DateTimeOffset CurrentBootTimeUtc() =>
        DateTimeOffset.UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount64);

    private static T Load<T>(string path) where T : new()
    {
        if (!File.Exists(path)) return new T();
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path)) ?? new T();
        }
        catch (Exception ex)
        {
            var corrupt = path + $".corrupt-{DateTime.Now:yyyyMMddHHmmss}";
            File.Move(path, corrupt, true);
            Debug.WriteLine($"Invalid user data moved to {corrupt}: {ex.Message}");
            return new T();
        }
    }

    private static void Save<T>(string path, T data)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, path, true);
    }

    private sealed class PendingRestartState
    {
        public DateTimeOffset BootTimeUtc { get; set; }
        public HashSet<string> TweakIds { get; set; } = [];
    }

    private sealed class UserProfile
    {
        public int Version { get; set; }
        public List<CustomScript> CustomScripts { get; set; } = [];
        public List<CustomRegistryTweak> CustomTweaks { get; set; } = [];
        public List<ExternalTool> CustomTools { get; set; } = [];
        public HashSet<string> FavoriteTools { get; set; } = [];
        public AppearanceSettings Appearance { get; set; } = new();
    }
}
