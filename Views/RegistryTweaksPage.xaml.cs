using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ToggleSwitch = ModernWpf.Controls.ToggleSwitch;
using TweakHub.Localization;
using TweakHub.Models;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views
{
    public partial class RegistryTweaksPage : Page
    {
        private readonly TweakService _tweakService;
        private readonly RegistryService _registryService;
        private readonly UserDataService _userData = UserDataService.Instance;
        private readonly WindowsUpdatePresetService _windowsUpdatePresets = WindowsUpdatePresetService.Instance;
        private readonly ObservableCollection<CustomRegistryTweak> _customTweaks = new();
        private readonly ObservableCollection<PerformanceTweak> _favoriteBuiltInTweaks = new();
        private readonly ObservableCollection<CustomRegistryTweak> _favoriteCustomTweaks = new();
        private HashSet<string> _favoriteTweakKeys = [];
        private bool _ignoreToggleEvent;
        private bool _loaded;
        public RegistryTweaksPage()
        {
            InitializeComponent();
            _tweakService = TweakService.Instance;
            _registryService = RegistryService.Instance;

            DataContext = _registryService;
            CustomTweaksList.ItemsSource = _customTweaks;
            FavoriteBuiltInTweaksList.ItemsSource = _favoriteBuiltInTweaks;
            FavoriteCustomTweaksList.ItemsSource = _favoriteCustomTweaks;

            Loaded += RegistryTweaksPage_Loaded;
            _tweakService.PropertyChanged += TweakService_PropertyChanged;
        }
        private readonly PowerShellService _powerShellService = PowerShellService.Instance;


        private void TweakService_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TweakService.HasAppliedTweaksThisSession))
            {
                Dispatcher.Invoke(() =>
                {
                    RestoreAllButton.Visibility = _tweakService.HasAppliedTweaksThisSession ? Visibility.Visible : Visibility.Collapsed;
                    RestoreAllButton.IsEnabled = _tweakService.HasAppliedTweaksThisSession;
                    RestoreAllButton.Opacity = _tweakService.HasAppliedTweaksThisSession ? 1.0 : 0.65;
                });
            }
        }

        private async void RegistryTweaksPage_Loaded(object sender, RoutedEventArgs e)
        {
            // The page is kept between sections: coming back only re-reads the toggles, which other sections may have changed.
            if (_loaded)
            {
                LoadCustomTweaks();
                LoadFavoriteTweaks();
                await _tweakService.RefreshTweakStatesAsync();
                NotifyMainWindowBadgeUpdate();
                return;
            }
            _loaded = true;

            // Show one-time per-session disclaimer on first visit to Registry Tweaks section
            if (!_tweakService.RegistryDisclaimerShown)
            {
                await AppDialog.ShowDisclaimerAsync(Window.GetWindow(this));
                _tweakService.RegistryDisclaimerShown = true;
            }

            await LoadTweaksAsync();
            LoadCustomTweaks();
            LoadFavoriteTweaks();
            await RefreshWindowsUpdatePresetAsync(initial: true);

            var tweaks = _tweakService.TweakCategories.SelectMany(category => category.Tweaks).ToList();
            RestartVerificationService.Instance.MigrateLegacyCurrentBoot(tweaks);
            var pending = RestartVerificationService.Instance.CurrentBootPendingTweakIds();
            foreach (var tweak in tweaks) tweak.IsRestartPending = pending.Contains(tweak.Id);
            await ShowRestartVerificationAsync();
        }

        private async Task ShowRestartVerificationAsync()
        {
            var results = await RestartVerificationService.Instance.VerifyAfterRebootAsync();
            if (results.Count == 0) return;

            new RestartVerificationDialog(results) { Owner = Window.GetWindow(this) }.ShowDialog();
        }

        private async Task LoadTweaksAsync()
        {
            await Preload.Get(Preload.TweakStates, _tweakService.LoadWithStatesAsync);
            TweakCategoriesControl.ItemsSource = _tweakService.TweakCategories;

            // Update sidebar badge immediately after initial state refresh
            NotifyMainWindowBadgeUpdate();
        }

        private async Task RefreshWindowsUpdatePresetAsync(bool initial = false)
        {
            var preset = await (initial ? Preload.Get(Preload.WindowsUpdate, _windowsUpdatePresets.DetectAsync) : _windowsUpdatePresets.DetectAsync());
            WindowsUpdatePresetComboBox.SelectedValue = preset == WindowsUpdatePreset.Custom ? null : preset.ToString();
            WindowsUpdatePresetStatusText.Text = L.Format("Tweaks:WindowsUpdateCurrentMode", L.Get($"Tweaks:WindowsUpdatePreset{preset}"));
            UpdateWindowsUpdatePresetDescription();
        }

        private void WindowsUpdatePreset_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            UpdateWindowsUpdatePresetDescription();

        private void UpdateWindowsUpdatePresetDescription()
        {
            if (WindowsUpdatePresetDescriptionText == null) return;
            var preset = WindowsUpdatePresetComboBox?.SelectedValue as string;
            WindowsUpdatePresetDescriptionText.Text = preset == null
                ? L.Get("Tweaks:WindowsUpdatePresetCustomDescription")
                : L.Get($"Tweaks:WindowsUpdatePreset{preset}Description");
            if (WindowsUpdatePresetPreview?.Visibility == Visibility.Visible) UpdateWindowsUpdatePresetPreview();
        }

        private async void ApplyWindowsUpdatePreset_Click(object sender, RoutedEventArgs e)
        {
            if (!Enum.TryParse<WindowsUpdatePreset>(WindowsUpdatePresetComboBox.SelectedValue as string, out var preset)) return;
            if (preset == WindowsUpdatePreset.Disabled && !await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    L.Get("Tweaks:WindowsUpdateDisableTitle"),
                    L.Get("Tweaks:WindowsUpdateDisableWarning"),
                    L.Get("Tweaks:Apply"),
                    L.Get("Tweaks:Cancel"))) return;

            WindowsUpdatePresetComboBox.IsEnabled = false;
            var progress = new ProgressWindow(L.Get("Tweaks:WindowsUpdateApplying"));
            progress.Show();
            try
            {
                var result = await _windowsUpdatePresets.ApplyAsync(preset);
                _tweakService.RefreshBackupState();
                if (!result.Success)
                {
                    await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:WindowsUpdateApplyFailed"), result.Error);
                    return;
                }

                await _tweakService.RefreshTweakStatesAsync();
                await RefreshWindowsUpdatePresetAsync();
                if (preset is WindowsUpdatePreset.Default or WindowsUpdatePreset.Disabled)
                    RestartVerificationService.Instance.TrackWindowsUpdate(preset);
                NotifyMainWindowBadgeUpdate();
                await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Tweaks:WindowsUpdateApplied"),
                    L.Format("Tweaks:WindowsUpdateAppliedMessage", L.Get($"Tweaks:WindowsUpdatePreset{preset}")));
                if (preset is WindowsUpdatePreset.Default or WindowsUpdatePreset.Disabled && !_tweakService.RestartNoticeShownThisSession)
                {
                    _tweakService.RestartNoticeShownThisSession = true;
                    await AppDialog.ShowRestartRequiredAsync(Window.GetWindow(this), L.Get("Tweaks:WindowsUpdateRestartMessage"));
                }
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:WindowsUpdateApplyFailed"), ex.Message);
            }
            finally
            {
                _tweakService.RefreshBackupState();
                progress.Close();
                WindowsUpdatePresetComboBox.IsEnabled = true;
            }
        }

        private void PreviewWindowsUpdatePreset_Click(object sender, RoutedEventArgs e)
        {
            WindowsUpdatePresetPreview.Visibility = WindowsUpdatePresetPreview.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
            if (WindowsUpdatePresetPreview.Visibility == Visibility.Visible) UpdateWindowsUpdatePresetPreview();
        }

        private void UpdateWindowsUpdatePresetPreview()
        {
            WindowsUpdatePresetPreviewText.Text = Enum.TryParse<WindowsUpdatePreset>(
                WindowsUpdatePresetComboBox.SelectedValue as string, out var preset)
                ? _windowsUpdatePresets.GetPreview(preset)
                : string.Empty;
        }

        private void NotifyMainWindowBadgeUpdate()
        {
            try
            {
                // Find MainWindow and update the badge
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.UpdateRegistryTweaksBadge();
                }
            }
            catch
            {
                // Ignore errors in badge update
            }
        }

        private void LoadCustomTweaks()
        {
            _customTweaks.Clear();
            foreach (var tweak in _userData.LoadCustomTweaks())
            {
                RefreshCustomTweakState(tweak);
                _customTweaks.Add(tweak);
            }
        }

        private void RefreshCustomTweakState(CustomRegistryTweak tweak)
        {
            try
            {
                var value = RegistryService.ParseData(tweak.ValueType, tweak.Data, out _);
                tweak.IsApplied = _registryService.IsValueSet(tweak.RegistryPath, tweak.RegistryKey, value);
            }
            catch { tweak.IsApplied = false; }
        }

        private void LoadFavoriteTweaks()
        {
            _favoriteTweakKeys = _userData.LoadFavoriteTweaks();
            foreach (var tweak in _tweakService.TweakCategories.SelectMany(category => category.Tweaks))
                tweak.IsFavorite = _favoriteTweakKeys.Contains(FavoriteKey(tweak));
            foreach (var tweak in _customTweaks)
                tweak.IsFavorite = _favoriteTweakKeys.Contains(FavoriteKey(tweak));
            RefreshFavoriteLists();
        }

        private void FavoriteTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            var key = button.DataContext switch
            {
                PerformanceTweak tweak => ToggleFavorite(tweak, FavoriteKey(tweak)),
                CustomRegistryTweak tweak => ToggleFavorite(tweak, FavoriteKey(tweak)),
                _ => null
            };
            if (key == null) return;
            _userData.SaveFavoriteTweaks(_favoriteTweakKeys);
            RefreshFavoriteLists();
        }

        private string ToggleFavorite(PerformanceTweak tweak, string key)
        {
            tweak.IsFavorite = !tweak.IsFavorite;
            if (tweak.IsFavorite) _favoriteTweakKeys.Add(key); else _favoriteTweakKeys.Remove(key);
            return key;
        }

        private string ToggleFavorite(CustomRegistryTweak tweak, string key)
        {
            tweak.IsFavorite = !tweak.IsFavorite;
            if (tweak.IsFavorite) _favoriteTweakKeys.Add(key); else _favoriteTweakKeys.Remove(key);
            return key;
        }

        private void RefreshFavoriteLists()
        {
            _favoriteBuiltInTweaks.Clear();
            foreach (var tweak in _tweakService.TweakCategories.SelectMany(category => category.Tweaks).Where(tweak => tweak.IsFavorite))
                _favoriteBuiltInTweaks.Add(tweak);
            _favoriteCustomTweaks.Clear();
            foreach (var tweak in _customTweaks.Where(tweak => tweak.IsFavorite))
                _favoriteCustomTweaks.Add(tweak);

            FavoriteBuiltInSection.Visibility = _favoriteBuiltInTweaks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            FavoriteCustomSection.Visibility = _favoriteCustomTweaks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            FavoritesEmptyText.Visibility = _favoriteBuiltInTweaks.Count + _favoriteCustomTweaks.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private static string FavoriteKey(PerformanceTweak tweak) => $"builtin:{tweak.Id}";
        private static string FavoriteKey(CustomRegistryTweak tweak) => $"custom:{tweak.Id}";

        private void ViewCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: CustomRegistryTweak tweak, Tag: Border preview }
                || preview.Tag is not TextBox textBox) return;

            textBox.Text = $"reg add \"{tweak.RegistryPath}\" /v \"{tweak.RegistryKey}\" /t {tweak.ValueType} /d {tweak.Data} /f";
            preview.Visibility = preview.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void ApplyCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: CustomRegistryTweak tweak }) return;
            try
            {
                var value = RegistryService.ParseData(tweak.ValueType, tweak.Data, out var kind);
                var success = await Task.Run(() => _registryService.ApplyValueWithBackup(tweak.RegistryPath, tweak.RegistryKey, value, kind));
                RefreshCustomTweakState(tweak);
                if (success)
                    await AppDialog.ShowAsync(Window.GetWindow(this), tweak.Name, L.Get("Tweaks:RegistryTweakApplied"));
                else
                    await AppDialog.ShowErrorAsync(Window.GetWindow(this), tweak.Name, L.Get("Tweaks:RegistryTweakApplyFailed"));
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), tweak.Name, ex.Message);
            }
        }

        private async void RestoreCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: CustomRegistryTweak tweak }) return;
            var success = await Task.Run(() => _registryService.RestoreRegistryValue(tweak.RegistryPath, tweak.RegistryKey));
            RefreshCustomTweakState(tweak);
            if (success)
                await AppDialog.ShowAsync(Window.GetWindow(this), tweak.Name, L.Get("Tweaks:RegistryValueRestored"));
            else
                await AppDialog.ShowWarningAsync(Window.GetWindow(this), tweak.Name, L.Get("Tweaks:NoBackupAvailable"));
        }

        private void EditCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: CustomRegistryTweak tweak }) OpenCustomTweakDialog(tweak);
        }

        private async void DeleteCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: CustomRegistryTweak tweak }
                || !await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Tweaks:Confirm"), L.Format("Tweaks:DeleteTweakMessage", tweak.Name))) return;

            _customTweaks.Remove(tweak);
            _favoriteTweakKeys.Remove(FavoriteKey(tweak));
            _userData.SaveCustomTweaks(_customTweaks);
            _userData.SaveFavoriteTweaks(_favoriteTweakKeys);
            RefreshFavoriteLists();
        }

        private void AddCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            OpenCustomTweakDialog();
        }

        private void OpenCustomTweakDialog(CustomRegistryTweak? existing = null)
        {
            var dialog = new CustomTweakDialog(existing) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true || dialog.Result is not { } result) return;

            if (existing == null)
            {
                _customTweaks.Add(result);
            }
            else
            {
                existing.Name = result.Name;
                existing.Description = result.Description;
                existing.RegistryPath = result.RegistryPath;
                existing.RegistryKey = result.RegistryKey;
                existing.ValueType = result.ValueType;
                existing.Data = result.Data;
            }
            foreach (var tweak in _customTweaks) RefreshCustomTweakState(tweak);
            _userData.SaveCustomTweaks(_customTweaks);
            CustomTweaksList.Items.Refresh();
            RefreshFavoriteLists();
        }

        private async void TweakToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_ignoreToggleEvent || sender is not ToggleSwitch toggle || toggle.Tag is not PerformanceTweak tweak
                || !toggle.IsKeyboardFocusWithin) return;

            var targetState = toggle.IsOn;
            if (tweak.RiskLevel >= 3 && !await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    L.Get("Tweaks:HighRiskWarning"),
                    L.Format("Tweaks:HighRiskMessage", tweak.RiskLevel, tweak.Description),
                    L.Get("Tweaks:Apply"),
                    L.Get("Tweaks:Cancel")))
            {
                RevertToggle(toggle, tweak, !targetState);
                return;
            }

            if (targetState && tweak.Type == TweakType.NetworkAdapter && !await ChooseNetworkAdaptersAsync())
            {
                RevertToggle(toggle, tweak, !targetState);
                return;
            }

            toggle.IsEnabled = false;
            try
            {
                var success = await _tweakService.ApplyTweakAsync(tweak, targetState);
                if (!success)
                {
                    await AppDialog.ShowErrorAsync(
                        Window.GetWindow(this),
                        L.Get("Tweaks:TweakApplicationFailed"),
                        L.Format("Tweaks:TweakApplicationFailedMessage", tweak.Name));
                    RevertToggle(toggle, tweak, !targetState);
                }
                else
                {
                    NotifyMainWindowBadgeUpdate();
                    if (tweak.RequiresRestart && !_tweakService.RestartNoticeShownThisSession)
                    {
                        _tweakService.RestartNoticeShownThisSession = true;
                        await AppDialog.ShowRestartRequiredAsync(
                            Window.GetWindow(this), L.Get("Tweaks:RestartForChanges"));
                    }
                }
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(
                    Window.GetWindow(this),
                    L.Get("Tweaks:TweakError"),
                    L.Format("Tweaks:TweakErrorMessage", ex.Message));
                RevertToggle(toggle, tweak, !targetState);
            }
            finally
            {
                toggle.IsEnabled = true;
            }
        }

        /// <summary>With more than one supported adapter the user picks where the preset goes; the choice is saved.</summary>
        private async Task<bool> ChooseNetworkAdaptersAsync()
        {
            var adapters = NetworkAdapterService.GetAdapters();
            var supported = adapters.Where(adapter => adapter.IsSupported).ToList();
            if (supported.Count == 0) return false;
            if (supported.Count > 1)
            {
                var selected = NetworkAdapterService.Selected(adapters);
                var chosen = await AppDialog.ChooseAsync(Window.GetWindow(this), L.Get("Tweaks:NetworkPresetChooseTitle"),
                    L.Get("Tweaks:NetworkPresetChooseMessage"),
                    supported.Select(adapter => (
                        $"{adapter.Name} — {adapter.Model}" + (adapter.Connected ? $" ({L.Get("Tweaks:NetworkAdapterConnected")})" : string.Empty),
                        selected.Contains(adapter))));
                if (chosen is null) return false;
                supported = [.. chosen.Select(index => supported[index])];
            }
            NetworkAdapterService.SaveSelection(supported);
            return true;
        }

        private void RevertToggle(ToggleSwitch toggle, PerformanceTweak tweak, bool state)
        {
            _ignoreToggleEvent = true;
            try
            {
                toggle.IsOn = state;
                tweak.IsEnabled = state;
            }
            finally
            {
                _ignoreToggleEvent = false;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await _tweakService.RefreshTweakStatesAsync();
            await RefreshWindowsUpdatePresetAsync();
            foreach (var tweak in _customTweaks) RefreshCustomTweakState(tweak);
            NotifyMainWindowBadgeUpdate();
        }

        private void ViewTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PerformanceTweak tweak)
            {
                if (!tweak.IsPreviewVisible) tweak.PreviewContent = BuildPreview(tweak);
                tweak.IsPreviewVisible = !tweak.IsPreviewVisible;
            }
        }

        private static string BuildPreview(PerformanceTweak tweak)
        {
            if (tweak.IsUnavailable) return tweak.UnavailableReason;
            var changes = tweak.Type == TweakType.NetworkAdapter
                ? NetworkAdapterService.PresetChanges()
                : TweakService.GetCompositeRegistryChanges(tweak.Id);
            if (changes.Count > 0)
            {
                var commands = changes.Select(change => change.Value is null
                    ? $"reg delete \"{change.KeyPath}\" /v \"{change.ValueName}\" /f"
                    : $"reg add \"{change.KeyPath}\" /v \"{change.ValueName}\" /t {change.Kind switch { Microsoft.Win32.RegistryValueKind.String => "REG_SZ", _ => "REG_DWORD" }} /d {change.Value} /f");
                return L.Format("Tweaks:CompositePreview", tweak.Name, tweak.RiskLevel, string.Join("\n", commands));
            }

            var command = tweak.Id switch
            {
                "disable_cpu_throttling" => "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 100",
                "disable_core_parking" => "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100",
                "high_performance_power_plan" => "powercfg /setactive SCHEME_MIN",
                "disable_sysmain" => "Set-Service -Name SysMain -StartupType Disabled\nStop-Service -Name SysMain -Force",
                _ => $"reg add \"{tweak.RegistryPath}\" /v \"{tweak.RegistryKey}\" /t {(tweak.EnabledValue is int ? "REG_DWORD" : tweak.EnabledValue is long ? "REG_QWORD" : "REG_SZ")} /d {(Equals(tweak.EnabledValue, -1) ? "0xffffffff" : tweak.EnabledValue)} /f"
            };
            return L.Format("Tweaks:Preview", tweak.Name, tweak.RiskLevel, command);
        }

        private async void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tweaks = _tweakService.TweakCategories.SelectMany(category => category.Tweaks).ToList();
                var count = _registryService.CreateBackup(tweaks);
                count += _registryService.CreateBackupValues(tweaks.SelectMany(tweak => TweakService.GetCompositeRegistryChanges(tweak.Id)));
                await AppDialog.ShowAsync(
                    Window.GetWindow(this),
                    L.Get("Tweaks:BackupCreated"),
                    L.Format("Tweaks:BackupCreatedMessage", count, System.IO.Path.Combine(_userData.DataDirectory, "registry-backup.json")));
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:BackupFailed"), ex.Message);
            }
        }

        private async void ApplyAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    L.Get("Tweaks:ApplyAllRecommended"),
                    L.Get("Tweaks:ApplyAllRecommendedMessage"),
                    L.Get("Tweaks:Apply"),
                    L.Get("Tweaks:Cancel"))) return;


            var recommendedTweaks = _tweakService.TweakCategories
                .SelectMany(c => c.Tweaks)
                .Where(t => TweakService.IsRecommended(t) && !t.IsEnabled)
                .ToList();
            var updateNeedsChange = await _windowsUpdatePresets.DetectAsync() != TweakService.RecommendedWindowsUpdatePreset;

            if (recommendedTweaks.Count == 0 && !updateNeedsChange)
            {
                await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Tweaks:NoChangesNeeded"),
                    L.Get("Tweaks:RecommendedAlreadyApplied"));
                return;
            }

            var total = recommendedTweaks.Count + (updateNeedsChange ? 1 : 0);
            if (await RestartElevatedForBulkAsync(total)) return;
            var progressWindow = new ProgressWindow(L.Format("Tweaks:ApplyingRecommended", total));
            progressWindow.Show();

            var applied = 0;
            var failed = 0;
            var requiresRestart = false;

            if (updateNeedsChange)
            {
                try
                {
                    var result = await _windowsUpdatePresets.ApplyAsync(TweakService.RecommendedWindowsUpdatePreset);
                    if (result.Success) applied++; else failed++;
                }
                catch
                {
                    failed++;
                }
                progressWindow.UpdateProgress((applied + failed) * 100 / total);
            }

            foreach (var tweak in recommendedTweaks)
            {
                try
                {
                    var success = await _tweakService.ApplyTweakAsync(tweak, true);
                    if (success)
                    {
                        applied++;
                        if (tweak.RequiresRestart) requiresRestart = true;
                    }
                    else failed++;
                }
                catch
                {
                    failed++;
                }

                progressWindow.UpdateProgress((applied + failed) * 100 / total);
            }

            _tweakService.RefreshBackupState();
            await RefreshWindowsUpdatePresetAsync();
            progressWindow.Close();

            var message = L.Format("Tweaks:AppliedCount", applied);
            if (failed > 0)
                message += "\n" + L.Format("Tweaks:FailedCount", failed);

            if (requiresRestart && !_tweakService.RestartNoticeShownThisSession)
            {
                _tweakService.RestartNoticeShownThisSession = true;
                await AppDialog.ShowRestartRequiredAsync(
                    Window.GetWindow(this), L.Get("Tweaks:SomeChangesRequireRestart"));
            }

            await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Tweaks:TweaksApplied"), message);
            NotifyMainWindowBadgeUpdate();
        }

        private async void CreateRestorePointButton_Click(object sender, RoutedEventArgs e)
        {
            var info = L.Get("Tweaks:CreateRestorePointMessage");
            if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Tweaks:CreateRestorePoint"), info, L.Get("Tweaks:Create"), L.Get("Tweaks:Cancel"))) return;

            var progress = new ProgressWindow(L.Get("Tweaks:CreatingRestorePoint"));
            progress.Show();

            try
            {
                progress.UpdateStatus(L.Get("Tweaks:RequestingRestorePoint"));
                progress.UpdateProgress(30);

                var script = @"
                    try {
                        Checkpoint-Computer -Description 'TweakHub - Pre Tweaks' -RestorePointType 'MODIFY_SETTINGS'
                        Write-Output 'OK'
                    } catch {
                        Write-Error $_.Exception.Message
                    }
                ";

                var result = await _powerShellService.ExecuteScriptAsync(script, requireAdministrator: true, timeout: TimeSpan.FromMinutes(5));

                progress.UpdateProgress(100);
                progress.Close();

                if (result.Success && result.Output.Contains("OK"))
                {
                    await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Tweaks:RestorePointCreated"), L.Get("Tweaks:RestorePointCreatedMessage"));
                }
                else
                {
                    await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:RestorePointFailed"), result.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:RestorePointFailed"), ex.Message);
            }
        }



        /// <summary>Each administrator change asks UAC once; one elevated restart asks only once. True when TweakHub is restarting.</summary>
        private async Task<bool> RestartElevatedForBulkAsync(int changes) =>
            changes > 1 && !Elevation.IsAdministrator
            && await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Scripts:PlaybookElevateTitle"),
                L.Format("Tweaks:BulkElevateMessage", changes), L.Get("Scripts:PlaybookElevateRestart"),
                L.Get("Scripts:PlaybookElevateContinue"), DialogKind.Shield)
            && App.RestartElevated(string.Empty);

        private async void RestoreAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    L.Get("Tweaks:RestoreAllChanges"),
                    L.Get("Tweaks:RestoreAllMessage"),
                    L.Get("Tweaks:Restore"),
                    L.Get("Tweaks:Cancel"))) return;
            var steps = _tweakService.TweakCategories.SelectMany(category => category.Tweaks).Count(TweakService.HasBackup)
                        + (_windowsUpdatePresets.HasBackup ? 1 : 0);
            if (await RestartElevatedForBulkAsync(steps)) return;

            var progress = new ProgressWindow(L.Get("Tweaks:RestoringAll"));
            progress.Show();

            try
            {
                progress.UpdateStatus(L.Get("Tweaks:Restoring"));
                progress.UpdateProgress(20);
                var (restored, failed) = await _tweakService.RestoreAllTweaksAsync();
                await RefreshWindowsUpdatePresetAsync();

                progress.UpdateProgress(100);
                progress.Close();

                var msg = L.Format("Tweaks:RestoredCount", restored);
                if (failed > 0) msg += "\n" + L.Format("Tweaks:RestoreFailedCount", failed);
                await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Tweaks:RestoreComplete"), msg);

                RestoreAllButton.Visibility = _tweakService.HasAppliedTweaksThisSession ? Visibility.Visible : Visibility.Collapsed;

                NotifyMainWindowBadgeUpdate();
            }
            catch (Exception ex)
            {
                progress.Close();
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:RestoreFailed"), ex.Message);
            }
        }

    }
}
