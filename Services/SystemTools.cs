using System.IO;

namespace TweakHub.Services;

/// <summary>
/// Full paths of the Windows tools TweakHub starts. A bare name is searched first in the folder of TweakHub.exe and in
/// the working directory, which a portable copy shares with the user: a planted powershell.exe there would run with
/// the administrator rights TweakHub asks for.
/// </summary>
internal static class SystemTools
{
    public static string PowerShell { get; } = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
    public static string Explorer { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
    public static string System32(string executable) => Path.Combine(Environment.SystemDirectory, executable);
    /// <summary>The App Installer alias; it is not in System32.</summary>
    public static string Winget { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "winget.exe");
}
