using System.Text.Json;

namespace TweakHub.Services;

/// <summary>Tolerant readers for JSON produced by PowerShell or third parties: wrong types and missing fields give defaults.</summary>
internal static class JsonRead
{
    private static bool TryGet(JsonElement element, string name, out JsonElement value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value);
    }

    public static string Text(JsonElement element, string name) =>
        TryGet(element, name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    /// <summary>Null when missing or outside Int32 (Defender reports never-run scans as UInt32.MaxValue).</summary>
    public static int? Int(JsonElement element, string name) =>
        TryGet(element, name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null;

    public static bool Bool(JsonElement element, string name, bool fallback = false) =>
        TryGet(element, name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.ValueKind == JsonValueKind.True
            : fallback;

    /// <summary>ConvertTo-Json writes a one-item array as a bare object, so accept both.</summary>
    public static IEnumerable<JsonElement> Items(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Array => element.EnumerateArray(),
        JsonValueKind.Object => [element],
        _ => []
    };

    public static IEnumerable<JsonElement> Items(JsonElement element, string name) =>
        TryGet(element, name, out var value) ? Items(value) : [];

    public static List<string> Texts(JsonElement element, string name) =>
        Items(element, name).Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()!).ToList();
}
