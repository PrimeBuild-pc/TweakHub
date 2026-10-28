using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using TweakHub.Localization;

namespace TweakHub.Services;

/// <summary>A value under a Policies key. Owned values belong to a TweakHub tweak and are left to Registry Tweaks.</summary>
public sealed record PolicyValue(string KeyPath, string Name, string Data, bool Owned)
{
    public string Line => $"{KeyPath}\\{(Name.Length == 0 ? "(Default)" : Name)} = {Data}{(Owned ? "   [TweakHub]" : string.Empty)}";
}

/// <summary>The forced policies of one area (for example Microsoft\Windows\WindowsUpdate or Google\Chrome).</summary>
public sealed class PolicyCategory(string name, IReadOnlyList<PolicyValue> values, IReadOnlyList<RegistryValueChange> undoable) : INotifyPropertyChanged
{
    private bool _isPreviewVisible;
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; } = name;
    public IReadOnlyList<PolicyValue> Values { get; } = values;
    /// <summary>Values this view deleted earlier and can put back.</summary>
    public IReadOnlyList<RegistryValueChange> Undoable { get; } = undoable;
    public IReadOnlyList<PolicyValue> Resettable => Values.Where(value => !value.Owned).ToList();
    public bool CanReset => Values.Any(value => !value.Owned);
    public bool CanUndo => Undoable.Count > 0;
    public string Summary => L.Format("Security:PoliciesCategorySummary", Values.Count, Resettable.Count, Undoable.Count);
    public string Preview => Values.Count == 0 ? L.Get("Security:PoliciesAllReset") : string.Join("\n", Values.Select(value => value.Line));
    public bool IsPreviewVisible
    {
        get => _isPreviewVisible;
        set { _isPreviewVisible = value; PropertyChanged?.Invoke(this, new(nameof(IsPreviewVisible))); }
    }
}

/// <summary>
/// Lists policies forced under HKLM/HKCU\SOFTWARE\Policies (often by other tweak tools) and puts them back to the Windows
/// default by deleting the values. Every reset keeps a registry backup, so it can be undone.
/// </summary>
public sealed class PolicyService
{
    public static PolicyService Instance { get; } = new();
    private PolicyService() { }

    internal const string MachineRoot = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies";
    internal const string UserRoot = @"HKEY_CURRENT_USER\SOFTWARE\Policies";
    private const int MaxValues = 5000;

    public Task<IReadOnlyList<PolicyCategory>> ScanAsync()
    {
        // The tweak catalogue is bound to the UI: read it on the calling thread, walk the registry in the background.
        var owned = OwnedLocations();
        return Task.Run<IReadOnlyList<PolicyCategory>>(() => Scan(owned));
    }

    private static IReadOnlyList<PolicyCategory> Scan(HashSet<string> owned)
    {
        var values = new List<(string KeyPath, string Name, string Data)>();
        using (var machine = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies")) Read(machine, MachineRoot, values);
        using (var user = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Policies")) Read(user, UserRoot, values);
        var backups = RegistryService.Instance.BackedUpValues
            .Where(value => IsUnderPolicies(value.KeyPath) && !owned.Contains(RegistryService.BackupKey(value.KeyPath, value.ValueName)));
        return Group(values, owned, backups);
    }

    internal static void Read(RegistryKey? key, string path, List<(string KeyPath, string Name, string Data)> values, int depth = 0)
    {
        if (key is null || depth > 16 || values.Count >= MaxValues) return;
        foreach (var name in key.GetValueNames())
        {
            if (values.Count >= MaxValues) return;
            values.Add((path, name, Format(key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames))));
        }
        foreach (var child in key.GetSubKeyNames())
        {
            try
            {
                using var sub = key.OpenSubKey(child);
                Read(sub, $"{path}\\{child}", values, depth + 1);
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or System.IO.IOException) { }
        }
    }

    private static string Format(object? value)
    {
        var text = value switch
        {
            int number => $"{number} (0x{number:X})",
            long number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            string[] lines => string.Join("; ", lines),
            byte[] bytes => Convert.ToHexString(bytes.Take(32).ToArray()) + (bytes.Length > 32 ? "…" : string.Empty),
            null => string.Empty,
            _ => value.ToString() ?? string.Empty
        };
        return text.Length > 200 ? text[..200] + "…" : text;
    }

    internal static bool IsUnderPolicies(string keyPath)
    {
        var canonical = RegistryService.BackupKey(keyPath, string.Empty).TrimEnd('\\');
        return canonical.StartsWith(MachineRoot + "\\", StringComparison.OrdinalIgnoreCase)
            || canonical.StartsWith(UserRoot + "\\", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Microsoft\Windows\X and Microsoft\Windows NT\X areas are split one level deeper than vendor keys such as Google\Chrome.</summary>
    internal static string CategoryOf(string keyPath)
    {
        var canonical = RegistryService.BackupKey(keyPath, string.Empty).TrimEnd('\\');
        var root = canonical.StartsWith(MachineRoot, StringComparison.OrdinalIgnoreCase) ? MachineRoot : UserRoot;
        var parts = canonical.Length > root.Length ? canonical[(root.Length + 1)..].Split('\\') : [];
        var depth = parts.Length > 1 && parts[0].Equals("Microsoft", StringComparison.OrdinalIgnoreCase)
                    && parts[1] is "Windows" or "Windows NT" ? 3 : 2;
        var scope = root == MachineRoot ? "HKLM" : "HKCU";
        return parts.Length == 0 ? scope : $"{scope} · {string.Join("\\", parts.Take(depth))}";
    }

    internal static List<PolicyCategory> Group(IEnumerable<(string KeyPath, string Name, string Data)> values, ISet<string> owned,
        IEnumerable<(string KeyPath, string ValueName)> backups)
    {
        var present = values.Select(value => new PolicyValue(value.KeyPath, value.Name, value.Data,
            owned.Contains(RegistryService.BackupKey(value.KeyPath, value.Name)))).ToList();
        // A backup whose value is back (a policy refresh, or set again by the other tool) can still be undone.
        var undoable = backups.Select(backup => new RegistryValueChange(backup.KeyPath, backup.ValueName, null)).ToList();
        return present.Select(value => CategoryOf(value.KeyPath)).Concat(undoable.Select(change => CategoryOf(change.KeyPath)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(category => new PolicyCategory(category,
                present.Where(value => CategoryOf(value.KeyPath).Equals(category, StringComparison.OrdinalIgnoreCase)).ToList(),
                undoable.Where(change => CategoryOf(change.KeyPath).Equals(category, StringComparison.OrdinalIgnoreCase)).ToList()))
            .OrderByDescending(category => category.CanReset).ThenBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Registry locations written by TweakHub's own tweaks, presets and the user's custom tweaks.</summary>
    internal static HashSet<string> OwnedLocations()
    {
        if (TweakService.Instance.TweakCategories.Count == 0) TweakService.Instance.LoadTweaks();
        var tweaks = TweakService.Instance.TweakCategories.SelectMany(category => category.Tweaks).ToList();
        return tweaks.Where(tweak => tweak.RegistryPath.Length > 0 && tweak.RegistryKey.Length > 0)
            .Select(tweak => (tweak.RegistryPath, tweak.RegistryKey))
            .Concat(tweaks.SelectMany(tweak => TweakService.GetCompositeRegistryChanges(tweak.Id)).Select(change => (change.KeyPath, change.ValueName)))
            .Concat(WindowsUpdatePresetService.PresetRegistryValues.Select(change => (change.KeyPath, change.ValueName)))
            .Concat(UserDataService.Instance.LoadCustomTweaks().Select(tweak => (tweak.RegistryPath, tweak.RegistryKey)))
            .Select(location => RegistryService.BackupKey(location.Item1, location.Item2))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public Task<bool> ResetAsync(PolicyCategory category) =>
        RegistryService.Instance.ApplyValuesWithBackupAsync(category.Resettable
            .Select(value => new RegistryValueChange(value.KeyPath, value.Name, null)).ToList());

    public Task<bool> UndoAsync(PolicyCategory category) => RegistryService.Instance.RestoreValuesAsync(category.Undoable);

    /// <summary>Domain-joined or MDM-enrolled PCs get their policies back at the next sync: resetting them is pointless there.</summary>
    public static bool IsManagedPc()
    {
        try
        {
            if (NetGetJoinInformation(null, out var name, out var status) == 0)
            {
                NetApiBufferFree(name);
                if (status == 3) return true; // NetSetupDomainName
            }
            using var enrollments = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Enrollments");
            return enrollments?.GetSubKeyNames().Any(id =>
            {
                using var enrollment = enrollments.OpenSubKey(id);
                return IsOrganizationEnrollment(enrollment?.GetValue("ProviderID") as string, enrollment?.GetValue("EnrollmentState"));
            }) == true;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or System.IO.IOException or DllNotFoundException)
        {
            return false;
        }
    }

    // Every Windows 11 PC has these local enrollments; only another provider (Intune is "MS DM Server") means an organization.
    private static readonly HashSet<string> BuiltInEnrollments = new(StringComparer.OrdinalIgnoreCase)
        { "Local Authority", "Deploy Authority", "Cloud Authority" };

    internal static bool IsOrganizationEnrollment(string? provider, object? state) =>
        provider is { Length: > 0 } && !BuiltInEnrollments.Contains(provider) && state is 1;

    [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
    private static extern int NetGetJoinInformation(string? server, out IntPtr name, out int status);

    [DllImport("netapi32.dll")]
    private static extern int NetApiBufferFree(IntPtr buffer);
}
