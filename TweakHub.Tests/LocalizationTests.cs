using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TweakHub.Localization;

namespace TweakHub.Tests;

public class LocalizationTests
{
    private static readonly string[] Domains = ["UI", "Tweaks", "Tools", "Scripts"];
    private static readonly string[] Cultures = ["ru", "zh-CN", "es", "it"];

    [TestCase("it-IT", "it")]
    [TestCase("ru-RU", "ru")]
    [TestCase("zh-TW", "zh-CN")]
    [TestCase("de-DE", "en")]
    public void SystemLanguageResolvesToSupportedCulture(string system, string expected) =>
        Assert.That(L.ResolveCulture("System", CultureInfo.GetCultureInfo(system)).Name, Is.EqualTo(expected));

    [Test]
    public void ExplicitLanguageOverridesSystemLanguage() =>
        Assert.That(L.ResolveCulture("es", CultureInfo.GetCultureInfo("de-DE")).Name, Is.EqualTo("es"));

    [TestCaseSource(nameof(ResourceCases))]
    public void TranslationHasSameKeysAndPlaceholders(string domain, string culture)
    {
        var manager = new ResourceManager($"TweakHub.Resources.{domain}", typeof(L).Assembly);
        var neutral = Read(manager.GetResourceSet(CultureInfo.InvariantCulture, true, false));
        var translated = Read(manager.GetResourceSet(CultureInfo.GetCultureInfo(culture), true, false));

        Assert.That(translated.Keys, Is.EquivalentTo(neutral.Keys), $"Missing or extra keys in {domain}.{culture}.resx");
        foreach (var key in neutral.Keys)
        {
            Assert.That(translated[key], Is.Not.Empty, $"Blank translation for {domain}:{key} ({culture})");
            Assert.That(Placeholders(translated[key]), Is.EquivalentTo(Placeholders(neutral[key])), $"Placeholder mismatch for {domain}:{key} ({culture})");
            Assert.That(translated[key].Count(character => character == '|'), Is.EqualTo(neutral[key].Count(character => character == '|')), $"File-filter separator mismatch for {domain}:{key} ({culture})");
        }
    }

    private static IEnumerable<TestCaseData> ResourceCases() =>
        from domain in Domains from culture in Cultures select new TestCaseData(domain, culture);

    private static Dictionary<string, string> Read(ResourceSet? resources) => resources?.Cast<DictionaryEntry>()
        .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!) ?? [];

    private static string[] Placeholders(string value) => Regex.Matches(value, @"\{\d+(?::[^}]*)?\}")
        .Select(match => match.Value).Order().ToArray();
}
