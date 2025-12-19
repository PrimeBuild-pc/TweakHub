using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public HashSet<string> LoadFavoriteTools()
        {
            try
            {
                if (!File.Exists(_favoritesFile)) return new();
                var data = JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(_favoritesFile));
                return data ?? new();
            }
            catch { return new(); }
        }

        public void SaveFavoriteTools(IEnumerable<string> favorites)
        {
            try
            {
                File.WriteAllText(_favoritesFile, JsonSerializer.Serialize(favorites));
            }
            catch { }
        }

        public ObservableCollection<CustomScript> LoadCustomScripts()
        {
            try
            {
                if (!File.Exists(_customScriptsFile)) return new();
                var data = JsonSerializer.Deserialize<List<CustomScript>>(File.ReadAllText(_customScriptsFile));
                return new ObservableCollection<CustomScript>(data ?? new List<CustomScript>());
            }
            catch { return new(); }
        }

        public void SaveCustomScripts(IEnumerable<CustomScript> scripts)
        {
            try
            {
                File.WriteAllText(_customScriptsFile, JsonSerializer.Serialize(scripts));
            }
            catch { }
        }

        public List<CustomRegistryTweak> LoadCustomTweaks()
        {
            try
            {
                if (!File.Exists(_customTweaksFile)) return new();
                var data = JsonSerializer.Deserialize<List<CustomRegistryTweak>>(File.ReadAllText(_customTweaksFile));
                return data ?? new();
            }
            catch { return new(); }
        }

        public void SaveCustomTweaks(IEnumerable<CustomRegistryTweak> tweaks)
        {
            try
            {
                File.WriteAllText(_customTweaksFile, JsonSerializer.Serialize(tweaks));
            }
            catch { }
        }
    }
}
