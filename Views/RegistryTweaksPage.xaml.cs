using System.Collections.ObjectModel;
using System.Linq;
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
        private readonly ObservableCollection<CustomRegistryTweak> _customTweaks = new();
        private readonly ObservableCollection<PerformanceTweak> _favoriteBuiltInTweaks = new();
        private readonly ObservableCollection<CustomRegistryTweak> _favoriteCustomTweaks = new();
        private HashSet<string> _favoriteTweakKeys = [];
        private bool _ignoreToggleEvent;
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
            // Show one-time per-session disclaimer on first visit to Registry Tweaks section
            if (!_tweakService.RegistryDisclaimerShown)
            {
                await AppDialog.ShowDisclaimerAsync(Window.GetWindow(this));
                _tweakService.RegistryDisclaimerShown = true;
            }

            await LoadTweaksAsync();
            LoadCustomTweaks();
            LoadFavoriteTweaks();
        }

        private async Task LoadTweaksAsync()
        {
            // Load the tweak data first
            _tweakService.LoadTweaks();

            // Then bind to UI and refresh states
            TweakCategoriesControl.ItemsSource = _tweakService.TweakCategories;
            await _tweakService.RefreshTweakStatesAsync();

            // Update sidebar badge immediately after initial state refresh
            NotifyMainWindowBadgeUpdate();
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
                var value = ParseRegistryData(tweak.ValueType, tweak.Data, out _);
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
                var value = ParseRegistryData(tweak.ValueType, tweak.Data, out var kind);
                var success = _registryService.ApplyValueWithBackup(tweak.RegistryPath, tweak.RegistryKey, value, kind);
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
            var success = _registryService.RestoreRegistryValue(tweak.RegistryPath, tweak.RegistryKey);
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

        private object? ParseRegistryData(string valueType, string data, out Microsoft.Win32.RegistryValueKind? explicitKind)
        {
            explicitKind = null;
            switch (valueType.ToUpperInvariant())
            {
                case "REG_DWORD":
                case "REG_DWORD (32-BIT)":
                    explicitKind = Microsoft.Win32.RegistryValueKind.DWord;
                    return int.TryParse(data, out var i) ? i : throw new FormatException(L.Get("Tweaks:DwordInvalid"));
                case "REG_QWORD":
                case "REG_QWORD (64-BIT)":
                    explicitKind = Microsoft.Win32.RegistryValueKind.QWord;
                    return long.TryParse(data, out var l) ? l : throw new FormatException(L.Get("Tweaks:QwordInvalid"));
                case "REG_BINARY":
                    explicitKind = Microsoft.Win32.RegistryValueKind.Binary;
                    return ParseHexToBytes(data);
                case "REG_MULTI_SZ":
                    explicitKind = Microsoft.Win32.RegistryValueKind.MultiString;
                    return data.Split(new[] { '\n', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s.Trim()).ToArray();
                case "REG_EXPAND_SZ":
                    explicitKind = Microsoft.Win32.RegistryValueKind.ExpandString;
                    return data;
                case "REG_SZ":
                default:
                    explicitKind = Microsoft.Win32.RegistryValueKind.String;
                    return data;
            }
        }

        private static byte[] ParseHexToBytes(string hex)
        {
            var cleaned = new string(hex.Where(c => !char.IsWhiteSpace(c) && c is not ',' and not '-').ToArray());
            if (cleaned.Length == 0 || cleaned.Length % 2 != 0 || cleaned.Any(c => !Uri.IsHexDigit(c)))
                throw new FormatException(L.Get("Tweaks:BinaryInvalid"));
            return Convert.FromHexString(cleaned);
        }

        private void AddCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            OpenCustomTweakDialog();
        }

        private void OpenCustomTweakDialog(CustomRegistryTweak? existing = null)
        {
            var dialog = new Window
            {
                Title = L.Get(existing == null ? "Tweaks:AddCustomRegistryTweak" : "Tweaks:EditCustomRegistryTweak"),
                Width = 480,
                Height = 480,
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = (System.Windows.Media.Brush)Application.Current.FindResource("WindowBackgroundBrush"),
                ResizeMode = ResizeMode.NoResize
            };
            dialog.KeyDown += (_, ke) => { if (ke.Key == System.Windows.Input.Key.Escape) dialog.Close(); };

            var grid = new Grid { Margin = new Thickness(16) };
            for (int n=0; n<12; n++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock { Text = L.Get(existing == null ? "Tweaks:CreateCustomTweak" : "Tweaks:EditCustomTweak"), FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,16), Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("SystemControlForegroundBaseHighBrush") };
            grid.Children.Add(title);

            var nameLbl = new TextBlock { Text = L.Get("Tweaks:Name"), Margin = new Thickness(0,0,0,4) }; Grid.SetRow(nameLbl,1); grid.Children.Add(nameLbl);
            var nameBox = new TextBox { Height = 32, Margin = new Thickness(0,0,0,8), Text = existing?.Name ?? "" };
            Grid.SetRow(nameBox,2); grid.Children.Add(nameBox);

            var pathLbl = new TextBlock { Text = L.Get("Tweaks:RegistryPathLabel") }; Grid.SetRow(pathLbl,3); grid.Children.Add(pathLbl);
            var pathBox = new TextBox { Height = 32, Margin = new Thickness(0,0,0,8), Text = existing?.RegistryPath ?? "" };
            Grid.SetRow(pathBox,4); grid.Children.Add(pathBox);

            var keyLbl = new TextBlock { Text = L.Get("Tweaks:ValueName") }; Grid.SetRow(keyLbl,5); grid.Children.Add(keyLbl);
            var keyBox = new TextBox { Height = 32, Margin = new Thickness(0,0,0,8), Text = existing?.RegistryKey ?? "" };
            Grid.SetRow(keyBox,6); grid.Children.Add(keyBox);

            var typeLbl = new TextBlock { Text = L.Get("Tweaks:Type") }; Grid.SetRow(typeLbl,7); grid.Children.Add(typeLbl);
            var typeBox = new ComboBox { Height = 32, Margin = new Thickness(0,0,0,8) };
            typeBox.ItemsSource = new[] { "REG_SZ", "REG_DWORD (32-bit)", "REG_QWORD (64-bit)", "REG_BINARY", "REG_MULTI_SZ", "REG_EXPAND_SZ" };
            typeBox.SelectedIndex = 0;
            if (existing != null)
            {
                var current = existing.ValueType.ToUpperInvariant();
                var map = new Dictionary<string,int> {
                    {"REG_SZ",0},{"REG_DWORD",1},{"REG_QWORD",2},{"REG_BINARY",3},{"REG_MULTI_SZ",4},{"REG_EXPAND_SZ",5}
                };
                if (map.TryGetValue(current, out var idx)) typeBox.SelectedIndex = idx; else typeBox.SelectedIndex = 0;
            }
            Grid.SetRow(typeBox,8); grid.Children.Add(typeBox);

            var dataLbl = new TextBlock { Text = L.Get("Tweaks:Value") }; Grid.SetRow(dataLbl,9); grid.Children.Add(dataLbl);
            var dataBox = new TextBox { Height = 28, Margin = new Thickness(0,0,0,8), Text = existing?.Data ?? "" };
            Grid.SetRow(dataBox,10); grid.Children.Add(dataBox);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0,8,0,0) };
            var cancel = new Button { Content = L.Get("Tweaks:Cancel"), Style = GetStyleOrDefault("SecondaryButtonStyle"), Margin = new Thickness(0,0,8,0), MinWidth = 96, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
            var create = new Button { Content = L.Get(existing == null ? "Tweaks:Create" : "Tweaks:Save"), Style = GetStyleOrDefault("ExecuteButtonStyle"), MinWidth = 100, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
            buttons.Children.Add(cancel); buttons.Children.Add(create);
            Grid.SetRow(buttons,11); grid.Children.Add(buttons);

            cancel.Click += (_, __) => dialog.Close();
            create.Click += async (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(pathBox.Text) || string.IsNullOrWhiteSpace(keyBox.Text))
                {
                    await AppDialog.ShowWarningAsync(dialog, L.Get("Tweaks:InvalidRegistryTweak"), L.Get("Tweaks:RequiredCustomFields"));
                    return;
                }
                string mapType(string s) => s.StartsWith("REG_DWORD") ? "REG_DWORD" : s.StartsWith("REG_QWORD") ? "REG_QWORD" : s;
                var path = pathBox.Text.Trim();
                var valueName = keyBox.Text.Trim();
                var valueType = mapType(typeBox.SelectedItem?.ToString() ?? "REG_SZ");
                var data = dataBox.Text ?? string.Empty;
                try
                {
                    RegistryService.ValidateLocation(path, valueName);
                    _ = ParseRegistryData(valueType, data, out var ignoredKind);
                }
                catch (Exception ex)
                {
                    await AppDialog.ShowWarningAsync(dialog, L.Get("Tweaks:InvalidRegistryTweak"), ex.Message);
                    return;
                }

                if (existing == null)
                {
                    _customTweaks.Add(new CustomRegistryTweak
                    {
                        Name = nameBox.Text.Trim(),
                        RegistryPath = path,
                        RegistryKey = valueName,
                        ValueType = valueType,
                        Data = data
                    });
                }
                else
                {
                    existing.Name = nameBox.Text.Trim();
                    existing.RegistryPath = path;
                    existing.RegistryKey = valueName;
                    existing.ValueType = valueType;
                    existing.Data = data;
                }
                foreach (var tweak in _customTweaks) RefreshCustomTweakState(tweak);
                _userData.SaveCustomTweaks(_customTweaks);
                CustomTweaksList.Items.Refresh();
                RefreshFavoriteLists();
                dialog.Close();
            };

            dialog.Content = grid;
            dialog.ShowDialog();
        }

        private Style GetStyleOrDefault(string key) => (Style)FindResource(key);

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
            var changes = TweakService.GetCompositeRegistryChanges(tweak.Id);
            if (changes.Count > 0)
            {
                var commands = changes.Select(change =>
                    $"reg add \"{change.KeyPath}\" /v \"{change.ValueName}\" /t {change.Kind switch { Microsoft.Win32.RegistryValueKind.String => "REG_SZ", _ => "REG_DWORD" }} /d {change.Value} /f");
                return L.Format("Tweaks:CompositePreview", tweak.Name, tweak.RiskLevel, string.Join("\n", commands));
            }

            var command = tweak.Id switch
            {
                "disable_cpu_throttling" => "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 100",
                "disable_core_parking" => "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100",
                "high_performance_power_plan" => "powercfg /setactive SCHEME_MIN",
                "disable_sysmain" => "Set-Service -Name SysMain -StartupType Disabled\nStop-Service -Name SysMain -Force",
                "disable_mouse_acceleration" => "reg add \"HKCU\\Control Panel\\Mouse\" /v MouseSpeed /t REG_SZ /d 0 /f\nreg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold1 /t REG_SZ /d 0 /f\nreg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold2 /t REG_SZ /d 0 /f",
                "windows_update_security_preset" => "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 2 /f\nreg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /t REG_DWORD /d 0 /f\nreg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DeferFeatureUpdates /t REG_DWORD /d 1 /f\nreg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DeferFeatureUpdatesPeriodInDays /t REG_DWORD /d 90 /f",
                _ => $"reg add \"{tweak.RegistryPath}\" /v \"{tweak.RegistryKey}\" /t {(tweak.EnabledValue is int ? "REG_DWORD" : tweak.EnabledValue is long ? "REG_QWORD" : "REG_SZ")} /d {(Equals(tweak.EnabledValue, -1) ? "ffffffff" : tweak.EnabledValue)} /f"
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

            // Create backup first
            try
            {
                _registryService.CreateBackup(_tweakService.TweakCategories.SelectMany(c => c.Tweaks));
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tweaks:BackupFailed"),
                    L.Format("Tweaks:BackupFailedMessage", ex.Message));
                return;
            }

            // Apply recommended tweaks (risk level 1-2)
            var recommendedTweaks = _tweakService.TweakCategories
                .SelectMany(c => c.Tweaks)
                .Where(t => t.RiskLevel <= 2 && !t.IsEnabled)
                .ToList();

            if (!recommendedTweaks.Any())
            {
                await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Tweaks:NoChangesNeeded"),
                    L.Get("Tweaks:RecommendedAlreadyApplied"));
                return;
            }

            var progressWindow = new ProgressWindow(L.Format("Tweaks:ApplyingRecommended", recommendedTweaks.Count));
            progressWindow.Show();

            int applied = 0;
            int failed = 0;
            bool requiresRestart = false;

            foreach (var tweak in recommendedTweaks)
            {
                try
                {
                    var success = await _tweakService.ApplyTweakAsync(tweak, true);
                    if (success)
                    {
                        applied++;
                        if (tweak.RequiresRestart)
                            requiresRestart = true;
                    }
                    else
                    {
                        failed++;
                    }
                }
                catch
                {
                    failed++;
                }

                progressWindow.UpdateProgress((applied + failed) * 100 / recommendedTweaks.Count);
            }

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



        private async void RestoreAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    L.Get("Tweaks:RestoreAllChanges"),
                    L.Get("Tweaks:RestoreAllMessage"),
                    L.Get("Tweaks:Restore"),
                    L.Get("Tweaks:Cancel"))) return;

            var progress = new ProgressWindow(L.Get("Tweaks:RestoringAll"));
            progress.Show();

            try
            {
                progress.UpdateStatus(L.Get("Tweaks:Restoring"));
                progress.UpdateProgress(20);
                var (restored, failed) = await _tweakService.RestoreAllTweaksAsync();

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
