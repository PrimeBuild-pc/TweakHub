using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using TweakHub.Services;

namespace TweakHub;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Initialize theme service FIRST to ensure proper icon colors from startup
        var themeService = ThemeService.Instance;

        // Check if running as administrator
        if (!IsRunningAsAdministrator())
        {
            var result = MessageBox.Show(
                "TweakHub requires administrator privileges to function properly.\n\n" +
                "Many system tweaks and optimizations require elevated permissions.\n\n" +
                "Would you like to restart TweakHub as administrator?",
                "Administrator Privileges Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                RestartAsAdministrator();
                return; // Exit current instance
            }
            else
            {
                MessageBox.Show(
                    "TweakHub will continue to run, but some features may not work correctly without administrator privileges.",
                    "Limited Functionality Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        base.OnStartup(e);

        // Set up global exception handling
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}", "TweakHub Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private bool IsRunningAsAdministrator()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private void RestartAsAdministrator()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "TweakHub.exe",
                UseShellExecute = true,
                Verb = "runas" // This triggers UAC elevation
            };

            Process.Start(processInfo);
            Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to restart as administrator:\n\n{ex.Message}\n\n" +
                "Please manually run TweakHub as administrator for full functionality.",
                "Restart Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}

