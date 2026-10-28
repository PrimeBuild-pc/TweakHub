using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TweakHub.Localization;

namespace TweakHub.Tests;

public class LocalizationTests
{
    private static readonly string[] Domains = ["UI", "Tweaks", "Tools", "Scripts", "Security", "Devices", "Maintenance"];
    private static readonly string[] Cultures = ["ru", "zh-CN", "es", "it", "ja"];

    [TestCase("ja-JP", "ja")]
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
            // Machine-translated text tends to carry zero-width spaces, and a space after 。 shows up as a gap in CJK text.
            Assert.That(translated[key], Does.Not.Match("[​‌‍⁠﻿]|。 "), $"Invisible character or stray space in {domain}:{key} ({culture})");
        }
    }

    // Names and technical terms that stay in English, for every language or (with "culture/") for one.
    private static readonly HashSet<string> SameAsEnglish =
    [
        "UI:MainWindowTweakHubWindows11TweakUtility", "Scripts:PowerShellFilter", "Scripts:ctt_winutil_Name", "Scripts:CmdBatch",
        "Security:SafetyScannerTitle", "Security:StartupActiveSetup", "Security:StartupAppInit", "Security:StartupWinlogon",
        "Security:TabDefender", "Security:CheckUpdates", "Security:CheckAntivirus", "Security:CheckFirewall", "Devices:LinkKindNvme",
        "Devices:ChipsetLabel", "Devices:SourceChipset", "Devices:UsbKindAudio", "Tools:CategoryAudio", "Tools:HttpsUrl",
        "es/Scripts:ImportFilter", "es/Scripts:PlaybookTypeScript", "es/Scripts:Playbooks", "es/Devices:PriorityNormal", "es/Maintenance:Manual",
        "it/Devices:ControllerMode", "it/Devices:InputController", "it/Devices:InputMouse", "it/Devices:UsbKindInput", "it/Maintenance:CpuLabel",
        "it/Scripts:PlaybookTypeScript", "it/Scripts:PlaybookTypeTweak", "it/Scripts:PlaybookTypeWinget", "it/Scripts:adobe_hosts_block_Category",
        "it/Scripts:adobe_hosts_unblock_Category", "it/Scripts:gaming_runtimes_Category", "it/Scripts:remove_windows_ai_Category",
        "it/Security:ColumnTrigger", "it/Security:StartupDriver", "it/Security:StartupIfeo", "it/Tools:CategoryDisplay"
    ];

    [TestCaseSource(nameof(ResourceCases))]
    public void TextIsTranslatedUnlessItIsANameOrTechnicalTerm(string domain, string culture)
    {
        var manager = new ResourceManager($"TweakHub.Resources.{domain}", typeof(L).Assembly);
        var neutral = Read(manager.GetResourceSet(CultureInfo.InvariantCulture, true, false));
        var translated = Read(manager.GetResourceSet(CultureInfo.GetCultureInfo(culture), true, false));

        var untranslated = neutral.Keys
            .Where(key => translated.TryGetValue(key, out var value) && value == neutral[key] && Regex.IsMatch(value, "[A-Za-z]{4,}"))
            .Where(key => !SameAsEnglish.Contains($"{domain}:{key}") && !SameAsEnglish.Contains($"{culture[..2]}/{domain}:{key}"))
            .ToList();
        Assert.That(untranslated, Is.Empty, $"Still in English in {domain}.{culture}.resx (translate it, or add a name or technical term to SameAsEnglish)");
    }

    private static IEnumerable<TestCaseData> ResourceCases() =>
        from domain in Domains from culture in Cultures select new TestCaseData(domain, culture);

    private static Dictionary<string, string> Read(ResourceSet? resources) => resources?.Cast<DictionaryEntry>()
        .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!) ?? [];

    private static string[] Placeholders(string value) => Regex.Matches(value, @"\{\d+(?::[^}]*)?\}")
        .Select(match => match.Value).Order().ToArray();
}
