using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using TweakHub.Models;

namespace TweakHub.Services
{
    public class UserDataService
    {
        private static UserDataService? _instance;
        public static UserDataService Instance => _instance ??= new UserDataService();

        private readonly string _basePath;
        private readonly string _favoritesFile;
        private readonly string _customScriptsFile;
        private readonly string _customTweaksFile;

        private UserDataService()
        {
            _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TweakHub");
            Directory.CreateDirectory(_basePath);
            _favoritesFile = Path.Combine(_basePath, "favorites.json");
            _customScriptsFile = Path.Combine(_basePath, "custom-scripts.json");
            _customTweaksFile = Path.Combine(_basePath, "custom-tweaks.json");
        }

        public HashSet<string> LoadFavoriteTools() => Load<HashSet<string>>(_favoritesFile);

        public void SaveFavoriteTools(IEnumerable<string> favorites) =>
            Save(_favoritesFile, favorites.ToHashSet(StringComparer.OrdinalIgnoreCase));

        public ObservableCollection<CustomScript> LoadCustomScripts() =>
            new(Load<List<CustomScript>>(_customScriptsFile));

        public void SaveCustomScripts(IEnumerable<CustomScript> scripts) =>
            Save(_customScriptsFile, scripts.ToList());

        public List<CustomRegistryTweak> LoadCustomTweaks() =>
            Load<List<CustomRegistryTweak>>(_customTweaksFile);

        public void SaveCustomTweaks(IEnumerable<CustomRegistryTweak> tweaks) =>
            Save(_customTweaksFile, tweaks.ToList());

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
            var temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, path, true);
        }
    }
}
