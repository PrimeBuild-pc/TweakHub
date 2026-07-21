using NUnit.Framework;
using System.IO;
using TweakHub.Services;

namespace TweakHub.Tests;

public class ScriptHistoryServiceTests
{
    [Test]
    public void CompletionIsMachineLocalAndInvalidatedWhenScriptChanges()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubHistory-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "history.json");
        try
        {
            var history = new ScriptHistoryService(path);
            history.MarkCompleted("repair", "Write-Output 'v1'");

            var restarted = new ScriptHistoryService(path);
            Assert.Multiple(() =>
            {
                Assert.That(restarted.TryGetCompletion("repair", "Write-Output 'v1'", out var completedAt), Is.True);
                Assert.That(completedAt, Is.Not.EqualTo(default(DateTimeOffset)));
                Assert.That(restarted.TryGetCompletion("repair", "Write-Output 'v2'", out _), Is.False);
            });
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Test]
    public void CorruptHistoryIsIgnoredAndMovedAside()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TweakHubHistory-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "history.json");
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(path, "not-json");
            var history = new ScriptHistoryService(path);

            Assert.That(history.TryGetCompletion("repair", "script", out _), Is.False);
            Assert.That(Directory.GetFiles(root, "history.json.corrupt-*"), Has.Length.EqualTo(1));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
