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

    public InterruptView()
    {
        InitializeComponent();
        foreach (var priority in Enum.GetValues<InterruptPriority>())
            PriorityBox.Items.Add(new ComboBoxItem { Content = L.Get($"Devices:Priority{priority}"), Tag = priority });
        // Affinity masks are 64 bits wide; larger machines use processor groups that this editor does not handle.
        for (var cpu = 0; cpu < Math.Min(Environment.ProcessorCount, 64); cpu++)
        {
            var button = new ToggleButton { Content = $"CPU {cpu}", Margin = new Thickness(0, 0, 6, 6), MinWidth = 64, Tag = cpu };
            AutomationProperties.SetName(button, $"CPU {cpu}");
            _cpuButtons.Add(button);
            CpuPanel.Children.Add(button);
        }
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
        RestoreButton.IsEnabled = device.HasTweakHubChanges;
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not InterruptDevice device) return;
        var mask = _cpuButtons.Where(button => button.IsChecked == true).Aggregate(0UL, (value, button) => value | 1UL << (int)button.Tag);
        bool? msi = device.MsiCapable && MsiCheckBox.IsChecked != device.MsiEnabled ? MsiCheckBox.IsChecked == true : null;
        InterruptPriority? priority = (PriorityBox.SelectedItem as ComboBoxItem)?.Tag is InterruptPriority level && level != device.Priority ? level : null;
        ulong? affinity = mask != 0 && mask != device.AffinityMask ? mask : null;
        if (msi is null && priority is null && affinity is null) return;

        if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Devices:ApplyConfirmTitle"),
                L.Format("Devices:ApplyConfirmMessage", device.Name), L.Get("Devices:Apply"), L.Get("Tools:Cancel"))) return;
        ApplyButton.IsEnabled = false;
        var success = await InterruptService.Instance.ApplyAsync(device, msi, priority, affinity);
        ApplyButton.IsEnabled = true;
        if (!success)
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), L.Get("Devices:ApplyFailedMessage"));
        else
            await AppDialog.ShowRestartRequiredAsync(Window.GetWindow(this), device.Name);
        await ScanAsync();
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not InterruptDevice device) return;
        RestoreButton.IsEnabled = false;
        if (!await InterruptService.Instance.RestoreAsync(device))
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), L.Get("Devices:ApplyFailedMessage"));
        else
            await AppDialog.ShowRestartRequiredAsync(Window.GetWindow(this), device.Name);
        await ScanAsync();
    }
}
