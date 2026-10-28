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
    // Sections keep their state between visits; their data was read once by the splash screen.
    private readonly Dictionary<string, System.Windows.Controls.Page> _pages = new();

    public MainWindow()
    {
        InitializeComponent();
        AppearanceStatusText.Text = _themeService.StatusText;
        _themeService.PropertyChanged += ThemeService_PropertyChanged;
        Closed += (_, _) => _themeService.PropertyChanged -= ThemeService_PropertyChanged;
        Loaded += MainWindow_Loaded;
        // Cached pages need no journal: drop it so visited pages are not held twice.
        MainFrame.Navigated += (_, _) => { while (MainFrame.CanGoBack) MainFrame.RemoveBackEntry(); };
        ContentRendered += MainWindow_ContentRendered;
        // With a custom chrome a maximized window overhangs the screen by its frame: pad it back in.
        // shortcut: frame metrics come from the primary monitor (verified on two same-DPI monitors); compute per monitor if a mixed-DPI setup overhangs.
        StateChanged += (_, _) => RootGrid.Margin = WindowState == WindowState.Maximized
            ? new Thickness(SystemParameters.WindowNonClientFrameThickness.Left, SystemParameters.WindowNonClientFrameThickness.Left,
                SystemParameters.WindowNonClientFrameThickness.Right, SystemParameters.WindowNonClientFrameThickness.Bottom)
            : new Thickness(0);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Navigation.SelectedItem = App.StartupPlaybookId is null ? RegistryTweaksItem : AutomatedScriptsItem;
        if (MainFrame.Content is null) Navigate(App.StartupPlaybookId is null ? "registry" : "scripts");
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
        if (!_pages.TryGetValue(destination, out var page))
        {
            _pages[destination] = page = destination switch
            {
                "tools" => new ExternalToolsPage(),
                "security" => new SecurityPage(),
                "startup" => new StartupPage(),
                "devices" => new DevicesPage(),
                "power" => new PowerPlanPage(),
                "scripts" => new AutomatedScriptsPage(),
                "quick" => new QuickAccessPage(),
                "settings" => new AboutPage(),
                _ => new RegistryTweaksPage()
            };
        }
        MainFrame.Navigate(page);
        if (destination == "registry") UpdateRegistryTweaksBadge();
    }

    /// <summary>Selects a section from inside another one, e.g. Security → System opening Startup.</summary>
    public System.Windows.Controls.Page ShowSection(string destination)
    {
        Navigation.SelectedItem = Navigation.MenuItems.OfType<NavigationViewItem>().First(item => (string)item.Tag == destination);
        Navigate(destination);
        return _pages[destination];
    }

    /// <summary>After a profile import every other section must re-read its lists.</summary>
    public void ResetPages()
    {
        foreach (var key in _pages.Where(pair => pair.Value != MainFrame.Content).Select(pair => pair.Key).ToList())
            _pages.Remove(key);
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
        Navigation.SelectedItem = SettingsItem;
        if (MainFrame.Content is AboutPage) return;
        Navigate("settings");
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
