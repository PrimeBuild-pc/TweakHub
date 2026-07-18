using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ToggleSwitch = ModernWpf.Controls.ToggleSwitch;
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
        private bool _ignoreToggleEvent;
        public RegistryTweaksPage()
        {
            InitializeComponent();
            _tweakService = TweakService.Instance;
            _registryService = RegistryService.Instance;

            DataContext = _registryService;
            CustomTweaksList.ItemsSource = _customTweaks;

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
            foreach (var t in _userData.LoadCustomTweaks()) _customTweaks.Add(t);
        }

        private void ViewCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: CustomRegistryTweak tweak, Tag: Border preview }
                || preview.Tag is not TextBox textBox) return;

            textBox.Text = $"reg add \"{tweak.RegistryPath}\" /v \"{tweak.RegistryKey}\" /t {tweak.ValueType} /d {tweak.Data} /f";
            preview.Visibility = preview.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ApplyCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: CustomRegistryTweak tweak }) return;
            try
            {
                var value = ParseRegistryData(tweak.ValueType, tweak.Data, out var kind);
                MessageBox.Show(_registryService.ApplyValueWithBackup(tweak.RegistryPath, tweak.RegistryKey, value, kind)
                    ? "Applied."
                    : "Failed to apply.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void RestoreCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: CustomRegistryTweak tweak })
                MessageBox.Show(_registryService.RestoreRegistryValue(tweak.RegistryPath, tweak.RegistryKey)
                    ? "Restored."
                    : "No backup is available for this value.");
        }

        private void EditCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: CustomRegistryTweak tweak }) OpenCustomTweakDialog(tweak);
        }

        private async void DeleteCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: CustomRegistryTweak tweak }
                || !await AppDialog.ConfirmAsync(Window.GetWindow(this), "Confirm", $"Delete '{tweak.Name}'?")) return;

            _customTweaks.Remove(tweak);
            _userData.SaveCustomTweaks(_customTweaks);
        }

        private object? ParseRegistryData(string valueType, string data, out Microsoft.Win32.RegistryValueKind? explicitKind)
        {
            explicitKind = null;
            switch (valueType.ToUpperInvariant())
            {
                case "REG_DWORD":
                case "REG_DWORD (32-BIT)":
                    explicitKind = Microsoft.Win32.RegistryValueKind.DWord;
                    return int.TryParse(data, out var i) ? i : throw new FormatException("DWORD must be a valid 32-bit integer.");
                case "REG_QWORD":
                case "REG_QWORD (64-BIT)":
                    explicitKind = Microsoft.Win32.RegistryValueKind.QWord;
                    return long.TryParse(data, out var l) ? l : throw new FormatException("QWORD must be a valid 64-bit integer.");
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
                throw new FormatException("Binary data must contain complete hexadecimal byte pairs.");
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
                Title = existing == null ? "Add Custom Registry Tweak" : "Edit Custom Registry Tweak",
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

            var title = new TextBlock { Text = existing == null ? "Create Custom Tweak" : "Edit Custom Tweak", FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,16), Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("SystemControlForegroundBaseHighBrush") };
            grid.Children.Add(title);

            var nameLbl = new TextBlock { Text = "Name", Margin = new Thickness(0,0,0,4) }; Grid.SetRow(nameLbl,1); grid.Children.Add(nameLbl);
            var nameBox = new TextBox { Height = 32, Margin = new Thickness(0,0,0,8), Text = existing?.Name ?? "" };
            Grid.SetRow(nameBox,2); grid.Children.Add(nameBox);

            var pathLbl = new TextBlock { Text = "Registry Path (e.g., HKCU\\Software\\...)" }; Grid.SetRow(pathLbl,3); grid.Children.Add(pathLbl);
            var pathBox = new TextBox { Height = 32, Margin = new Thickness(0,0,0,8), Text = existing?.RegistryPath ?? "" };
            Grid.SetRow(pathBox,4); grid.Children.Add(pathBox);

            var keyLbl = new TextBlock { Text = "Value Name" }; Grid.SetRow(keyLbl,5); grid.Children.Add(keyLbl);
            var keyBox = new TextBox { Height = 32, Margin = new Thickness(0,0,0,8), Text = existing?.RegistryKey ?? "" };
            Grid.SetRow(keyBox,6); grid.Children.Add(keyBox);

            var typeLbl = new TextBlock { Text = "Type" }; Grid.SetRow(typeLbl,7); grid.Children.Add(typeLbl);
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

            var dataLbl = new TextBlock { Text = "Value" }; Grid.SetRow(dataLbl,9); grid.Children.Add(dataLbl);
            var dataBox = new TextBox { Height = 28, Margin = new Thickness(0,0,0,8), Text = existing?.Data ?? "" };
            Grid.SetRow(dataBox,10); grid.Children.Add(dataBox);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0,8,0,0) };
            var cancel = new Button { Content = "Cancel", Style = GetStyleOrDefault("SecondaryButtonStyle"), Margin = new Thickness(0,0,8,0), MinWidth = 96, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
            var create = new Button { Content = existing == null ? "Create" : "Save", Style = GetStyleOrDefault("ExecuteButtonStyle"), MinWidth = 100, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
            buttons.Children.Add(cancel); buttons.Children.Add(create);
            Grid.SetRow(buttons,11); grid.Children.Add(buttons);

            cancel.Click += (_, __) => dialog.Close();
            create.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(pathBox.Text) || string.IsNullOrWhiteSpace(keyBox.Text))
                {
                    MessageBox.Show("Please fill in name, path and value name.");
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
                    MessageBox.Show(ex.Message, "Invalid Registry Tweak", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                _userData.SaveCustomTweaks(_customTweaks);
                CustomTweaksList.Items.Refresh();
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
            if (tweak.RiskLevel >= 3 && MessageBox.Show(
                    $"This tweak has a high risk level ({tweak.RiskLevel}/5).\n\n" +
                    $"Description: {tweak.Description}\n\n" +
                    "Are you sure you want to apply this change?",
                    "High Risk Tweak Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
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
                    MessageBox.Show(
                        $"Failed to apply tweak: {tweak.Name}\n\n" +
                        "This may be due to insufficient permissions or system restrictions.",
                        "Tweak Application Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    RevertToggle(toggle, tweak, !targetState);
                }
                else
                {
                    NotifyMainWindowBadgeUpdate();
                    if (tweak.RequiresRestart && !_tweakService.RestartNoticeShownThisSession)
                    {
                        _tweakService.RestartNoticeShownThisSession = true;
                        await AppDialog.ShowRestartRequiredAsync(
                            Window.GetWindow(this), "A system restart is required for the changes to take effect.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while applying the tweak:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
            var command = tweak.Id switch
            {
                "disable_cpu_throttling" => "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 100",
                "disable_core_parking" => "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100",
                "high_performance_power_plan" => "powercfg /setactive SCHEME_MIN",
                "disable_mouse_acceleration" => "reg add \"HKCU\\Control Panel\\Mouse\" /v MouseSpeed /t REG_SZ /d 0 /f\nreg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold1 /t REG_SZ /d 0 /f\nreg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold2 /t REG_SZ /d 0 /f",
                "windows_update_security_preset" => "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 2 /f\nreg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /t REG_DWORD /d 0 /f\nreg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DeferFeatureUpdates /t REG_DWORD /d 1 /f\nreg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DeferFeatureUpdatesPeriodInDays /t REG_DWORD /d 90 /f",
                _ => $"reg add \"{tweak.RegistryPath}\" /v \"{tweak.RegistryKey}\" /t {(tweak.EnabledValue is int ? "REG_DWORD" : tweak.EnabledValue is long ? "REG_QWORD" : "REG_SZ")} /d {(Equals(tweak.EnabledValue, -1) ? "ffffffff" : tweak.EnabledValue)} /f"
            };
            return $"# {tweak.Name}\n# Risk: {tweak.RiskLevel}/5\n\n# Apply\n{command}\n\n# Restore\nTweakHub restores the captured original value.";
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var count = _registryService.CreateBackup(_tweakService.TweakCategories.SelectMany(c => c.Tweaks));
                MessageBox.Show(
                    $"Registry backup has been created successfully ({count} new value(s) captured).\n\n" +
                    "Backup location: %AppData%\\TweakHub\\registry-backup.json",
                    "Backup Created",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to create backup:\n\n{ex.Message}",
                    "Backup Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void ApplyAllButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will apply all recommended performance tweaks.\n\n" +
                "A backup will be created automatically before applying changes.\n\n" +
                "Do you want to continue?",
                "Apply All Recommended Tweaks",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // Create backup first
            try
            {
                _registryService.CreateBackup(_tweakService.TweakCategories.SelectMany(c => c.Tweaks));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to create backup. Operation cancelled.\n\n{ex.Message}",
                    "Backup Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Apply recommended tweaks (risk level 1-2)
            var recommendedTweaks = _tweakService.TweakCategories
                .SelectMany(c => c.Tweaks)
                .Where(t => t.RiskLevel <= 2 && !t.IsEnabled)
                .ToList();

            if (!recommendedTweaks.Any())
            {
                MessageBox.Show(
                    "All recommended tweaks are already applied.",
                    "No Changes Needed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var progressWindow = new ProgressWindow($"Applying {recommendedTweaks.Count} recommended tweaks...");
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

            var message = $"Applied {applied} tweaks successfully.";
            if (failed > 0)
                message += $"\n{failed} tweaks failed to apply.";

            if (requiresRestart && !_tweakService.RestartNoticeShownThisSession)
            {
                _tweakService.RestartNoticeShownThisSession = true;
                await AppDialog.ShowRestartRequiredAsync(
                    Window.GetWindow(this), "Some changes require a system restart to take effect.");
            }

            MessageBox.Show(message, "Tweaks Applied", MessageBoxButton.OK, MessageBoxImage.Information);
            NotifyMainWindowBadgeUpdate();
        }

        private async void CreateRestorePointButton_Click(object sender, RoutedEventArgs e)
        {
            var info = "This will create a System Restore Point named 'TweakHub - Pre Tweaks'.\n\n" +
                       "Note: Creating a restore point may take a minute and requires administrator privileges.";
            var proceed = MessageBox.Show(info + "\n\nProceed?", "Create Restore Point", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (proceed != MessageBoxResult.Yes) return;

            var progress = new ProgressWindow("Creating system restore point...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Requesting restore point...");
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
                    MessageBox.Show("Restore point created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to create restore point.\n\n{result.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show($"An error occurred:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async void RestoreAllButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "This will attempt to restore all registry tweaks to their original values.\n\nProceed?",
                "Restore All Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
                return;

            var progress = new ProgressWindow("Restoring all registry tweaks...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Restoring...");
                progress.UpdateProgress(20);
                var (restored, failed) = await _tweakService.RestoreAllTweaksAsync();

                progress.UpdateProgress(100);
                progress.Close();

                var msg = $"Restored {restored} tweak(s).";
                if (failed > 0) msg += $"\n{failed} tweak(s) failed to restore.";
                MessageBox.Show(msg, "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                RestoreAllButton.Visibility = _tweakService.HasAppliedTweaksThisSession ? Visibility.Visible : Visibility.Collapsed;

                NotifyMainWindowBadgeUpdate();
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show($"An error occurred while restoring:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
