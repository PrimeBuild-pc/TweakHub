using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using TweakHub.Localization;
using TweakHub.Models;

namespace TweakHub.Services;

public sealed record ProfileSummary(int Scripts, int Tweaks, int Tools, int Playbooks);

public sealed record ProfileImportResult(
    AppearanceSettings Appearance,
    int Scripts,
    int Tweaks,
    int Tools,
    int Playbooks,
    string RecoveryPath);

public sealed class UserDataService
{
    private const int ProfileVersion = 2;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _favoritesFile;
    private readonly string _favoriteTweaksFile;
    private readonly string _customScriptsFile;
    private readonly string _customTweaksFile;
    private readonly string _customToolsFile;
    private readonly string _appearanceFile;
    private readonly string _playbooksFile;
    private readonly string _pendingRestartsFile;

    public static UserDataService Instance { get; } = new();
    public string DataDirectory { get; }
    internal string LegacyPendingRestartsFile => _pendingRestartsFile;

    private UserDataService() : this(AppDataPath.BasePath) { }

    internal UserDataService(string dataDirectory)
    {
        DataDirectory = dataDirectory;
        Directory.CreateDirectory(dataDirectory);
        _favoritesFile = Path.Combine(dataDirectory, "favorites.json");
        _favoriteTweaksFile = Path.Combine(dataDirectory, "favorite-tweaks.json");
        _customScriptsFile = Path.Combine(dataDirectory, "custom-scripts.json");
        _customTweaksFile = Path.Combine(dataDirectory, "custom-tweaks.json");
        _customToolsFile = Path.Combine(dataDirectory, "custom-tools.json");
        _appearanceFile = Path.Combine(dataDirectory, "appearance.json");
        _playbooksFile = Path.Combine(dataDirectory, "playbooks.json");
        _pendingRestartsFile = Path.Combine(dataDirectory, "pending-restarts.json");
    }

    public HashSet<string> LoadFavoriteTools() => Load<HashSet<string>>(_favoritesFile).ToHashSet(StringComparer.OrdinalIgnoreCase);
    public void SaveFavoriteTools(IEnumerable<string> favorites) =>
        Save(_favoritesFile, favorites.ToHashSet(StringComparer.OrdinalIgnoreCase));

    public HashSet<string> LoadFavoriteTweaks() => Load<HashSet<string>>(_favoriteTweaksFile).ToHashSet(StringComparer.OrdinalIgnoreCase);
    public void SaveFavoriteTweaks(IEnumerable<string> favorites) =>
        Save(_favoriteTweaksFile, favorites.ToHashSet(StringComparer.OrdinalIgnoreCase));

    public ObservableCollection<CustomScript> LoadCustomScripts() => new(Load<List<CustomScript>>(_customScriptsFile));
    public void SaveCustomScripts(IEnumerable<CustomScript> scripts) => Save(_customScriptsFile, scripts.ToList());

    public List<CustomRegistryTweak> LoadCustomTweaks() => Load<List<CustomRegistryTweak>>(_customTweaksFile);
    public void SaveCustomTweaks(IEnumerable<CustomRegistryTweak> tweaks) => Save(_customTweaksFile, tweaks.ToList());

    public List<ExternalTool> LoadCustomTools() => Load<List<ExternalTool>>(_customToolsFile);
    public void SaveCustomTools(IEnumerable<ExternalTool> tools) => Save(_customToolsFile, tools.ToList());

    public List<Playbook> LoadPlaybooks() => Load<List<Playbook>>(_playbooksFile);
    public void SavePlaybooks(IEnumerable<Playbook> playbooks) => Save(_playbooksFile, playbooks.ToList());

    public AppearanceSettings LoadAppearance() => Load<AppearanceSettings>(_appearanceFile);
    public void SaveAppearance(AppearanceSettings appearance) => Save(_appearanceFile, appearance);

    public void ExportProfile(string path)
    {
        var profile = new UserProfile
        {
            Version = ProfileVersion,
            CustomScripts = Load<List<CustomScript>>(_customScriptsFile),
            CustomTweaks = Load<List<CustomRegistryTweak>>(_customTweaksFile),
            CustomTools = Load<List<ExternalTool>>(_customToolsFile),
            Playbooks = Load<List<Playbook>>(_playbooksFile),
            FavoriteTools = LoadFavoriteTools(),
            FavoriteTweaks = LoadFavoriteTweaks(),
            Appearance = LoadAppearance()
        };
        Save(path, profile);
    }

    public ProfileSummary InspectProfile(string path)
    {
        var profile = ReadProfile(path);
        return new(profile.CustomScripts.Count, profile.CustomTweaks.Count, profile.CustomTools.Count, profile.Playbooks.Count);
    }

    public ProfileImportResult ImportProfile(string path)
    {
        var profile = ReadProfile(path);
        var recoveryDirectory = Path.Combine(DataDirectory, "ProfileBackups");
        Directory.CreateDirectory(recoveryDirectory);
        var recoveryPath = Path.Combine(recoveryDirectory, $"pre-import-{DateTime.Now:yyyyMMdd-HHmmss-fff}.tweakhub.json");
        ExportProfile(recoveryPath);

        var writes = new Dictionary<string, object>
        {
            [_customScriptsFile] = profile.CustomScripts,
            [_customTweaksFile] = profile.CustomTweaks,
            [_customToolsFile] = profile.CustomTools,
            [_playbooksFile] = profile.Playbooks,
            [_favoritesFile] = profile.FavoriteTools,
            [_favoriteTweaksFile] = profile.FavoriteTweaks,
            [_appearanceFile] = profile.Appearance
        };
        ReplaceAll(writes);

        return new(profile.Appearance, profile.CustomScripts.Count, profile.CustomTweaks.Count,
            profile.CustomTools.Count, profile.Playbooks.Count, recoveryPath);
    }

    private static UserProfile ReadProfile(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists) throw new FileNotFoundException(L.Get("UI:ProfileInvalid"), path);
        if (file.Length > 10 * 1024 * 1024) throw new InvalidDataException(L.Get("UI:ProfileTooLarge"));
        var profile = JsonSerializer.Deserialize<UserProfile>(File.ReadAllText(path))
            ?? throw new InvalidDataException(L.Get("UI:ProfileInvalid"));
        Normalize(profile);
        ValidateProfile(profile);
        return profile;
    }

    public static void ValidateCustomTool(ExternalTool tool)
    {
        tool.Name = (tool.Name ?? string.Empty).Trim();
        tool.Description = (tool.Description ?? string.Empty).Trim();
        tool.Category = (tool.Category ?? string.Empty).Trim();
        tool.WingetId = (tool.WingetId ?? string.Empty).Trim();
        tool.DownloadUrl = (tool.DownloadUrl ?? string.Empty).Trim();
        tool.PowerShellCommand = (tool.PowerShellCommand ?? string.Empty).Trim();
        tool.TerminalCommand = (tool.TerminalCommand ?? string.Empty).Trim();
        tool.ExecutableName = (tool.ExecutableName ?? string.Empty).Trim();
        if (tool.Name.Length is < 1 or > 100 || tool.Category.Length is < 1 or > 80 || tool.Description.Length > 500)
            throw new InvalidDataException(L.Get("UI:CustomToolFieldsInvalid"));
        var actions = new[] { tool.WingetId, tool.DownloadUrl, tool.PowerShellCommand }.Count(value => value.Length > 0);
        if (actions != 1) throw new InvalidDataException(L.Get("UI:CustomToolActionRequired"));
        if (tool.WingetId.Length > 0) ValidateWingetId(tool.WingetId);
        if (tool.DownloadUrl.Length > 0 && (!Uri.TryCreate(tool.DownloadUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidDataException(L.Get("UI:HttpsRequired"));
        if (tool.PowerShellCommand.Length > 8192) throw new InvalidDataException(L.Get("UI:PowerShellTooLong"));
        tool.Id = string.IsNullOrWhiteSpace(tool.Id) ? Guid.NewGuid().ToString("N") : tool.Id.Trim();
        if (!IsStableId(tool.Id)) throw new InvalidDataException(L.Get("UI:ProfileInvalid"));
        tool.IsCustom = true;
    }

    public static void ValidateWingetId(string id)
    {
        if (!Regex.IsMatch(id, "^[A-Za-z0-9][A-Za-z0-9._-]{1,127}$"))
            throw new InvalidDataException(L.Get("UI:WingetIdInvalid"));
    }

    public static void ValidateCustomTweak(CustomRegistryTweak tweak)
    {
        tweak.Id = (tweak.Id ?? string.Empty).Trim();
        tweak.Name = (tweak.Name ?? string.Empty).Trim();
        tweak.Description = (tweak.Description ?? string.Empty).Trim();
        tweak.RegistryPath = (tweak.RegistryPath ?? string.Empty).Trim();
        tweak.RegistryKey = (tweak.RegistryKey ?? string.Empty).Trim();
        tweak.ValueType = (tweak.ValueType ?? string.Empty).Trim().ToUpperInvariant();
        tweak.Data ??= string.Empty;
        if (!IsStableId(tweak.Id) || tweak.Name.Length is < 1 or > 100 || tweak.Description.Length > 500)
            throw new InvalidDataException(L.Get("UI:ProfileInvalid"));
        RegistryService.ValidateLocation(tweak.RegistryPath, tweak.RegistryKey);
        _ = RegistryService.ParseData(tweak.ValueType, tweak.Data, out _);
    }

    private static void Normalize(UserProfile profile)
    {
        profile.CustomScripts ??= [];
        profile.CustomTweaks ??= [];
        profile.CustomTools ??= [];
        profile.Playbooks ??= [];
        profile.FavoriteTools ??= [];
        profile.FavoriteTweaks ??= [];
        profile.Appearance ??= new();
        profile.FavoriteTools = profile.FavoriteTools.ToHashSet(StringComparer.OrdinalIgnoreCase);
        profile.FavoriteTweaks = profile.FavoriteTweaks.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateProfile(UserProfile profile)
    {
        if (profile.Version is not (1 or ProfileVersion))
            throw new InvalidDataException(L.Format("UI:ProfileVersionUnsupported", profile.Version));
        if (profile.CustomScripts.Count > 500 || profile.CustomTweaks.Count > 500 || profile.CustomTools.Count > 500
            || profile.Playbooks.Count > 100 || profile.FavoriteTools.Count > 1000 || profile.FavoriteTweaks.Count > 1000)
            throw new InvalidDataException(L.Get("UI:ProfileTooManyEntries"));

        foreach (var script in profile.CustomScripts)
        {
            script.Id = (script.Id ?? string.Empty).Trim();
            script.Name = (script.Name ?? string.Empty).Trim();
            script.Content ??= string.Empty;
            if (!IsStableId(script.Id) || script.Name.Length is < 1 or > 100 || script.Content.Length > 1024 * 1024
                || !Enum.IsDefined(script.Language)) throw new InvalidDataException(L.Get("UI:ProfileInvalid"));
        }
        EnsureUnique(profile.CustomScripts.Select(script => script.Id));

        foreach (var tweak in profile.CustomTweaks) ValidateCustomTweak(tweak);
        EnsureUnique(profile.CustomTweaks.Select(tweak => tweak.Id));

        foreach (var tool in profile.CustomTools) ValidateCustomTool(tool);
        EnsureUnique(profile.CustomTools.Select(tool => tool.Id));

        foreach (var playbook in profile.Playbooks) ValidatePlaybook(playbook);
        EnsureUnique(profile.Playbooks.Select(playbook => playbook.Id));

        if (profile.Appearance.Theme is not ("System" or "Light" or "Dark")
            || profile.Appearance.AccentColor.Length > 0 && !Regex.IsMatch(profile.Appearance.AccentColor, "^#[0-9A-Fa-f]{6}$")
            || profile.Appearance.Language is not ("System" or "en" or "ru" or "zh-CN" or "es" or "it" or "ja")
            || profile.FavoriteTools.Concat(profile.FavoriteTweaks).Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 200))
            throw new InvalidDataException(L.Get("UI:ProfileInvalid"));
    }

    public static void ValidatePlaybook(Playbook playbook)
    {
        playbook.Id = (playbook.Id ?? string.Empty).Trim();
        playbook.Name = (playbook.Name ?? string.Empty).Trim();
        playbook.Description = (playbook.Description ?? string.Empty).Trim();
        playbook.Steps ??= [];
        if (!IsStableId(playbook.Id) || playbook.Name.Length is < 1 or > 100 || playbook.Description.Length > 500 || playbook.Steps.Count > 100)
            throw new InvalidDataException(L.Get("Scripts:PlaybookInvalid"));
        foreach (var step in playbook.Steps)
        {
            step.Id = (step.Id ?? string.Empty).Trim();
            step.ReferenceId = (step.ReferenceId ?? string.Empty).Trim();
            step.Name = (step.Name ?? string.Empty).Trim();
            step.WingetId = (step.WingetId ?? string.Empty).Trim();
            if (!IsStableId(step.Id) || step.Name.Length is < 1 or > 100 || !Enum.IsDefined(step.Type))
                throw new InvalidDataException(L.Get("Scripts:PlaybookInvalid"));
            if (step.Type == PlaybookStepType.Winget) ValidateWingetId(step.WingetId);
            else if (step.ReferenceId.Length == 0) throw new InvalidDataException(L.Get("Scripts:PlaybookInvalid"));
        }
        EnsureUnique(playbook.Steps.Select(step => step.Id));
    }

    private static bool IsStableId(string id) => Regex.IsMatch(id, "^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$");

    private static void EnsureUnique(IEnumerable<string> ids)
    {
        var values = ids.ToList();
        if (values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Count)
            throw new InvalidDataException(L.Get("UI:ProfileDuplicateIds"));
    }

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
        File.WriteAllText(temp, JsonSerializer.Serialize(data, JsonOptions));
        File.Move(temp, path, true);
    }

    private static void ReplaceAll(IReadOnlyDictionary<string, object> writes)
    {
        var transaction = Guid.NewGuid().ToString("N");
        var staged = writes.ToDictionary(pair => pair.Key, pair => pair.Key + $".import-{transaction}.tmp");
        var backups = writes.Keys.ToDictionary(path => path, path => path + $".import-{transaction}.bak");
        var committed = new List<string>();
        var succeeded = false;
        try
        {
            foreach (var pair in writes)
                File.WriteAllText(staged[pair.Key], JsonSerializer.Serialize(pair.Value, pair.Value.GetType(), JsonOptions));
            foreach (var path in writes.Keys)
            {
                if (File.Exists(path)) File.Move(path, backups[path], true);
                File.Move(staged[path], path);
                committed.Add(path);
            }
            succeeded = true;
        }
        catch (Exception importError)
        {
            foreach (var path in committed)
                try { File.Delete(path); } catch { }
            Exception? rollbackError = null;
            foreach (var path in writes.Keys)
                try
                {
                    if (File.Exists(backups[path])) File.Move(backups[path], path, true);
                }
                catch (Exception ex) { rollbackError ??= ex; }
            if (rollbackError != null)
                throw new IOException($"Profile import failed and rollback was incomplete; backup files were retained. Original error: {importError.Message}", rollbackError);
            throw;
        }
        finally
        {
            foreach (var path in staged.Values)
                try { File.Delete(path); } catch { }
            if (succeeded)
                foreach (var path in backups.Values)
                    try { File.Delete(path); } catch { }
        }
    }

    private sealed class UserProfile
    {
        public int Version { get; set; }
        public List<CustomScript> CustomScripts { get; set; } = [];
        public List<CustomRegistryTweak> CustomTweaks { get; set; } = [];
        public List<ExternalTool> CustomTools { get; set; } = [];
        public List<Playbook> Playbooks { get; set; } = [];
        public HashSet<string> FavoriteTools { get; set; } = [];
        public HashSet<string> FavoriteTweaks { get; set; } = [];
        public AppearanceSettings Appearance { get; set; } = new();
    }
}
