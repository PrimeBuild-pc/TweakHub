using System.IO;

namespace TweakHub.Services;

public static class AppDataPath
{
    public static string RootPath { get; } = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
    public static string AppsPath { get; } = Path.Combine(RootPath, "Apps");
    public static string MachinePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TweakHub");
    public static bool IsPortable { get; } = File.Exists(Path.Combine(RootPath, "portable.flag"));
    public static string BasePath { get; } = IsPortable
        ? Path.Combine(RootPath, "Data")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TweakHub");

    public static void EnsureAppsDirectory() => Directory.CreateDirectory(AppsPath);
}
