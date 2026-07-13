using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using TweakHub.Services;

namespace TweakHub;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (Environment.OSVersion.Version.Build < 22000)
        {
            MessageBox.Show("TweakHub supports Windows 11 build 22000 or newer.", "Unsupported Windows version", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        _ = ThemeService.Instance;
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
            Dispatcher.Invoke(() => ThemeService.Instance.RefreshSystemThemeIfNeeded());
        }
        catch
        {
            // Ignore theme refresh errors
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TweakHub", "crash.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"{DateTimeOffset.Now:O}{Environment.NewLine}{e.Exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { }

        MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nDetails: {logPath}", "TweakHub Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Shutdown(-1);
    }

}

