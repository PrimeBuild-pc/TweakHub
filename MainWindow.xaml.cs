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
    private Button? _activeButton;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
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