using System.Windows;
using System.Windows.Controls;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

public partial class OptionalServicesView : UserControl
{
    public OptionalServicesView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        ContentPanel.IsEnabled = false;
        StatusText.Text = L.Get("Devices:Scanning");
        try
        {
            ServicesList.ItemsSource = await OptionalServicesService.Instance.ScanAsync();
            StatusText.Text = L.Get("Maintenance:ServicesNoStop");
        }
        catch (Exception ex) { StatusText.Text = ex.Message; }
        finally { ContentPanel.IsEnabled = true; }
    }

    private async void Change_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: OptionalService service, Tag: string action }) return;
        int? mode = action == "Restore" ? null : int.Parse(action, System.Globalization.CultureInfo.InvariantCulture);
        if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Maintenance:ServicesTitle"),
                L.Format("Maintenance:ServiceConfirm", service.Name, service.Explanation,
                    mode is null ? L.Get("Devices:RestoreOriginal") : L.Get(mode == 3 ? "Maintenance:Manual" : "Maintenance:Disabled")),
                L.Get("Tweaks:Apply"), L.Get("UI:Cancel"))) return;
        ContentPanel.IsEnabled = false;
        try
        {
            if (!await OptionalServicesService.Instance.SetModeAsync(service, mode))
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), L.Get("Maintenance:ServiceFailed"));
        }
        catch (Exception ex)
        {
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), ex.Message);
        }
        finally { await RefreshAsync(); }
    }
}
