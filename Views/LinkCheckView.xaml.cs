using System.Windows;
using System.Windows.Controls;
using TweakHub.Localization;
using TweakHub.Services;

namespace TweakHub.Views;

public partial class LinkCheckView : UserControl
{
    public LinkCheckView() => InitializeComponent();

    public bool HasScanned { get; private set; }

    private async void Scan_Click(object sender, RoutedEventArgs e) => await ScanAsync();

    public async Task ScanAsync(bool initial = false)
    {
        HasScanned = true;
        ScanButton.IsEnabled = false;
        Loading.Show(L.Get("Devices:LoadingLinks"));
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            // A rescan after moving a device must read the new links, not the splash inventory.
            if (!initial) Preload.Invalidate(Preload.Devices);
            var report = await LinkService.ScanAsync(await Preload.Get(Preload.Devices, DeviceService.Instance.GetDevicesAsync));
            BoardText.Text = report.Board.Length > 0 ? report.Board : "–";
            CpuText.Text = report.Cpu.Length > 0 ? report.Cpu : "–";
            ChipsetText.Text = report.Chipset.Chipset?.Name ?? "–";
            UplinkText.Text = report.Chipset.Chipset is { } chipset ? $"{chipset.UplinkText} · {chipset.GBps.ToString("0.#", L.Culture)} GB/s" : "–";
            PlatformCard.DataContext = report.Chipset;
            PlatformCard.Visibility = Visibility.Visible;
            LinkList.ItemsSource = report.Devices;
            UsbList.ItemsSource = report.Usb;
            NoDevicesText.Visibility = report.Devices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Text.Json.JsonException)
        {
            ErrorText.Text = L.Format("Devices:ScanFailed", ex.Message);
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            Loading.Hide();
            ScanButton.IsEnabled = true;
        }
    }
}
