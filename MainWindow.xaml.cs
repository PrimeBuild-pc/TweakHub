using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TweakHub.Services;
using TweakHub.Views;

namespace TweakHub;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ThemeService _themeService;

    public MainWindow()
    {
        InitializeComponent();
        _themeService = ThemeService.Instance;

        Loaded += MainWindow_Loaded;
        UpdateThemeButton();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigate to Registry Tweaks by default
        NavigateToRegistryTweaks();
    }

    private void RegistryTweaksButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToRegistryTweaks();
    }

    private void ExternalToolsButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToExternalTools();
    }

    private void AutomatedScriptsButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToAutomatedScripts();
    }

    private void QuickAccessButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToQuickAccess();
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToAbout();
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _themeService.CurrentTheme = _themeService.CurrentTheme == Services.AppTheme.Dark
            ? Services.AppTheme.Light
            : Services.AppTheme.Dark;
        UpdateThemeButton();
    }

    private void UpdateThemeButton()
    {
        var isDark = _themeService.CurrentTheme == Services.AppTheme.Dark ||
                     (_themeService.CurrentTheme == Services.AppTheme.System && IsSystemDarkTheme());

        ThemeIcon.Text = isDark ? "☀️" : "🌙";
        ThemeText.Text = isDark ? "Light Theme" : "Dark Theme";
    }

    private bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }

    private void NavigateToRegistryTweaks()
    {
        MainFrame.Navigate(new RegistryTweaksPage());
    }

    private void NavigateToExternalTools()
    {
        MainFrame.Navigate(new ExternalToolsPage());
    }

    private void NavigateToAutomatedScripts()
    {
        MainFrame.Navigate(new AutomatedScriptsPage());
    }

    private void NavigateToQuickAccess()
    {
        MainFrame.Navigate(new QuickAccessPage());
    }

    private void NavigateToAbout()
    {
        MainFrame.Navigate(new Views.AboutPage());
    }
}