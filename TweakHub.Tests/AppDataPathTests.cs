using System.IO;
using NUnit.Framework;
using TweakHub.Services;

namespace TweakHub.Tests;

public class AppDataPathTests
{
    [Test]
    public void SourceBuildKeepsPortableDataBesideTheApplication()
    {
        Assert.That(AppDataPath.IsPortable, Is.True);
        Assert.That(AppDataPath.BasePath, Is.EqualTo(Path.Combine(AppContext.BaseDirectory, "Data")));
    }
}
