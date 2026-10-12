using System.IO;
using System.Text.Json;

namespace TweakHub.Services;

/// <summary>Small JSON files that must survive crashes: atomic writes, and unreadable files kept aside instead of overwritten.</summary>
internal static class JsonStore
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static T Load<T>(string path, Action<string>? onCorrupt = null) where T : new()
    {
        if (!File.Exists(path)) return new T();
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path)) ?? new T();
        }
        catch (Exception ex) when (ex is JsonException or IOException or NotSupportedException)
        {
            var corrupt = path + $".corrupt-{DateTime.Now:yyyyMMddHHmmss}";
            try { File.Move(path, corrupt, true); } catch (IOException) { }
            onCorrupt?.Invoke($"Unreadable data moved to {corrupt}: {ex.Message}");
            return new T();
        }
    }

    public static void Save<T>(string path, T data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Indented));
        File.Move(temp, path, true);
    }
}
