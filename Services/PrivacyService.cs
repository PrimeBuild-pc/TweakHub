using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TweakHub.Localization;

namespace TweakHub.Services;

public sealed record AccountSite(string Name, string Url, string Difficulty, string Notes, IReadOnlyList<string> Domains)
{
    public string DifficultyText => L.Get($"Security:Difficulty{Difficulty}");
    public bool HasUrl => Url.Length > 0;
}

public sealed record DnsAdapter(int Index, string Alias, IReadOnlyList<string> Servers, string StaticServers, string StaticServers6 = "")
{
    public string ServersText => Servers.Count == 0 ? "–" : string.Join(", ", Servers);
}

public sealed record DnsPreset(string Id, string[] Servers);

public sealed record MetadataTag(string Group, string Name, string Value, bool Sensitive);

public sealed class PrivacyService
{
    public static PrivacyService Instance { get; } = new();
    private PrivacyService() { }

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TweakHub");
        return client;
    }

    // Pwned Passwords (k-anonymity)

    /// <summary>Only the first five SHA-1 hex characters leave the PC; padding hides the response size.</summary>
    public static async Task<int> GetPasswordBreachCountAsync(string password, CancellationToken cancellationToken = default)
    {
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.pwnedpasswords.com/range/{hash[..5]}");
        request.Headers.Add("Add-Padding", "true");
        using var response = await Http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return ParseRangeResponse(await response.Content.ReadAsStringAsync(cancellationToken), hash[5..]);
    }

    internal static int ParseRangeResponse(string body, string suffix)
    {
        foreach (var line in body.Split('\n'))
        {
            var parts = line.Trim().Split(':');
            if (parts.Length == 2 && parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase) && int.TryParse(parts[1], out var count))
                return count;
        }
        return 0;
    }

    // JustDeleteMe directory (MIT), downloaded only on request and cached locally.

    private const string AccountDirectoryUrl = "https://raw.githubusercontent.com/justdeleteme/justdelete.me/master/sites.json";
    private static string AccountCachePath => Path.Combine(AppDataPath.BasePath, "justdeleteme-sites.json");

    public static bool HasCachedAccountDirectory => File.Exists(AccountCachePath);

    public static async Task<IReadOnlyList<AccountSite>> LoadAccountDirectoryAsync(bool download, CancellationToken cancellationToken = default)
    {
        string json;
        if (download || !HasCachedAccountDirectory)
        {
            json = await Http.GetStringAsync(AccountDirectoryUrl, cancellationToken);
            var sites = ParseAccountDirectory(json, L.Culture.TwoLetterISOLanguageName);
            if (sites.Count == 0) throw new InvalidDataException(L.Get("Security:AccountsInvalidData"));
            Directory.CreateDirectory(AppDataPath.BasePath);
            await File.WriteAllTextAsync(AccountCachePath, json, cancellationToken);
            return sites;
        }

        json = await File.ReadAllTextAsync(AccountCachePath, cancellationToken);
        return ParseAccountDirectory(json, L.Culture.TwoLetterISOLanguageName);
    }

    internal static List<AccountSite> ParseAccountDirectory(string json, string language)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array) return [];
        var noteKey = language switch { "en" => "notes", "pt" => "notes_pt_br", "ca" => "notes_cat", var code => "notes_" + code };
        var sites = new List<AccountSite>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            string Text(string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
            var name = Text("name");
            var url = Text("url");
            // Only open web pages: entries may carry mailto: or malformed links.
            if (name.Length == 0 || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")) url = string.Empty;
            var difficulty = Text("difficulty") switch { "easy" => "Easy", "medium" => "Medium", "hard" => "Hard", "impossible" => "Impossible", _ => "Unknown" };
            var notes = Text(noteKey) is { Length: > 0 } localized ? localized : Text("notes");
            var domains = item.TryGetProperty("domains", out var list) && list.ValueKind == JsonValueKind.Array
                ? list.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()!).ToList()
                : [];
            if (name.Length > 0) sites.Add(new AccountSite(name, url, difficulty, notes, domains));
        }
        return sites;
    }

    public static IEnumerable<AccountSite> SearchAccounts(IEnumerable<AccountSite> sites, string query, string? difficulty)
    {
        query = query.Trim();
        return sites
            .Where(site => difficulty is null || site.Difficulty == difficulty)
            .Where(site => query.Length == 0
                || site.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || site.Domains.Any(domain => domain.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(site => site.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(site => site.Name, StringComparer.OrdinalIgnoreCase);
    }

    // DNS

    public static readonly DnsPreset[] DnsPresets =
    [
        new("Quad9", ["9.9.9.9", "149.112.112.112", "2620:fe::fe", "2620:fe::9"]),
        new("CloudflareMalware", ["1.1.1.2", "1.0.0.2", "2606:4700:4700::1112", "2606:4700:4700::1002"]),
        new("AdGuard", ["94.140.14.14", "94.140.15.15", "2a10:50c0::ad1:ff", "2a10:50c0::ad2:ff"])
    ];

    private static string DnsBackupPath => Path.Combine(AppDataPath.BasePath, "dns-backup.json");
    public static bool HasDnsBackup => File.Exists(DnsBackupPath);

    public async Task<IReadOnlyList<DnsAdapter>> GetDnsAdaptersAsync()
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync("""
            $ErrorActionPreference = 'SilentlyContinue'
            @(Get-NetIPConfiguration | Where-Object { $_.IPv4DefaultGateway -and $_.NetAdapter.Status -eq 'Up' } | ForEach-Object {
                $guid = $_.NetAdapter.InterfaceGuid
                [pscustomobject]@{ Index = [int]$_.InterfaceIndex; Alias = [string]$_.InterfaceAlias
                    Servers = @($_.DNSServer.ServerAddresses | Select-Object -Unique)
                    Static = [string](Get-ItemProperty -LiteralPath "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\$guid").NameServer
                    Static6 = [string](Get-ItemProperty -LiteralPath "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces\$guid").NameServer }
            }) | ConvertTo-Json -Depth 3 -Compress
            """ + PowerShellService.AsciiJson, timeout: TimeSpan.FromMinutes(1));
        if (!result.Success) throw new InvalidOperationException(result.Error);
        return ParseDnsAdapters(result.Output);
    }

    internal static List<DnsAdapter> ParseDnsAdapters(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        using var document = JsonDocument.Parse(json);
        var items = document.RootElement.ValueKind == JsonValueKind.Array ? document.RootElement.EnumerateArray().ToList() : [document.RootElement];
        return items.Select(item => new DnsAdapter(
            item.TryGetProperty("Index", out var index) && index.TryGetInt32(out var number) ? number : 0,
            item.TryGetProperty("Alias", out var alias) ? alias.GetString() ?? "" : "",
            item.TryGetProperty("Servers", out var servers) && servers.ValueKind == JsonValueKind.Array
                ? servers.EnumerateArray().Select(value => value.GetString() ?? "").Where(value => value.Length > 0).ToList()
                : [],
            item.TryGetProperty("Static", out var stat) ? stat.GetString() ?? "" : "",
            item.TryGetProperty("Static6", out var stat6) ? stat6.GetString() ?? "" : "")).Where(adapter => adapter.Index > 0).ToList();
    }

    /// <summary>Parses user input into canonical IP addresses; anything else is rejected before it reaches PowerShell.</summary>
    public static bool TryParseServers(string input, out string[] servers, int max = 4)
    {
        var parts = input.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries);
        var parsed = parts.Select(part => (part.Contains('.') || part.Contains(':')) && IPAddress.TryParse(part, out var address) ? address.ToString() : null).ToArray();
        servers = parsed.All(value => value != null) ? parsed.Select(value => value!).ToArray() : [];
        return servers.Length > 0 && servers.Length <= max;
    }

    public async Task<PowerShellResult> ApplyDnsAsync(IReadOnlyList<DnsAdapter> adapters, string[]? servers)
    {
        if (!HasDnsBackup)
        {
            Directory.CreateDirectory(AppDataPath.BasePath);
            await File.WriteAllTextAsync(DnsBackupPath, JsonSerializer.Serialize(adapters));
        }
        return await PowerShellService.Instance.ExecuteScriptAsync(BuildDnsScript(adapters.Select(a => (a.Index, servers)).ToList()),
            requireAdministrator: true, TimeSpan.FromMinutes(2));
    }

    public async Task<PowerShellResult> RestoreDnsAsync()
    {
        var saved = JsonSerializer.Deserialize<List<DnsAdapter>>(await File.ReadAllTextAsync(DnsBackupPath)) ?? [];
        var plan = saved.Select(adapter => (adapter.Index,
            TryParseServers($"{adapter.StaticServers},{adapter.StaticServers6}", out var servers, max: 8) ? servers : null)).ToList();
        var result = await PowerShellService.Instance.ExecuteScriptAsync(BuildDnsScript(plan), requireAdministrator: true, TimeSpan.FromMinutes(2));
        if (result.Success) File.Delete(DnsBackupPath);
        return result;
    }

    internal static string BuildDnsScript(IReadOnlyList<(int Index, string[]? Servers)> plan)
    {
        var builder = new StringBuilder();
        foreach (var (index, servers) in plan)
        {
            // Reset first so a family missing from the new list (often IPv6) goes back to automatic instead of keeping old servers.
            builder.AppendLine($"Set-DnsClientServerAddress -InterfaceIndex {index} -ResetServerAddresses");
            if (servers is not null)
                builder.AppendLine($"Set-DnsClientServerAddress -InterfaceIndex {index} -ServerAddresses @({string.Join(",", servers.Select(PowerShellService.Literal))})");
        }
        builder.AppendLine("Clear-DnsClientCache");
        return builder.ToString();
    }

    /// <summary>Sends one DNS query for example.com straight to the server, bypassing the Windows resolver and its fallbacks.</summary>
    public static async Task<bool> TestDnsServerAsync(string server, CancellationToken cancellationToken = default)
    {
        if (!IPAddress.TryParse(server, out var address)) return false;
        using var udp = new UdpClient(address.AddressFamily);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        var query = BuildDnsQuery((ushort)Random.Shared.Next(1, ushort.MaxValue), "example.com");
        try
        {
            await udp.SendAsync(query, new IPEndPoint(address, 53), timeout.Token);
            var reply = await udp.ReceiveAsync(timeout.Token);
            return IsDnsAnswer(query, reply.Buffer);
        }
        catch (Exception ex) when (ex is OperationCanceledException or SocketException)
        {
            return false;
        }
    }

    /// <summary>192.0.2.1 (TEST-NET-1) is never routable: an answer from it means something on the path rewrites DNS traffic.</summary>
    public static Task<bool> IsDnsInterceptedAsync(CancellationToken cancellationToken = default) =>
        TestDnsServerAsync("192.0.2.1", cancellationToken);

    internal static byte[] BuildDnsQuery(ushort id, string name)
    {
        var bytes = new List<byte> { (byte)(id >> 8), (byte)id, 0x01, 0x00, 0, 1, 0, 0, 0, 0, 0, 0 };
        foreach (var label in name.Split('.'))
        {
            bytes.Add((byte)label.Length);
            bytes.AddRange(Encoding.ASCII.GetBytes(label));
        }
        bytes.AddRange([0, 0, 1, 0, 1]);
        return bytes.ToArray();
    }

    // Same transaction id, response flag set and RCODE 0 (no error).
    internal static bool IsDnsAnswer(byte[] query, byte[] reply) =>
        reply.Length >= 12 && reply[0] == query[0] && reply[1] == query[1] && (reply[2] & 0x80) != 0 && (reply[3] & 0x0F) == 0;

    // ExifTool

    public static string? FindExifTool()
    {
        var directories = new[] { EnvironmentVariableTarget.Process, EnvironmentVariableTarget.Machine, EnvironmentVariableTarget.User }
            .SelectMany(target => (Environment.GetEnvironmentVariable("PATH", target) ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            .Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Links"))
            .Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ExifTool"));
        foreach (var directory in directories)
        {
            try
            {
                var candidate = Path.Combine(Environment.ExpandEnvironmentVariables(directory.Trim('"')), "exiftool.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch (ArgumentException)
            {
            }
        }
        return null;
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunExifToolAsync(string exifTool, IEnumerable<string> arguments, string file)
    {
        // An UTF-8 argument file is ExifTool's documented way to pass non-ASCII Windows paths.
        var argFile = Path.Combine(Path.GetTempPath(), $"TweakHub-{Guid.NewGuid():N}.args");
        try
        {
            await File.WriteAllLinesAsync(argFile, arguments.Append(file), new UTF8Encoding(false));
            var startInfo = new ProcessStartInfo(exifTool)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            foreach (var argument in new[] { "-charset", "filename=utf8", "-@", argFile }) startInfo.ArgumentList.Add(argument);
            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("ExifTool");
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, await output, await error);
        }
        finally
        {
            try { File.Delete(argFile); } catch { }
        }
    }

    public static async Task<IReadOnlyList<MetadataTag>> ReadMetadataAsync(string exifTool, string file)
    {
        var (exitCode, output, error) = await RunExifToolAsync(exifTool, ["-j", "-G1", "-a", "-s"], file);
        if (exitCode != 0 && string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException(error.Trim());
        return ParseMetadata(output);
    }

    public static async Task<string> RemoveMetadataAsync(string exifTool, string file)
    {
        // ExifTool keeps the untouched copy as "<name>_original" next to the file.
        var (exitCode, output, error) = await RunExifToolAsync(exifTool, ["-all="], file);
        if (exitCode != 0) throw new InvalidOperationException((error + output).Trim());
        return file + "_original";
    }

    private static readonly string[] SensitiveTags =
        ["GPS", "Author", "Creator", "Artist", "Owner", "Serial", "Comment", "LastModifiedBy", "Company", "Manager", "Make", "Model", "Copyright", "Keywords", "Location", "City", "Country", "Software", "HostComputer"];

    internal static List<MetadataTag> ParseMetadata(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement.ValueKind == JsonValueKind.Array ? document.RootElement.EnumerateArray().FirstOrDefault() : document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return [];
        var tags = new List<MetadataTag>();
        foreach (var property in root.EnumerateObject())
        {
            var separator = property.Name.IndexOf(':');
            var group = separator < 0 ? "" : property.Name[..separator];
            var name = separator < 0 ? property.Name : property.Name[(separator + 1)..];
            // File-system facts and ExifTool's own info are not embedded metadata.
            if (property.Name == "SourceFile" || group is "System" or "ExifTool") continue;
            var value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? "" : property.Value.GetRawText();
            tags.Add(new MetadataTag(group, name, value, SensitiveTags.Any(tag => name.Contains(tag, StringComparison.OrdinalIgnoreCase))));
        }
        return tags.OrderByDescending(tag => tag.Sensitive).ThenBy(tag => tag.Group).ThenBy(tag => tag.Name).ToList();
    }
}
