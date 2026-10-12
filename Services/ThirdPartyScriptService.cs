using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using TweakHub.Localization;

namespace TweakHub.Services;

public sealed record ThirdPartyScript(string Id, string Name, string PageUrl, string ScriptUrl)
{
    public string Description => L.Get($"Devices:Script{Id}");
    /// <summary>The author's documented one-liner, shown to the user before it runs.</summary>
    public string Command => $"irm {ScriptUrl} | iex";
}

/// <summary>Marius Heier's interactive latency scripts, run like the author's one-liner in an elevated PowerShell window.</summary>
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

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>Downloads the script once, so the version the user agreed to is the exact code that runs. Returns the file and its SHA-256.</summary>
    public static async Task<(string Path, string Hash)> DownloadAsync(ThirdPartyScript script)
    {
        var bytes = await Http.GetByteArrayAsync(script.ScriptUrl);
        var path = Path.Combine(Path.GetTempPath(), $"TweakHub-{script.Id}-{Guid.NewGuid():N}.ps1");
        await File.WriteAllBytesAsync(path, bytes);
        return (path, Convert.ToHexString(SHA256.HashData(bytes)));
    }

    // Consent covers one version of the script: when the author changes it, the warning appears again.
    public static bool HasConsent(ThirdPartyScript script, string hash) =>
        JsonStore.Load<HashSet<string>>(ConsentPath).Contains($"{script.Id}:{hash}");

    public static void SaveConsent(ThirdPartyScript script, string hash)
    {
        var accepted = JsonStore.Load<HashSet<string>>(ConsentPath);
        accepted.RemoveWhere(entry => entry == script.Id || entry.StartsWith(script.Id + ":", StringComparison.Ordinal));
        accepted.Add($"{script.Id}:{hash}");
        JsonStore.Save(ConsentPath, accepted);
    }

    // The copy is read and deleted before it runs, like irm | iex; -NoExit keeps the window open for the script's menus and results.
    internal static string BuildArguments(ThirdPartyScript script, string path) =>
        $"-NoExit -NoProfile -ExecutionPolicy Bypass -Command \"$Host.UI.RawUI.WindowTitle = '{script.Name} (Marius Heier)'; " +
        $"$code = Get-Content -LiteralPath {PowerShellService.Literal(path)} -Raw -Encoding UTF8; " +
        $"Remove-Item -LiteralPath {PowerShellService.Literal(path)}; iex $code\"";

    /// <summary>Opens the elevated window on the downloaded copy; false when the user declines UAC.</summary>
    public static bool Run(ThirdPartyScript script, string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo("powershell.exe", BuildArguments(script, path)) { UseShellExecute = true, Verb = "runas" });
            return true;
        }
        catch (Win32Exception)
        {
            File.Delete(path);
            return false;
        }
    }
}
