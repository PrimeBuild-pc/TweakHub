using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using TweakHub.Services;

namespace TweakHub.Views;

public partial class AboutPage : Page
{
    private readonly ThemeService _themeService = ThemeService.Instance;

    public AboutPage()
    {
        InitializeComponent();
        VersionText.Text = $"TweakHub v{UpdateService.CurrentVersion}";
        ThemeModeComboBox.SelectedValue = _themeService.ThemeMode;
        UseSystemAccentCheckBox.IsChecked = _themeService.UseSystemAccent;
        AccentColorTextBox.Text = _themeService.UseSystemAccent ? "#0078D4" : _themeService.CustomAccent;
        TransparencyCheckBox.IsChecked = _themeService.TransparencyEnabled;
        UpdateAccentInput();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch { }
        e.Handled = true;
    }

    private void UseSystemAccent_Changed(object sender, RoutedEventArgs e) => UpdateAccentInput();

    private void UpdateAccentInput()
    {
        if (AccentColorTextBox != null)
            AccentColorTextBox.IsEnabled = UseSystemAccentCheckBox.IsChecked != true;
    }

    private void ApplyAppearance_Click(object sender, RoutedEventArgs e)
    {
        var mode = ThemeModeComboBox.SelectedValue as string ?? "System";
        if (_themeService.SetPreferences(mode, UseSystemAccentCheckBox.IsChecked == true,
                AccentColorTextBox.Text, TransparencyCheckBox.IsChecked == true, out var error))
        {
            AppearanceResultText.Text = "Applied";
        }
        else
        {
            AppearanceResultText.Text = error;
        }
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        CheckForUpdatesButton.IsEnabled = false;
        UpdateStatusText.Text = "Checking...";
        await UpdateService.Instance.CheckAndPromptAsync(Window.GetWindow(this), showNoUpdate: true);
        UpdateStatusText.Text = "Checked just now";
        CheckForUpdatesButton.IsEnabled = true;
    }
}
