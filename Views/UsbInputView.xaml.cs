using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

public partial class UsbInputView : UserControl
{
    private sealed record Hop(string Label, string Detail, bool IsHub, Visibility ArrowVisibility);

    private sealed record InputItem(UsbInputChain Chain, InterruptDevice? Controller)
    {
        public string Icon => Chain.Kind switch { InputKind.Mouse => "", InputKind.Keyboard => "", _ => "" };
        public bool SuspendAllowed => Chain.UsbDevice is { } usb && UsbService.IsDeviceSuspendAllowed(usb.Id);
        public bool HasBackup => Chain.UsbDevice is { } usb && UsbService.HasDeviceSuspendBackup(usb.Id);
        public string ControllerModeText => Controller is null ? L.Get("Devices:ControllerUnknown")
            : L.Format("Devices:ControllerMode", Controller.ModeText);
        public string SuspendText => !Chain.IsUsb ? string.Empty
            : L.Get(SuspendAllowed ? "Devices:DeviceSuspendOn" : "Devices:DeviceSuspendOff");
        public string SuspendActionText => L.Get(HasBackup && !SuspendAllowed ? "Devices:RestoreOriginal" : "Devices:DeviceSuspendDisable");
        public Visibility SuspendActionVisibility => Chain.IsUsb && (SuspendAllowed || HasBackup) ? Visibility.Visible : Visibility.Collapsed;

        // Device on the left, CPU on the right: the path an input report travels.
        public IReadOnlyList<Hop> Hops
        {
            get
            {
                var nodes = Chain.Path.Where(device => device == Chain.UsbDevice || UsbService.IsHub(device) || device == Chain.Controller)
                    .Select(device => new Hop(
                        device == Chain.Controller ? L.Get("Devices:HopController") : device == Chain.UsbDevice ? Chain.Name : L.Get("Devices:HopHub"),
                        device.DisplayName, device != Chain.UsbDevice && device != Chain.Controller, Visibility.Visible))
                    .ToList();
                if (nodes.Count == 0) nodes.Add(new Hop(Chain.Name, Chain.Input.DisplayName, false, Visibility.Visible));
                nodes.Add(new Hop("CPU", L.Get("Devices:HopCpu"), false, Visibility.Collapsed));
                return nodes;
            }
        }
    }

    public UsbInputView() => InitializeComponent();

    public bool HasScanned { get; private set; }

    private async void Scan_Click(object sender, RoutedEventArgs e) => await ScanAsync();

    public async Task ScanAsync(bool initial = false)
    {
        HasScanned = true;
        ScanButton.IsEnabled = false;
        Loading.Show(L.Get("Devices:LoadingUsb"));
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            if (!initial)
            {
                Preload.Invalidate(Preload.Devices);
                Preload.Invalidate(Preload.Interrupts);
            }
            var devicesTask = Preload.Get(Preload.Devices, DeviceService.Instance.GetDevicesAsync);
            var interruptsTask = Preload.Get(Preload.Interrupts, InterruptService.Instance.GetDevicesAsync);
            var chains = UsbService.BuildChains(await devicesTask);
            var controllers = (await interruptsTask).ToDictionary(device => device.Id, StringComparer.OrdinalIgnoreCase);
            var items = chains.Select(chain => new InputItem(chain, chain.Controller is { } controller ? controllers.GetValueOrDefault(controller.Id) : null)).ToList();
            InputList.ItemsSource = items;
            NoInputText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            await RefreshPlanSuspendAsync();
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

    private async Task RefreshPlanSuspendAsync()
    {
        var value = await UsbService.GetPlanSuspendAsync();
        PlanSuspendText.Text = L.Get(value switch { 1 => "Devices:PlanSuspendOn", 0 => "Devices:PlanSuspendOff", _ => "Devices:PlanSuspendUnknown" });
        PlanSuspendIcon.SetResourceReference(TextBlock.ForegroundProperty, value == 1 ? "WarningBrush" : value == 0 ? "SuccessBrush" : "IconBrush");
        PlanSuspendButton.IsEnabled = value == 1;
        PlanSuspendRestoreButton.IsEnabled = UsbService.HasPlanSuspendBackup;
    }

    private async void PlanSuspend_Click(object sender, RoutedEventArgs e)
    {
        PlanSuspendButton.IsEnabled = false;
        if (!await UsbService.DisablePlanSuspendAsync())
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), L.Get("Devices:ApplyFailedMessage"));
        await RefreshPlanSuspendAsync();
    }

    private async void PlanSuspendRestore_Click(object sender, RoutedEventArgs e)
    {
        PlanSuspendRestoreButton.IsEnabled = false;
        if (!await UsbService.RestorePlanSuspendAsync())
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), L.Get("Devices:ApplyFailedMessage"));
        await RefreshPlanSuspendAsync();
    }

    private async void DeviceSuspend_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: InputItem { Chain.UsbDevice: { } usb } item } button) return;
        button.IsEnabled = false;
        var success = item.HasBackup && !item.SuspendAllowed
            ? await UsbService.Instance.RestoreDeviceSuspendAsync(usb.Id)
            : await UsbService.Instance.DisableDeviceSuspendAsync(usb.Id);
        if (!success)
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), L.Get("Devices:ApplyFailedMessage"));
        else
            await AppDialog.ShowAsync(Window.GetWindow(this), item.Chain.Name, L.Get("Devices:ReconnectNote"));
        await ScanAsync();
    }

    // Raw input polling test

    private const int WmInput = 0x00FF;
    private const int RidInput = 0x10000003;
    private const int RimTypeMouse = 0;
    private static readonly TimeSpan TestLength = TimeSpan.FromSeconds(4);
    private readonly Dictionary<IntPtr, List<long>> _samples = [];
    private HwndSource? _source;
    private DispatcherTimer? _timer;

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RawInputDevice[] devices, uint count, uint size);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(IntPtr rawInput, uint command, out RawInputHeader data, ref uint size, uint headerSize);

    private void Polling_Click(object sender, RoutedEventArgs e)
    {
        if (_timer != null) return;
        if (PresentationSource.FromVisual(this) is not HwndSource source) return;
        _samples.Clear();
        _source = source;
        _source.AddHook(OnMessage);
        // Generic desktop page (1), mouse usage (2); messages arrive only while TweakHub has focus.
        RegisterRawInputDevices([new RawInputDevice { UsagePage = 1, Usage = 2, Target = source.Handle }], 1, (uint)Marshal.SizeOf<RawInputDevice>());
        PollingButton.IsEnabled = false;
        PollingStatus.Text = L.Get("Devices:PollingMove");
        PollingChart.Children.Clear();
        _timer = new DispatcherTimer { Interval = TestLength };
        _timer.Tick += (_, _) => StopPolling();
        _timer.Start();
    }

    private IntPtr OnMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmInput) return IntPtr.Zero;
        var timestamp = Stopwatch.GetTimestamp();
        var size = (uint)Marshal.SizeOf<RawInputHeader>();
        if (GetRawInputData(lParam, RidInput, out var header, ref size, (uint)Marshal.SizeOf<RawInputHeader>()) != uint.MaxValue
            && header.Type == RimTypeMouse)
        {
            if (!_samples.TryGetValue(header.Device, out var list)) _samples[header.Device] = list = [];
            list.Add(timestamp);
        }
        return IntPtr.Zero;
    }

    private void StopPolling()
    {
        _timer?.Stop();
        _timer = null;
        // RIDEV_REMOVE (1) with no target stops raw mouse input for this process.
        RegisterRawInputDevices([new RawInputDevice { UsagePage = 1, Usage = 2, Flags = 1, Target = IntPtr.Zero }], 1, (uint)Marshal.SizeOf<RawInputDevice>());
        _source?.RemoveHook(OnMessage);
        _source = null;
        PollingButton.IsEnabled = true;

        // The device that sent the most reports is the one the user moved.
        var stamps = _samples.Values.OrderByDescending(list => list.Count).FirstOrDefault() ?? [];
        var intervals = stamps.Zip(stamps.Skip(1), (first, second) => (second - first) * 1000.0 / Stopwatch.Frequency).ToList();
        var (hz, histogram) = UsbService.AnalyzeIntervals(intervals);
        if (hz <= 0)
        {
            PollingValue.Text = "–";
            PollingStatus.Text = L.Get("Devices:PollingTooFew");
            return;
        }
        PollingValue.Text = Math.Round(hz).ToString("N0", L.Culture);
        PollingStatus.Text = L.Format("Devices:PollingResult", intervals.Count);
        var max = Math.Max(1, histogram.Max());
        for (var index = 0; index < histogram.Count; index++)
        {
            if (histogram[index] == 0) continue;
            var label = index + 1 < UsbService.PollingBuckets.Length
                ? $"{UsbService.PollingBuckets[index]}–{UsbService.PollingBuckets[index + 1]} Hz"
                : $"{UsbService.PollingBuckets[index]}+ Hz";
            var share = histogram[index] * 100.0 / histogram.Sum();
            PollingChart.Children.Add(SecurityCharts.Row(label, [new(histogram[index], "AccentBrush", $"{share:0.#}%")], max,
                $"{share:0}%", selected: false, onClick: null, labelWidth: 110, valueWidth: 44));
        }
    }
}
