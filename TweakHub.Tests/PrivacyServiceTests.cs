using System.IO;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class PrivacyServiceTests
{
    [Test]
    public void RangeResponseMatchesSuffixAndIgnoresPadding()
    {
        const string body = "0018A45C4D1DEF81644B54AB7F969B88D65:1\r\n00D4F6E8FA6EECAD2A3AA415EEC418D38EC:0\r\n1E4C9B93F3F0682250B6CF8331B7EE68FD8:3861493\r\n";
        Assert.Multiple(() =>
        {
            Assert.That(PrivacyService.ParseRangeResponse(body, "1e4c9b93f3f0682250b6cf8331b7ee68fd8"), Is.EqualTo(3861493));
            Assert.That(PrivacyService.ParseRangeResponse(body, "00D4F6E8FA6EECAD2A3AA415EEC418D38EC"), Is.EqualTo(0));
            Assert.That(PrivacyService.ParseRangeResponse(body, "FFFFF"), Is.EqualTo(0));
        });
    }

    [Test]
    public void AccountDirectoryUsesLocalizedNotesAndOnlyWebLinks()
    {
        const string json = """
            [{"name":"Example","url":"https://example.com/delete","difficulty":"hard","notes":"English","notes_it":"Italiano","domains":["example.com"]},
             {"name":"Mail only","url":"mailto:privacy@example.org","difficulty":"impossible","notes":"Write to support"},
             {"name":"Script","url":"javascript:alert(1)","difficulty":"easy"},
             {"name":"","url":"https://nameless.example","difficulty":"easy"}]
            """;
        var italian = PrivacyService.ParseAccountDirectory(json, "it");
        var english = PrivacyService.ParseAccountDirectory(json, "en");

        Assert.Multiple(() =>
        {
            Assert.That(italian.Select(site => site.Name), Is.EqualTo(new[] { "Example", "Mail only", "Script" }));
            Assert.That(italian[0].Notes, Is.EqualTo("Italiano"));
            Assert.That(english[0].Notes, Is.EqualTo("English"));
            Assert.That(italian[0].Difficulty, Is.EqualTo("Hard"));
            Assert.That(italian[1].HasUrl, Is.False);
            Assert.That(italian[2].HasUrl, Is.False);
            Assert.That(PrivacyService.ParseAccountDirectory("{}", "en"), Is.Empty);
        });
    }

    [Test]
    public void AccountSearchMatchesNamesAndDomainsWithPrefixFirst()
    {
        AccountSite[] sites =
        [
            new("MyFacebook Tools", "https://a", "Easy", "", []),
            new("Facebook", "https://b", "Medium", "", ["facebook.com"]),
            new("Instagram", "https://c", "Medium", "", ["instagram.com", "cdninstagram.com"])
        ];

        Assert.Multiple(() =>
        {
            Assert.That(PrivacyService.SearchAccounts(sites, "face", null).Select(site => site.Name), Is.EqualTo(new[] { "Facebook", "MyFacebook Tools" }));
            Assert.That(PrivacyService.SearchAccounts(sites, "cdninstagram", null).Single().Name, Is.EqualTo("Instagram"));
            Assert.That(PrivacyService.SearchAccounts(sites, "", "Medium").Count(), Is.EqualTo(2));
        });
    }

    [TestCase("192.168.1.2", true, new[] { "192.168.1.2" })]
    [TestCase("9.9.9.9, 2620:fe::fe", true, new[] { "9.9.9.9", "2620:fe::fe" })]
    [TestCase("1.1.1.1; $(Remove-Item C:\\)", false, new string[0])]
    [TestCase("1.1.1.1'; calc; '", false, new string[0])]
    [TestCase("12", false, new string[0])]
    [TestCase("", false, new string[0])]
    [TestCase("1.1.1.1,1.1.1.2,1.1.1.3,1.1.1.4,1.1.1.5", false, new[] { "1.1.1.1", "1.1.1.2", "1.1.1.3", "1.1.1.4", "1.1.1.5" })]
    public void ServerInputIsCanonicalIpsOnly(string input, bool valid, string[] expected)
    {
        var ok = PrivacyService.TryParseServers(input, out var servers);
        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.EqualTo(valid));
            if (valid) Assert.That(servers, Is.EqualTo(expected));
        });
    }

    [Test]
    public void DnsScriptResetsOrSetsEachAdapter()
    {
        var script = PrivacyService.BuildDnsScript([(12, null), (7, ["9.9.9.9", "2620:fe::fe"])]);
        var lines = script.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines, Is.EqualTo(new[]
        {
            "Set-DnsClientServerAddress -InterfaceIndex 12 -ResetServerAddresses",
            "Set-DnsClientServerAddress -InterfaceIndex 7 -ResetServerAddresses",
            "Set-DnsClientServerAddress -InterfaceIndex 7 -ServerAddresses @('9.9.9.9','2620:fe::fe')",
            "Clear-DnsClientCache"
        }));
    }

    [Test]
    public void DnsAdaptersKeepStaticIpv4AndIpv6Servers()
    {
        var adapters = PrivacyService.ParseDnsAdapters(
            """{"Index":21,"Alias":"Ethernet","Servers":["8.8.8.8"],"Static":"8.8.8.8,8.8.4.4","Static6":"2001:4860:4860::8888"}""");
        var legacyBackup = System.Text.Json.JsonSerializer.Deserialize<List<DnsAdapter>>(
            """[{"Index":3,"Alias":"Wi-Fi","Servers":[],"StaticServers":"1.1.1.1"}]""")!;

        Assert.Multiple(() =>
        {
            Assert.That(adapters.Single().StaticServers6, Is.EqualTo("2001:4860:4860::8888"));
            Assert.That(legacyBackup.Single().StaticServers6, Is.Empty, "Backups written before IPv6 support still load");
            Assert.That(PrivacyService.TryParseServers("8.8.8.8,8.8.4.4,192.168.1.1,2001:4860:4860::8888,2001:4860:4860::8844", out _), Is.False);
            Assert.That(PrivacyService.TryParseServers("8.8.8.8,8.8.4.4,192.168.1.1,2001:4860:4860::8888,2001:4860:4860::8844", out var all, max: 8), Is.True);
            Assert.That(all, Has.Length.EqualTo(5));
        });
    }

    [Test]
    public void DnsQueryIsWellFormedAndAnswerIsValidated()
    {
        var query = PrivacyService.BuildDnsQuery(0xBEEF, "example.com");
        var answer = (byte[])query.Clone();
        answer[2] |= 0x80;
        var refused = (byte[])answer.Clone();
        refused[3] = 0x05;
        var otherId = (byte[])answer.Clone();
        otherId[1] = 0x00;

        Assert.Multiple(() =>
        {
            Assert.That(query[..2], Is.EqualTo(new byte[] { 0xBE, 0xEF }));
            Assert.That(query[12..], Is.EqualTo(new byte[] { 7, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e', 3, (byte)'c', (byte)'o', (byte)'m', 0, 0, 1, 0, 1 }));
            Assert.That(PrivacyService.IsDnsAnswer(query, answer), Is.True);
            Assert.That(PrivacyService.IsDnsAnswer(query, query), Is.False, "A query is not an answer");
            Assert.That(PrivacyService.IsDnsAnswer(query, refused), Is.False);
            Assert.That(PrivacyService.IsDnsAnswer(query, otherId), Is.False);
        });
    }

    [Test]
    public void MetadataSkipsFileSystemTagsAndFlagsSensitiveOnes()
    {
        const string json = """
            [{"SourceFile":"C:/p.jpg","ExifTool:ExifToolVersion":13.1,"System:FileName":"p.jpg","IFD0:Make":"Apple",
              "GPS:GPSLatitude":"45 deg 27' 51.00\" N","XMP-dc:Creator":"Lorenzo","File:ImageWidth":4032}]
            """;
        var tags = PrivacyService.ParseMetadata(json);

        Assert.Multiple(() =>
        {
            Assert.That(tags.Select(tag => tag.Name), Is.EquivalentTo(new[] { "Make", "GPSLatitude", "Creator", "ImageWidth" }));
            Assert.That(tags.Where(tag => tag.Sensitive).Select(tag => tag.Name), Is.EquivalentTo(new[] { "Make", "GPSLatitude", "Creator" }));
            Assert.That(tags.Last().Name, Is.EqualTo("ImageWidth"), "Sensitive tags are listed first");
            Assert.That(tags.Single(tag => tag.Name == "ImageWidth").Value, Is.EqualTo("4032"));
        });
    }

    [Test]
    public async Task AsciiJsonKeepsNonAsciiTextIntact()
    {
        const string text = "Città ファイル 🔒";
        var result = await PowerShellService.Instance.ExecuteScriptAsync(
            $"[pscustomobject]@{{ Name = {PowerShellService.Literal(text)} }} | ConvertTo-Json -Compress" + PowerShellService.AsciiJson);
        Assert.Multiple(() =>
        {
            Assert.That(result.Output.Trim().All(character => character < 128), Is.True, result.Output);
            Assert.That(System.Text.Json.JsonDocument.Parse(result.Output).RootElement.GetProperty("Name").GetString(), Is.EqualTo(text));
        });
    }

    [Test]
    public async Task PowerShellLiteralSurvivesTypographicQuotes()
    {
        const string hostile = "C:\\Temp\\a\u2019; Write-Output INJECTED; \u2018b'c.txt";
        // Compare the length rather than the text so console code pages cannot affect the result.
        var result = await PowerShellService.Instance.ExecuteScriptAsync($"$v = {PowerShellService.Literal(hostile)}; Write-Output ('LEN=' + $v.Length)");
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(result.Output.Trim(), Is.EqualTo($"LEN={hostile.Length}"));
        });
    }
}
