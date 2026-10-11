using System.Windows.Controls;

namespace TweakHub.Views;

public partial class DevicesPage : Page
{
    public DevicesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (!DeviceList.HasScanned) await DeviceList.ScanAsync();
        };
    }

    // Every tab scans on first use only: each scan starts PowerShell and some take several seconds.
    private async void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, Tabs)) return;
        if (Tabs.SelectedItem == InterruptsTab && !Interrupts.HasScanned) await Interrupts.ScanAsync();
        else if (Tabs.SelectedItem == UsbTab && !Usb.HasScanned) await Usb.ScanAsync();
        else if (Tabs.SelectedItem == ChecksTab && !Checks.HasScanned) await Checks.ScanAsync(elevated: false);
        else if (Tabs.SelectedItem == ToolsTab && ToolsFrame.Content is null) ToolsFrame.Navigate(new ExternalToolsPage("Latency & Input"));
    }
}
