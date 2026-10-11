using System.Windows;
using System.Windows.Controls;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

public partial class TweakCheckView : UserControl
{
    private bool _elevated;

    public TweakCheckView() => InitializeComponent();

    public bool HasScanned { get; private set; }

    private async void Scan_Click(object sender, RoutedEventArgs e) => await ScanAsync(_elevated);

    private async void ElevatedScan_Click(object sender, RoutedEventArgs e) => await ScanAsync(elevated: true);

    public async Task ScanAsync(bool elevated, bool initial = false)
    {
        HasScanned = true;
        _elevated |= elevated;
        ScanButton.IsEnabled = ElevatedScanButton.IsEnabled = false;
        Loading.Show(L.Get("Devices:LoadingChecks"));
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            var checks = await (initial && !elevated
                ? Preload.Get(Preload.TweakChecks, () => TweakCheckService.Instance.ScanAsync(elevated: false))
                : TweakCheckService.Instance.ScanAsync(elevated));
            ChecksList.ItemsSource = checks;
            SummaryText.Text = L.Format("Devices:ChecksSummary", checks.Count, checks.Count(check => check.Status == TweakCheckStatus.Flagged));
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Text.Json.JsonException)
        {
            ErrorText.Text = L.Format("Devices:ScanFailed", ex.Message);
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            Loading.Hide();
            ScanButton.IsEnabled = ElevatedScanButton.IsEnabled = true;
        }
    }

    private async void Repair_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: TweakCheck check } button) return;
        var message = L.Format("Devices:RepairConfirmMessage", check.Title);
        if (check.Repair.Count == 0) message += "\n\n" + L.Get("Devices:RepairNoUndo");
        if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Devices:RepairConfirmTitle"), message,
                L.Get("Devices:Repair"), L.Get("Tools:Cancel"))) return;

        button.IsEnabled = false;
        var result = await TweakCheckService.Instance.RepairAsync(check);
        if (!result.Success)
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"),
                string.IsNullOrWhiteSpace(result.Error) ? L.Get("Devices:ApplyFailedMessage") : result.Error.Trim());
        else if (check.RequiresRestart)
            await AppDialog.ShowRestartRequiredAsync(Window.GetWindow(this), check.Title);
        await ScanAsync(_elevated);
    }

    private async void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: TweakCheck check } button) return;
        button.IsEnabled = false;
        if (!await TweakCheckService.Instance.UndoAsync(check))
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), L.Get("Devices:ApplyFailedMessage"));
        else if (check.RequiresRestart)
            await AppDialog.ShowRestartRequiredAsync(Window.GetWindow(this), check.Title);
        await ScanAsync(_elevated);
    }
}
