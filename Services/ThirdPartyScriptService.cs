using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows;
using TweakHub.Localization;
using TweakHub.Views.Dialogs;

namespace TweakHub.Services;

public sealed record ThirdPartyScript(string Id, string Name, string Author, string PageUrl, string ScriptUrl)
{
    public string Description => L.Get($"Devices:Script{Id}");
    /// <summary>The author's documented one-liner, shown to the user before it runs.</summary>
    public string Command => $"irm {ScriptUrl} | iex";
}

/// <summary>
/// Scripts by other authors (Marius Heier's latency scripts, Chris Titus Tech's WinUtil), run like the author's
/// one-liner in an elevated PowerShell window, but only the downloaded copy whose SHA-256 the user approved.
/// </summary>
public static class ThirdPartyScriptService
{
    public static readonly ThirdPartyScript[] MariusHeier =
    [
        new("CpuDirect", "CPU Direct", "Marius Heier", "https://tools.mariusheier.com/cpudirect.html", "https://tools.mariusheier.com/cpudirect.ps1"),
        new("DeepPoll", "DeepPoll", "Marius Heier", "https://tools.mariusheier.com/deeppoll.html", "https://tools.mariusheier.com/deeppoll.ps1"),
        new("DeepLog", "DeepLog", "Marius Heier", "https://tools.mariusheier.com/deeplog.html", "https://tools.mariusheier.com/deeplog.ps1"),
        new("Rig", "USB Rig Profiles", "Marius Heier", "https://tools.mariusheier.com/rig.html", "https://tools.mariusheier.com/rig.ps1")
    ];

    public static readonly ThirdPartyScript WinUtil =
        new("WinUtil", "WinUtil", "Chris Titus Tech", "https://github.com/ChrisTitusTech/winutil", "https://christitus.com/win");

    private static string ConsentPath => Path.Combine(AppDataPath.UndoPath, "third-party-consent.json");

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

    // The copy is read once and deleted before it runs, like irm | iex; -NoExit keeps the window open for the script's
    // menus and results. The approved hash travels on the command line: a copy replaced after the UAC prompt never runs.
    internal static string BuildArguments(ThirdPartyScript script, string path, string hash) =>
        $"-NoExit -NoProfile -ExecutionPolicy Bypass -Command \"$Host.UI.RawUI.WindowTitle = '{script.Name} ({script.Author})'; " +
        $"$bytes = [IO.File]::ReadAllBytes({PowerShellService.Literal(path)}); Remove-Item -LiteralPath {PowerShellService.Literal(path)}; " +
        "if ([BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($bytes)).Replace('-', '') -ne " +
        $"'{hash}') {{ Write-Host 'The script changed after it was approved; nothing was run.' -ForegroundColor Red }} " +
        "else { iex ([Text.Encoding]::UTF8.GetString($bytes).TrimStart([char]0xFEFF)) }\"";

    /// <summary>Opens the elevated window on the downloaded copy; false when the user declines UAC.</summary>
    public static bool Run(ThirdPartyScript script, string path, string hash)
    {
        try
        {
            Process.Start(new ProcessStartInfo(SystemTools.PowerShell, BuildArguments(script, path, hash)) { UseShellExecute = true, Verb = "runas" });
            return true;
        }
        catch (Win32Exception)
        {
            File.Delete(path);
            return false;
        }
    }

    /// <summary>Download, show the SHA-256 and ask when this version is new, then run elevated. False when nothing ran.</summary>
    public static async Task<bool> ConfirmAndRunAsync(Window? owner, ThirdPartyScript script)
    {
        string path, hash;
        try
        {
            (path, hash) = await DownloadAsync(script);
        }
        catch (Exception ex)
        {
            await AppDialog.ShowErrorAsync(owner, L.Get("Devices:ScriptWarningTitle"), ex.Message);
            return false;
        }
        if (!HasConsent(script, hash))
        {
            // Topmost: the warning must not hide behind other windows, it is the last stop before remote code runs elevated.
            if (!await AppDialog.ConfirmAsync(owner, L.Get("Devices:ScriptWarningTitle"),
                    L.Format("Devices:ScriptWarningMessage", script.Name, script.Command, script.Author, hash),
                    L.Get("Devices:ScriptRun"), L.Get("UI:Cancel"), DialogKind.Warning, topmost: true))
            {
                File.Delete(path);
                return false;
            }
            SaveConsent(script, hash);
        }
        return Run(script, path, hash);
    }
}
