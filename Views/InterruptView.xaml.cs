using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

public partial class InterruptView : UserControl
{
    private IReadOnlyList<InterruptDevice> _devices = [];
    private string? _classFilter;
    private readonly List<ToggleButton> _cpuButtons = [];
    private readonly bool _canSetAffinity;
    private bool _affinityOnly;
    public bool AffinityOnly
    {
        get => _affinityOnly;
        set
        {
            _affinityOnly = value;
            DescriptionText.Text = L.Get(value ? "Maintenance:AffinityDescription" : "Devices:InterruptsDescription");
            SummaryGrid.Columns = value ? 2 : 3;
            MsiTile.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
            ModeLabel.Visibility = MsiCheckBox.Visibility = PriorityLabel.Visibility = PriorityBox.Visibility = MsiChartPanel.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
            AffinityLabel.Visibility = AffinityEditor.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            foreach (var index in new[] { 2, 3, 4 }) DevicesGrid.Columns[index].Visibility = value ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public InterruptView()
    {
        InitializeComponent();
        foreach (var priority in Enum.GetValues<InterruptPriority>())
            PriorityBox.Items.Add(new ComboBoxItem { Content = L.Get($"Devices:Priority{priority}"), Tag = priority });
        var topology = CpuTopology.Read();
        _canSetAffinity = CpuTopology.Supported && topology.Count == CpuTopology.Count && topology.Count > 0;
        AffinityHelp.Text = L.Get(_canSetAffinity ? "Maintenance:AffinityHint" : "Maintenance:AffinityUnsupported");
        foreach (var cpu in topology)
        {
            var label = L.Format("Maintenance:CpuLabel", cpu.Index, cpu.Core);
            var button = new ToggleButton { Content = label, Margin = new Thickness(0, 0, 6, 6), MinWidth = 96, Tag = cpu.Index };
            AutomationProperties.SetName(button, label);
            _cpuButtons.Add(button);
            CpuPanel.Children.Add(button);
        }
        AffinityOnly = false;
        LegendMsi.Content = new WrapPanel
        {
            Children =
            {
                SecurityCharts.LegendItem(L.Get("Devices:InterruptMsi"), "SuccessBrush"),
                SecurityCharts.LegendItem(L.Get("Devices:InterruptLineBased"), "WarningBrush")
            }
        };
    }

    public bool HasScanned { get; private set; }

    private async void Scan_Click(object sender, RoutedEventArgs e) => await ScanAsync();

    public async Task ScanAsync(bool initial = false)
    {
        HasScanned = true;
        ScanButton.IsEnabled = false;
        Loading.Show(L.Get("Devices:LoadingInterrupts"));
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            var selectedId = (DevicesGrid.SelectedItem as InterruptDevice)?.Id;
            if (!initial) Preload.Invalidate(Preload.Interrupts);
            _devices = await Preload.Get(Preload.Interrupts, InterruptService.Instance.GetDevicesAsync);
            DeviceCountValue.Text = _devices.Count.ToString(L.Culture);
            MsiValue.Text = _devices.Count(device => device.MsiEnabled).ToString(L.Culture);
            AffinityValue.Text = _devices.Count(device => device.HasAffinity).ToString(L.Culture);
            RenderClassChart();
            ApplyFilter();
            DevicesGrid.SelectedItem = _devices.FirstOrDefault(device => device.Id == selectedId);
            StatusText.Text = L.Get("Devices:RestartNote");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Text.Json.JsonException)
        {
            ErrorText.Text = L.Format("Devices:ScanFailed", ex.Message);
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            Loading.Hide();
            ScanButton.IsEnabled = true;
        }
    }

    private void RenderClassChart()
    {
        ClassChart.Children.Clear();
        var groups = _devices.GroupBy(device => device.Class).OrderByDescending(group => group.Count()).ToList();
        var max = groups.Count == 0 ? 1 : groups.Max(group => group.Count());
        foreach (var group in groups)
        {
            var name = group.Key;
            var msi = group.Count(device => device.MsiEnabled);
            ClassChart.Children.Add(SecurityCharts.Row(name,
                [
                    new(msi, "SuccessBrush", $"{L.Get("Devices:InterruptMsi")}: {msi}"),
                    new(group.Count() - msi, "WarningBrush", $"{L.Get("Devices:InterruptLineBased")}: {group.Count() - msi}")
                ], max, group.Count().ToString(L.Culture), _classFilter == name, () =>
                {
                    _classFilter = _classFilter == name ? null : name;
                    RenderClassChart();
                    ApplyFilter();
                }, labelWidth: 120));
        }
    }

    private void ApplyFilter() =>
        DevicesGrid.ItemsSource = _devices.Where(device => _classFilter is null || device.Class == _classFilter).ToList();

    private void Device_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not InterruptDevice device)
        {
            EditorCard.Visibility = Visibility.Collapsed;
            return;
        }
        EditorCard.Visibility = Visibility.Visible;
        EditorTitle.Text = device.Name;
        MsiCheckBox.IsEnabled = device.MsiCapable;
        MsiCheckBox.IsChecked = device.MsiEnabled;
        MsiCheckBox.ToolTip = device.MsiCapable ? null : L.Get("Devices:MsiNotSupported");
        PriorityBox.SelectedIndex = (int)device.Priority;
        foreach (var button in _cpuButtons)
            button.IsChecked = device.HasAffinity && (device.AffinityMask >> (int)button.Tag & 1) == 1;
        AutomaticAffinity.IsChecked = !device.HasAffinity;
        CpuPanel.IsEnabled = _canSetAffinity && AutomaticAffinity.IsChecked != true;
        RestoreButton.IsEnabled = AffinityOnly ? device.HasAffinityChanges : device.HasTweakHubChanges;
    }

    private void AffinityMode_Changed(object sender, RoutedEventArgs e)
    {
        if (CpuPanel is not null) CpuPanel.IsEnabled = _canSetAffinity && AutomaticAffinity.IsChecked != true;
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not InterruptDevice device) return;
        var mask = _cpuButtons.Where(button => button.IsChecked == true).Aggregate(0UL, (value, button) => value | 1UL << (int)button.Tag);
        bool? msi = !AffinityOnly && device.MsiCapable && MsiCheckBox.IsChecked != device.MsiEnabled ? MsiCheckBox.IsChecked == true : null;
        InterruptPriority? priority = !AffinityOnly && (PriorityBox.SelectedItem as ComboBoxItem)?.Tag is InterruptPriority level && level != device.Priority ? level : null;
        if (AffinityOnly && AutomaticAffinity.IsChecked != true && (!_canSetAffinity || mask == 0))
        {
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), L.Get("Maintenance:AffinitySelectionRequired"));
            return;
        }
        ulong? affinity = !AffinityOnly ? null : AutomaticAffinity.IsChecked == true
            ? device.Policy == InterruptDevice.SpecifiedProcessorsPolicy ? 0UL : null
            : !device.HasAffinity || mask != device.AffinityMask ? mask : null;
        if (msi is null && priority is null && affinity is null) return;

        if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Devices:ApplyConfirmTitle"),
                L.Format("Devices:ApplyConfirmMessage", device.Name)
                    + (AffinityOnly ? "\n\n" + (affinity == 0 ? L.Get("Maintenance:AutomaticAffinity") : InterruptService.FormatMask(mask)) : string.Empty),
                L.Get("Devices:Apply"), L.Get("Tools:Cancel"))) return;
        EditorCard.IsEnabled = false;
        try
        {
            var success = await InterruptService.Instance.ApplyAsync(device, msi, priority, affinity);
            if (!success)
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), L.Get("Devices:ApplyFailedMessage"));
            else
                await AppDialog.ShowRestartRequiredAsync(Window.GetWindow(this), device.Name);
            await ScanAsync();
        }
        catch (Exception ex) { await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), ex.Message); }
        finally { EditorCard.IsEnabled = true; }
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not InterruptDevice device) return;
        EditorCard.IsEnabled = false;
        try
        {
            if (!await InterruptService.Instance.RestoreAsync(device, affinityOnly: AffinityOnly))
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), L.Get("Devices:ApplyFailedMessage"));
            else
                await AppDialog.ShowRestartRequiredAsync(Window.GetWindow(this), device.Name);
            await ScanAsync();
        }
        catch (Exception ex) { await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), ex.Message); }
        finally { EditorCard.IsEnabled = true; }
    }
}
