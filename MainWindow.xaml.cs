using System.Linq;
using System.Windows;
using System.Windows.Input;
using ModernWpf.Controls;
using TweakHub.Services;
using TweakHub.Views;

namespace TweakHub;

public partial class MainWindow : Window
{
    private readonly ThemeService _themeService = ThemeService.Instance;
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
        Navigation.SelectedItem = RegistryTweaksItem;
        if (MainFrame.Content is null) Navigate("registry");
    }

    private async void MainWindow_ContentRendered(object? sender, EventArgs e)
    {
        if (_updateCheckStarted) return;
        _updateCheckStarted = true;
        while (!TweakService.Instance.RegistryDisclaimerShown) await Task.Delay(500);
        await Task.Delay(1000);
        await UpdateService.Instance.CheckAndPromptAsync(this, showNoUpdate: false);
    }

    private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem { Tag: string destination }) Navigate(destination);
    }

    private void Navigate(string destination)
    {
        MainFrame.Navigate(destination switch
        {
            "registry" => new RegistryTweaksPage(),
            "tools" => new ExternalToolsPage(),
            "scripts" => new AutomatedScriptsPage(),
            "quick" => new QuickAccessPage(),
            _ => new AboutPage()
        });
        if (destination == "registry") UpdateRegistryTweaksBadge();
    }

    private void AppearanceSettings_Click(object sender, MouseButtonEventArgs e) => NavigateToAppearance();

    private void AppearanceSettings_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space)) return;
        e.Handled = true;
        NavigateToAppearance();
    }

    private void NavigateToAppearance()
    {
        Navigation.SelectedItem = AboutItem;
        if (MainFrame.Content is AboutPage) return;
        Navigate("about");
    }

    private void ThemeService_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        AppearanceStatusText.Text = _themeService.StatusText;

    public void UpdateRegistryTweaksBadge()
    {
        var activeCount = TweakService.Instance.TweakCategories
            .SelectMany(category => category.Tweaks)
            .Count(tweak => tweak.IsEnabled);
        RegistryTweaksBadge.Visibility = activeCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        RegistryTweaksBadgeText.Text = activeCount.ToString();
    }
}
