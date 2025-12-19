using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        private string FormatRegistryValue(object? value)
        {
            if (value is null) return "<null>";
            return value is bool b ? (b ? "1 (REG_DWORD)" : "0 (REG_DWORD)") :
                   value is int i ? $"{i} (REG_DWORD)" :
                   value is long l ? $"{l} (REG_QWORD)" :
                   value is string s ? $"\"{s}\" (REG_SZ)" : value.ToString() ?? string.Empty;
        }

        public RegistryTweaksPage()
        {
            InitializeComponent();
            _tweakService = TweakService.Instance;
            _registryService = RegistryService.Instance;

            DataContext = _registryService;

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
                    if (this.FindName("RestoreAllButton") is Button restoreBtn)
                    {
                        restoreBtn.Visibility = _tweakService.HasAppliedTweaksThisSession ? Visibility.Visible : Visibility.Collapsed;
                        restoreBtn.IsEnabled = _tweakService.HasAppliedTweaksThisSession;
                        restoreBtn.Opacity = _tweakService.HasAppliedTweaksThisSession ? 1.0 : 0.65;
                    }
                });
            }
        }

        private void RegistryTweaksPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Show one-time per-session disclaimer on first visit to Registry Tweaks section
            if (!_tweakService.RegistryDisclaimerShown)
            {
                var dialog = new TweakHub.Views.Dialogs.DisclaimerDialog
                {
                    Owner = Window.GetWindow(this)
                };
                dialog.ShowDialog();
                _tweakService.RegistryDisclaimerShown = true;
            }

            LoadTweaks();
            LoadCustomTweaks();
            RenderCustomTweaks();
        }

        private void LoadTweaks()
        {
            // Load the tweak data first
            _tweakService.LoadTweaks();

            // Then bind to UI and refresh states
            if (this.FindName("TweakCategoriesControl") is ItemsControl tweakCategoriesControl)
            {
                tweakCategoriesControl.ItemsSource = _tweakService.TweakCategories;
            }
            _tweakService.RefreshTweakStates();

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

        private void RenderCustomTweaks()
        {
            if (this.FindName("CustomTweaksList") is ItemsControl list)
            {
                list.Items.Clear();
                foreach (var t in _customTweaks)
                {
                    list.Items.Add(CreateCustomTweakCard(t));
                }
            }
        }

        private Border CreateCustomTweakCard(CustomRegistryTweak t)
        {
            var card = new Border { Style = (Style)FindResource("TweakItemStyle") };
            var root = new StackPanel();

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var info = new StackPanel();
            var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,8) };
            header.Children.Add(new TextBlock { Text = "🧩", FontSize = 16, Margin = new Thickness(0,0,8,0) });
            header.Children.Add(new TextBlock { Text = t.Name, FontSize = 16, FontWeight = FontWeights.Medium });
            info.Children.Add(header);
            info.Children.Add(new TextBlock { Text = $"{t.RegistryPath}\\{t.RegistryKey}", Margin = new Thickness(16,0,0,0), Opacity = 0.8 });
            info.Children.Add(new TextBlock { Text = $"{t.ValueType}: {t.Data}", Margin = new Thickness(16,2,0,0), Opacity = 0.8 });
            Grid.SetColumn(info,0); grid.Children.Add(info);

            var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var viewBtn = new Button { Style = GetStyleOrDefault("ModernButtonStyle"), Margin = new Thickness(0,0,8,0) };
            viewBtn.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "👁️", Margin = new Thickness(0,0,8,0) }, new TextBlock { Text = "View" } } };
            var applyBtn = new Button { Style = GetStyleOrDefault("ExecuteButtonStyle"), Margin = new Thickness(0,0,8,0) };
            applyBtn.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "▶️", Margin = new Thickness(0,0,8,0) }, new TextBlock { Text = "Apply" } } };
            var editBtn = new Button { Style = GetStyleOrDefault("ModernButtonStyle"), Margin = new Thickness(0,0,8,0) };
            editBtn.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "✏️", Margin = new Thickness(0,0,8,0) }, new TextBlock { Text = "Edit" } } };
            var deleteBtn = new Button { Style = GetStyleOrDefault("DangerButtonStyle") };
            deleteBtn.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "🗑️", Margin = new Thickness(0,0,8,0) }, new TextBlock { Text = "Delete" } } };
            actions.Children.Add(viewBtn); actions.Children.Add(applyBtn); actions.Children.Add(editBtn); actions.Children.Add(deleteBtn);
            Grid.SetColumn(actions,1); grid.Children.Add(actions);

            var preview = new Border { Margin = new Thickness(0,12,0,0), CornerRadius = new CornerRadius(8), BorderBrush = GetBrushOrDefault("SystemControlBorderBaseLowBrush"), BorderThickness = new Thickness(1), Background = GetBrushOrDefault("SystemControlBackgroundBaseLowBrush"), Visibility = Visibility.Collapsed, Name = $"customPreview_{SanitizeName(t.Id)}" };
            var pGrid = new Grid(); pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(150) });
            var pHead = new DockPanel { Margin = new Thickness(12,8,12,4) }; pHead.Children.Add(new TextBlock { Text = "Details", FontWeight = FontWeights.SemiBold }); pGrid.Children.Add(pHead);
            var inner = new Border { Margin = new Thickness(12), CornerRadius = new CornerRadius(6), BorderBrush = GetBrushOrDefault("SystemControlBorderBaseLowBrush"), BorderThickness = new Thickness(1), Background = GetBrushOrDefault("SystemControlBackgroundChromeMediumLowBrush") };
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var tb = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, FontFamily = new System.Windows.Media.FontFamily("Consolas"), BorderThickness = new Thickness(0), Background = System.Windows.Media.Brushes.Transparent, Name = $"customPreviewText_{SanitizeName(t.Id)}" };
            scroll.Content = tb; inner.Child = scroll; Grid.SetRow(inner,1); pGrid.Children.Add(inner);
            preview.Child = pGrid;

            // Handlers
            viewBtn.Click += (_, __) =>
            {
                var cmd = $"reg add \"{t.RegistryPath}\" /v \"{t.RegistryKey}\" /t {t.ValueType} /d {t.Data} /f";
                tb.Text = cmd;
                preview.Visibility = preview.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            };
            applyBtn.Click += (_, __) =>
            {
                try
                {
                    object? parsed = ParseRegistryData(t.ValueType, t.Data, out var kind);
                    var ok = _registryService.SetRegistryValue(t.RegistryPath, t.RegistryKey, parsed, kind);
                    MessageBox.Show(ok ? "Applied." : "Failed to apply.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            };

            editBtn.Click += (_, __) => OpenCustomTweakDialog(t);
            deleteBtn.Click += (_, __) =>
            {
                var owner = Window.GetWindow(this);
                var proceed = StyledMessageDialog.ShowYesNo(owner, "Confirm", $"Delete '{t.Name}'?");
                if (!proceed) return;
                var toRemove = _customTweaks.FirstOrDefault(x => x.Id == t.Id);
                if (toRemove != null) _customTweaks.Remove(toRemove);
                _userData.SaveCustomTweaks(_customTweaks);
                RenderCustomTweaks();
            };

            root.Children.Add(grid);
            root.Children.Add(preview);
            card.Child = root;
            return card;
        }

        private object? ParseRegistryData(string valueType, string data, out Microsoft.Win32.RegistryValueKind? explicitKind)
        {
            explicitKind = null;
            switch (valueType.ToUpperInvariant())
            {
                case "REG_DWORD":
                case "REG_DWORD (32-BIT)":
                    explicitKind = Microsoft.Win32.RegistryValueKind.DWord;
                    return int.TryParse(data, out var i) ? i : 0;
                case "REG_QWORD":
                case "REG_QWORD (64-BIT)":
                    explicitKind = Microsoft.Win32.RegistryValueKind.QWord;
                    return long.TryParse(data, out var l) ? l : 0L;
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
            var cleaned = new string(hex.Where(c => Uri.IsHexDigit(c)).ToArray());
            if (cleaned.Length % 2 == 1) cleaned = "0" + cleaned;
            var bytes = new byte[cleaned.Length / 2];
            for (int n = 0; n < bytes.Length; n++) bytes[n] = Convert.ToByte(cleaned.Substring(n * 2, 2), 16);
            return bytes;
        }

        private void AddCustomTweak_Click(object sender, RoutedEventArgs e)
        {
            OpenCustomTweakDialog();
        }

        private void OpenCustomTweakDialog(CustomRegistryTweak? existing = null)
        {
            var theme = Services.ThemeService.Instance;
            var isDark = theme != null && (theme.CurrentTheme == Services.AppTheme.Dark || (theme.CurrentTheme == Services.AppTheme.System));

            var dialog = new Window
            {
                Title = existing == null ? "Add Custom Registry Tweak" : "Edit Custom Registry Tweak",
                Width = 480,
                Height = 480,
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = isDark ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30,30,30)) : (System.Windows.Media.Brush)Application.Current.FindResource("SystemControlBackgroundBaseLowBrush"),
                ResizeMode = ResizeMode.NoResize
            };
            dialog.KeyDown += (_, ke) => { if (ke.Key == System.Windows.Input.Key.Escape) dialog.Close(); };

            var grid = new Grid { Margin = new Thickness(16) };
            for (int n=0; n<12; n++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock { Text = existing == null ? "Create Custom Tweak" : "Edit Custom Tweak", FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,12), Foreground = isDark ? System.Windows.Media.Brushes.White : (System.Windows.Media.Brush)Application.Current.FindResource("SystemControlForegroundBaseHighBrush") };
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
            var cancel = new Button { Content = "Cancel", Style = GetStyleOrDefault("ModernButtonStyle"), Margin = new Thickness(0,0,8,0), MinWidth = 96, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
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
                if (existing == null)
                {
                    var tweak = new CustomRegistryTweak
                    {
                        Name = nameBox.Text.Trim(),
                        RegistryPath = pathBox.Text.Trim(),
                        RegistryKey = keyBox.Text.Trim(),
                        ValueType = mapType(typeBox.SelectedItem?.ToString() ?? "REG_SZ"),
                        Data = dataBox.Text ?? string.Empty
                    };
                    _customTweaks.Add(tweak);
                }
                else
                {
                    existing.Name = nameBox.Text.Trim();
                    existing.RegistryPath = pathBox.Text.Trim();
                    existing.RegistryKey = keyBox.Text.Trim();
                    existing.ValueType = mapType(typeBox.SelectedItem?.ToString() ?? "REG_SZ");
                    existing.Data = dataBox.Text ?? string.Empty;
                }
                _userData.SaveCustomTweaks(_customTweaks);
                RenderCustomTweaks();
                dialog.Close();
            };

            dialog.Content = grid;
            dialog.ShowDialog();
        }

        private Style GetStyleOrDefault(string key)
        {
            var style = TryFindResource(key) as Style ?? Application.Current.TryFindResource(key) as Style;
            return style ?? (Application.Current.TryFindResource("ModernButtonStyle") as Style ?? new Style(typeof(Button)));
        }

        private System.Windows.Media.Brush GetBrushOrDefault(string key, string fallback = "#444444")
        {
            var brush = TryFindResource(key) as System.Windows.Media.Brush ?? Application.Current.TryFindResource(key) as System.Windows.Media.Brush;
            if (brush != null) return brush;
            var conv = new System.Windows.Media.BrushConverter();
            var obj = conv.ConvertFromString(fallback);
            return obj as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Gray;
        }

        private string SanitizeName(string id)
        {
            var arr = id.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
            return new string(arr);
        }

        private async void TweakToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is PerformanceTweak tweak)
            {
                // Desired final state after applying this tweak
                var targetState = checkBox.IsChecked == true;
                
                // Show confirmation for high-risk tweaks
                if (tweak.RiskLevel >= 3)
                {
                    var result = MessageBox.Show(
                        $"This tweak has a high risk level ({tweak.RiskLevel}/5).\n\n" +
                        $"Description: {tweak.Description}\n\n" +
                        "Are you sure you want to apply this change?",
                        "High Risk Tweak Warning",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                    {
                        // Revert the checkbox state
                        checkBox.IsChecked = !targetState;
                        tweak.IsEnabled = !targetState;
                        return;
                    }



                }

                // Disable the checkbox while processing
                checkBox.IsEnabled = false;

                try
                {
                    var success = await _tweakService.ApplyTweakAsync(tweak, targetState);

                    if (success)
                    {
                        NotifyMainWindowBadgeUpdate();
                    }

                    if (!success)
                    {
                        MessageBox.Show(
                            $"Failed to apply tweak: {tweak.Name}\n\n" +
                            "This may be due to insufficient permissions or system restrictions.",
                            "Tweak Application Failed",



                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                        // Revert the checkbox state
                        checkBox.IsChecked = !targetState;
                        tweak.IsEnabled = !targetState;
                    }
                    else if (tweak.RequiresRestart && !_tweakService.RestartNoticeShownThisSession)
                    {
                        _tweakService.RestartNoticeShownThisSession = true;
                        var dialog = new TweakHub.Views.Dialogs.RestartRequiredDialog(
                            "A system restart is required for the changes to take effect.")
                        {
                            Owner = Window.GetWindow(this)
                        };
                        dialog.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"An error occurred while applying the tweak:\n\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);


                    // Revert the checkbox state
                    checkBox.IsChecked = !targetState;
                    tweak.IsEnabled = !targetState;
                }
                finally
                {
                    checkBox.IsEnabled = true;
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _tweakService.RefreshTweakStates();
        }

        private void ViewTweak_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PerformanceTweak tweak)
            {
                // Toggle preview visibility and populate content on first open
                if (!tweak.IsPreviewVisible)
                {
                    string regType = (tweak.EnabledValue is int || tweak.EnabledValue is bool)
                        ? "REG_DWORD"
                        : (tweak.EnabledValue is long) ? "REG_QWORD" : "REG_SZ";

                    string enabledData = tweak.EnabledValue is bool b ? (b ? "1" : "0") : (tweak.EnabledValue?.ToString() ?? "");
                    string disabledData = tweak.DisabledValue is bool b2 ? (b2 ? "1" : "0") : (tweak.DisabledValue?.ToString() ?? "");

                    var enableCmd = $"reg add \"{tweak.RegistryPath}\" /v \"{tweak.RegistryKey}\" /t {regType} /d {enabledData} /f";
                    var disableCmd = string.IsNullOrWhiteSpace(disabledData)
                        ? "(no restore command available)"
                        : $"reg add \"{tweak.RegistryPath}\" /v \"{tweak.RegistryKey}\" /t {regType} /d {disabledData} /f";

                    tweak.PreviewContent =
                        $"# {tweak.Name}\n" +
                        $"# Risk: {tweak.RiskLevel}/5\n" +
                        $"# Path: {tweak.RegistryPath}\n" +
                        $"# Value: {tweak.RegistryKey}\n\n" +
                        $"# Apply (Enabled)\n{enableCmd}\n\n" +
                        $"# Restore (Disabled)\n{disableCmd}";
                }

                tweak.IsPreviewVisible = !tweak.IsPreviewVisible;
            }
        }

        private void CopyPreview_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PerformanceTweak tweak)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(tweak.PreviewContent))
                    {
                        Clipboard.SetText(tweak.PreviewContent);
                    }
                }
                catch
                {
                    // Ignore clipboard exceptions
                }
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _registryService.SaveBackupsToFile();
                MessageBox.Show(
                    "Registry backup has been created successfully.\n\n" +
                    "Backup location: %AppData%\\TweakHub\\Backups",
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
                _registryService.SaveBackupsToFile();
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
                var dialog = new TweakHub.Views.Dialogs.RestartRequiredDialog(
                    "Some changes require a system restart to take effect.")
                {
                    Owner = Window.GetWindow(this)
                };
                dialog.ShowDialog();
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

                var result = await _powerShellService.ExecuteScriptAsync(script);

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(90);
                await Task.Delay(500);

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

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(90);
                await Task.Delay(300);

                progress.UpdateProgress(100);
                progress.Close();

                var msg = $"Restored {restored} tweak(s).";
                if (failed > 0) msg += $"\n{failed} tweak(s) failed to restore.";
                MessageBox.Show(msg, "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                // Update visibility of Restore All button
                if (this.FindName("RestoreAllButton") is Button restoreBtn)
                {
                    restoreBtn.Visibility = _tweakService.HasAppliedTweaksThisSession ? Visibility.Visible : Visibility.Collapsed;
                }
                
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
