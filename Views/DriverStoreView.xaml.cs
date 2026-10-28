using System.Windows;
using System.Windows.Controls;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

/// <summary>Old driver packages in the driver store, removed with a backup and restorable. Scanning needs administrator rights.</summary>
public partial class DriverStoreView : UserControl
{
    private IReadOnlyList<DriverPackage> _old = [];

    public DriverStoreView() => InitializeComponent();

    /// <summary>Backups are read from disk without elevation; the UAC prompt waits for the Scan button.</summary>
    public void LoadBackups()
    {
        var backups = DriverStoreService.LoadBackups();
        BackupsList.ItemsSource = backups;
        NoBackupsText.Visibility = backups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        ScanButton.IsEnabled = RemoveButton.IsEnabled = false;
        StatusText.Text = L.Get("Devices:DriversScanning");
        try
        {
            var packages = await DriverStoreService.Instance.GetPackagesAsync();
            _old = DriverStoreService.OldPackages(packages);
            PackagesGrid.ItemsSource = _old;
            PackagesGrid.Visibility = _old.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            StatusText.Text = _old.Count > 0
                ? L.Format("Devices:DriversScanned", packages.Count, _old.Count)
                : L.Format("Devices:DriversNone", packages.Count);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Text.Json.JsonException)
        {
            StatusText.Text = L.Format("Devices:ScanFailed", ex.Message);
        }
        finally
        {
            ScanButton.IsEnabled = true;
            RemoveButton.IsEnabled = _old.Count > 0;
        }
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        var chosen = await AppDialog.ChooseAsync(Window.GetWindow(this), L.Get("Devices:DriversRemove"), L.Get("Devices:DriversRemoveMessage"),
            _old.Select(package => ($"{package.Name} {package.Version} ({package.Provider}) → {package.NewerVersion}", true)));
        if (chosen is null) return;
        ScanButton.IsEnabled = RemoveButton.IsEnabled = false;
        StatusText.Text = L.Get("Devices:DriversRemoving");
        try
        {
            var selected = chosen.Select(index => _old[index]).ToList();
            var failed = await DriverStoreService.Instance.RemoveAsync(selected);
            if (failed.Count > 0)
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:DriversRemove"), L.Format("Devices:DriversFailed", string.Join("\n", failed)));
            StatusText.Text = L.Format("Devices:DriversRemoved", selected.Count - failed.Count);
            // The list is stale now: removed packages are gone and inf numbers may change. Scan again to see what is left.
            _old = [];
            PackagesGrid.ItemsSource = null;
            PackagesGrid.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            StatusText.Text = string.Empty;
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:DriversRemove"), ex.Message);
        }
        finally
        {
            ScanButton.IsEnabled = true;
            RemoveButton.IsEnabled = _old.Count > 0;
            LoadBackups();
        }
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: DriverBackup backup } button) return;
        if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Devices:DriversRestore"),
                L.Format("Devices:DriversRestoreConfirm", backup.Title), L.Get("Devices:DriversRestore"), L.Get("UI:Cancel"))) return;
        button.IsEnabled = false;
        try
        {
            var result = await DriverStoreService.Instance.RestoreAsync(backup);
            if (!result.Success)
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:DriversRestore"), result.Error.Trim());
        }
        finally
        {
            LoadBackups();
        }
    }
}
