using System.Windows;
using ModernWpf.Controls;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views;

namespace TweakHub;

public partial class MainWindow : Window
{
    private readonly ThemeService _themeService = ThemeService.Instance;
    private bool _startupChecksStarted;
    // Sections keep their state between visits.
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
        // With a custom chrome a maximized window overhangs the screen by its frame: pad it back in.
        // shortcut: frame metrics come from the primary monitor (verified on two same-DPI monitors); compute per monitor if a mixed-DPI setup overhangs.
        StateChanged += (_, _) => RootGrid.Margin = WindowState == WindowState.Maximized
            ? new Thickness(SystemParameters.WindowNonClientFrameThickness.Left, SystemParameters.WindowNonClientFrameThickness.Left,
                SystemParameters.WindowNonClientFrameThickness.Right, SystemParameters.WindowNonClientFrameThickness.Bottom)
            : new Thickness(0);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SelectSection(App.StartupPlaybookId is null ? RegistryTweaksItem : AutomatedScriptsItem);
    }

    // The first page calls this when its startup dialogs or playbook have finished.
    internal async Task RunDeferredStartupChecksAsync()
    {
        if (_startupChecksStarted) return;
        _startupChecksStarted = true;
        await Task.Delay(1000);
        if (!IsVisible) return;
        await UpdateService.Instance.CheckAndPromptAsync(this, showNoUpdate: false);
        // A driver update (often the GPU one) recreates the device key and drops the suggested layout without a word.
        if (!IsVisible) return;
        var reset = await Task.Run(InterruptService.ResetAffinityDevices);
        if (IsVisible && reset.Count > 0)
            await Views.Dialogs.AppDialog.ShowWarningAsync(this, L.Get("Maintenance:LayoutResetTitle"),
                L.Format("Maintenance:LayoutResetMessage", string.Join("\n", reset)));
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
        SelectSection(Navigation.MenuItems.OfType<NavigationViewItem>().First(item => (string)item.Tag == destination));
        return _pages[destination];
    }

    private void SelectSection(NavigationViewItem item)
    {
        if (!ReferenceEquals(Navigation.SelectedItem, item))
            Navigation.SelectedItem = item;
        else if (MainFrame.Content is null)
            Navigate((string)item.Tag);
    }

    /// <summary>After a profile import every other section must re-read its lists.</summary>
    public void ResetPages()
    {
        foreach (var key in _pages.Where(pair => pair.Value != MainFrame.Content).Select(pair => pair.Key).ToList())
            _pages.Remove(key);
    }

    private void AppearanceSettings_Click(object sender, RoutedEventArgs e) => SelectSection(SettingsItem);

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
