using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using TweakHub.Localization;

namespace TweakHub.Services;

public enum CheckStatus { Passed, Failed, Unknown }

public sealed record SecurityCheck(
    string Id,
    int Weight,
    CheckStatus Status,
    string? FixScript = null,
    bool FixNeedsRestart = false,
    string? SettingsUri = null)
{
    public string Title => L.Get($"Security:Check{Id}");
    public string Detail => L.Get($"Security:Check{Id}{Status}");
    public bool CanFix => Status == CheckStatus.Failed && FixScript != null;
}

public sealed record DefenderThreat(string Name, int Severity, DateTime? Detected, bool Resolved, string Resource);

public sealed record DefenderStatus(
    bool IsActive,
    bool RealTimeProtection,
    bool TamperProtection,
    string SignatureVersion,
    string EngineVersion,
    int? SignatureAge,
    int? QuickScanAge,
    int? FullScanAge,
    IReadOnlyList<DefenderThreat> Threats);

public sealed record SecuritySnapshot(IReadOnlyList<SecurityCheck> Checks, DefenderStatus? Defender)
{
    public int Score => SecurityService.CalculateScore(Checks);
}

public sealed record FileSignature(string Status, string Signer, string Issuer, DateTime? ValidUntil, string Timestamp, string Type);

public sealed record FileReport(
    string Path,
    long Size,
    string Sha256,
    string Sha1,
    string Md5,
    FileSignature? Signature,
    FileVersionInfo Version);

public sealed class SecurityService
{
    public static SecurityService Instance { get; } = new();
    private SecurityService() { }

    /// <summary>Checks that come with a fix script; playbook steps may only name these.</summary>
    internal static readonly string[] FixableIds = ["Antivirus", "Signatures", "Firewall", "Uac", "Smb1", "RemoteDesktop"];

    // Everything here is readable without elevation; fixes run elevated separately.
    private const string StatusScript = """
        $ErrorActionPreference = 'SilentlyContinue'
        function Reg($path, $name) { (Get-ItemProperty -LiteralPath $path -Name $name -ErrorAction SilentlyContinue).$name }
        function Iso($date) { if ($date) { ([datetime]$date).ToString('o') } }
        $mp = Get-MpComputerStatus
        $names = @{}; Get-MpThreat | ForEach-Object { $names[[string]$_.ThreatID] = @($_.ThreatName, $_.SeverityID) }
        $threats = @(Get-MpThreatDetection | Sort-Object InitialDetectionTime -Descending | Select-Object -First 20 | ForEach-Object {
            $info = $names[[string]$_.ThreatID]
            [pscustomobject]@{ Name = $info[0]; Severity = [int]$info[1]; Detected = Iso $_.InitialDetectionTime
                Resolved = [bool]$_.ActionSuccess; Resource = (@($_.Resources) | Select-Object -First 1) -replace '^\w+:_', '' }
        })
        $defender = $null
        if ($mp) { $defender = [pscustomobject]@{
            Mode = [string]$mp.AMRunningMode; RealTime = [bool]$mp.RealTimeProtectionEnabled; Tamper = [bool]$mp.IsTamperProtected
            Signature = [string]$mp.AntivirusSignatureVersion; Engine = [string]$mp.AMProductVersion
            SignatureAge = $mp.AntivirusSignatureAge; QuickScanAge = $mp.QuickScanAge; FullScanAge = $mp.FullScanAge } }
        $fw = 'HKLM:\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy'
        $sys = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'
        [pscustomobject]@{
            Antivirus = @(Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct | ForEach-Object {
                [pscustomobject]@{ Name = $_.displayName; State = [int]$_.productState } })
            Defender = $defender
            Threats = $threats
            Firewall = @('DomainProfile', 'StandardProfile', 'PublicProfile' | ForEach-Object {
                $policy = Reg "HKLM:\SOFTWARE\Policies\Microsoft\WindowsFirewall\$_" 'EnableFirewall'
                if ($null -ne $policy) { $policy } else { Reg "$fw\$_" 'EnableFirewall' } })
            EnableLUA = Reg $sys 'EnableLUA'
            ConsentPrompt = Reg $sys 'ConsentPromptBehaviorAdmin'
            Smb1 = (Get-CimInstance Win32_OptionalFeature -Filter "Name='SMB1Protocol'").InstallState
            RdpDenied = Reg 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server' 'fDenyTSConnections'
            BitLocker = (New-Object -ComObject Shell.Application).NameSpace($env:SystemDrive + '\').Self.ExtendedProperty('System.Volume.BitLockerProtection')
            SecureBoot = Reg 'HKLM:\SYSTEM\CurrentControlSet\Control\SecureBoot\State' 'UEFISecureBootEnabled'
            MemoryIntegrity = Reg 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' 'Enabled'
            LastUpdateDays = (Get-HotFix | Where-Object InstalledOn | ForEach-Object { ((Get-Date) - $_.InstalledOn).Days } | Measure-Object -Minimum).Minimum
        } | ConvertTo-Json -Depth 4 -Compress
        """ + PowerShellService.AsciiJson;

    public async Task<SecuritySnapshot> GetSnapshotAsync()
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(StatusScript, timeout: TimeSpan.FromMinutes(1));
        if (!result.Success) throw new InvalidOperationException(result.Error);
        return ParseSnapshot(result.Output);
    }

    internal static SecuritySnapshot ParseSnapshot(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        static int? Int(JsonElement element, string name) => JsonRead.Int(element, name);
        static bool Bool(JsonElement element, string name) => JsonRead.Bool(element, name);
        static string Text(JsonElement element, string name) => JsonRead.Text(element, name);
        IEnumerable<JsonElement> Array(string name) => JsonRead.Items(root, name);

        var defenderElement = root.TryGetProperty("Defender", out var d) ? d : default;
        DefenderStatus? defender = defenderElement.ValueKind != JsonValueKind.Object ? null : new(
            IsActive: Text(defenderElement, "Mode").Equals("Normal", StringComparison.OrdinalIgnoreCase),
            RealTimeProtection: Bool(defenderElement, "RealTime"),
            TamperProtection: Bool(defenderElement, "Tamper"),
            SignatureVersion: Text(defenderElement, "Signature"),
            EngineVersion: Text(defenderElement, "Engine"),
            SignatureAge: Int(defenderElement, "SignatureAge"),
            QuickScanAge: Int(defenderElement, "QuickScanAge"),
            FullScanAge: Int(defenderElement, "FullScanAge"),
            Threats: Array("Threats").Select(threat => new DefenderThreat(
                Text(threat, "Name") is { Length: > 0 } name ? name : "?",
                Int(threat, "Severity") ?? 0,
                DateTime.TryParse(Text(threat, "Detected"), out var detected) ? detected : null,
                Bool(threat, "Resolved"),
                Text(threat, "Resource"))).ToList());

        // productState bits 12-15: 1 = on; bits 4-7: 0 = definitions up to date.
        var antivirus = Array("Antivirus").Select(product => Int(product, "State") ?? 0).ToList();
        var thirdPartyActive = defender is not { IsActive: true } && antivirus.Any(state => (state >> 12 & 0xF) == 1);

        CheckStatus Is(bool? passed) => passed switch { true => CheckStatus.Passed, false => CheckStatus.Failed, null => CheckStatus.Unknown };
        var firewall = Array("Firewall").Select(value => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : (int?)null).ToList();
        var enableLua = Int(root, "EnableLUA");
        var consent = Int(root, "ConsentPrompt");
        var smb1 = Int(root, "Smb1");
        var rdpDenied = Int(root, "RdpDenied");
        var bitLocker = Int(root, "BitLocker");
        var secureBoot = Int(root, "SecureBoot");
        var lastUpdateDays = Int(root, "LastUpdateDays");

        var checks = new List<SecurityCheck>
        {
            new("Antivirus", 3,
                thirdPartyActive ? CheckStatus.Passed : defender is null ? Is(antivirus.Count == 0 ? null : false) : Is(defender.IsActive && defender.RealTimeProtection),
                defender is null || thirdPartyActive ? null : "Set-MpPreference -DisableRealtimeMonitoring $false",
                SettingsUri: "windowsdefender://threatsettings"),
            new("Signatures", 2,
                thirdPartyActive ? Is(antivirus.Any(state => (state >> 4 & 0xF) == 0)) : Is(defender?.SignatureAge is { } age ? age <= 3 : null),
                thirdPartyActive || defender is null ? null : "Update-MpSignature",
                SettingsUri: "windowsdefender://threat"),
            new("Firewall", 3, Is(firewall.Count == 0 || firewall.Any(value => value is null) ? null : firewall.All(value => value == 1)),
                "Set-NetFirewallProfile -Profile Domain,Private,Public -Enabled True",
                SettingsUri: "windowsdefender://network"),
            new("Uac", 3, Is(enableLua is null ? null : enableLua == 1 && consent != 0),
                """
                $key = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'
                Set-ItemProperty -LiteralPath $key -Name EnableLUA -Value 1 -Type DWord
                Set-ItemProperty -LiteralPath $key -Name ConsentPromptBehaviorAdmin -Value 5 -Type DWord
                Set-ItemProperty -LiteralPath $key -Name PromptOnSecureDesktop -Value 1 -Type DWord
                """,
                FixNeedsRestart: enableLua != 1,
                SettingsUri: "UserAccountControlSettings.exe"),
            new("Updates", 2, Is(lastUpdateDays is { } days ? days <= 45 : null), SettingsUri: "ms-settings:windowsupdate"),
            new("Smb1", 2, Is(smb1 is null ? null : smb1 != 1),
                "Disable-WindowsOptionalFeature -Online -FeatureName SMB1Protocol -NoRestart | Out-Null",
                FixNeedsRestart: true,
                SettingsUri: "optionalfeatures.exe"),
            new("RemoteDesktop", 1, Is(rdpDenied is null ? true : rdpDenied == 1),
                "Set-ItemProperty -LiteralPath 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Terminal Server' -Name fDenyTSConnections -Value 1 -Type DWord",
                SettingsUri: "ms-settings:remotedesktop"),
            // BitLockerProtection: 1 on, 3 encrypting, 2/4/5 off/decrypting/suspended, other = not supported.
            new("BitLocker", 2, Is(bitLocker switch { 1 or 3 => true, 2 or 4 or 5 => false, _ => null }),
                SettingsUri: "control.exe /name Microsoft.BitLockerDriveEncryption"),
            new("SecureBoot", 2, Is(secureBoot is null ? null : secureBoot == 1), SettingsUri: "windowsdefender://devicesecurity"),
            new("MemoryIntegrity", 1, Is(Int(root, "MemoryIntegrity") == 1), SettingsUri: "windowsdefender://coreisolation")
        };
        return new SecuritySnapshot(checks, defender);
    }

    internal static int CalculateScore(IEnumerable<SecurityCheck> checks)
    {
        var known = checks.Where(check => check.Status != CheckStatus.Unknown).ToList();
        var total = known.Sum(check => check.Weight);
        return total == 0 ? 0 : (int)Math.Round(100.0 * known.Where(check => check.Status == CheckStatus.Passed).Sum(check => check.Weight) / total);
    }

    public Task<PowerShellResult> RunElevatedAsync(string script, TimeSpan? timeout = null) =>
        PowerShellService.Instance.ExecuteScriptAsync(script, requireAdministrator: true, timeout ?? TimeSpan.FromMinutes(5));

    public static void OpenSettings(string target)
    {
        var parts = target.Split(' ', 2);
        Process.Start(new ProcessStartInfo(parts[0], parts.Length > 1 ? parts[1] : string.Empty) { UseShellExecute = true });
    }

    public static async Task<FileReport> AnalyzeFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var info = new FileInfo(path);
        var (sha256, sha1, md5) = await Task.Run(() =>
        {
            using var stream = File.OpenRead(path);
            using var h256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var h1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
            using var h5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
            var buffer = new byte[1 << 20];
            int read;
            while ((read = stream.Read(buffer)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                h256.AppendData(buffer, 0, read);
                h1.AppendData(buffer, 0, read);
                h5.AppendData(buffer, 0, read);
            }
            return (Convert.ToHexString(h256.GetHashAndReset()), Convert.ToHexString(h1.GetHashAndReset()), Convert.ToHexString(h5.GetHashAndReset()));
        }, cancellationToken);

        var result = await PowerShellService.Instance.ExecuteScriptAsync($$"""
            $s = Get-AuthenticodeSignature -LiteralPath {{PowerShellService.Literal(path)}}
            $c = $s.SignerCertificate; $t = $s.TimeStamperCertificate
            [pscustomobject]@{ Status = [string]$s.Status; Type = [string]$s.SignatureType
                ValidUntil = $(if ($c) { $c.NotAfter.ToString('o') })
                Signer = $(if ($c) { $c.GetNameInfo('SimpleName', $false) }); Issuer = $(if ($c) { $c.GetNameInfo('SimpleName', $true) })
                Timestamp = $(if ($t) { $t.GetNameInfo('SimpleName', $false) }) } | ConvertTo-Json -Compress
            """ + PowerShellService.AsciiJson, timeout: TimeSpan.FromSeconds(30), cancellationToken: cancellationToken);

        return new FileReport(path, info.Length, sha256.ToLowerInvariant(), sha1.ToLowerInvariant(), md5.ToLowerInvariant(),
            result.Success ? ParseSignature(result.Output) : null, FileVersionInfo.GetVersionInfo(path));
    }

    internal static FileSignature? ParseSignature(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            string Text(string name) => JsonRead.Text(root, name);
            return new FileSignature(Text("Status"), Text("Signer"), Text("Issuer"),
                DateTime.TryParse(Text("ValidUntil"), out var until) ? until : null, Text("Timestamp"), Text("Type"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Microsoft publishes x86 and x64 builds only; Windows 11 on ARM runs the x64 build through emulation.
    private const string SafetyScannerUrl = "https://go.microsoft.com/fwlink/?LinkId=212732";

    public static async Task<string> DownloadSafetyScannerAsync(IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        AppDataPath.EnsureAppsDirectory();
        var target = Path.Combine(AppDataPath.AppsPath, "msert.exe");
        var temp = target + ".download";
        using (var client = new HttpClient())
        using (var response = await client.GetAsync(SafetyScannerUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? 0;
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var file = File.Create(temp);
            var buffer = new byte[1 << 16];
            long copied = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                copied += read;
                if (total > 0) progress.Report((int)(copied * 100 / total));
            }
        }

        // Never run the download unless Windows validates a Microsoft signature on it.
        var check = await PowerShellService.Instance.ExecuteScriptAsync(
            $"$s = Get-AuthenticodeSignature -LiteralPath {PowerShellService.Literal(temp)}; if ($s.Status -ne 'Valid' -or $s.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {{ exit 1 }}",
            timeout: TimeSpan.FromMinutes(1), cancellationToken: cancellationToken);
        if (!check.Success)
        {
            File.Delete(temp);
            throw new InvalidOperationException(L.Get("Security:SafetyScannerSignatureInvalid"));
        }

        File.Move(temp, target, overwrite: true);
        return target;
    }
}
