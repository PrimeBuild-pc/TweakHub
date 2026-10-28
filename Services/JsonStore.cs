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

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> Locks = new(StringComparer.OrdinalIgnoreCase);

    public static void Save<T>(string path, T data)
    {
        // Replacing a file another save is replacing at the same moment fails: one writer per file at a time.
        lock (Locks.GetOrAdd(Path.GetFullPath(path), _ => new object())) SaveUnlocked(path, data);
    }

    private static void SaveUnlocked<T>(string path, T data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // A unique temp name: two saves of the same file never write or move each other's temp file.
        var temp = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(data, Indented));
            File.Move(temp, path, true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}
