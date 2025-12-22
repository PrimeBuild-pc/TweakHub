using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views
{
    public partial class AboutPage : Page
    {
        private const string CurrentVersion = "0.1.0-beta";
        private const string GitHubApiUrl = "https://api.github.com/repos/PrimeBuild-pc/TweakHub/releases/latest";

        private static readonly HttpClient _httpClient = new HttpClient();

        static AboutPage()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TweakHub/1.0");
        }

        public AboutPage()
        {
            InitializeComponent();
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

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
                button.IsEnabled = false;

            try
            {
                var response = await _httpClient.GetStringAsync(GitHubApiUrl);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (release == null || string.IsNullOrEmpty(release.TagName))
                {
                    ShowError();
                    return;
                }

                var latestVersion = release.TagName.TrimStart('v');
                var isUpdateAvailable = CompareVersions(CurrentVersion, latestVersion);

                var ownerWindow = Window.GetWindow(this);

                if (isUpdateAvailable)
                {
                    var message = $"A new version is available!\n\nInstalled: {CurrentVersion}\nLatest: {latestVersion}\n\nWould you like to open the download page?";
                    var result = StyledMessageDialog.ShowYesNo(ownerWindow, "Update Available", message);

                    if (result)
                    {
                        Process.Start(new ProcessStartInfo(release.HtmlUrl) { UseShellExecute = true });
                    }
                }
                else
                {
                    StyledMessageDialog.ShowOk(ownerWindow, "Check for Updates", "You're running the latest version.");
                }
            }
            catch (Exception)
            {
                ShowError();
            }
            finally
            {
                if (button != null)
                    button.IsEnabled = true;
            }
        }

        private void ShowError()
        {
            var ownerWindow = Window.GetWindow(this);
            StyledMessageDialog.ShowOk(ownerWindow, "Check for Updates", "Unable to check for updates. Please check your internet connection.");
        }

        private bool CompareVersions(string current, string latest)
        {
            // Strip suffixes like "-beta" for comparison
            var currentClean = current.Split('-')[0];
            var latestClean = latest.Split('-')[0];

            if (Version.TryParse(currentClean, out var currentVer) &&
                Version.TryParse(latestClean, out var latestVer))
            {
                return latestVer > currentVer;
            }

            // Fallback: string comparison
            return string.Compare(latestClean, currentClean, StringComparison.OrdinalIgnoreCase) > 0;
        }

        private class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = string.Empty;

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; } = string.Empty;
        }
    }
}
