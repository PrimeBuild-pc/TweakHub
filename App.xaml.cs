using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using TweakHub.Localization;
using TweakHub.Services;

namespace TweakHub;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string RestartArgument = "--restart";
    private FileStream? _dataLock;

    /// <summary>Playbook to run once the elevated restart has opened the main window.</summary>
    public static string? StartupPlaybookId { get; set; }

    /// <summary>Fingerprint of the playbook the user confirmed before the restart; a different one is confirmed again.</summary>
    public static string? StartupPlaybookFingerprint { get; set; }

    /// <summary>Starts TweakHub again as administrator and closes this instance; false when UAC is declined.</summary>
    public static bool RestartElevated(params string[] arguments)
    {
        try
        {
            Restart(elevated: true, arguments);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
        return true;
    }

    internal static void Restart(bool elevated = false, params string[] arguments)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true };
        if (elevated) start.Verb = "runas";
        start.ArgumentList.Add(RestartArgument);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start);
        Current.Shutdown();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        var restarting = e.Args is [RestartArgument, ..];
        var arguments = restarting ? e.Args[1..] : e.Args;
        try
        {
            // A launched replacement may reach startup before the old process finishes closing.
            _dataLock = await DataDirectoryLock.AcquireAsync(AppDataPath.BasePath,
                restarting ? TimeSpan.FromSeconds(10) : TimeSpan.Zero);
            if (_dataLock is null)
            {
                ShowStartupFailure("UI:DataDirectoryInUse");
                return;
            }
            L.Initialize(UserDataService.Instance.LoadAppearance().Language);
            RegistryService.Instance.Initialize();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or TypeInitializationException)
        {
            Debug.WriteLine(ex);
            ShowStartupFailure("UI:DataDirectoryUnavailable");
            return;
        }

        if (arguments is ["--run-playbook", var playbookId, var fingerprint])
            (StartupPlaybookId, StartupPlaybookFingerprint) = (playbookId, fingerprint);
        try { AppDataPath.EnsureAppsDirectory(); } catch { }

        if (Environment.OSVersion.Version.Build < 22000)
        {
            MessageBox.Show(L.Get("UI:WindowsVersionMessage"), L.Get("UI:WindowsVersionTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        ThemeService.Instance.PropertyChanged += ThemeService_PropertyChanged;
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(Window_Loaded));
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        base.OnStartup(e);
        new Views.SplashWindow().Show();
    }

    private void ShowStartupFailure(string message)
    {
        L.Initialize(null);
        MessageBox.Show(L.Get(message), "TweakHub", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(-1);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            if (_dataLock is not null) ThemeService.Instance.PropertyChanged -= ThemeService_PropertyChanged;
        }
        catch
        {
            // Ignore unhook errors
        }
        try { base.OnExit(e); }
        finally { _dataLock?.Dispose(); }
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        // Theme changes usually surface as preference changes; refresh theme if we follow System.
        try
        {
            Dispatcher.Invoke(() =>
            {
                ThemeService.Instance.RefreshSystemThemeIfNeeded();
                foreach (Window window in Windows) ApplyWindows11Style(window);
            });
        }
        catch
        {
            // Ignore theme refresh errors
        }
    }

    private void ThemeService_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        foreach (Window window in Windows) ApplyWindows11Style(window);
    }

    private static void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window) ApplyWindows11Style(window);
    }

    private static void ApplyWindows11Style(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;

        var darkMode = ThemeService.Instance.IsDark ? 1 : 0;
        var roundedCorners = 2;
        var backdrop = !ThemeService.Instance.TransparencyEnabled || SystemParameters.HighContrast ? 1 : window is MainWindow ? 2 : 3;
        DwmSetWindowAttribute(handle, 20, ref darkMode, sizeof(int));
        DwmSetWindowAttribute(handle, 33, ref roundedCorners, sizeof(int));
        DwmSetWindowAttribute(handle, 38, ref backdrop, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TweakHub", "crash.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"{DateTimeOffset.Now:O}{Environment.NewLine}{e.Exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { }

        MessageBox.Show(L.Format("UI:UnexpectedErrorMessage", e.Exception.Message, logPath), L.Get("UI:UnexpectedErrorTitle"),
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Shutdown(-1);
    }

}

