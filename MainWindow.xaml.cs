using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TweakHub.Services;
using TweakHub.Views;

namespace TweakHub;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ThemeService _themeService = ThemeService.Instance;
    private Button? _activeButton;
    private bool _updateCheckStarted;

    public MainWindow()
    {
        InitializeComponent();
        AppearanceStatusText.Text = _themeService.StatusText;
        _themeService.PropertyChanged += ThemeService_PropertyChanged;
        Closed += (_, _) => _themeService.PropertyChanged -= ThemeService_PropertyChanged;
        Loaded += MainWindow_Loaded;
        ContentRendered += MainWindow_ContentRendered;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigate to Registry Tweaks by default
        NavigateToRegistryTweaks();
    }

    private async void MainWindow_ContentRendered(object? sender, EventArgs e)
    {
        if (_updateCheckStarted) return;
        _updateCheckStarted = true;
        while (Application.Current.Windows.Cast<Window>().Any(window => window.Title == "TweakHub Disclaimer" && window.IsVisible))
            await Task.Delay(500);
        await Task.Delay(1000);
        await UpdateService.Instance.CheckAndPromptAsync(this, showNoUpdate: false);
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

    private void AboutButton_Click(object sender, RoutedEventArgs e) => NavigateToAbout();

    private void AppearanceSettings_Click(object sender, MouseButtonEventArgs e) => NavigateToAbout();

    private void AppearanceSettings_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space)) return;
        e.Handled = true;
        NavigateToAbout();
    }

    private void ThemeService_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        AppearanceStatusText.Text = _themeService.StatusText;


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
        var activeCount = TweakService.Instance.TweakCategories
            .SelectMany(category => category.Tweaks)
            .Count(tweak => tweak.IsEnabled);
        RegistryTweaksBadge.Visibility = activeCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        RegistryTweaksBadgeText.Text = activeCount.ToString();
    }
}