using System.Diagnostics;
using Microsoft.Win32;
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
        DataPathText.Text = $"Data location: {UserDataService.Instance.DataDirectory}";
        LoadAppearanceControls();
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

    private void LoadAppearanceControls()
    {
        ThemeModeComboBox.SelectedValue = _themeService.ThemeMode;
        UseSystemAccentCheckBox.IsChecked = _themeService.UseSystemAccent;
        AccentColorTextBox.Text = _themeService.UseSystemAccent ? "#0078D4" : _themeService.CustomAccent;
        TransparencyCheckBox.IsChecked = _themeService.TransparencyEnabled;
        UpdateAccentInput();
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

    private void ExportProfile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "TweakHub profile (*.tweakhub.json)|*.tweakhub.json|JSON files (*.json)|*.json",
            FileName = $"TweakHub-profile-{DateTime.Now:yyyyMMdd}.tweakhub.json"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            UserDataService.Instance.ExportProfile(dialog.FileName);
            ProfileResultText.Text = "Profile exported";
        }
        catch (Exception ex)
        {
            ProfileResultText.Text = ex.Message;
        }
    }

    private void ImportProfile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "TweakHub profile (*.tweakhub.json;*.json)|*.tweakhub.json;*.json"
        };
        if (dialog.ShowDialog() != true) return;
        if (MessageBox.Show("Importing replaces the current custom profile. Continue?", "Import TweakHub Profile",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            var appearance = UserDataService.Instance.ImportProfile(dialog.FileName);
            _themeService.ImportAppearance(appearance);
            ShortcutService.Instance.Initialize();
            LoadAppearanceControls();
            ProfileResultText.Text = "Profile imported";
        }
        catch (Exception ex)
        {
            ProfileResultText.Text = ex.Message;
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
