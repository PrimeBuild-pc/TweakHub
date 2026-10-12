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
            if (!DeviceList.HasScanned) await DeviceList.ScanAsync(initial: true);
        };
    }

    // Every tab scans on first use only: each scan starts PowerShell and some take several seconds.
    private async void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, Tabs)) return;
        if (Tabs.SelectedItem == InterruptsTab && !Interrupts.HasScanned) await Interrupts.ScanAsync(initial: true);
        else if (Tabs.SelectedItem == UsbTab && !Usb.HasScanned) await Usb.ScanAsync(initial: true);
        else if (Tabs.SelectedItem == ChecksTab && !Checks.HasScanned) await Checks.ScanAsync(elevated: false, initial: true);
        else if (Tabs.SelectedItem == ToolsTab && ToolsFrame.Content is null) ToolsFrame.Navigate(new ExternalToolsPage("Latency & Input"));
    }

    private async void RunScript_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ThirdPartyScript script) return;
        string path, hash;
        try
        {
            (path, hash) = await ThirdPartyScriptService.DownloadAsync(script);
        }
        catch (Exception ex)
        {
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ScriptWarningTitle"), ex.Message);
            return;
        }
        if (!ThirdPartyScriptService.HasConsent(script, hash))
        {
            // Topmost: the warning must not hide behind other windows, it is the last stop before remote code runs elevated.
            if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Devices:ScriptWarningTitle"),
                    L.Format("Devices:ScriptWarningMessage", script.Name, script.Command), L.Get("Devices:ScriptRun"), L.Get("UI:Cancel"),
                    DialogKind.Warning, topmost: true))
            {
                File.Delete(path);
                return;
            }
            ThirdPartyScriptService.SaveConsent(script, hash);
        }
        ThirdPartyScriptService.Run(script, path);
    }

    private void ScriptPage_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ThirdPartyScript script)
            Process.Start(new ProcessStartInfo(script.PageUrl) { UseShellExecute = true });
    }
}
