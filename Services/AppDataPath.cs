using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace TweakHub.Services;

public static class AppDataPath
{
    private static readonly Lazy<string> UndoScope = new(ReadUndoScope);
    internal const string UndoDirectoryName = "Undo";

    // Store payloads are immutable. Keep writable data and downloaded tools outside WindowsApps.
    public static string RootPath { get; } = StoreDistribution.IsStoreBuild
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TweakHub", "Store")
        : AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
    public static string AppsPath { get; } = Path.Combine(RootPath, "Apps");
    public static string MachinePath { get; } = StoreDistribution.IsStoreBuild
        ? Path.Combine(RootPath, "Machine")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TweakHub");
    public static bool IsPortable { get; } = !StoreDistribution.IsStoreBuild && File.Exists(Path.Combine(RootPath, "portable.flag"));
    public static string BasePath { get; } = StoreDistribution.IsStoreBuild || IsPortable
        ? Path.Combine(RootPath, "Data")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TweakHub");

    internal static string UndoPath => GetUndoPath(BasePath);

    internal static string GetUndoPath(string dataPath) => Path.Combine(dataPath, UndoDirectoryName, UndoScope.Value);

    internal static string ComputeUndoScope(Guid machineId, string userSid) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{machineId:D}:{userSid}")));

    private static string ReadUndoScope()
    {
        using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var cryptography = machine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        using var identity = WindowsIdentity.GetCurrent();
        if (!Guid.TryParse(cryptography?.GetValue("MachineGuid") as string, out var machineId) || identity.User is null)
            throw new InvalidOperationException("Windows did not provide a machine or user identity for the backup directory.");
        return ComputeUndoScope(machineId, identity.User.Value);
    }

    public static void EnsureAppsDirectory() => Directory.CreateDirectory(AppsPath);
}
