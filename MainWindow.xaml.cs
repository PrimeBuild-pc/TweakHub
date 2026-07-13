using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TweakHub.Services;
using TweakHub.Views;

namespace TweakHub;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ThemeService _themeService;
    private Button? _activeButton = null;

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
        UpdateActiveSidebarButton(RegistryTweaksButton);
        UpdateRegistryTweaksBadge();
    }

    private void NavigateToExternalTools()
    {
        MainFrame.Navigate(new ExternalToolsPage());
        UpdateActiveSidebarButton(ExternalToolsButton);
    }

    private void NavigateToAutomatedScripts()
    {
        MainFrame.Navigate(new AutomatedScriptsPage());
        UpdateActiveSidebarButton(AutomatedScriptsButton);
    }

    private void NavigateToQuickAccess()
    {
        MainFrame.Navigate(new QuickAccessPage());
        UpdateActiveSidebarButton(QuickAccessButton);
    }

    private void NavigateToAbout()
    {
        MainFrame.Navigate(new Views.AboutPage());
        UpdateActiveSidebarButton(AboutButton);
    }

    private void UpdateActiveSidebarButton(Button activeButton)
    {
        // Reset previous active button to normal style
        if (_activeButton != null)
        {
            _activeButton.Style = (Style)FindResource("SidebarButtonStyle");
        }

        // Set new active button to active style
        _activeButton = activeButton;
        if (_activeButton != null)
        {
            _activeButton.Style = (Style)FindResource("ActiveSidebarButtonStyle");
        }
    }

    public void UpdateRegistryTweaksBadge()
    {
        try
        {
            var tweakService = TweakHub.Services.TweakService.Instance;
            var activeCount = tweakService.TweakCategories
                .SelectMany(c => c.Tweaks)
                .Count(t => t.IsEnabled);

            if (this.FindName("RegistryTweaksBadge") is Border badge &&
                this.FindName("RegistryTweaksBadgeText") is TextBlock text)
            {
                if (activeCount > 0)
                {
                    badge.Visibility = Visibility.Visible;
                    text.Text = activeCount.ToString();
                }
                else
                {
                    badge.Visibility = Visibility.Collapsed;
                }
            }
        }
        catch
        {
            // Ignore errors in badge update
        }
    }
}