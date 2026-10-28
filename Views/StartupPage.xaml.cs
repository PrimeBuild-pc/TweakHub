using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Win32;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

/// <summary>Autoruns-style view of everything that starts with Windows: disable, delete with backup, restore.</summary>
public partial class StartupPage : Page
{
    private static readonly string[] Categories = ["All", "Logon", "Tasks", "Services", "Drivers", "Explorer", "Hijacks", "Wmi"];
    private SystemTriage? _triage;

    public StartupPage()
    {
        InitializeComponent();
        RenderCategories();
        RenderBackups();
        Loaded += async (_, _) => { if (_triage is null && ScanButton.IsEnabled) await ScanAsync(elevated: false); };
    }

    private async void Scan_Click(object sender, RoutedEventArgs e) => await ScanAsync(elevated: false);

    private async void ElevatedScan_Click(object sender, RoutedEventArgs e) => await ScanAsync(elevated: true);

    private async Task ScanAsync(bool elevated)
    {
        ScanButton.IsEnabled = ElevatedScanButton.IsEnabled = ExportButton.IsEnabled = false;
        Loading.Show(L.Get("Security:SystemScanning"));
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            _triage = await SystemTriageService.Instance.CollectAsync(elevated, startupOnly: true);
            StatusText.Text = L.Format(_triage.Elevated ? "Security:SystemScannedElevated" : "Security:StartupScannedPartial",
                DateTime.Now.ToString("t", L.Culture));
            RenderCategories();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorText.Text = L.Format("Security:SystemScanFailed", ex.Message);
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            Loading.Hide();
            ScanButton.IsEnabled = ElevatedScanButton.IsEnabled = true;
            ExportButton.IsEnabled = _triage != null;
        }
    }

    // Only the counts change: rebuilding the items would fire SelectionChanged and refilter the list each time.
    private void RenderCategories()
    {
        if (CategoryBox.Items.Count == 0)
        {
            foreach (var category in Categories) CategoryBox.Items.Add(new ComboBoxItem { Tag = category });
            CategoryBox.SelectedIndex = 0;
        }
        foreach (ComboBoxItem item in CategoryBox.Items)
        {
            var category = (string)item.Tag;
            var count = _triage?.Startup.Count(entry => category == "All" || entry.Category == category);
            var text = L.Get($"Security:StartupCategory{category}");
            item.Content = count is null ? text : $"{text} ({count})";
        }
    }

    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        if (_triage is null || StartupGrid is null) return;
        var category = (CategoryBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "All";
        var query = SearchBox.Text.Trim();
        StartupGrid.ItemsSource = _triage.Startup
            .Where(entry => category == "All" || entry.Category == category)
            .Where(entry => HideMicrosoft.IsChecked != true || !entry.IsMicrosoft)
            .Where(entry => UnsignedOnly.IsChecked != true || entry.Trust is TrustState.Unsigned or TrustState.ScriptHost)
            .Where(entry => MissingOnly.IsChecked != true || entry.IsMissing)
            .Where(entry => query.Length == 0 || new[] { entry.Name, entry.Command, entry.Location, entry.Signer, entry.Path, entry.Trigger }
                .Any(field => field.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(entry => Array.IndexOf(Categories, entry.Category))
            .ThenBy(entry => entry.IsMissing || entry.Trust is TrustState.Unsigned or TrustState.ScriptHost ? 0 : 1)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void RenderBackups()
    {
        var backups = SystemTriageService.LoadBackups();
        BackupsGrid.ItemsSource = backups;
        BackupsExpander.Header = L.Format("Security:StartupBackupsTitle", backups.Count);
        System.Windows.Automation.AutomationProperties.SetName(BackupsExpander, (string)BackupsExpander.Header);
    }

    /// <summary>Updates one row in place, keeping the scroll position; an entry from before a rescan is left alone.</summary>
    private void Replace(StartupEntry old, StartupEntry? updated)
    {
        if (_triage is null) return;
        var startup = _triage.Startup.ToList();
        var index = startup.FindIndex(item => ReferenceEquals(item, old));
        if (index < 0) return;
        if (updated is null) startup.RemoveAt(index); else startup[index] = updated;
        _triage = _triage with { Startup = startup };
        if (StartupGrid.ItemsSource is List<StartupEntry> rows && rows.FindIndex(item => ReferenceEquals(item, old)) is var row and >= 0)
        {
            if (updated is null) rows.RemoveAt(row); else rows[row] = updated;
            StartupGrid.Items.Refresh();
        }
        RenderCategories();
    }

    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: StartupEntry entry } button) return;
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
        Replace(entry, entry with { Enabled = !entry.Enabled });
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: StartupEntry { CanDelete: true } entry } button) return;
        var owner = Window.GetWindow(this);
        if (!await AppDialog.ConfirmAsync(owner, L.Get("Security:StartupDeleteTitle"),
                L.Format(entry.Kind == "Service" ? "Security:StartupDeleteServiceConfirm" : "Security:StartupDeleteConfirm", entry.Name),
                L.Get("Security:StartupDelete"), L.Get("UI:Cancel"), DialogKind.Warning))
            return;
        button.IsEnabled = false;
        try
        {
            await SystemTriageService.Instance.DeleteAsync(entry);
            Replace(entry, null);
            RenderBackups();
            StatusText.Text = L.Format("Security:StartupDeleted", entry.Name);
        }
        catch (InvalidOperationException ex)
        {
            button.IsEnabled = true;
            await AppDialog.ShowErrorAsync(owner, L.Get("Security:StartupDeleteFailedTitle"), ex.Message);
        }
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: StartupBackup backup } button) return;
        button.IsEnabled = false;
        try
        {
            await SystemTriageService.Instance.RestoreAsync(backup);
            RenderBackups();
            // No row is added: its state and signature are only known after a scan.
            StatusText.Text = L.Format(backup.Kind == "Service" ? "Security:StartupRestoredService" : "Security:StartupRestored", backup.Name);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            button.IsEnabled = true;
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Security:StartupRestoreFailedTitle"), ex.Message);
        }
    }

    private async void JumpToEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: StartupEntry entry }) return;
        try
        {
            switch (entry.Kind)
            {
                case "Folder":
                    Process.Start(new ProcessStartInfo(SystemTools.Explorer, $"/select,\"{System.IO.Path.Combine(entry.Location, entry.Name)}\"") { UseShellExecute = true });
                    break;
                case "Task":
                    Process.Start(new ProcessStartInfo(SystemTools.System32("taskschd.msc")) { UseShellExecute = true });
                    break;
                case "Wmi":
                    await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Security:OpenFailedTitle"), entry.Location);
                    break;
                default:
                    // Regedit opens at the key it last showed; -m starts a new window even if one is open.
                    var key = entry.Kind is "Service" or "Driver" ? @"HKLM:\SYSTEM\CurrentControlSet\Services\" + entry.Location : entry.Location;
                    var full = key.Replace("HKLM:", "HKEY_LOCAL_MACHINE").Replace("HKCU:", "HKEY_CURRENT_USER");
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Applets\Regedit", "LastKey", @"Computer\" + full);
                    Process.Start(new ProcessStartInfo(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "regedit.exe"), "-m")
                        { UseShellExecute = true });
                    break;
            }
        }
        catch (Win32Exception ex)
        {
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Security:OpenFailedTitle"), ex.Message);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SHObjectProperties(IntPtr owner, uint type, string name, string? page);

    private async void Properties_Click(object sender, RoutedEventArgs e)
    {
        const uint ShopFilePath = 2;
        if (sender is FrameworkElement { DataContext: StartupEntry entry } && File.Exists(entry.Path)
            && SHObjectProperties(new WindowInteropHelper(Window.GetWindow(this)).Handle, ShopFilePath, entry.Path, null))
            return;
        await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Security:OpenFailedTitle"), L.Get("Security:PathUnavailable"));
    }

    private async void AnalyzeFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: StartupEntry entry } && File.Exists(entry.Path)
            && Window.GetWindow(this) is MainWindow main && main.ShowSection("security") is SecurityPage security)
            await security.AnalyzeFileAsync(entry.Path);
        else
            await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Security:OpenFailedTitle"), L.Get("Security:PathUnavailable"));
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_triage is not null) await CsvExport.SaveAsync(this, $"TweakHub-startup-{DateTime.Now:yyyyMMdd-HHmm}.csv", SystemTriageService.ToCsv(_triage));
    }
}
