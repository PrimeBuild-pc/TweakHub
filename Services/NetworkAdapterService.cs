using System.IO;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace TweakHub.Services;

public sealed record NetworkAdapter(
    string InstanceId, string Name, string Model, string Vendor, bool Wireless, bool Connected,
    IReadOnlyList<RegistryValueChange> Changes)
{
    public bool IsSupported => Changes.Count > 0;
}

/// <summary>
/// The low-latency network preset. Only adapters whose vendor settings are documented here are touched, and only
/// settings the driver itself declares with the "off" value, so an unknown driver is never changed blindly.
/// </summary>
public static partial class NetworkAdapterService
{
    internal const string ClassPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
    internal const string ClassKey = @"HKEY_LOCAL_MACHINE\" + ClassPath;
    private const int EthernetIfType = 6;
    private const int WirelessIfType = 71;

    // Interrupt moderation and power saving off. Standard NDIS keywords (*) mean the same on every driver;
    // the others are from Intel and Realtek driver documentation.
    private static readonly (string Keyword, string Off)[] IntelEthernet =
        [("*InterruptModeration", "0"), ("*EEE", "0"), ("EEELinkAdvertisement", "0"), ("ULPMode", "0"), ("SipsEnabled", "0")];
    private static readonly (string Keyword, string Off)[] RealtekEthernet =
        [("*InterruptModeration", "0"), ("*EEE", "0"), ("AdvancedEEE", "0"), ("EnableGreenEthernet", "0"), ("GigaLite", "0"), ("PowerSavingMode", "0")];
    // Wi-Fi: power saving and packet batching only; 3 is "No SMPS" (MIMO power save off).
    private static readonly (string Keyword, string Off)[] IntelWireless =
        [("*PacketCoalescing", "0"), ("MIMOPowerSaveMode", "3"), ("uAPSDSupport", "0")];

    internal static readonly string[] ManagedKeywords =
        [.. IntelEthernet.Concat(RealtekEthernet).Concat(IntelWireless).Select(setting => setting.Keyword).Distinct()];

    private static string SelectionPath => Path.Combine(AppDataPath.BasePath, "network-adapters.json");

    internal static string VendorName(string vendorId) => vendorId switch
    {
        "8086" => "Intel",
        "10EC" or "0BDA" => "Realtek",
        _ => vendorId
    };

    /// <summary>The documented settings for a vendor (PCI or USB ID) and adapter type; empty when undocumented.</summary>
    internal static (string Keyword, string Off)[] SettingsFor(string vendorId, bool wireless) => (vendorId, wireless) switch
    {
        ("8086", false) => IntelEthernet,
        ("8086", true) => IntelWireless,
        ("10EC" or "0BDA", false) => RealtekEthernet,
        _ => []
    };

    internal static string VendorId(string? hardwareId) =>
        hardwareId is not null && VendorPattern().Match(hardwareId) is { Success: true } match ? match.Groups[1].Value.ToUpperInvariant() : string.Empty;

    [GeneratedRegex(@"(?:VEN|VID)_([0-9A-Fa-f]{4})")]
    private static partial Regex VendorPattern();

    /// <summary>Installed physical Ethernet and Wi-Fi adapters; ghosts of removed adapters are skipped.</summary>
    public static IReadOnlyList<NetworkAdapter> GetAdapters()
    {
        Dictionary<string, NetworkInterface> present;
        try
        {
            // Windows still lists removed adapters, as NotPresent.
            present = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus != OperationalStatus.NotPresent)
                .GroupBy(nic => nic.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }
        catch (NetworkInformationException)
        {
            return [];
        }

        var adapters = new List<NetworkAdapter>();
        try
        {
            using var root = Registry.LocalMachine.OpenSubKey(ClassPath);
            foreach (var name in root?.GetSubKeyNames().Where(name => name.All(char.IsAsciiDigit)) ?? [])
            {
                using var key = root!.OpenSubKey(name);
                // NCF_PHYSICAL (0x4): skips virtual, VPN and Hyper-V adapters.
                if (key?.GetValue("Characteristics") is not int characteristics || (characteristics & 4) == 0
                    || key.GetValue("*IfType") is not int ifType || ifType is not (EthernetIfType or WirelessIfType)
                    || key.GetValue("NetCfgInstanceId") is not string instanceId || !present.TryGetValue(instanceId, out var nic))
                    continue;
                var wireless = ifType == WirelessIfType;
                var vendor = VendorId(key.GetValue("MatchingDeviceId") as string ?? key.GetValue("ComponentId") as string);
                var changes = SettingsFor(vendor, wireless)
                    .Where(setting => key.GetValue(setting.Keyword) is string && DeclaresValue(key, setting.Keyword, setting.Off))
                    .Select(setting => new RegistryValueChange($@"{ClassKey}\{name}", setting.Keyword, setting.Off, RegistryValueKind.String))
                    .ToList();
                // The interface description tells identical adapters apart ("... I226-V #2").
                adapters.Add(new(instanceId, nic.Name, nic.Description, VendorName(vendor),
                    wireless, nic.OperationalStatus == OperationalStatus.Up, changes));
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
        }
        return adapters;
    }

    /// <summary>The driver lists the values it accepts for each setting; the "off" value must be one of them.</summary>
    private static bool DeclaresValue(RegistryKey adapter, string keyword, string value)
    {
        try
        {
            using var values = adapter.OpenSubKey($@"Ndi\Params\{keyword}\enum");
            return values?.GetValueNames().Contains(value, StringComparer.OrdinalIgnoreCase) == true;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>The adapters chosen last time, or by default the connected supported ones (all supported if none is connected).</summary>
    public static IReadOnlyList<NetworkAdapter> Selected(IReadOnlyList<NetworkAdapter> adapters)
    {
        var saved = JsonStore.Load<List<string>>(SelectionPath);
        var selected = adapters.Where(adapter => adapter.IsSupported && saved.Contains(adapter.InstanceId, StringComparer.OrdinalIgnoreCase)).ToList();
        return selected.Count > 0 ? selected : DefaultSelection(adapters);
    }

    internal static IReadOnlyList<NetworkAdapter> DefaultSelection(IReadOnlyList<NetworkAdapter> adapters)
    {
        var supported = adapters.Where(adapter => adapter.IsSupported).ToList();
        var connected = supported.Where(adapter => adapter.Connected).ToList();
        return connected.Count > 0 ? connected : supported;
    }

    public static void SaveSelection(IEnumerable<NetworkAdapter> adapters) =>
        JsonStore.Save(SelectionPath, adapters.Select(adapter => adapter.InstanceId).ToList());

    public static IReadOnlyList<RegistryValueChange> PresetChanges() => [.. Selected(GetAdapters()).SelectMany(adapter => adapter.Changes)];

    /// <summary>Every adapter value the preset changed, including adapters no longer selected, so restore puts all of them back.</summary>
    public static IReadOnlyList<RegistryValueChange> BackedUpChanges() =>
    [
        .. RegistryService.Instance.BackedUpValues
            .Where(value => value.KeyPath.StartsWith(ClassKey + @"\", StringComparison.OrdinalIgnoreCase)
                && ManagedKeywords.Contains(value.ValueName, StringComparer.OrdinalIgnoreCase))
            .Select(value => new RegistryValueChange(value.KeyPath, value.ValueName, null))
    ];
}
