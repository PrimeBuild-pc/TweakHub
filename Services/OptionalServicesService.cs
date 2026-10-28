using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TweakHub.Localization;

namespace TweakHub.Services;

public sealed record OptionalService(string Name, int Start, int? Delayed, string Status, bool Exists)
{
    [JsonIgnore] public bool Managed => OptionalServicesService.Instance.HasBackup(Name);
    [JsonIgnore] public bool CanChange => Exists && OptionalServicesService.CanManage(Name) && !OptionalServicesService.LegacyManaged(Name);
    [JsonIgnore] public string Explanation => L.Get($"Maintenance:Service{Name}")
        + (OptionalServicesService.LegacyManaged(Name) ? "\n" + L.Get("Maintenance:ServiceManagedElsewhere") : string.Empty);
    [JsonIgnore] public string ModeText => !Exists ? L.Get("Maintenance:Missing") : L.Get(Start switch
    {
        2 when Delayed == 1 => "Maintenance:DelayedAuto", 2 => "Maintenance:Automatic",
        3 => "Maintenance:Manual", 4 => "Maintenance:Disabled", _ => "Maintenance:Unknown"
    });
}

/// <summary>Curated startup configuration only: never stops services or changes their triggers.</summary>
public sealed class OptionalServicesService
{
    public static OptionalServicesService Instance { get; } = new(AppDataPath.BasePath);
    internal static readonly string[] Names = ["SysMain", "MapsBroker", "RetailDemo", "DiagTrack", "dmwappushservice", "WbioSrvc", "WerSvc", "PcaSvc"];
    private readonly string _path;
    private readonly Func<Task<IReadOnlyList<OptionalService>>> _scan;
    private readonly Func<string, Task<PowerShellResult>> _run;
    private readonly Func<string, bool> _legacyManaged;
    internal OptionalServicesService(string dataPath, Func<Task<IReadOnlyList<OptionalService>>>? scan = null,
        Func<string, Task<PowerShellResult>>? run = null, Func<string, bool>? legacyManaged = null)
    {
        _path = Path.Combine(dataPath, "optional-services.json");
        _scan = scan ?? ScanLiveAsync;
        _legacyManaged = legacyManaged ?? LegacyManaged;
        _run = run ?? (script => PowerShellService.Instance.ExecuteScriptAsync(script, requireAdministrator: true, timeout: TimeSpan.FromMinutes(1)));
    }
    private Dictionary<string, OptionalService> Load() => JsonStore.Load<Dictionary<string, OptionalService>>(_path);
    public bool HasBackup(string name) => Load().ContainsKey(name);
    internal static bool CanManage(string name) => Names.Contains(name, StringComparer.Ordinal)
        && name is not ("SysMain" or "DiagTrack" or "WerSvc");
    internal static bool LegacyManaged(string name) => JsonStore.Load<Dictionary<string, string>>(
        Path.Combine(AppDataPath.BasePath, "disabled-services.json")).ContainsKey(name);

    internal const string ScanScript = """
        $ErrorActionPreference = 'Stop'
        @('SysMain','MapsBroker','RetailDemo','DiagTrack','dmwappushservice','WbioSrvc','WerSvc','PcaSvc' | ForEach-Object {
            $service = Get-Service -Name $_ -ErrorAction SilentlyContinue
            $key = Get-ItemProperty -LiteralPath "HKLM:\SYSTEM\CurrentControlSet\Services\$_" -ErrorAction SilentlyContinue
            [pscustomobject]@{ Name = $_; Start = $key.Start; Delayed = $key.DelayedAutoStart
                Status = [string]$service.Status; Exists = $null -ne $service -and $null -ne $key }
        }) | ConvertTo-Json -Compress
        """ + PowerShellService.AsciiJson;

    internal static IReadOnlyList<OptionalService> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonRead.Items(document.RootElement).Where(item => Names.Contains(JsonRead.Text(item, "Name")))
            .Select(item => new OptionalService(JsonRead.Text(item, "Name"), JsonRead.Int(item, "Start") ?? -1,
                JsonRead.Int(item, "Delayed"), JsonRead.Text(item, "Status"), JsonRead.Bool(item, "Exists"))).ToList();
    }

    public Task<IReadOnlyList<OptionalService>> ScanAsync() => _scan();

    private static async Task<IReadOnlyList<OptionalService>> ScanLiveAsync()
    {
        var result = await PowerShellService.Instance.ExecuteScriptAsync(ScanScript);
        if (!result.Success) throw new InvalidOperationException(result.Error);
        return Parse(result.Output);
    }

    internal static string BuildScript(string name, int start, int? delayed)
    {
        if (!CanManage(name) || start is not (2 or 3 or 4) || delayed is not (null or 0 or 1))
            throw new ArgumentException(nameof(name));
        var mode = start switch { 2 when delayed == 1 => "delayed-auto", 2 => "auto", 3 => "demand", _ => "disabled" };
        var key = PowerShellService.Literal($@"HKLM:\SYSTEM\CurrentControlSet\Services\{name}");
        return $$"""
            $ErrorActionPreference = 'Stop'
            Get-Service -Name '{{name}}' -ErrorAction Stop | Out-Null
            & sc.exe config '{{name}}' start= {{mode}}
            if ($LASTEXITCODE -ne 0) { throw 'Service configuration failed.' }
            {{(delayed.HasValue ? $"Set-ItemProperty -LiteralPath {key} -Name DelayedAutoStart -Type DWord -Value {delayed.Value}" : $"if ((Get-Item {key}).GetValueNames() -contains 'DelayedAutoStart') {{ Remove-ItemProperty -LiteralPath {key} -Name DelayedAutoStart }}")}}
            """;
    }

    public async Task<bool> SetModeAsync(OptionalService selected, int? start)
    {
        if (!selected.Exists || !CanManage(selected.Name) || (start is not null && _legacyManaged(selected.Name)) || start is not (null or 3 or 4)) return false;
        var current = (await ScanAsync()).Single(service => service.Name == selected.Name);
        if (!current.Exists || !CanManage(current.Name) || (start is not null && _legacyManaged(current.Name))
            || current.Start is not (2 or 3 or 4) || current.Delayed is not (null or 0 or 1)) return false;
        var backups = Load();
        OptionalService target;
        if (start is { } mode)
        {
            if (current.Start == mode) return true;
            if (!backups.ContainsKey(current.Name))
            {
                backups[current.Name] = current;
                JsonStore.Save(_path, backups);
            }
            target = current with { Start = mode };
        }
        else if (!backups.TryGetValue(current.Name, out target!)) return false;
        var result = await _run(BuildScript(target.Name, target.Start, target.Delayed));
        var verified = result.Success && (await ScanAsync()).Any(service => service.Name == target.Name
            && service.Start == target.Start && service.Delayed == target.Delayed);
        if (verified && start is null)
        {
            backups.Remove(current.Name);
            JsonStore.Save(_path, backups);
        }
        return verified;
    }
}
