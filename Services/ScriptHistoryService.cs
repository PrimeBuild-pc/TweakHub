using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TweakHub.Services;

internal sealed class ScriptHistoryService
{
    private readonly string _path;
    private Dictionary<string, Entry> _entries;

    public static ScriptHistoryService Instance { get; } = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TweakHub",
        "machine-script-history.json"));

    internal ScriptHistoryService(string path)
    {
        _path = path;
        _entries = Load(path);
    }

    public bool TryGetCompletion(string id, string script, out DateTimeOffset completedAt)
    {
        if (_entries.TryGetValue(id, out var entry) && entry.ScriptHash == Hash(script))
        {
            completedAt = entry.CompletedAtUtc;
            return true;
        }
        completedAt = default;
        return false;
    }

    public void MarkCompleted(string id, string script)
    {
        _entries[id] = new Entry { ScriptHash = Hash(script), CompletedAtUtc = DateTimeOffset.UtcNow };
        Save();
    }

    internal static string Hash(string script) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(script)));

    private static Dictionary<string, Entry> Load(string path)
    {
        if (!File.Exists(path)) return new(StringComparer.OrdinalIgnoreCase);
        try
        {
            var entries = JsonSerializer.Deserialize<Dictionary<string, Entry>>(File.ReadAllText(path));
            return entries == null
                ? new(StringComparer.OrdinalIgnoreCase)
                : new(entries, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            var corrupt = path + $".corrupt-{DateTime.Now:yyyyMMddHHmmss}";
            try { File.Move(path, corrupt, true); } catch { }
            Debug.WriteLine($"Invalid script history moved to {corrupt}: {ex.Message}");
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, _path, true);
    }

    internal sealed class Entry
    {
        public string ScriptHash { get; set; } = string.Empty;
        public DateTimeOffset CompletedAtUtc { get; set; }
    }
}
