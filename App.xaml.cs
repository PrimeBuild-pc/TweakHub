using System;
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
        MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}", "TweakHub Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

}

