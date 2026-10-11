using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

public partial class SecurityPage : Page
{
    private sealed record ThreatItem(DefenderThreat Threat)
    {
        public string Name => Threat.Name;
        public string Resource => Threat.Resource;
        public bool Resolved => Threat.Resolved;
        public string SeverityText => L.Get(Threat.Severity switch
        {
            >= 5 => "Security:SeveritySevere",
            4 => "Security:SeverityHigh",
            2 or 3 => "Security:SeverityModerate",
            _ => "Security:SeverityLow"
        });
        public string DetectedText => Threat.Detected?.ToString("g", L.Culture) ?? string.Empty;
    }

    private SecuritySnapshot? _snapshot;
    private FileReport? _fileReport;
    private bool _busy;

    public SecurityPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (_snapshot is null) await RefreshAsync();
        };
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        RefreshButton.IsEnabled = false;
        LoadingRing.IsActive = true;
        ScoreText.Visibility = ScoreLabel.Visibility = Visibility.Hidden;
        LoadErrorText.Visibility = Visibility.Collapsed;
        try
        {
            _snapshot = await SecurityService.Instance.GetSnapshotAsync();
            ChecksList.ItemsSource = _snapshot.Checks;
            DrawScoreRing(_snapshot);
            ShowDefender(_snapshot.Defender);
        }
        catch (Exception ex)
        {
            LoadErrorText.Text = L.Format("Security:LoadFailed", ex.Message);
            LoadErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoadingRing.IsActive = false;
            ScoreText.Visibility = ScoreLabel.Visibility = Visibility.Visible;
            RefreshButton.IsEnabled = true;
        }
    }

    private static string StatusBrush(SecurityCheck check) => check.Status switch
    {
        CheckStatus.Passed => "SuccessBrush",
        CheckStatus.Failed when check.Weight >= 3 => "DangerBrush",
        CheckStatus.Failed => "WarningBrush",
        _ => "SystemControlBorderBaseMediumBrush"
    };

    private void DrawScoreRing(SecuritySnapshot snapshot)
    {
        const double center = 100, radius = 86, thickness = 14, gap = 3;
        ScoreRing.Children.Clear();
        var total = snapshot.Checks.Sum(check => check.Weight);
        var angle = 0.0;
        Point At(double degrees) => new(
            center + radius * Math.Sin(degrees * Math.PI / 180),
            center - radius * Math.Cos(degrees * Math.PI / 180));

        foreach (var check in snapshot.Checks)
        {
            var sweep = 360.0 * check.Weight / total;
            var figure = new PathFigure { StartPoint = At(angle + gap / 2) };
            figure.Segments.Add(new ArcSegment(At(angle + sweep - gap / 2), new Size(radius, radius), 0, sweep - gap > 180, SweepDirection.Clockwise, true));
            var segment = new System.Windows.Shapes.Path
            {
                Data = new PathGeometry([figure]),
                StrokeThickness = thickness,
                Cursor = Cursors.Hand,
                ToolTip = $"{check.Title}\n{check.Detail}"
            };
            segment.SetResourceReference(Shape.StrokeProperty, StatusBrush(check));
            segment.MouseEnter += (_, _) => segment.StrokeThickness = thickness + 6;
            segment.MouseLeave += (_, _) => segment.StrokeThickness = thickness;
            segment.MouseLeftButtonUp += (_, _) => FocusCheck(check);
            ScoreRing.Children.Add(segment);
            angle += sweep;
        }

        var score = snapshot.Score;
        ScoreText.Text = score.ToString(L.Culture);
        ScoreText.SetResourceReference(TextBlock.ForegroundProperty, score >= 80 ? "SuccessBrush" : score >= 50 ? "WarningBrush" : "DangerBrush");
        ScoreLabel.Text = L.Get(score >= 80 ? "Security:ScoreGood" : score >= 50 ? "Security:ScoreFair" : "Security:ScorePoor");
        ScoreSummary.Text = L.Format("Security:ScoreSummary",
            snapshot.Checks.Count(check => check.Status == CheckStatus.Passed), snapshot.Checks.Count);
    }

    private void FocusCheck(SecurityCheck check)
    {
        if (ChecksList.ItemContainerGenerator.ContainerFromItem(check) is not FrameworkElement container) return;
        container.BringIntoView();
        if (VisualTreeHelper.GetChildrenCount(container) > 0 && VisualTreeHelper.GetChild(container, 0) is UIElement card)
            card.Focus();
    }

    private async void Fix_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: SecurityCheck { FixScript: { } script } check }) return;
        var message = L.Format("Security:FixConfirmMessage", check.Title, script.Trim());
        if (check.FixNeedsRestart) message += "\n\n" + L.Get("Security:FixRestartNote");
        if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Security:FixConfirmTitle"), message,
                L.Get("Security:Fix"), L.Get("Tools:Cancel")))
            return;

        var result = await SecurityService.Instance.RunElevatedAsync(script);
        if (!result.Success)
        {
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Security:FixFailedTitle"),
                L.Format("Security:FixFailed", string.IsNullOrWhiteSpace(result.Error) ? result.ExitCode.ToString() : result.Error.Trim()));
        }
        else if (check.FixNeedsRestart)
        {
            await AppDialog.ShowRestartRequiredAsync(Window.GetWindow(this), check.Title);
        }
        await RefreshAsync();
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SecurityCheck { SettingsUri: { } uri } }) Open(uri);
    }

    private void OpenUri_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string uri }) Open(uri);
    }

    private async void Open(string target)
    {
        try
        {
            SecurityService.OpenSettings(target);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Security:OpenFailedTitle"), ex.Message);
        }
    }

    // Defender

    private void ShowDefender(DefenderStatus? defender)
    {
        DefenderInactiveBanner.Visibility = defender is { IsActive: true } ? Visibility.Collapsed : Visibility.Visible;
        ScanButtons.IsEnabled = defender is { IsActive: true };
        if (defender is null) return;

        SetState(RealTimeIcon, RealTimeValue, defender.RealTimeProtection);
        SetState(TamperIcon, TamperValue, defender.TamperProtection);
        SignatureValue.Text = string.IsNullOrEmpty(defender.SignatureVersion) ? "–" : defender.SignatureVersion;
        EngineValue.Text = string.IsNullOrEmpty(defender.EngineVersion) ? "–" : defender.EngineVersion;

        AgeChart.Children.Clear();
        AddAgeBar("Security:AgeSignatures", defender.SignatureAge, 3);
        AddAgeBar("Security:AgeQuickScan", defender.QuickScanAge, 7);
        AddAgeBar("Security:AgeFullScan", defender.FullScanAge, 30);

        ThreatsList.ItemsSource = defender.Threats.Select(threat => new ThreatItem(threat)).ToList();
        NoThreatsText.Visibility = defender.Threats.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void SetState(TextBlock icon, TextBlock value, bool on)
    {
        value.Text = L.Get(on ? "Security:On" : "Security:Off");
        icon.SetResourceReference(TextBlock.ForegroundProperty, on ? "SuccessBrush" : "DangerBrush");
    }

    // Bar length is the age against twice the recommended interval, so the midpoint marks the limit.
    private void AddAgeBar(string labelKey, int? days, int recommended)
    {
        var fraction = days is { } value ? Math.Clamp(value / (2.0 * recommended), 0.02, 1) : 1;
        var row = new Grid { Margin = new Thickness(0, 0, 0, 14), ToolTip = L.Format("Security:AgeRecommended", recommended) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

        var label = new TextBlock { Text = L.Get(labelKey), FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        label.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseHighBrush");

        var track = new Grid { Height = 8, VerticalAlignment = VerticalAlignment.Center };
        track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(fraction, GridUnitType.Star) });
        track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - fraction, GridUnitType.Star) });
        var background = new Border { CornerRadius = new CornerRadius(4) };
        background.SetResourceReference(Border.BackgroundProperty, "SubtleFillBrush");
        Grid.SetColumnSpan(background, 2);
        var bar = new Border { CornerRadius = new CornerRadius(4) };
        bar.SetResourceReference(Border.BackgroundProperty, days is { } age && age <= recommended ? "SuccessBrush" : "WarningBrush");
        var limit = new Rectangle { Width = 2, Height = 14, HorizontalAlignment = HorizontalAlignment.Center };
        limit.SetResourceReference(Shape.FillProperty, "SystemControlForegroundBaseMediumBrush");
        Grid.SetColumnSpan(limit, 2);
        track.Children.Add(background);
        track.Children.Add(bar);
        track.Children.Add(limit);
        Grid.SetColumn(track, 1);

        var text = new TextBlock
        {
            Text = days switch { null => L.Get("Security:AgeNever"), 0 => L.Get("Security:AgeToday"), _ => L.Format("Security:AgeDays", days) },
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");
        Grid.SetColumn(text, 2);

        row.Children.Add(label);
        row.Children.Add(track);
        row.Children.Add(text);
        AgeChart.Children.Add(row);
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || sender is not Button { Tag: string kind }) return;
        var (labelKey, script, timeout) = kind switch
        {
            "Quick" => ("Security:QuickScan", "Start-MpScan -ScanType QuickScan", TimeSpan.FromHours(2)),
            "Full" => ("Security:FullScan", "Start-MpScan -ScanType FullScan", TimeSpan.FromHours(24)),
            "Offline" => ("Security:OfflineScan", "Start-MpWDOScan", TimeSpan.FromMinutes(5)),
            _ => ("Security:UpdateSignatures", "Update-MpSignature", TimeSpan.FromMinutes(15))
        };
        if (kind == "Offline" && !await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Security:OfflineConfirmTitle"),
                L.Get("Security:OfflineConfirmMessage"), L.Get("Security:OfflineScan"), L.Get("Tools:Cancel")))
            return;

        var label = L.Get(labelKey);
        SetBusy(true, L.Format("Security:ActionRunning", label));
        var result = await SecurityService.Instance.RunElevatedAsync(script, timeout);
        SetBusy(false);
        ScanResultText.Text = result.Success
            ? L.Format("Security:ActionCompleted", label)
            : L.Format("Security:ActionFailed", label, string.IsNullOrWhiteSpace(result.Error) ? result.ExitCode.ToString() : result.Error.Trim());
        if (result.Success) await RefreshAsync();
    }

    private void SetBusy(bool busy, string? message = null)
    {
        _busy = busy;
        ScanButtons.IsEnabled = SafetyScannerButton.IsEnabled = !busy;
        ScanProgressPanel.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        ScanProgressBar.IsIndeterminate = true;
        ScanProgressText.Text = message ?? string.Empty;
        if (busy) ScanResultText.Text = string.Empty;
    }

    private async void SafetyScanner_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        SetBusy(true, L.Format("Security:SafetyScannerDownloading", 0));
        try
        {
            var progress = new Progress<int>(percent =>
            {
                ScanProgressBar.IsIndeterminate = false;
                ScanProgressBar.Value = percent;
                ScanProgressText.Text = L.Format("Security:SafetyScannerDownloading", percent);
            });
            var path = await SecurityService.DownloadSafetyScannerAsync(progress);
            // /N = detection only: Safety Scanner reports but never removes files.
            Process.Start(new ProcessStartInfo(path, "/N") { UseShellExecute = true });
            ScanResultText.Text = L.Get("Security:SafetyScannerStarted");
        }
        catch (Exception ex)
        {
            ScanResultText.Text = L.Format("Security:SafetyScannerFailed", ex.Message);
        }
        finally
        {
            _busy = false;
            ScanButtons.IsEnabled = _snapshot?.Defender is { IsActive: true };
            SafetyScannerButton.IsEnabled = true;
            ScanProgressPanel.Visibility = Visibility.Collapsed;
        }
    }

    // File analysis

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files) await AnalyzeAsync(files[0]);
    }

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { CheckFileExists = true };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) await AnalyzeAsync(dialog.FileName);
    }

    private async Task AnalyzeAsync(string path)
    {
        FileErrorText.Visibility = FileResultPanel.Visibility = Visibility.Collapsed;
        if (!File.Exists(path))
        {
            ShowFileError(L.Get("Security:FileNotAFile"));
            return;
        }

        FileProgressPanel.Visibility = Visibility.Visible;
        try
        {
            _fileReport = await SecurityService.AnalyzeFileAsync(path);
            ShowFileReport(_fileReport);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ShowFileError(ex.Message);
        }
        finally
        {
            FileProgressPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowFileError(string message)
    {
        FileErrorText.Text = L.Format("Security:FileFailed", message);
        FileErrorText.Visibility = Visibility.Visible;
    }

    private void ShowFileReport(FileReport report)
    {
        FileNameText.Text = System.IO.Path.GetFileName(report.Path);
        FilePathText.Text = report.Path;

        HashGrid.Children.Clear();
        HashGrid.RowDefinitions.Clear();
        foreach (var (label, value) in new[] { ("SHA-256", report.Sha256), ("SHA-1", report.Sha1), ("MD5", report.Md5) })
        {
            var row = HashGrid.RowDefinitions.Count;
            HashGrid.RowDefinitions.Add(new RowDefinition());
            AddCell(HashGrid, row, 0, label, "SystemControlForegroundBaseMediumBrush");
            var text = AddCell(HashGrid, row, 1, value, "SystemControlForegroundBaseHighBrush");
            text.FontFamily = new FontFamily("Cascadia Mono, Consolas");
            text.FontSize = 11;
            var copy = new Button
            {
                Content = new TextBlock { Text = "", FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = 12 },
                ToolTip = L.Get("Security:CopyHash"),
                Margin = new Thickness(8, 2, 0, 2)
            };
            AutomationProperties.SetName(copy, $"{L.Get("Security:CopyHash")} {label}");
            copy.SetResourceReference(StyleProperty, "PreviewIconButtonStyle");
            copy.Click += (_, _) => Clipboard.SetText(value);
            Grid.SetRow(copy, row);
            Grid.SetColumn(copy, 2);
            HashGrid.Children.Add(copy);
        }

        var signature = report.Signature;
        var (statusText, brush, icon) = signature switch
        {
            { Status: "Valid" } => (L.Get("Security:SignatureValid"), "SuccessBrush", ""),
            { Status: "NotSigned" } => (L.Get("Security:SignatureNotSigned"), "WarningBrush", ""),
            // Non-PE files (documents, archives) report UnknownError without any certificate.
            { Status: "NotSupportedFileFormat" } or { Status: "UnknownError", Signer: "" } =>
                (L.Get("Security:SignatureNotApplicable"), "IconSecondaryBrush", ""),
            null => (L.Get("Security:SignatureUnknown"), "IconSecondaryBrush", ""),
            _ => (L.Format("Security:SignatureInvalid", signature.Status), "DangerBrush", "")
        };
        SignatureStatusText.Text = statusText;
        SignatureIcon.Text = icon;
        SignatureIcon.SetResourceReference(TextBlock.ForegroundProperty, brush);

        DetailsGrid.Children.Clear();
        DetailsGrid.RowDefinitions.Clear();
        var version = report.Version;
        foreach (var (key, value) in new (string, string?)[]
        {
            ("Security:Signer", signature?.Signer),
            ("Security:Issuer", signature?.Issuer),
            ("Security:ValidUntil", signature?.ValidUntil?.ToString("d", L.Culture)),
            ("Security:Timestamp", signature?.Timestamp),
            ("Security:SignatureType", signature?.Type switch
            {
                "Authenticode" => L.Get("Security:SignatureTypeEmbedded"),
                "Catalog" => L.Get("Security:SignatureTypeCatalog"),
                _ => null
            }),
            ("Security:Company", version.CompanyName),
            ("Security:Product", version.ProductName),
            ("Security:FileDescription", version.FileDescription),
            ("Security:FileVersion", version.FileVersion),
            ("Security:OriginalName", version.OriginalFilename),
            ("Security:Size", FormatSize(report.Size))
        })
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            var row = DetailsGrid.RowDefinitions.Count;
            DetailsGrid.RowDefinitions.Add(new RowDefinition());
            AddCell(DetailsGrid, row, 0, L.Get(key), "SystemControlForegroundBaseMediumBrush");
            AddCell(DetailsGrid, row, 1, value, "SystemControlForegroundBaseHighBrush");
        }

        FileResultPanel.Visibility = Visibility.Visible;
    }

    private static TextBlock AddCell(Grid grid, int row, int column, string text, string brush)
    {
        var cell = new TextBlock
        {
            Text = text,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 8, 4),
            VerticalAlignment = VerticalAlignment.Center
        };
        cell.SetResourceReference(TextBlock.ForegroundProperty, brush);
        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, column);
        grid.Children.Add(cell);
        return cell;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1L << 30 => $"{bytes / (double)(1L << 30):0.##} GB",
        >= 1L << 20 => $"{bytes / (double)(1L << 20):0.##} MB",
        >= 1L << 10 => $"{bytes / 1024.0:0.##} KB",
        _ => $"{bytes} B"
    };

    private void SearchVirusTotal_Click(object sender, RoutedEventArgs e)
    {
        if (_fileReport is { } report) Open($"https://www.virustotal.com/gui/file/{report.Sha256}");
    }

    private void SearchTriage_Click(object sender, RoutedEventArgs e)
    {
        if (_fileReport is { } report) Open($"https://tria.ge/s?q={report.Sha256}");
    }

    private void CopyReport_Click(object sender, RoutedEventArgs e)
    {
        if (_fileReport is not { } report) return;
        var version = report.Version;
        Clipboard.SetText(string.Join(Environment.NewLine, new[]
        {
            report.Path,
            $"{L.Get("Security:Size")}: {FormatSize(report.Size)}",
            $"SHA-256: {report.Sha256}",
            $"SHA-1: {report.Sha1}",
            $"MD5: {report.Md5}",
            $"{L.Get("Security:Signature")}: {SignatureStatusText.Text}",
            $"{L.Get("Security:Signer")}: {report.Signature?.Signer}",
            $"{L.Get("Security:Company")}: {version.CompanyName}",
            $"{L.Get("Security:FileVersion")}: {version.FileVersion}"
        }));
    }

    private async void SystemView_AnalyzeFileRequested(string path)
    {
        Tabs.SelectedItem = FileTab;
        await AnalyzeAsync(path);
    }

    // Heavier tabs load on first use.
    private async void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, Tabs)) return;
        if (Tabs.SelectedItem == ToolsTab && ToolsFrame.Content is null)
            ToolsFrame.Navigate(new ExternalToolsPage(securityOnly: true));
        else if (Tabs.SelectedItem == SystemTab && !SystemView.HasScanned)
            await SystemView.ScanAsync(elevated: false);
        else if (Tabs.SelectedItem == PrivacyTab)
            await PrivacyView.LoadAsync();
    }
}
