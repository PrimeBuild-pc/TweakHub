using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace TweakHub.Services;

public sealed record PowerPlan(Guid Id, string Name, string Description, bool Active);

public sealed record PowerValueOption(uint Value, string Label);

public sealed record PowerSettingDefinition(Guid Subgroup, string SubgroupName, Guid Id, string Name, string Description, bool Hidden,
    IReadOnlyList<PowerValueOption> Options, uint Min, uint Max, uint Increment, string Units)
{
    public bool IsRange => Options.Count == 0;

    public string Format(uint value) => IsRange
        ? $"{value.ToString(CultureInfo.CurrentCulture)}{(Units.Length > 0 ? " " + Units : string.Empty)}"
        : Options.FirstOrDefault(option => option.Value == value)?.Label ?? value.ToString(CultureInfo.CurrentCulture);
}

/// <summary>One setting's value in a plan: what playbooks store and the editor writes.</summary>
public sealed record PowerSettingValue(Guid Subgroup, Guid Setting, uint Ac, uint Dc);

/// <summary>
/// Reads and writes every power setting through powrprof, the API behind powercfg: names and option labels come back
/// already localized, hidden settings included, and writing a plan's values needs no elevation.
/// </summary>
public static unsafe class PowerPlanService
{
    public static readonly Guid Balanced = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    private static readonly Guid NoSubgroup = new("fea3413e-7e05-4911-9a71-700331f1c294");
    private const uint AccessScheme = 16, AccessSubgroup = 17, AccessIndividualSetting = 18;
    private const uint AttributeHide = 1;
    private const uint ErrorSuccess = 0;
    // Guards the enumeration loops against a driver-defined setting that never reports its end.
    private const uint MaxItems = 512;

    private static IReadOnlyList<PowerSettingDefinition>? _definitions;
    private static readonly object DefinitionsLock = new();

    // Read once: the editor asks for every row, and a PC does not gain or lose a battery while the app runs.
    public static bool HasBattery { get; } = GetSystemPowerStatus(out var status) && status.BatteryFlag is not (128 or 255);

    public static IReadOnlyList<PowerPlan> GetPlans()
    {
        var active = ActiveScheme();
        return Enumerate(null, null, AccessScheme)
            .Select(id => new PowerPlan(id, ReadName(id, null, null), ReadDescription(id, null, null), id == active))
            .OrderBy(plan => plan.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Every setting Windows defines, read once: the definitions do not change while the app runs.</summary>
    public static IReadOnlyList<PowerSettingDefinition> LoadDefinitions()
    {
        lock (DefinitionsLock) return _definitions ??= ReadDefinitions();
    }

    private static List<PowerSettingDefinition> ReadDefinitions()
    {
        var definitions = new List<PowerSettingDefinition>();
        // Subgroups of the active plan cover every installed setting; NO_SUBGROUP holds settings such as "require a password on wake".
        var scheme = ActiveScheme();
        foreach (var subgroup in Enumerate(scheme, null, AccessSubgroup).Append(NoSubgroup).Distinct())
        {
            var subgroupName = subgroup == NoSubgroup ? string.Empty : ReadName(null, subgroup, null);
            foreach (var setting in Enumerate(scheme, subgroup, AccessIndividualSetting))
            {
                // Range first: for range settings PowerReadPossibleValue answers success for any index.
                uint min = 0, increment = 1;
                var hasRange = PowerReadValueMax(IntPtr.Zero, &subgroup, &setting, out var max) == ErrorSuccess;
                var options = hasRange ? [] : ReadOptions(subgroup, setting);
                if (options.Count == 0 && !hasRange) continue;
                if (hasRange)
                {
                    PowerReadValueMin(IntPtr.Zero, &subgroup, &setting, out min);
                    if (PowerReadValueIncrement(IntPtr.Zero, &subgroup, &setting, out increment) != ErrorSuccess || increment == 0) increment = 1;
                }
                definitions.Add(new PowerSettingDefinition(subgroup, subgroupName, setting, ReadName(null, subgroup, setting),
                    ReadDescription(null, subgroup, setting), (PowerReadSettingAttributes(&subgroup, &setting) & AttributeHide) != 0,
                    options, min, max, increment, hasRange ? ReadUnits(subgroup, setting) : string.Empty));
            }
        }
        return definitions;
    }

    public static (uint Ac, uint Dc)? ReadValue(Guid scheme, Guid subgroup, Guid setting) =>
        PowerReadACValueIndex(IntPtr.Zero, &scheme, &subgroup, &setting, out var ac) == ErrorSuccess
        && PowerReadDCValueIndex(IntPtr.Zero, &scheme, &subgroup, &setting, out var dc) == ErrorSuccess
            ? (ac, dc)
            : null;

    public static List<PowerSettingValue> ReadValues(Guid scheme) => LoadDefinitions()
        .Select(definition => ReadValue(scheme, definition.Subgroup, definition.Id) is { } value
            ? new PowerSettingValue(definition.Subgroup, definition.Id, value.Ac, value.Dc)
            : null)
        .OfType<PowerSettingValue>()
        .ToList();

    /// <summary>Writes the values and re-applies the plan when it is active; returns the values Windows refused.</summary>
    public static List<PowerSettingValue> WriteValues(Guid scheme, IEnumerable<PowerSettingValue> values)
    {
        var failed = new List<PowerSettingValue>();
        foreach (var value in values)
        {
            var subgroup = value.Subgroup;
            var setting = value.Setting;
            if (PowerWriteACValueIndex(IntPtr.Zero, &scheme, &subgroup, &setting, value.Ac) != ErrorSuccess
                | PowerWriteDCValueIndex(IntPtr.Zero, &scheme, &subgroup, &setting, value.Dc) != ErrorSuccess)
                failed.Add(value);
        }
        // Windows applies a plan's values when the plan is (re)activated.
        if (ActiveScheme() == scheme) PowerSetActiveScheme(IntPtr.Zero, &scheme);
        return failed;
    }

    /// <summary>The powercfg commands for values the API refused, run in one elevated script.</summary>
    internal static string BuildElevatedWriteScript(Guid scheme, IEnumerable<PowerSettingValue> values) => string.Join("\n", values.Select(value =>
        $"powercfg /setacvalueindex {scheme} {value.Subgroup} {value.Setting} {value.Ac}\npowercfg /setdcvalueindex {scheme} {value.Subgroup} {value.Setting} {value.Dc}"))
        + $"\nif ((powercfg /getactivescheme) -match '{scheme}') {{ powercfg /setactive {scheme} }}";

    public static bool Activate(Guid scheme) => PowerSetActiveScheme(IntPtr.Zero, &scheme) == ErrorSuccess;

    /// <summary>Copies <paramref name="source"/>; with <paramref name="requestedId"/> the copy gets that GUID instead of a new one.</summary>
    public static Guid? Duplicate(Guid source, string name, Guid? requestedId = null)
    {
        var requested = requestedId.GetValueOrDefault();
        IntPtr created = requestedId is null ? IntPtr.Zero : (IntPtr)(&requested);
        if (PowerDuplicateScheme(IntPtr.Zero, &source, &created) != ErrorSuccess || created == IntPtr.Zero) return null;
        var id = Marshal.PtrToStructure<Guid>(created);
        if (created != (IntPtr)(&requested)) LocalFree(created);
        Rename(id, name);
        return id;
    }

    public static bool Rename(Guid scheme, string name)
    {
        var bytes = Encoding.Unicode.GetBytes(name + "\0");
        fixed (byte* buffer = bytes) return PowerWriteFriendlyName(IntPtr.Zero, &scheme, null, null, buffer, (uint)bytes.Length) == ErrorSuccess;
    }

    /// <summary>Windows refuses to delete the active plan.</summary>
    public static bool Delete(Guid scheme) => PowerDeleteScheme(IntPtr.Zero, &scheme) == ErrorSuccess;

    public static Guid? Import(string path)
    {
        IntPtr created = IntPtr.Zero;
        if (PowerImportPowerScheme(IntPtr.Zero, path, &created) != ErrorSuccess || created == IntPtr.Zero) return null;
        var id = Marshal.PtrToStructure<Guid>(created);
        LocalFree(created);
        return id;
    }

    public static Guid? ActiveScheme()
    {
        IntPtr active = IntPtr.Zero;
        if (PowerGetActiveScheme(IntPtr.Zero, &active) != ErrorSuccess || active == IntPtr.Zero) return null;
        var id = Marshal.PtrToStructure<Guid>(active);
        LocalFree(active);
        return id;
    }

    private static List<Guid> Enumerate(Guid? scheme, Guid? subgroup, uint access)
    {
        var result = new List<Guid>();
        var schemeValue = scheme.GetValueOrDefault();
        var subgroupValue = subgroup.GetValueOrDefault();
        Guid item;
        for (uint index = 0; index < MaxItems; index++)
        {
            var size = (uint)sizeof(Guid);
            if (PowerEnumerate(IntPtr.Zero, scheme is null ? null : &schemeValue, subgroup is null ? null : &subgroupValue,
                    access, index, (byte*)&item, &size) != ErrorSuccess) break;
            result.Add(item);
        }
        return result;
    }

    private static List<PowerValueOption> ReadOptions(Guid subgroup, Guid setting)
    {
        var options = new List<PowerValueOption>();
        // Lambdas capture these pointers, never the GUIDs themselves: captured locals cannot have their address taken.
        Guid* group = &subgroup, item = &setting;
        for (uint index = 0; index < MaxItems; index++)
        {
            uint value = 0, size = sizeof(uint), type = 0;
            if (PowerReadPossibleValue(IntPtr.Zero, group, item, &type, index, (byte*)&value, &size) != ErrorSuccess) break;
            var current = index;
            var label = ReadString((buffer, length) => PowerReadPossibleFriendlyName(IntPtr.Zero, group, item, current, buffer, length));
            options.Add(new PowerValueOption(value, label.Length > 0 ? label : value.ToString(CultureInfo.CurrentCulture)));
        }
        return options;
    }

    private static string ReadName(Guid? scheme, Guid? subgroup, Guid? setting) =>
        ReadString((buffer, size) => WithGuids(scheme, subgroup, setting, (s, g, p) => PowerReadFriendlyName(IntPtr.Zero, s, g, p, buffer, size)));

    private static string ReadDescription(Guid? scheme, Guid? subgroup, Guid? setting) =>
        ReadString((buffer, size) => WithGuids(scheme, subgroup, setting, (s, g, p) => PowerReadDescription(IntPtr.Zero, s, g, p, buffer, size)));

    private static string ReadUnits(Guid subgroup, Guid setting)
    {
        Guid* group = &subgroup, item = &setting;
        return ReadString((buffer, size) => PowerReadValueUnitsSpecifier(IntPtr.Zero, group, item, buffer, size));
    }

    private delegate uint GuidCall(Guid* scheme, Guid* subgroup, Guid* setting);
    private delegate uint StringCall(byte* buffer, uint* size);

    private static uint WithGuids(Guid? scheme, Guid? subgroup, Guid? setting, GuidCall call)
    {
        var s = scheme.GetValueOrDefault();
        var g = subgroup.GetValueOrDefault();
        var p = setting.GetValueOrDefault();
        return call(scheme is null ? null : &s, subgroup is null ? null : &g, setting is null ? null : &p);
    }

    /// <summary>The powrprof string pattern: ask for the size first, then read the UTF-16 text.</summary>
    private static string ReadString(StringCall call)
    {
        uint size = 0;
        if (call(null, &size) != ErrorSuccess || size < 2) return string.Empty;
        var bytes = new byte[size];
        fixed (byte* buffer = bytes)
            if (call(buffer, &size) != ErrorSuccess) return string.Empty;
        return Encoding.Unicode.GetString(bytes).TrimEnd('\0').Trim();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte AcLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag;
        public int BatteryLifeTime, BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")] private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr memory);
    [DllImport("powrprof.dll")] private static extern uint PowerGetActiveScheme(IntPtr root, IntPtr* scheme);
    [DllImport("powrprof.dll")] private static extern uint PowerSetActiveScheme(IntPtr root, Guid* scheme);
    [DllImport("powrprof.dll")] private static extern uint PowerEnumerate(IntPtr root, Guid* scheme, Guid* subgroup, uint access, uint index, byte* buffer, uint* size);
    [DllImport("powrprof.dll")] private static extern uint PowerReadFriendlyName(IntPtr root, Guid* scheme, Guid* subgroup, Guid* setting, byte* buffer, uint* size);
    [DllImport("powrprof.dll")] private static extern uint PowerReadDescription(IntPtr root, Guid* scheme, Guid* subgroup, Guid* setting, byte* buffer, uint* size);
    [DllImport("powrprof.dll")] private static extern uint PowerWriteFriendlyName(IntPtr root, Guid* scheme, Guid* subgroup, Guid* setting, byte* buffer, uint size);
    [DllImport("powrprof.dll")] private static extern uint PowerReadPossibleValue(IntPtr root, Guid* subgroup, Guid* setting, uint* type, uint index, byte* buffer, uint* size);
    [DllImport("powrprof.dll")] private static extern uint PowerReadPossibleFriendlyName(IntPtr root, Guid* subgroup, Guid* setting, uint index, byte* buffer, uint* size);
    [DllImport("powrprof.dll")] private static extern uint PowerReadValueMin(IntPtr root, Guid* subgroup, Guid* setting, out uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerReadValueMax(IntPtr root, Guid* subgroup, Guid* setting, out uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerReadValueIncrement(IntPtr root, Guid* subgroup, Guid* setting, out uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerReadValueUnitsSpecifier(IntPtr root, Guid* subgroup, Guid* setting, byte* buffer, uint* size);
    [DllImport("powrprof.dll")] private static extern uint PowerReadSettingAttributes(Guid* subgroup, Guid* setting);
    [DllImport("powrprof.dll")] private static extern uint PowerReadACValueIndex(IntPtr root, Guid* scheme, Guid* subgroup, Guid* setting, out uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerReadDCValueIndex(IntPtr root, Guid* scheme, Guid* subgroup, Guid* setting, out uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerWriteACValueIndex(IntPtr root, Guid* scheme, Guid* subgroup, Guid* setting, uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerWriteDCValueIndex(IntPtr root, Guid* scheme, Guid* subgroup, Guid* setting, uint value);
    [DllImport("powrprof.dll")] private static extern uint PowerDuplicateScheme(IntPtr root, Guid* source, IntPtr* destination);
    [DllImport("powrprof.dll")] private static extern uint PowerDeleteScheme(IntPtr root, Guid* scheme);
    [DllImport("powrprof.dll", CharSet = CharSet.Unicode)] private static extern uint PowerImportPowerScheme(IntPtr root, string path, IntPtr* destination);
}
