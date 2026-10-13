using System.Runtime.InteropServices;
using System.Security.Principal;

namespace TweakHub.Services;

internal static class Elevation
{
    public static bool IsAdministrator
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    /// <summary>
    /// True for an administrator account running with its limited token, so an elevated restart stays the same user.
    /// A standard user elevates as another account, whose HKCU and AppData are not this user's.
    /// </summary>
    public static bool CanRestartElevated
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            return GetTokenInformation(identity.Token, TokenElevationType, out var type, sizeof(int), out _)
                && type == TokenElevationTypeLimited;
        }
    }

    private const int TokenElevationType = 18;
    private const int TokenElevationTypeLimited = 3;

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(IntPtr token, int infoClass, out int info, int length, out int returnLength);
}
