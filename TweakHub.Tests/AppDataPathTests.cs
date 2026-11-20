using System.IO;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class AppDataPathTests
{
    [Test]
    public void SourceBuildKeepsPortableDataBesideTheApplication()
    {
        if (StoreDistribution.IsStoreBuild) Assert.Ignore("Portable mode belongs to the EXE channel.");
        Assert.That(AppDataPath.IsPortable, Is.True);
        Assert.That(AppDataPath.BasePath, Is.EqualTo(Path.Combine(AppContext.BaseDirectory, "Data")));
    }

    [Test]
    public void UndoScopeChangesWhenThePcOrAccountChanges()
    {
        var pc = Guid.NewGuid();
        var otherPc = Guid.NewGuid();
        const string user = "S-1-5-21-1-2-3-1000";
        const string otherUser = "S-1-5-21-1-2-3-1001";
        var scope = AppDataPath.ComputeUndoScope(pc, user);

        Assert.Multiple(() =>
        {
            Assert.That(AppDataPath.ComputeUndoScope(pc, user), Is.EqualTo(scope));
            Assert.That(AppDataPath.ComputeUndoScope(otherPc, user), Is.Not.EqualTo(scope));
            Assert.That(AppDataPath.ComputeUndoScope(pc, otherUser), Is.Not.EqualTo(scope));
            Assert.That(AppDataPath.UndoPath, Does.StartWith(Path.Combine(AppDataPath.BasePath, "Undo") + Path.DirectorySeparatorChar));
        });
    }

    [Test]
    public async Task OnlyOneWriterCanOpenTheDirectoryAndRestartWaitsForIt()
    {
        var directory = Directory.CreateTempSubdirectory("TweakHubDataLock-");
        try
        {
            using var active = await DataDirectoryLock.AcquireAsync(directory.FullName, TimeSpan.Zero);
            Assert.That(active, Is.Not.Null);
            using var duplicate = await DataDirectoryLock.AcquireAsync(Path.Combine(directory.FullName, "."), TimeSpan.Zero);
            Assert.That(duplicate, Is.Null);

            var restarting = DataDirectoryLock.AcquireAsync(directory.FullName, TimeSpan.FromSeconds(3));
            Assert.That(restarting.IsCompleted, Is.False);
            active!.Dispose();
            using var replacement = await restarting;
            Assert.That(replacement, Is.Not.Null);
        }
        finally
        {
            File.Delete(Path.Combine(directory.FullName, ".tweakhub.lock"));
            directory.Delete();
        }
    }
}
