using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

public partial class DevicesPage : Page
{
    public DevicesPage()
    {
        InitializeComponent();
        ScriptsList.ItemsSource = ThirdPartyScriptService.MariusHeier;
        Loaded += async (_, _) =>
        {
            if (!DeviceList.HasScanned) await DeviceList.ScanAsync();
        };
    }

    private async void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, Tabs) || Tabs.SelectedItem is not TabItem tab) return;
        // Create each editor on first use, then keep its selection and controls between visits.
        switch (tab.Name)
        {
            case nameof(DriversTab):
                var drivers = (DriverStoreView)(tab.Content ??= new DriverStoreView());
                drivers.LoadBackups();
                break;
            case nameof(InterruptsTab):
            case nameof(AffinityTab):
                // Refresh on selection to reflect changes made in either editor.
                var interrupts = (InterruptView)(tab.Content ??= new InterruptView { AffinityOnly = tab == AffinityTab });
                await interrupts.ScanAsync();
                break;
            case nameof(UsbTab) when tab.Content is null:
                var usb = new UsbInputView();
                tab.Content = usb;
                await usb.ScanAsync();
                break;
            case nameof(LinksTab) when tab.Content is null:
                var links = new LinkCheckView();
                tab.Content = links;
                await links.ScanAsync();
                break;
            case nameof(ChecksTab) when tab.Content is null:
                var checks = new TweakCheckView();
                tab.Content = checks;
                await checks.ScanAsync(elevated: false);
                break;
            case nameof(ToolsTab) when ToolsFrame.Content is null:
                ToolsFrame.Navigate(new ExternalToolsPage("Latency & Input"));
                break;
        }
    }

    private async void RunScript_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ThirdPartyScript script)
            await ThirdPartyScriptService.ConfirmAndRunAsync(Window.GetWindow(this), script);
    }

    private void ScriptPage_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ThirdPartyScript script)
            Process.Start(new ProcessStartInfo(script.PageUrl) { UseShellExecute = true });
    }
}
