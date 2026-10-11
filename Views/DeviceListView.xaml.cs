using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

public partial class DeviceListView : UserControl
{
    private IReadOnlyList<PnpDeviceInfo> _devices = [];
    private DeviceCategory? _categoryFilter;

    public DeviceListView() => InitializeComponent();

    public bool HasScanned { get; private set; }

    private async void Scan_Click(object sender, RoutedEventArgs e) => await ScanAsync();

    /// <param name="initial">First load: reuse the startup read instead of starting another scan.</param>
    public async Task ScanAsync(bool initial = false)
    {
        HasScanned = true;
        ScanButton.IsEnabled = ExportButton.IsEnabled = false;
        Loading.Show(L.Get("Devices:Scanning"));
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            if (!initial) Preload.Invalidate(Preload.Devices);
            _devices = await Preload.Get(Preload.Devices, DeviceService.Instance.GetDevicesAsync);
            Render();
            StatusText.Text = L.Format("Devices:Scanned", DateTime.Now.ToString("t", L.Culture));
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Text.Json.JsonException)
        {
            ErrorText.Text = L.Format("Devices:ScanFailed", ex.Message);
            ErrorText.Visibility = Visibility.Visible;
            StatusText.Text = string.Empty;
        }
        finally
        {
            Loading.Hide();
            ScanButton.IsEnabled = true;
            ExportButton.IsEnabled = _devices.Count > 0;
            EnableAllButton.IsEnabled = DeviceService.HasDisabledDevices;
        }
    }

    private static bool IsSuggestion(PnpDeviceInfo device) => device.Rule is { Advice: not DeviceAdvice.Avoid } && !device.IsDisabled;

    private void Render()
    {
        TotalValue.Text = _devices.Count.ToString(L.Culture);
        SuggestedValue.Text = _devices.Count(IsSuggestion).ToString(L.Culture);
        DisabledValue.Text = _devices.Count(device => device.IsDisabled).ToString(L.Culture);
        var problems = _devices.Count(device => device.Category == DeviceCategory.Problem);
        ProblemValue.Text = problems.ToString(L.Culture);
        ProblemIcon.SetResourceReference(TextBlock.ForegroundProperty, problems == 0 ? "SuccessBrush" : "WarningBrush");
        RenderCategoryChart();
        ApplyFilter();
    }

    private static string CategoryBrush(DeviceCategory category) => category switch
    {
        DeviceCategory.Healthy => "SuccessBrush",
        DeviceCategory.Problem => "DangerBrush",
        DeviceCategory.Disabled => "SystemControlForegroundBaseMediumBrush",
        DeviceCategory.IdleBus => "AccentBrush",
        _ => "WarningBrush"
    };

    private void RenderCategoryChart()
    {
        CategoryChart.Children.Clear();
        var max = Math.Max(1, Enum.GetValues<DeviceCategory>().Max(category => _devices.Count(device => device.Category == category)));
        foreach (var category in Enum.GetValues<DeviceCategory>())
        {
            var count = _devices.Count(device => device.Category == category);
            var label = L.Get($"Devices:Category{category}");
            CategoryChart.Children.Add(SecurityCharts.Row(label, [new(count, CategoryBrush(category), L.Get($"Devices:Category{category}Hint"))],
                max, count.ToString(L.Culture), _categoryFilter == category, () =>
                {
                    _categoryFilter = _categoryFilter == category ? null : category;
                    RenderCategoryChart();
                    ApplyFilter();
                }, labelWidth: 150));
        }
    }

    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        if (DevicesGrid is null) return;
        var query = SearchBox.Text.Trim();
        DevicesGrid.ItemsSource = _devices
            .Where(device => _categoryFilter is null || device.Category == _categoryFilter)
            .Where(device => SuggestedOnly.IsChecked != true || IsSuggestion(device))
            .Where(device => query.Length == 0 || new[] { device.DisplayName, device.Class, device.Id, device.Service }
                .Any(field => field.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(IsSuggestion).ThenBy(device => device.Category).ThenBy(device => device.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PnpDeviceInfo device } button) return;
        var enable = device.IsDisabled;
        if (!enable)
        {
            var message = L.Format("Devices:DisableConfirmMessage", device.DisplayName);
            if (device.Rule is not null) message += "\n\n" + device.ReasonText + "\n" + L.Get("Devices:Breaks") + " " + device.BreaksText;
            if (device.Rule?.Advice == DeviceAdvice.Avoid) message += "\n\n" + L.Get("Devices:AvoidWarning");
            else if (device.HighRisk) message += "\n\n" + L.Get("Devices:HighRiskWarning");
            if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Devices:DisableConfirmTitle"), message,
                    L.Get("Devices:Disable"), L.Get("Tools:Cancel"))) return;
        }

        button.IsEnabled = false;
        var result = await DeviceService.Instance.SetEnabledAsync(device, enable);
        if (!result.Success)
        {
            button.IsEnabled = true;
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ToggleFailedTitle"),
                string.IsNullOrWhiteSpace(result.Error) ? result.ExitCode.ToString(L.Culture) : result.Error.Trim());
            return;
        }
        await ScanAsync();
    }

    private async void EnableAll_Click(object sender, RoutedEventArgs e)
    {
        if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Devices:EnableAll"), L.Get("Devices:EnableAllHint"),
                L.Get("Devices:Enable"), L.Get("Tools:Cancel"))) return;
        EnableAllButton.IsEnabled = false;
        var result = await DeviceService.Instance.EnableAllTrackedAsync();
        if (!result.Success)
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ToggleFailedTitle"), result.Error.Trim());
        await ScanAsync();
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { FileName = $"TweakHub-devices-{DateTime.Now:yyyyMMdd-HHmm}.csv", Filter = "CSV (*.csv)|*.csv" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try
        {
            await File.WriteAllTextAsync(dialog.FileName, DeviceService.ToCsv(_devices), new UTF8Encoding(true));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Security:ExportFailedTitle"), ex.Message);
        }
    }
}
