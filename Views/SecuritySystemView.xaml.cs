using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

public partial class SecuritySystemView : UserControl
{
    private SystemTriage? _triage;
    private string? _processFilter;

    public event Action<string>? AnalyzeFileRequested;

    public SecuritySystemView()
    {
        InitializeComponent();
    }

    public bool HasScanned => _triage != null;

    private async void Scan_Click(object sender, RoutedEventArgs e) => await ScanAsync(elevated: false);

    private async void ElevatedScan_Click(object sender, RoutedEventArgs e) => await ScanAsync(elevated: true);

    public async Task ScanAsync(bool elevated)
    {
        ScanButton.IsEnabled = ElevatedScanButton.IsEnabled = ExportButton.IsEnabled = false;
        Loading.Show(L.Get("Security:SystemScanning"));
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            _triage = await SystemTriageService.Instance.CollectAsync(elevated);
            _processFilter = null;
            Render();
            StatusText.Text = L.Format(_triage.Elevated ? "Security:SystemScannedElevated" : "Security:SystemScannedPartial",
                DateTime.Now.ToString("t", L.Culture));
        }
        catch (Exception ex)
        {
            ErrorText.Text = L.Format("Security:SystemScanFailed", ex.Message);
            ErrorText.Visibility = Visibility.Visible;
            StatusText.Text = string.Empty;
        }
        finally
        {
            Loading.Hide();
            ScanButton.IsEnabled = ElevatedScanButton.IsEnabled = true;
            ExportButton.IsEnabled = _triage != null;
        }
    }

    private static bool NeedsReview(TrustState trust) => trust is TrustState.Unsigned or TrustState.ScriptHost or TrustState.Missing;

    private void Render()
    {
        if (_triage is null) return;
        var connections = _triage.Connections;
        EstablishedValue.Text = connections.Count(c => !c.IsListening && !c.IsLoopback).ToString(L.Culture);
        ListeningValue.Text = connections.Count(c => c.IsListening).ToString(L.Culture);
        var review = connections.Where(c => NeedsReview(c.Trust)).Select(c => c.Path)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count();
        ReviewValue.Text = review.ToString(L.Culture);
        ReviewIcon.SetResourceReference(TextBlock.ForegroundProperty, review == 0 ? "SuccessBrush" : "WarningBrush");

        RenderProcessChart();
        ApplyConnectionFilter();
    }

    private void RenderProcessChart()
    {
        ProcessChart.Children.Clear();
        var groups = _triage!.Connections.Where(c => !c.IsLoopback)
            .GroupBy(c => c.Process, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count()).Take(8).ToList();
        var max = groups.Count == 0 ? 1 : groups.Max(group => group.Count());
        foreach (var group in groups)
        {
            var name = group.Key;
            var established = group.Count(c => !c.IsListening);
            var listening = group.Count() - established;
            ProcessChart.Children.Add(SecurityCharts.Row(name,
                [
                    new(established, "AccentBrush", L.Format("Security:ChartEstablished", established)),
                    new(listening, "SystemControlForegroundBaseMediumBrush", L.Format("Security:ChartListening", listening))
                ],
                max, group.Count().ToString(L.Culture), _processFilter == name, () =>
                {
                    _processFilter = _processFilter == name ? null : name;
                    RenderProcessChart();
                    ApplyConnectionFilter();
                }));
        }
    }

    private void ConnectionFilter_Changed(object sender, RoutedEventArgs e) => ApplyConnectionFilter();

    private static bool Matches(string query, params string[] fields) =>
        query.Length == 0 || fields.Any(field => field.Contains(query, StringComparison.OrdinalIgnoreCase));

    private void ApplyConnectionFilter()
    {
        if (_triage is null || ConnectionsGrid is null) return;
        var query = ConnectionSearch.Text.Trim();
        ConnectionsGrid.ItemsSource = _triage.Connections
            .Where(c => _processFilter is null || c.Process.Equals(_processFilter, StringComparison.OrdinalIgnoreCase))
            .Where(c => EstablishedOnly.IsChecked != true || !c.IsListening)
            .Where(c => HideLoopback.IsChecked != true || !c.IsLoopback)
            .Where(c => ConnectionsReviewOnly.IsChecked != true || NeedsReview(c.Trust))
            .Where(c => Matches(query, c.Process, c.Path, c.LocalText, c.RemoteText, c.Pid.ToString(L.Culture)))
            .OrderBy(c => NeedsReview(c.Trust) ? 0 : 1).ThenBy(c => c.Process, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? PathOf(object sender) => sender is FrameworkElement { DataContext: var item } ? item switch
    {
        NetworkConnection c => c.Path,
        _ => null
    } : null;

    private async void OpenLocation_Click(object sender, RoutedEventArgs e)
    {
        if (PathOf(sender) is { } path && File.Exists(path))
            Process.Start(new ProcessStartInfo(SystemTools.Explorer, $"/select,\"{path}\"") { UseShellExecute = true });
        else
            await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Security:OpenFailedTitle"), L.Get("Security:PathUnavailable"));
    }

    private async void AnalyzeFile_Click(object sender, RoutedEventArgs e)
    {
        if (PathOf(sender) is { } path && File.Exists(path))
            AnalyzeFileRequested?.Invoke(path);
        else
            await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Security:OpenFailedTitle"), L.Get("Security:PathUnavailable"));
    }

    // Opens the browser with the remote IP only, on explicit click.
    private void CheckRemoteAddress_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: NetworkConnection { HasPublicRemote: true } connection })
            SecurityService.OpenSettings($"https://www.abuseipdb.com/check/{connection.Remote}");
    }

    private void OpenStartup_Click(object sender, RoutedEventArgs e) => (Window.GetWindow(this) as MainWindow)?.ShowSection("startup");

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_triage is not null) await CsvExport.SaveAsync(this, $"TweakHub-security-{DateTime.Now:yyyyMMdd-HHmm}.csv", SystemTriageService.ToCsv(_triage));
    }
}
