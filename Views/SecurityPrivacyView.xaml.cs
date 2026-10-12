using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using TweakHub.Localization;
using TweakHub.Models;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

public partial class SecurityPrivacyView : UserControl
{
    private const int MaxAccountResults = 40;
    private static readonly string[] Difficulties = ["Easy", "Medium", "Hard", "Impossible"];

    private IReadOnlyList<AccountSite> _sites = [];
    private string? _difficultyFilter;
    private IReadOnlyList<DnsAdapter> _adapters = [];
    private string? _metadataFile;
    private bool _loaded;

    public SecurityPrivacyView()
    {
        InitializeComponent();
        DnsPresetBox.Items.Add(new ComboBoxItem { Content = L.Get("Security:DnsAutomatic"), Tag = "Automatic" });
        foreach (var preset in PrivacyService.DnsPresets)
            DnsPresetBox.Items.Add(new ComboBoxItem { Content = L.Get($"Security:Dns{preset.Id}"), Tag = preset.Id });
        DnsPresetBox.Items.Add(new ComboBoxItem { Content = L.Get("Security:DnsCustom"), Tag = "Custom" });
        DnsPresetBox.SelectedIndex = 1;
        if (PrivacyService.HasCachedAccountDirectory) AccountsLoadText.Text = L.Get("Security:AccountsUpdate");
    }

    /// <summary>Reads local state only; nothing here touches the network.</summary>
    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;
        UpdateExifToolState();
        if (PrivacyService.HasCachedAccountDirectory) await LoadAccountsAsync(download: false);
        await RefreshDnsAsync();
    }

    // Password

    private async void CheckPassword_Click(object sender, RoutedEventArgs e) => await CheckPasswordAsync();

    private async void PasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await CheckPasswordAsync();
    }

    private async Task CheckPasswordAsync()
    {
        if (PasswordInput.Password.Length == 0) return;
        PasswordCheckButton.IsEnabled = false;
        try
        {
            var count = await PrivacyService.GetPasswordBreachCountAsync(PasswordInput.Password);
            ShowPasswordResult(count == 0 ? "" : "", count == 0 ? "SuccessBrush" : "DangerBrush",
                count == 0 ? L.Get("Security:PasswordSafe") : L.Format("Security:PasswordBreached", count.ToString("N0", L.Culture)));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ShowPasswordResult("", "WarningBrush", L.Format("Security:NetworkFailed", ex.Message));
        }
        finally
        {
            PasswordInput.Clear();
            PasswordCheckButton.IsEnabled = true;
        }
    }

    private void ShowPasswordResult(string icon, string brush, string text)
    {
        PasswordResultIcon.Text = icon;
        PasswordResultIcon.SetResourceReference(TextBlock.ForegroundProperty, brush);
        PasswordResultText.Text = text;
        PasswordResult.Visibility = Visibility.Visible;
    }

    // DNS

    private async Task RefreshDnsAsync()
    {
        try
        {
            _adapters = await PrivacyService.Instance.GetDnsAdaptersAsync();
            DnsAdaptersList.ItemsSource = _adapters;
            DnsStatusText.Text = _adapters.Count == 0 ? L.Get("Security:DnsNoAdapters") : string.Empty;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Text.Json.JsonException)
        {
            DnsStatusText.Text = ex.Message;
        }
        DnsRestoreButton.IsEnabled = PrivacyService.HasDnsBackup;
        DnsApplyButton.IsEnabled = _adapters.Count > 0;
    }

    private void DnsPreset_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        CustomDnsInput.Visibility = SelectedPreset == "Custom" ? Visibility.Visible : Visibility.Collapsed;

    private string SelectedPreset => (DnsPresetBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "Automatic";

    /// <summary>Null servers mean "automatic (DHCP)".</summary>
    private bool TryGetSelectedServers(out string[]? servers)
    {
        servers = null;
        switch (SelectedPreset)
        {
            case "Automatic":
                return true;
            case "Custom":
                if (PrivacyService.TryParseServers(CustomDnsInput.Text, out var custom))
                {
                    servers = custom;
                    return true;
                }
                DnsStatusText.Text = L.Get("Security:DnsInvalid");
                return false;
            default:
                servers = PrivacyService.DnsPresets.First(preset => preset.Id == SelectedPreset).Servers;
                return true;
        }
    }

    private async void TestDns_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedServers(out var servers)) return;
        if (servers is null)
        {
            DnsStatusText.Text = L.Get("Security:DnsTestAutomatic");
            return;
        }
        DnsTestButton.IsEnabled = false;
        DnsStatusText.Text = L.Get("Security:DnsTesting");
        var intercepted = PrivacyService.IsDnsInterceptedAsync();
        var results = await Task.WhenAll(servers.Select(async server => (server, ok: await PrivacyService.TestDnsServerAsync(server))));
        DnsStatusText.Text = await intercepted
            ? L.Get("Security:DnsIntercepted")
            : string.Join("\n", results.Select(result => L.Format(result.ok ? "Security:DnsTestOk" : "Security:DnsTestFailed", result.server)));
        DnsTestButton.IsEnabled = true;
    }

    private async void ApplyDns_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedServers(out var servers)) return;
        var target = servers is null ? L.Get("Security:DnsAutomatic") : string.Join(", ", servers);
        var message = L.Format("Security:DnsConfirmMessage", string.Join(", ", _adapters.Select(adapter => adapter.Alias)), target);
        if (SelectedPreset == "Custom") message += "\n\n" + L.Get("Security:DnsPiholeNote");
        if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Security:DnsConfirmTitle"), message, L.Get("Security:DnsApply"), L.Get("Tools:Cancel")))
            return;

        DnsApplyButton.IsEnabled = false;
        var result = await PrivacyService.Instance.ApplyDnsAsync(_adapters, servers);
        DnsStatusText.Text = result.Success ? L.Get("Security:DnsApplied") : L.Format("Security:DnsFailed", result.Error.Trim());
        await RefreshDnsAsync();
    }

    private async void RestoreDns_Click(object sender, RoutedEventArgs e)
    {
        DnsRestoreButton.IsEnabled = false;
        var result = await PrivacyService.Instance.RestoreDnsAsync();
        DnsStatusText.Text = result.Success ? L.Get("Security:DnsRestored") : L.Format("Security:DnsFailed", result.Error.Trim());
        await RefreshDnsAsync();
    }

    // Accounts

    private async void LoadAccounts_Click(object sender, RoutedEventArgs e) => await LoadAccountsAsync(download: true);

    private async Task LoadAccountsAsync(bool download)
    {
        AccountsLoadButton.IsEnabled = false;
        AccountsStatusText.Text = download ? L.Get("Security:AccountsDownloading") : string.Empty;
        try
        {
            _sites = await PrivacyService.LoadAccountDirectoryAsync(download);
            AccountsPanel.Visibility = Visibility.Visible;
            AccountsLoadText.Text = L.Get("Security:AccountsUpdate");
            AccountsStatusText.Text = L.Format("Security:AccountsLoaded", _sites.Count);
            RenderDifficultyChart();
            ApplyAccountFilter();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or System.Text.Json.JsonException)
        {
            AccountsStatusText.Text = L.Format("Security:NetworkFailed", ex.Message);
        }
        finally
        {
            AccountsLoadButton.IsEnabled = true;
        }
    }

    private void RenderDifficultyChart()
    {
        DifficultyChart.Children.Clear();
        var max = Math.Max(1, Difficulties.Max(level => _sites.Count(site => site.Difficulty == level)));
        foreach (var level in Difficulties)
        {
            var count = _sites.Count(site => site.Difficulty == level);
            var brush = level switch { "Easy" => "SuccessBrush", "Medium" => "WarningBrush", "Hard" => "DangerBrush", _ => "SystemControlBorderBaseMediumBrush" };
            var label = L.Get($"Security:Difficulty{level}");
            DifficultyChart.Children.Add(SecurityCharts.Row(label, [new(count, brush, $"{label}: {count}")], max,
                count.ToString(L.Culture), _difficultyFilter == level, () =>
                {
                    _difficultyFilter = _difficultyFilter == level ? null : level;
                    RenderDifficultyChart();
                    ApplyAccountFilter();
                }));
        }
    }

    private void AccountSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyAccountFilter();

    private void ApplyAccountFilter()
    {
        var matches = PrivacyService.SearchAccounts(_sites, AccountSearch.Text, _difficultyFilter).ToList();
        AccountsList.ItemsSource = matches.Take(MaxAccountResults).ToList();
        AccountsMoreText.Text = matches.Count > MaxAccountResults
            ? L.Format("Security:AccountsMore", matches.Count - MaxAccountResults)
            : matches.Count == 0 ? L.Get("Security:AccountsNone") : string.Empty;
    }

    private void OpenAccount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AccountSite { HasUrl: true } site })
            Process.Start(new ProcessStartInfo(site.Url) { UseShellExecute = true });
    }

    // Metadata

    private void UpdateExifToolState()
    {
        var installed = PrivacyService.FindExifTool() != null;
        ExifInstallButton.Visibility = installed ? Visibility.Collapsed : Visibility.Visible;
        MetadataPickButton.IsEnabled = installed;
        if (!installed) MetadataStatusText.Text = L.Get("Security:MetadataMissing");
        else if (_metadataFile is null) MetadataStatusText.Text = string.Empty;
    }

    private async void InstallExifTool_Click(object sender, RoutedEventArgs e)
    {
        var tool = ShortcutService.Instance.ExternalTools.FirstOrDefault(item => item.WingetId == "OliverBetz.ExifTool")
                   ?? new ExternalTool { Name = "ExifTool", WingetId = "OliverBetz.ExifTool" };
        ExifInstallButton.IsEnabled = false;
        MetadataBusy.IsActive = true;
        var progress = new Progress<ToolProgress>(update => MetadataStatusText.Text = update.Message);
        var installed = await ToolDownloadService.Instance.InstallWithWinget(tool, progress);
        MetadataBusy.IsActive = false;
        ExifInstallButton.IsEnabled = true;
        UpdateExifToolState();
        if (!installed) MetadataStatusText.Text = L.Get("Security:MetadataInstallFailed");
    }

    private async void PickMetadataFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { CheckFileExists = true };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        _metadataFile = dialog.FileName;
        await ReadMetadataAsync();
    }

    private async Task ReadMetadataAsync()
    {
        if (_metadataFile is null || PrivacyService.FindExifTool() is not { } exifTool) return;
        MetadataBusy.IsActive = true;
        try
        {
            var tags = await PrivacyService.ReadMetadataAsync(exifTool, _metadataFile);
            MetadataGrid.ItemsSource = tags;
            MetadataGrid.Visibility = tags.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            MetadataStatusText.Text = L.Format("Security:MetadataSummary", Path.GetFileName(_metadataFile), tags.Count, tags.Count(tag => tag.Sensitive));
            MetadataRemoveButton.IsEnabled = tags.Count > 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or IOException or System.Text.Json.JsonException)
        {
            MetadataStatusText.Text = L.Format("Security:MetadataFailed", ex.Message);
            MetadataGrid.Visibility = Visibility.Collapsed;
            MetadataRemoveButton.IsEnabled = false;
        }
        finally
        {
            MetadataBusy.IsActive = false;
        }
    }

    private async void RemoveMetadata_Click(object sender, RoutedEventArgs e)
    {
        if (_metadataFile is null || PrivacyService.FindExifTool() is not { } exifTool) return;
        if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Security:MetadataRemoveTitle"),
                L.Format("Security:MetadataRemoveMessage", Path.GetFileName(_metadataFile)), L.Get("Security:MetadataRemove"), L.Get("Tools:Cancel")))
            return;

        MetadataBusy.IsActive = true;
        try
        {
            var backup = await PrivacyService.RemoveMetadataAsync(exifTool, _metadataFile);
            MetadataBusy.IsActive = false;
            await ReadMetadataAsync();
            MetadataStatusText.Text += "\n" + L.Format("Security:MetadataRemoved", backup);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or IOException)
        {
            MetadataBusy.IsActive = false;
            MetadataStatusText.Text = L.Format("Security:MetadataFailed", ex.Message);
        }
    }
}
