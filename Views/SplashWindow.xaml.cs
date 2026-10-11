using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using TweakHub.Localization;
using TweakHub.Services;

namespace TweakHub.Views;

/// <summary>Reads everything the sections show while telling the user what is being read, then opens the main window.</summary>
public partial class SplashWindow : Window
{
    private sealed class Step(string label, string key, Func<Task> load) : INotifyPropertyChanged
    {
        public string Label { get; } = label;
        public string Key { get; } = key;
        public Func<Task> Load { get; } = load;
        public bool IsRunning { get; private set; } = true;
        public bool Failed { get; private set; }
        public string Icon => IsRunning ? string.Empty : Failed ? "" : "";
        public string Elapsed { get; private set; } = string.Empty;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void Finish(bool failed, TimeSpan elapsed)
        {
            IsRunning = false;
            Failed = failed;
            Elapsed = failed ? L.Get("UI:SplashStepFailed") : L.Format("UI:SplashSeconds", elapsed.TotalSeconds.ToString("0.0", L.Culture));
            foreach (var name in new[] { nameof(IsRunning), nameof(Failed), nameof(Icon), nameof(Elapsed) })
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // A stuck read must not keep the app closed; the section shows its own loading state for whatever is still running.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(45);

    public SplashWindow()
    {
        InitializeComponent();
        SubtitleText.Text = L.Get("UI:SplashSubtitle");
        Loaded += async (_, _) => await RunAsync();
    }

    private async Task RunAsync()
    {
        ShortcutService.Instance.Initialize();
        Step[] steps =
        [
            new(L.Get("UI:SplashTweaks"), Preload.TweakStates, TweakService.Instance.LoadWithStatesAsync),
            new(L.Get("UI:SplashWindowsUpdate"), Preload.WindowsUpdate, WindowsUpdatePresetService.Instance.DetectAsync),
            new(L.Get("UI:SplashPower"), Preload.PowerSchemes, () => Task.Run(PowerPlanService.LoadDefinitions)),
            new(L.Get("UI:SplashSecurity"), Preload.Security, SecurityService.Instance.GetSnapshotAsync),
            new(L.Get("UI:SplashDevices"), Preload.Devices, DeviceService.Instance.GetDevicesAsync),
            new(L.Get("UI:SplashInterrupts"), Preload.Interrupts, InterruptService.Instance.GetDevicesAsync),
            new(L.Get("UI:SplashChecks"), Preload.TweakChecks, () => TweakCheckService.Instance.ScanAsync(elevated: false))
        ];
        StepsList.ItemsSource = steps;
        Progress.Maximum = steps.Length;

        var clock = Stopwatch.StartNew();
        var running = steps.ToDictionary(step => Preload.Start(step.Key, step.Load), step => step);
        var deadline = Task.Delay(Timeout);
        while (running.Count > 0)
        {
            StatusText.Text = L.Format("UI:SplashReading", string.Join(", ", running.Values.Select(step => step.Label)));
            var finished = await Task.WhenAny(running.Keys.Append(deadline));
            if (finished == deadline) break;
            running.Remove(finished, out var step);
            step!.Finish(finished.IsFaulted, clock.Elapsed);
            Progress.Value += 1;
        }
        OpenMainWindow();
    }

    private void OpenMainWindow()
    {
        var main = new MainWindow();
        Application.Current.MainWindow = main;
        main.Show();
        Close();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
}
