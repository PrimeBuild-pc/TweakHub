using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using TweakHub.Localization;

namespace TweakHub.Services;

public sealed record ThirdPartyScript(string Id, string Name, string PageUrl, string ScriptUrl)
{
    public string Description => L.Get($"Devices:Script{Id}");
    /// <summary>The author's documented one-liner, shown to the user before it runs.</summary>
    public string Command => $"irm {ScriptUrl} | iex";
}

/// <summary>Marius Heier's interactive latency scripts, run as the author documents them in an elevated PowerShell window.</summary>
public static class ThirdPartyScriptService
{
    public static readonly ThirdPartyScript[] MariusHeier =
    [
        new("CpuDirect", "CPU Direct", "https://tools.mariusheier.com/cpudirect.html", "https://tools.mariusheier.com/cpudirect.ps1"),
        new("DeepPoll", "DeepPoll", "https://tools.mariusheier.com/deeppoll.html", "https://tools.mariusheier.com/deeppoll.ps1"),
        new("DeepLog", "DeepLog", "https://tools.mariusheier.com/deeplog.html", "https://tools.mariusheier.com/deeplog.ps1"),
        new("Rig", "USB Rig Profiles", "https://tools.mariusheier.com/rig.html", "https://tools.mariusheier.com/rig.ps1")
    ];

    private static string ConsentPath => Path.Combine(AppDataPath.BasePath, "third-party-consent.json");

    public static bool HasConsent(ThirdPartyScript script) => JsonStore.Load<HashSet<string>>(ConsentPath).Contains(script.Id);

    public static void SaveConsent(ThirdPartyScript script)
    {
        var accepted = JsonStore.Load<HashSet<string>>(ConsentPath);
        if (accepted.Add(script.Id)) JsonStore.Save(ConsentPath, accepted);
    }

    // PowerShell 5.1 still defaults to TLS 1.0 on some systems; -NoExit keeps the window open for the script's menus and results.
    internal static string BuildArguments(ThirdPartyScript script) =>
        $"-NoExit -NoProfile -ExecutionPolicy Bypass -Command \"$Host.UI.RawUI.WindowTitle = '{script.Name} (Marius Heier)'; " +
        $"[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; {script.Command}\"";

    /// <summary>Opens the elevated window; false when the user declines UAC.</summary>
    public static bool Run(ThirdPartyScript script)
    {
        try
        {
            Process.Start(new ProcessStartInfo("powershell.exe", BuildArguments(script)) { UseShellExecute = true, Verb = "runas" });
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
