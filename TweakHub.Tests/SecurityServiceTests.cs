using System.IO;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class SecurityServiceTests
{
    private const string HealthyDefender = """
        {"Antivirus":[{"Name":"Windows Defender","State":397568}],
         "Defender":{"Mode":"Normal","RealTime":true,"Tamper":true,"Signature":"1.459.666.0","Engine":"4.18.26080.4","SignatureAge":0,"QuickScanAge":4294967295,"FullScanAge":129},
         "Threats":[{"Name":"VulnerableDriver:WinNT/Winring0","Severity":5,"Detected":"2026-09-10T02:13:32.0490000+02:00","Resolved":true,"Resource":"C:\\Tools\\WinRing0.sys"}],
         "Firewall":[1,1,1],"EnableLUA":1,"ConsentPrompt":5,"Smb1":2,"RdpDenied":1,"BitLocker":1,"SecureBoot":1,"MemoryIntegrity":1,"LastUpdateDays":18}
        """;

    private static SecurityCheck Check(SecuritySnapshot snapshot, string id) => snapshot.Checks.Single(check => check.Id == id);

    [Test]
    public void HealthySystemPassesEveryCheckAndScoresFull()
    {
        var snapshot = SecurityService.ParseSnapshot(HealthyDefender);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Checks.Select(check => check.Status), Is.All.EqualTo(CheckStatus.Passed));
            Assert.That(snapshot.Score, Is.EqualTo(100));
            Assert.That(snapshot.Checks.Select(check => check.Id), Is.Unique);
            Assert.That(snapshot.Defender!.QuickScanAge, Is.Null, "UInt32.MaxValue means never scanned");
            Assert.That(snapshot.Defender.FullScanAge, Is.EqualTo(129));
            Assert.That(snapshot.Defender.Threats.Single().Resolved, Is.True);
        });
    }

    [Test]
    public void WeakConfigurationFailsWithFixesAndRestartFlags()
    {
        var json = HealthyDefender
            .Replace("\"RealTime\":true", "\"RealTime\":false")
            .Replace("\"SignatureAge\":0", "\"SignatureAge\":9")
            .Replace("[1,1,1]", "[1,0,1]")
            .Replace("\"EnableLUA\":1", "\"EnableLUA\":0")
            .Replace("\"Smb1\":2", "\"Smb1\":1")
            .Replace("\"RdpDenied\":1", "\"RdpDenied\":0")
            .Replace("\"BitLocker\":1", "\"BitLocker\":2")
            .Replace("\"LastUpdateDays\":18", "\"LastUpdateDays\":90");
        var snapshot = SecurityService.ParseSnapshot(json);

        Assert.Multiple(() =>
        {
            foreach (var id in new[] { "Antivirus", "Signatures", "Firewall", "Uac", "Smb1", "RemoteDesktop", "BitLocker", "Updates" })
                Assert.That(Check(snapshot, id).Status, Is.EqualTo(CheckStatus.Failed), id);
            foreach (var id in new[] { "Antivirus", "Signatures", "Firewall", "Uac", "Smb1", "RemoteDesktop" })
                Assert.That(Check(snapshot, id).CanFix, Is.True, id);
            Assert.That(Check(snapshot, "BitLocker").CanFix, Is.False, "BitLocker needs a recovery key, never auto-enabled");
            Assert.That(Check(snapshot, "Uac").FixNeedsRestart, Is.True);
            Assert.That(Check(snapshot, "Smb1").FixNeedsRestart, Is.True);
            Assert.That(snapshot.Score, Is.LessThan(50));
        });
    }

    [Test]
    public void UacWithoutConsentPromptFails() =>
        Assert.That(Check(SecurityService.ParseSnapshot(HealthyDefender.Replace("\"ConsentPrompt\":5", "\"ConsentPrompt\":0")), "Uac").Status,
            Is.EqualTo(CheckStatus.Failed));

    [Test]
    public void ThirdPartyAntivirusReplacesDefenderChecks()
    {
        const string json = """
            {"Antivirus":[{"Name":"Windows Defender","State":393472},{"Name":"Other AV","State":266240}],
             "Defender":{"Mode":"Passive Mode","RealTime":false,"SignatureAge":40},"Firewall":[1,1,1]}
            """;
        var snapshot = SecurityService.ParseSnapshot(json);

        Assert.Multiple(() =>
        {
            Assert.That(Check(snapshot, "Antivirus").Status, Is.EqualTo(CheckStatus.Passed));
            Assert.That(Check(snapshot, "Antivirus").FixScript, Is.Null);
            Assert.That(Check(snapshot, "Signatures").Status, Is.EqualTo(CheckStatus.Passed));
            Assert.That(snapshot.Defender!.IsActive, Is.False);
        });
    }

    [Test]
    public void MissingValuesAreUnknownAndExcludedFromScore()
    {
        var snapshot = SecurityService.ParseSnapshot("""{"Firewall":[1,1,1],"SecureBoot":0}""");

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Defender, Is.Null);
            Assert.That(Check(snapshot, "Antivirus").Status, Is.EqualTo(CheckStatus.Unknown));
            Assert.That(Check(snapshot, "BitLocker").Status, Is.EqualTo(CheckStatus.Unknown));
            Assert.That(Check(snapshot, "SecureBoot").Status, Is.EqualTo(CheckStatus.Failed));
            Assert.That(Check(snapshot, "RemoteDesktop").Status, Is.EqualTo(CheckStatus.Passed), "Missing key means RDP was never enabled");
            // Known checks: Firewall 3 passed, SecureBoot 2 failed, RemoteDesktop 1 passed, MemoryIntegrity 1 failed.
            Assert.That(snapshot.Score, Is.EqualTo(57));
        });
    }

    [Test]
    public void ScoreIsZeroWhenNothingIsKnown() =>
        Assert.That(SecurityService.CalculateScore([new SecurityCheck("Firewall", 3, CheckStatus.Unknown)]), Is.EqualTo(0));

    [Test]
    public void SignatureParsingHandlesSignedAndInvalidJson()
    {
        var signed = SecurityService.ParseSignature("""
            {"Status":"Valid","Signer":"Microsoft Windows","Issuer":"Microsoft Windows Production PCA 2011",
             "ValidUntil":"2027-01-15T19:00:00.0000000+01:00","Timestamp":"","Type":"Catalog"}
            """);

        Assert.Multiple(() =>
        {
            Assert.That(signed!.Status, Is.EqualTo("Valid"));
            Assert.That(signed.Signer, Is.EqualTo("Microsoft Windows"));
            Assert.That(signed.Issuer, Is.EqualTo("Microsoft Windows Production PCA 2011"));
            Assert.That(signed.ValidUntil?.Year, Is.EqualTo(2027));
            Assert.That(signed.Type, Is.EqualTo("Catalog"));
            Assert.That(SecurityService.ParseSignature("not json"), Is.Null);
        });
    }

    [Test]
    public async Task AnalyzeFileHandlesNonAsciiPaths()
    {
        var directory = Directory.CreateTempSubdirectory();
        var path = Path.Combine(directory.FullName, "Città ファイル 🔒.txt");
        try
        {
            await File.WriteAllTextAsync(path, "abc");
            var report = await SecurityService.AnalyzeFileAsync(path);
            // A mangled path makes Get-AuthenticodeSignature fail and leaves no signature result.
            Assert.That(report.Signature, Is.Not.Null);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Test]
    public async Task AnalyzeFileHashesMatchKnownVectors()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "abc");
            var report = await SecurityService.AnalyzeFileAsync(path);

            Assert.Multiple(() =>
            {
                Assert.That(report.Size, Is.EqualTo(3));
                Assert.That(report.Sha256, Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
                Assert.That(report.Sha1, Is.EqualTo("a9993e364706816aba3e25717850c26c9cd0d89d"));
                Assert.That(report.Md5, Is.EqualTo("900150983cd24fb0d6963f7d28e17f72"));
                Assert.That(report.Signature?.Status, Is.Not.EqualTo("Valid"));
            });
        }
        finally
        {
            File.Delete(path);
        }
    }
}
