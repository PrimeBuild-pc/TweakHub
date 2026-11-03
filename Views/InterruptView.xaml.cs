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
            LayoutButton.Visibility = value && _canSetAffinity ? Visibility.Visible : Visibility.Collapsed;
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

    // shortcut: a fixed threshold on a 2-second idle sample; the load during a game is different, so it only warns.
    private const double BusyCpuPercent = 30;

    /// <summary>Suggested one-core-per-role affinity, previewed with a checkbox per device before anything is written.</summary>
    private async void Layout_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        var layout = InterruptLayoutPlanner.Plan(CpuTopology.Read(), CpuTopology.ReadL3());
        if (layout.Unavailable)
        {
            await AppDialog.ShowWarningAsync(owner, L.Get("Maintenance:LayoutTitle"), L.Get("Maintenance:LayoutUnavailable"));
            return;
        }
        string Role(InterruptRole role) => L.Get($"Maintenance:LayoutRole{role}");
        IReadOnlyList<DeviceNode> nodes;
        IReadOnlyList<AudioEndpoint> endpoints;
        IReadOnlyList<CpuLoadSample> load;
        LayoutButton.IsEnabled = false;
        try
        {
            // Device tree, default audio devices and two seconds of per-core load, all read-only.
            var sample = CpuLoad.SampleAsync(TimeSpan.FromSeconds(2));
            (nodes, endpoints) = await Task.Run(() => (DeviceTree.Read(), AudioEndpoints.ReadDefaults()));
            load = await sample;
        }
        finally { LayoutButton.IsEnabled = true; }
        var usb = InterruptLayoutPlanner.UsbUse(nodes, endpoints.Select(endpoint => endpoint.Id));
        var rows = InterruptLayoutPlanner.Rows(layout, _devices, usb);
        var todo = rows.Where(row => !row.AlreadySet && row.Skip is null).ToList();
        var audio = InterruptLayoutPlanner.HasAudio(rows);
        var usbNumbers = rows.Where(row => row.Device.Class == "USB").Select((row, index) => (row.Device.Id, index + 1))
            .ToDictionary(item => item.Id, item => item.Item2, StringComparer.OrdinalIgnoreCase);
        string Name(InterruptLayoutRow row) => row.Device.Name
            + (usbNumbers.TryGetValue(row.Device.Id, out var number) ? $" (USB {number})" : string.Empty)
            + (row.AudioDevice is { } device ? $" – {device}" : string.Empty);
        var lines = new List<string> { L.Get("Maintenance:LayoutIntro"), string.Empty };
        lines.AddRange(layout.CpuByRole.Where(entry => audio || entry.Key != InterruptRole.Audio)
            .Select(entry => $"{Role(entry.Key)}: CPU {entry.Value}"));
        if (!audio) lines.Add(L.Get("Maintenance:LayoutAudioUsb"));

        lines.Add(string.Empty);
        foreach (var (id, number) in usbNumbers)
        {
            var use = usb.GetValueOrDefault(id);
            var parts = new List<string>();
            if (use?.Mouse == true) parts.Add(L.Get("Maintenance:LayoutUsbMouse"));
            if (use?.Keyboard == true) parts.Add(L.Get("Maintenance:LayoutUsbKeyboard"));
            if (use?.Camera == true) parts.Add(L.Get("Maintenance:LayoutUsbCamera"));
            if (use is { Audio.Count: > 0 }) parts.Add(L.Format("Maintenance:LayoutUsbAudio", string.Join(", ", use.Audio)));
            lines.Add(L.Format("Maintenance:LayoutUsbController", number,
                parts.Count > 0 ? string.Join(", ", parts) : L.Get("Maintenance:LayoutUsbNothing")));
        }
        var sharedAudio = usbNumbers.Keys.Select(id => usb.GetValueOrDefault(id)).Where(use => use is { Audio.Count: > 0, HasInput: true }).ToList();
        if (sharedAudio.Count > 0 && usbNumbers.Keys.Any(id => usb.GetValueOrDefault(id) is not { HasInput: true }))
            lines.AddRange(sharedAudio.SelectMany(use => use!.Audio).Distinct().Select(device => L.Format("Maintenance:LayoutUsbAudioShared", device)));
        var names = nodes.DistinctBy(node => node.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(node => node.Id, node => node.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var endpoint in endpoints.Where(endpoint => endpoint.FrontJack))
            lines.Add(L.Format("Maintenance:LayoutFrontJack", names.GetValueOrDefault(@"SWD\MMDEVAPI\" + endpoint.Id) ?? endpoint.Id));
        if (layout.HybridPerformanceOnly) lines.Add(L.Get("Maintenance:LayoutNoteHybrid"));
        if (layout.CacheGroupChosen) lines.Add(L.Get("Maintenance:LayoutNoteCache"));
        if (layout.SharedNetworkAudio) lines.Add(L.Get("Maintenance:LayoutNoteShared"));
        if (rows.Any(row => row.Role == InterruptRole.Network)) lines.Add(L.Get("Maintenance:LayoutRss"));
        // Set in the registry is not the same as in use: the interrupts Windows allocated in this boot tell.
        lines.AddRange(rows.Where(row => row.AlreadySet).Select(row => L.Format(
            InterruptLayoutPlanner.IsActive(row.Mask, DeviceTree.AllocatedInterruptAffinities(row.Device.Id)) ? "Maintenance:LayoutActive" : "Maintenance:LayoutPendingRestart",
            Name(row))));
        lines.AddRange(rows.Where(row => row.Skip is not null).Select(row => L.Format($"Maintenance:Layout{row.Skip}", Name(row))));
        lines.AddRange(InterruptLayoutPlanner.PinnedStorage(_devices).Select(device =>
            L.Format("Maintenance:LayoutStoragePinned", device.Name, InterruptService.FormatMask(device.AffinityMask))));

        if (load.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add(L.Get("Maintenance:LayoutLoadTitle"));
            // CPU 0 for comparison, then only the cores some device will really get.
            var used = rows.Where(row => row.Skip is null).Select(row => row.Cpu);
            foreach (var cpu in used.Prepend(0).Distinct().Where(cpu => cpu < load.Count))
            {
                var sample = load[cpu];
                lines.Add(L.Format("Maintenance:LayoutLoadCpu", cpu, sample.BusyPercent, sample.InterruptDpcPercent, sample.InterruptsPerSecond));
                if (cpu != 0 && sample.BusyPercent >= BusyCpuPercent) lines.Add(L.Format("Maintenance:LayoutLoadBusy", cpu, sample.BusyPercent));
            }
        }
        var msi = InterruptLayoutPlanner.MsiCandidates(rows);
        if (todo.Count == 0 && msi.Count == 0)
        {
            lines.Add(string.Empty);
            lines.Add(L.Get("Maintenance:LayoutNothingToDo"));
            await AppDialog.ShowAsync(owner, L.Get("Maintenance:LayoutTitle"), string.Join("\n", lines));
            return;
        }
        // MSI boxes come after the affinity ones and start unticked: MSI is a driver change the user opts into.
        var chosen = await AppDialog.ChooseAsync(owner, L.Get("Maintenance:LayoutTitle"), string.Join("\n", lines),
            todo.Select(row => (L.Format("Maintenance:LayoutChoice", Role(row.Role), row.Cpu, Name(row)), true))
                .Concat(msi.Select(row => (L.Format("Maintenance:LayoutMsiChoice", Name(row)), false))));
        if (chosen is not { Count: > 0 }) return;
        EditorCard.IsEnabled = LayoutButton.IsEnabled = false;
        try
        {
            if (!await InterruptService.Instance.ApplyAffinitiesAsync(
                    chosen.Where(index => index < todo.Count).Select(index => (todo[index].Device, todo[index].Mask)).ToList(),
                    chosen.Where(index => index >= todo.Count).Select(index => msi[index - todo.Count].Device).ToList()))
                await AppDialog.ShowErrorAsync(owner, L.Get("Devices:ApplyFailedTitle"), L.Get("Devices:ApplyFailedMessage"));
            else
                await AppDialog.ShowRestartRequiredAsync(owner, L.Get("Maintenance:LayoutReminder"));
            await ScanAsync();
        }
        catch (Exception ex) { await AppDialog.ShowErrorAsync(owner, L.Get("Devices:ApplyFailedTitle"), ex.Message); }
        finally { EditorCard.IsEnabled = LayoutButton.IsEnabled = true; }
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
