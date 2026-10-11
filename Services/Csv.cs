using TweakHub.Localization;

namespace TweakHub.Services;

internal static class Csv
{
    /// <summary>One quoted CSV line; values starting like a formula get a leading quote so spreadsheets never evaluate them.</summary>
    public static string Row(params object?[] values) => string.Join(",", values.Select(value =>
    {
        var text = Convert.ToString(value, L.Culture) ?? string.Empty;
        if (text.Length > 0 && "=+-@".Contains(text[0])) text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }));
}
