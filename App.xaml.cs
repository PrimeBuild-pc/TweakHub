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
    protected override void OnStartup(StartupEventArgs e)
    {
        L.Initialize(UserDataService.Instance.LoadAppearance().Language);
        try { AppDataPath.EnsureAppsDirectory(); } catch { }

        if (Environment.OSVersion.Version.Build < 22000)
        {
            MessageBox.Show(L.Get("UI:WindowsVersionMessage"), L.Get("UI:WindowsVersionTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        ThemeService.Instance.PropertyChanged += ThemeService_PropertyChanged;
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(Window_Loaded));
        RegistryService.Instance.Initialize();
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        Exit += App_Exit;
        base.OnStartup(e);
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        try
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            ThemeService.Instance.PropertyChanged -= ThemeService_PropertyChanged;
        }
        catch
        {
            // Ignore unhook errors
        }

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

