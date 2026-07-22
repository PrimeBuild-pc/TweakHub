using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;
using System.Windows.Markup;

namespace TweakHub.Localization;

public static class L
{
    private static readonly ConcurrentDictionary<string, ResourceManager> Managers = new();
    private static readonly string[] Supported = ["en", "ru", "zh-CN", "es", "it"];
    private static CultureInfo _systemCulture = CultureInfo.CurrentUICulture;

    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("en");

    public static void Initialize(string? language)
    {
        _systemCulture = CultureInfo.CurrentUICulture;
        Culture = ResolveCulture(language, _systemCulture);
        CultureInfo.CurrentUICulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
    }

    internal static CultureInfo ResolveCulture(string? language, CultureInfo systemCulture)
    {
        var normalized = Normalize(language);
        if (normalized != "System") return CultureInfo.GetCultureInfo(normalized);
        return CultureInfo.GetCultureInfo(Supported.FirstOrDefault(code =>
            systemCulture.Name.Equals(code, StringComparison.OrdinalIgnoreCase)
            || systemCulture.Name.StartsWith(code + "-", StringComparison.OrdinalIgnoreCase)
            || code == "zh-CN" && systemCulture.TwoLetterISOLanguageName == "zh") ?? "en");
    }

    public static bool RequiresRestart(string? language) => ResolveCulture(language, _systemCulture).Name != Culture.Name;

    public static string Normalize(string? language) => language switch
    {
        null or "" or "System" => "System",
        var value when Supported.Contains(value, StringComparer.OrdinalIgnoreCase) =>
            Supported.First(code => code.Equals(value, StringComparison.OrdinalIgnoreCase)),
        _ => "System"
    };

    public static string Get(string reference)
    {
        var separator = reference.IndexOf(':');
        var domain = separator < 0 ? "UI" : reference[..separator];
        var key = separator < 0 ? reference : reference[(separator + 1)..];
        var manager = Managers.GetOrAdd(domain, name => new ResourceManager($"TweakHub.Resources.{name}", typeof(L).Assembly));
        return manager.GetString(key, Culture) ?? key;
    }

    public static string Format(string reference, params object?[] args) =>
        string.Format(Culture, Get(reference), args);
}

[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension(string key) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => L.Get(key);
}
