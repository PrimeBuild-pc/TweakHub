using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

public partial class PowerPlanPage : Page
{
    /// <summary>One setting of the edited plan, with its value in the comparison plan.</summary>
    public sealed class SettingRow(PowerSettingDefinition definition, (uint Ac, uint Dc) value, (uint Ac, uint Dc)? compare, string compareName)
        : INotifyPropertyChanged
    {
        private uint _ac = value.Ac, _dc = value.Dc;

        public PowerSettingDefinition Definition { get; } = definition;
        public uint OriginalAc { get; } = value.Ac;
        public uint OriginalDc { get; } = value.Dc;
        public (uint Ac, uint Dc)? Compare { get; } = compare;
        public string Name => Definition.Name;
        public string Description => Definition.Description;
        public string Tooltip => $"{Definition.SubgroupName}\n{Definition.Subgroup}\n{Definition.Id}".Trim();
        public IReadOnlyList<PowerValueOption> Options => Definition.Options;
        public double Minimum => Definition.Min;
        public double Maximum => Definition.Max;
        public double Increment => Definition.Increment;
        public string Units => Definition.Units;

        public uint Ac { get => _ac; set => Set(ref _ac, value); }
        public uint Dc { get => _dc; set => Set(ref _dc, value); }
        // NumberBox reports NaN while the box is empty: keep the last valid value.
        public double AcNumber { get => _ac; set { if (!double.IsNaN(value)) Ac = Clamp(value); } }
        public double DcNumber { get => _dc; set { if (!double.IsNaN(value)) Dc = Clamp(value); } }

        public bool IsModified => _ac != OriginalAc || _dc != OriginalDc;
        public bool DiffersFromCompare => Compare is { } other && (other.Ac != _ac || PowerPlanService.HasBattery && other.Dc != _dc);
        public string CompareText => Compare is { } other
            ? L.Format("Tweaks:PowerCompareValue", compareName, PowerPlanService.HasBattery
                ? $"{Definition.Format(other.Ac)} / {Definition.Format(other.Dc)}"
                : Definition.Format(other.Ac))
            : string.Empty;

        public Visibility HiddenVisibility => Definition.Hidden ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ModifiedVisibility => IsModified ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CompareVisibility => DiffersFromCompare ? Visibility.Visible : Visibility.Collapsed;
        public static Visibility DcVisibility => PowerPlanService.HasBattery ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? Edited;

        public void UseCompare()
        {
            if (Compare is not { } other) return;
            Ac = other.Ac;
            Dc = other.Dc;
        }

        // Max/Min instead of Math.Clamp: tweaking tools sometimes leave a maximum below the minimum.
        private uint Clamp(double number) => (uint)Math.Max(Definition.Min, Math.Min(Definition.Max, Math.Round(number)));

        private void Set(ref uint field, uint newValue, [CallerMemberName] string? name = null)
        {
            if (field == newValue) return;
            field = newValue;
            foreach (var property in new[] { name!, name + "Number", nameof(IsModified), nameof(ModifiedVisibility), nameof(DiffersFromCompare), nameof(CompareVisibility) })
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            Edited?.Invoke();
        }
    }

    private sealed record SubgroupItem(Guid? Id, string Label, int Count);

    private sealed record PlanItem(PowerPlan Plan)
    {
        public string Detail => Plan.Description.Length > 0 ? $"{Plan.Description} · {Plan.Id}" : Plan.Id.ToString();
        public Visibility ActiveVisibility => Plan.Active ? Visibility.Visible : Visibility.Collapsed;
        public Visibility InactiveVisibility => Plan.Active ? Visibility.Collapsed : Visibility.Visible;
        public bool CanDelete => !Plan.Active;
    }

    private sealed record PowerPlanOption(string Guid, string Label);

    private static readonly PowerPlan NoComparison = new(Guid.Empty, L.Get("Tweaks:PowerNoComparison"), string.Empty, false);

    private List<SettingRow> _rows = [];
    private ICollectionView? _view;
    private bool _loaded;
    private bool _suppressPlanChange;
    private PowerPlan? _editedPlan;

    public PowerPlanPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (_loaded) return;
            _loaded = true;
            await Preload.Get(Preload.PowerSchemes, () => Task.Run(PowerPlanService.LoadDefinitions));
            RefreshPlans();
            await RefreshPerformancePlanAsync();
        };
    }

    private int PendingCount => _rows.Count(row => row.IsModified);

    /// <summary>Re-reads the plan list and keeps the edited and compared plans when they still exist.</summary>
    private void RefreshPlans(Guid? edit = null)
    {
        var plans = PowerPlanService.GetPlans();
        PlansList.ItemsSource = plans.Select(plan => new PlanItem(plan)).ToList();

        _suppressPlanChange = true;
        var editId = edit ?? _editedPlan?.Id ?? plans.FirstOrDefault(plan => plan.Active)?.Id;
        EditPlanBox.ItemsSource = plans;
        EditPlanBox.SelectedItem = plans.FirstOrDefault(plan => plan.Id == editId) ?? plans.FirstOrDefault();
        var compareId = (ComparePlanBox.SelectedItem as PowerPlan)?.Id ?? PowerPlanService.Balanced;
        var compareChoices = plans.Prepend(NoComparison).ToList();
        ComparePlanBox.ItemsSource = compareChoices;
        ComparePlanBox.SelectedItem = compareChoices.FirstOrDefault(plan => plan.Id == compareId && plan.Id != editId) ?? NoComparison;
        _suppressPlanChange = false;
        LoadRows();
    }

    private void LoadRows()
    {
        _editedPlan = EditPlanBox.SelectedItem as PowerPlan;
        var compare = ComparePlanBox.SelectedItem is PowerPlan { Id: var id } plan && id != Guid.Empty ? plan : null;
        _rows = [];
        if (_editedPlan is not null)
        {
            foreach (var definition in PowerPlanService.LoadDefinitions())
            {
                if (PowerPlanService.ReadValue(_editedPlan.Id, definition.Subgroup, definition.Id) is not { } value) continue;
                var row = new SettingRow(definition, value,
                    compare is null ? null : PowerPlanService.ReadValue(compare.Id, definition.Subgroup, definition.Id), compare?.Name ?? string.Empty);
                row.Edited += UpdatePending;
                _rows.Add(row);
            }
        }

        var selected = (SubgroupList.SelectedItem as SubgroupItem)?.Id;
        var groups = _rows.GroupBy(row => row.Definition.Subgroup)
            .Select(group => new SubgroupItem(group.Key, group.First().Definition.SubgroupName is { Length: > 0 } name
                ? name
                : $"{L.Get("Tweaks:PowerOtherSettings")} · {group.Key.ToString()[..8]}", group.Count()))
            .OrderBy(group => group.Label, StringComparer.CurrentCultureIgnoreCase)
            .Prepend(new SubgroupItem(null, L.Get("Tweaks:PowerAllSettings"), _rows.Count))
            .ToList();
        SubgroupList.ItemsSource = groups;
        SubgroupList.SelectedItem = groups.FirstOrDefault(group => group.Id == selected) ?? groups[0];

        _view = CollectionViewSource.GetDefaultView(_rows);
        _view.Filter = Matches;
        SettingsList.ItemsSource = _view;
        UpdateEmptyState();
        UpdatePending();
    }

    private bool Matches(object item)
    {
        var row = (SettingRow)item;
        var query = SearchBox.Text.Trim();
        return (SubgroupList.SelectedItem is not SubgroupItem { Id: { } subgroup } || row.Definition.Subgroup == subgroup)
               && (ShowHiddenBox.IsChecked == true || !row.Definition.Hidden)
               && (DifferentOnlyBox.IsChecked != true || row.DiffersFromCompare || row.IsModified)
               && (query.Length == 0
                   || row.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                   || row.Description.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                   || row.Definition.SubgroupName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                   || row.Definition.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (_view is null) return;
        _view.Refresh();
        UpdateEmptyState();
    }

    private void UpdateEmptyState() =>
        NoSettingsText.Visibility = _view?.IsEmpty != false ? Visibility.Visible : Visibility.Collapsed;

    private void UpdatePending()
    {
        var pending = PendingCount;
        PendingText.Text = pending == 0
            ? L.Format("Tweaks:PowerNoPending", _editedPlan?.Name ?? string.Empty)
            : L.Format("Tweaks:PowerPending", pending);
        ApplyButton.IsEnabled = DiscardButton.IsEnabled = pending > 0;
    }

    private async void EditPlan_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressPlanChange) return;
        if (PendingCount > 0 && !await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Tweaks:PowerDiscardTitle"),
                L.Format("Tweaks:PowerDiscardMessage", PendingCount), L.Get("Tweaks:PowerDiscard"), L.Get("UI:Cancel")))
        {
            _suppressPlanChange = true;
            EditPlanBox.SelectedItem = _editedPlan;
            _suppressPlanChange = false;
            return;
        }
        LoadRows();
    }

    private void ComparePlan_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressPlanChange) return;
        // Edited values survive a change of comparison plan.
        var edits = _rows.Where(row => row.IsModified).ToDictionary(row => row.Definition.Id, row => (row.Ac, row.Dc));
        LoadRows();
        foreach (var row in _rows)
            if (edits.TryGetValue(row.Definition.Id, out var edit)) (row.Ac, row.Dc) = edit;
    }

    private void UseCompare_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is SettingRow row) row.UseCompare();
    }

    private void Discard_Click(object sender, RoutedEventArgs e) => LoadRows();

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (_editedPlan is not { } plan) return;
        var changes = _rows.Where(row => row.IsModified)
            .Select(row => new PowerSettingValue(row.Definition.Subgroup, row.Definition.Id, row.Ac, row.Dc)).ToList();
        ApplyButton.IsEnabled = false;
        var failed = await Task.Run(() => PowerPlanService.WriteValues(plan.Id, changes));
        // Some settings are locked to administrators: retry only those, in one UAC prompt.
        if (failed.Count > 0)
        {
            var result = await PowerShellService.Instance.ExecuteScriptAsync(PowerPlanService.BuildElevatedWriteScript(plan.Id, failed), requireAdministrator: true);
            if (!result.Success)
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:PowerApplyFailed"),
                    L.Format("Tweaks:PowerApplyFailedMessage", failed.Count, result.Error));
        }
        LoadRows();
        PendingText.Text = L.Format("Tweaks:PowerApplied", changes.Count, plan.Name);
    }

    // Plans tab

    private static PowerPlan? PlanOf(object sender) => ((sender as FrameworkElement)?.DataContext as PlanItem)?.Plan;

    private async void ActivatePlan_Click(object sender, RoutedEventArgs e)
    {
        if (PlanOf(sender) is not { } plan) return;
        if (!PowerPlanService.Activate(plan.Id))
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:PowerActionFailed"), plan.Name);
        RefreshPlans();
        await RefreshPerformancePlanAsync();
    }

    private void EditPlan_Click(object sender, RoutedEventArgs e)
    {
        if (PlanOf(sender) is not { } plan) return;
        EditPlanBox.SelectedItem = (EditPlanBox.ItemsSource as IEnumerable<PowerPlan>)?.FirstOrDefault(item => item.Id == plan.Id);
        Tabs.SelectedItem = EditorTab;
    }

    private async void DuplicatePlan_Click(object sender, RoutedEventArgs e)
    {
        if (PlanOf(sender) is not { } plan
            || await AppDialog.PromptAsync(Window.GetWindow(this), L.Get("Tweaks:PowerDuplicate"), L.Get("Tweaks:PowerNewName"),
                L.Format("Tweaks:PowerCopyName", plan.Name)) is not { } name) return;
        if (PowerPlanService.Duplicate(plan.Id, name) is null)
        {
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:PowerActionFailed"), plan.Name);
            return;
        }
        RefreshPlans();
        await RefreshPerformancePlanAsync();
    }

    private async void RenamePlan_Click(object sender, RoutedEventArgs e)
    {
        if (PlanOf(sender) is not { } plan
            || await AppDialog.PromptAsync(Window.GetWindow(this), L.Get("Tweaks:PowerRename"), L.Get("Tweaks:PowerNewName"), plan.Name) is not { } name) return;
        if (!PowerPlanService.Rename(plan.Id, name))
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:PowerActionFailed"), plan.Name);
        RefreshPlans();
        await RefreshPerformancePlanAsync();
    }

    private async void DeletePlan_Click(object sender, RoutedEventArgs e)
    {
        if (PlanOf(sender) is not { } plan
            || !await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Tweaks:PowerDelete"), L.Format("Tweaks:PowerDeleteMessage", plan.Name),
                L.Get("Tweaks:PowerDelete"), L.Get("UI:Cancel"), DialogKind.Warning)) return;
        if (!PowerPlanService.Delete(plan.Id))
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:PowerActionFailed"), plan.Name);
        RefreshPlans(plan.Id == _editedPlan?.Id ? PowerPlanService.ActiveScheme() : null);
        await RefreshPerformancePlanAsync();
    }

    private async void ExportPlan_Click(object sender, RoutedEventArgs e)
    {
        if (PlanOf(sender) is not { } plan) return;
        var dialog = new SaveFileDialog { Filter = L.Get("Tweaks:PowerFileFilter"), FileName = string.Concat(plan.Name.Split(System.IO.Path.GetInvalidFileNameChars())) + ".pow" };
        if (dialog.ShowDialog() != true) return;
        // powercfg /export needs the backup privilege, so this one action asks for UAC.
        var result = await PowerService.RunMutationAsync("/export", dialog.FileName, plan.Id.ToString());
        if (!result.Success) await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:PowerActionFailed"), result.Error);
    }

    private async void ImportPlan_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = L.Get("Tweaks:PowerFileFilter") };
        if (dialog.ShowDialog() != true) return;
        if (PowerPlanService.Import(dialog.FileName) is not { } created)
        {
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:PowerActionFailed"), dialog.FileName);
            return;
        }
        RefreshPlans(created);
        await RefreshPerformancePlanAsync();
    }

    private void OpenPowerOptions_Click(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo("powercfg.cpl") { UseShellExecute = true });

    // Plan used by the "Force high performance" tweak (moved here from Registry Tweaks).

    private async Task RefreshPerformancePlanAsync()
    {
        var schemes = await PowerService.Instance.GetSchemesAsync();
        var options = schemes.Select(scheme => new PowerPlanOption(scheme.Guid,
            scheme.Active ? L.Format("Tweaks:PowerPlanActive", scheme.Name) : scheme.Name)).ToList();
        // Debloaters often delete the Windows plan; offer it anyway, TweakHub recreates it from the template.
        if (schemes.All(scheme => scheme.Guid != PowerService.HighPerformanceScheme))
            options.Add(new PowerPlanOption(PowerService.HighPerformanceScheme, L.Get("Tweaks:PowerPlanRecreateHighPerformance")));

        var selected = PowerService.Instance.PreferredScheme ?? schemes.FirstOrDefault(scheme => scheme.Active)?.Guid;
        PowerPlanComboBox.ItemsSource = options;
        PowerPlanComboBox.SelectedItem = options.FirstOrDefault(option => option.Guid == selected) ?? options.FirstOrDefault();
        var active = schemes.FirstOrDefault(scheme => scheme.Active);
        PowerPlanStatusText.Text = active is null ? string.Empty : L.Format("Tweaks:PowerPlanCurrent", active.Name);
    }

    private async void ApplyPowerPlan_Click(object sender, RoutedEventArgs e)
    {
        if (PowerPlanComboBox.SelectedItem is not PowerPlanOption option) return;
        ApplyPowerPlanButton.IsEnabled = false;
        try
        {
            if (!await PowerService.Instance.ApplyPerformancePlanAsync(option.Guid))
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:PowerPlanApplyFailed"), L.Get("Tweaks:PowerPlanApplyFailedMessage"));
                return;
            }
            TweakService.Instance.RefreshBackupState();
            RefreshPlans();
            await RefreshPerformancePlanAsync();
        }
        finally
        {
            ApplyPowerPlanButton.IsEnabled = true;
        }
    }
}
