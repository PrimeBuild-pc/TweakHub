using System.Diagnostics;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

public partial class AboutPage : Page
{
    private readonly ThemeService _themeService = ThemeService.Instance;

    public AboutPage()
    {
        InitializeComponent();
        VersionText.Text = $"TweakHub v{UpdateService.CurrentVersion}";
        DataPathText.Text = L.Format("UI:DataLocation", UserDataService.Instance.DataDirectory);
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
        LanguageComboBox.SelectedValue = L.Normalize(UserDataService.Instance.LoadAppearance().Language);
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
        var language = LanguageComboBox.SelectedValue as string ?? "System";
        if (_themeService.SetPreferences(mode, UseSystemAccentCheckBox.IsChecked == true,
                AccentColorTextBox.Text, TransparencyCheckBox.IsChecked == true, language, out var error))
        {
            AppearanceResultText.Text = L.Get(L.RequiresRestart(language) ? "UI:AppliedRestartLanguage" : "UI:Applied");
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
            Filter = L.Get("UI:ProfileSaveFilter"),
            FileName = $"TweakHub-profile-{DateTime.Now:yyyyMMdd}.tweakhub.json"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            UserDataService.Instance.ExportProfile(dialog.FileName);
            ProfileResultText.Text = L.Get("UI:ProfileExported");
        }
        catch (Exception ex)
        {
            ProfileResultText.Text = ex.Message;
        }
    }

    private async void ImportProfile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = L.Get("UI:ProfileOpenFilter")
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var summary = UserDataService.Instance.InspectProfile(dialog.FileName);
            if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("UI:ImportProfileTitle"),
                    L.Format("UI:ImportProfileSummary", summary.Scripts, summary.Tweaks, summary.Tools, summary.Playbooks),
                    L.Get("UI:Import"), L.Get("UI:Cancel"))) return;
            var result = UserDataService.Instance.ImportProfile(dialog.FileName);
            _themeService.ImportAppearance(result.Appearance);
            ShortcutService.Instance.Initialize();
            LoadAppearanceControls();
            var message = L.Format("UI:ProfileImportedSummary",
                result.Scripts, result.Tweaks, result.Tools, result.Playbooks, result.RecoveryPath)
                + (L.RequiresRestart(result.Appearance.Language) ? Environment.NewLine + L.Get("UI:ProfileImportedRestart") : string.Empty);
            ProfileResultText.Text = L.Get("UI:ProfileImported");
            await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("UI:ImportProfileTitle"), message);
        }
        catch (Exception ex)
        {
            ProfileResultText.Text = ex.Message;
        }
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        CheckForUpdatesButton.IsEnabled = false;
        UpdateStatusText.Text = L.Get("UI:Checking");
        await UpdateService.Instance.CheckAndPromptAsync(Window.GetWindow(this), showNoUpdate: true);
        UpdateStatusText.Text = L.Get("UI:CheckedJustNow");
        CheckForUpdatesButton.IsEnabled = true;
    }
}
