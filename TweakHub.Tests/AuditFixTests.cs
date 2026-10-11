using System.IO;
using System.Text.Json;
using NUnit.Framework;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Tests;

public class AuditFixTests
{
    [Test]
    public void EditorsAndProfileImportShareTheNameLimit()
    {
        var script = new CustomScript { Name = new string('s', 101), Content = "Write-Output ok" };
        var tweak = new CustomRegistryTweak { Name = new string('t', 101), RegistryPath = @"HKCU\Software\TweakHubTest", RegistryKey = "Value", ValueType = "REG_SZ" };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidDataException>(() => UserDataService.ValidateCustomScript(script));
            Assert.Throws<InvalidDataException>(() => UserDataService.ValidateCustomTweak(tweak));
            script.Name = "  Clean name  ";
            Assert.DoesNotThrow(() => UserDataService.ValidateCustomScript(script));
            Assert.That(script.Name, Is.EqualTo("Clean name"));
        });
    }

    [Test]
    public async Task CmdScriptsKeepNonAsciiPaths()
    {
        var directory = Directory.CreateTempSubdirectory("Città-");
        try
        {
            var script = new CustomScript
            {
                Language = ScriptLanguage.Cmd,
                Content = $"if exist \"{directory.FullName}\" (exit /b 0) else (exit /b 3)"
            };
            var result = await PowerShellService.Instance.ExecuteCustomScriptAsync(script);
            Assert.That(result.ExitCode, Is.EqualTo(0), result.Output + result.Error);
        }
        finally
        {
            directory.Delete();
        }
    }

    [Test]
    public void JsonReadToleratesPowerShellShapes()
    {
        using var single = JsonDocument.Parse("""{"Items":{"Name":"one","Age":4294967295,"On":true,"Tags":["a",1,"b"]}}""");
        var item = JsonRead.Items(single.RootElement, "Items").Single();

        Assert.Multiple(() =>
        {
            Assert.That(JsonRead.Text(item, "Name"), Is.EqualTo("one"));
            Assert.That(JsonRead.Text(item, "Age"), Is.Empty, "Numbers are not text");
            Assert.That(JsonRead.Int(item, "Age"), Is.Null, "UInt32.MaxValue does not fit Int32");
            Assert.That(JsonRead.Bool(item, "On"), Is.True);
            Assert.That(JsonRead.Bool(item, "Missing", fallback: true), Is.True);
            Assert.That(JsonRead.Texts(item, "Tags"), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(JsonRead.Items(single.RootElement, "Missing"), Is.Empty);
        });
    }

    [Test]
    public void JsonStoreMovesUnreadableFilesAsideInsteadOfOverwriting()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(directory.FullName, "data.json");
            File.WriteAllText(path, "{ not json");
            string? reported = null;
            var loaded = JsonStore.Load<Dictionary<string, string>>(path, message => reported = message);
            JsonStore.Save(path, new Dictionary<string, string> { ["a"] = "b" });

            Assert.Multiple(() =>
            {
                Assert.That(loaded, Is.Empty);
                Assert.That(reported, Does.Contain(".corrupt-"));
                Assert.That(Directory.GetFiles(directory.FullName, "data.json.corrupt-*"), Has.Length.EqualTo(1));
                Assert.That(JsonStore.Load<Dictionary<string, string>>(path)["a"], Is.EqualTo("b"));
            });
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
