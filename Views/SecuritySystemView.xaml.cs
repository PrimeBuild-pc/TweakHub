using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

public partial class SecuritySystemView : UserControl
{
    private SystemTriage? _triage;
    private string? _processFilter;
    private string? _kindFilter;

    public event Action<string>? AnalyzeFileRequested;

    public SecuritySystemView()
    {
        InitializeComponent();
        StartupLegend.Children.Add(SecurityCharts.LegendItem(L.Get("Security:LegendSigned"), TrustBrush(TrustState.Signed)));
        StartupLegend.Children.Add(SecurityCharts.LegendItem(L.Get("Security:TrustUnsigned"), TrustBrush(TrustState.Unsigned)));
        StartupLegend.Children.Add(SecurityCharts.LegendItem(L.Get("Security:TrustScriptHost"), TrustBrush(TrustState.ScriptHost)));
        StartupLegend.Children.Add(SecurityCharts.LegendItem(L.Get("Security:TrustUnknown"), TrustBrush(TrustState.Unknown)));
    }

    public bool HasScanned => _triage != null;

    private async void Scan_Click(object sender, RoutedEventArgs e) => await ScanAsync(elevated: false);

    private async void ElevatedScan_Click(object sender, RoutedEventArgs e) => await ScanAsync(elevated: true);

    public async Task ScanAsync(bool elevated)
    {
        ScanButton.IsEnabled = ElevatedScanButton.IsEnabled = ExportButton.IsEnabled = false;
        BusyRing.IsActive = true;
        ErrorText.Visibility = Visibility.Collapsed;
        StatusText.Text = L.Get("Security:SystemScanning");
        try
        {
            _triage = await SystemTriageService.Instance.CollectAsync(elevated);
            _processFilter = _kindFilter = null;
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
            BusyRing.IsActive = false;
            ScanButton.IsEnabled = ElevatedScanButton.IsEnabled = true;
            ExportButton.IsEnabled = _triage != null;
        }
    }

    private static bool NeedsReview(TrustState trust) => trust is TrustState.Unsigned or TrustState.ScriptHost or TrustState.Missing;

    private static string TrustBrush(TrustState trust) => trust switch
    {
        TrustState.Signed => "SuccessBrush",
        TrustState.Unsigned => "DangerBrush",
        TrustState.ScriptHost or TrustState.Missing => "WarningBrush",
        _ => "SystemControlBorderBaseMediumBrush"
    };

    private void Render()
    {
        if (_triage is null) return;
        var connections = _triage.Connections;
        EstablishedValue.Text = connections.Count(c => !c.IsListening && !c.IsLoopback).ToString(L.Culture);
        ListeningValue.Text = connections.Count(c => c.IsListening).ToString(L.Culture);
        StartupValue.Text = _triage.Startup.Count.ToString(L.Culture);
        var review = connections.Where(c => NeedsReview(c.Trust)).Select(c => c.Path)
            .Concat(_triage.Startup.Where(s => NeedsReview(s.Trust)).Select(s => s.Command))
            .Distinct(StringComparer.OrdinalIgnoreCase).Count();
        ReviewValue.Text = review.ToString(L.Culture);
        ReviewIcon.SetResourceReference(TextBlock.ForegroundProperty, review == 0 ? "SuccessBrush" : "WarningBrush");

        RenderProcessChart();
        RenderStartupChart();
        ApplyConnectionFilter();
        ApplyStartupFilter();
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

    private void RenderStartupChart()
    {
        StartupChart.Children.Clear();
        var groups = _triage!.Startup.GroupBy(s => s.Kind).OrderByDescending(group => group.Count()).ToList();
        var max = groups.Count == 0 ? 1 : groups.Max(group => group.Count());
        foreach (var group in groups)
        {
            var kind = group.Key;
            var segments = new[] { TrustState.Signed, TrustState.Unsigned, TrustState.ScriptHost, TrustState.Missing, TrustState.Unknown }
                .Select(trust => (Trust: trust, Count: group.Count(s => s.Trust == trust)))
                .Where(item => item.Count > 0)
                .Select(item => new SecurityCharts.Segment(item.Count, TrustBrush(item.Trust),
                    $"{SystemTriageService.TrustText(item.Trust, L.Get("Security:LegendSigned"))}: {item.Count}"))
                .ToList();
            StartupChart.Children.Add(SecurityCharts.Row(group.First().KindText, segments, max, group.Count().ToString(L.Culture),
                _kindFilter == kind, () =>
                {
                    _kindFilter = _kindFilter == kind ? null : kind;
                    RenderStartupChart();
                    ApplyStartupFilter();
                }));
        }
    }

    private void ConnectionFilter_Changed(object sender, RoutedEventArgs e) => ApplyConnectionFilter();

    private void StartupFilter_Changed(object sender, RoutedEventArgs e) => ApplyStartupFilter();

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

    private void ApplyStartupFilter()
    {
        if (_triage is null || StartupGrid is null) return;
        var query = StartupSearch.Text.Trim();
        StartupGrid.ItemsSource = _triage.Startup
            .Where(s => _kindFilter is null || s.Kind == _kindFilter)
            .Where(s => StartupReviewOnly.IsChecked != true || NeedsReview(s.Trust))
            .Where(s => Matches(query, s.Name, s.Command, s.Location, s.Signer))
            .OrderBy(s => NeedsReview(s.Trust) ? 0 : 1).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? PathOf(object sender) => sender is FrameworkElement { DataContext: var item } ? item switch
    {
        NetworkConnection c => c.Path,
        StartupEntry s => s.Path,
        _ => null
    } : null;

    private async void OpenLocation_Click(object sender, RoutedEventArgs e)
    {
        if (PathOf(sender) is { } path && File.Exists(path))
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
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

    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (_triage is null || sender is not Button { DataContext: StartupEntry entry } button) return;
        button.IsEnabled = false;
        var result = await SystemTriageService.Instance.SetStartupEnabledAsync(entry, !entry.Enabled);
        if (!result.Success)
        {
            button.IsEnabled = true;
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Security:StartupToggleFailedTitle"),
                string.IsNullOrWhiteSpace(result.Error) ? result.ExitCode.ToString(L.Culture) : result.Error.Trim());
            return;
        }

        // Update in place: a full rescan takes several seconds.
        var updated = entry with { Enabled = !entry.Enabled };
        _triage = _triage with { Startup = _triage.Startup.Select(item => ReferenceEquals(item, entry) ? updated : item).ToList() };
        ApplyStartupFilter();
    }

    private async void Manage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: StartupEntry entry }) return;
        var (file, arguments) = entry.Kind switch
        {
            "Task" => ("taskschd.msc", ""),
            "Service" => ("services.msc", ""),
            "Winlogon" => ("regedit.exe", ""),
            _ => ("taskmgr.exe", "/0 /startup")
        };
        try
        {
            Process.Start(new ProcessStartInfo(file, arguments) { UseShellExecute = true });
        }
        catch (Win32Exception ex)
        {
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Security:OpenFailedTitle"), ex.Message);
        }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_triage is null) return;
        var dialog = new SaveFileDialog
        {
            FileName = $"TweakHub-security-{DateTime.Now:yyyyMMdd-HHmm}.csv",
            Filter = "CSV (*.csv)|*.csv"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try
        {
            // BOM so Excel opens non-ASCII paths correctly.
            await File.WriteAllTextAsync(dialog.FileName, SystemTriageService.ToCsv(_triage), new UTF8Encoding(true));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Security:ExportFailedTitle"), ex.Message);
        }
    }
}
