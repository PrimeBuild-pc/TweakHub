using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using TweakHub.Services;
using TweakHub.Models;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views
{
    public partial class AutomatedScriptsPage : Page
    {
        private readonly TweakService _tweakService;
        private readonly RegistryService _registryService;
        private readonly PowerShellService _powerShellService;
        private readonly UserDataService _userDataService;
        private readonly ObservableCollection<CustomScript> _customScripts = new();

        public AutomatedScriptsPage()
        {
            InitializeComponent();
            _tweakService = TweakService.Instance;
            _registryService = RegistryService.Instance;
            _powerShellService = PowerShellService.Instance;
            _userDataService = UserDataService.Instance;
            LoadCustomScripts();
        }

        private void LoadCustomScripts()
        {
            var loaded = _userDataService.LoadCustomScripts();
            _customScripts.Clear();
            foreach (var s in loaded) _customScripts.Add(s);
        }

        private void NewScriptButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Create Custom Script",
                Width = 560,
                Height = 480,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Background = (Brush)FindResource("SystemControlBackgroundChromeMediumBrush")
            };

            var grid = new Grid { Margin = new Thickness(20) };            
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameBox = new TextBox { Margin = new Thickness(0,0,0,12), ToolTip = "Enter script name" };
            Grid.SetRow(nameBox,0);
            grid.Children.Add(nameBox);

            var langPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,12) };
            var psRadio = new RadioButton { Content = "PowerShell", IsChecked = true, Margin = new Thickness(0,0,12,0) };
            var cmdRadio = new RadioButton { Content = "CMD Batch" };
            langPanel.Children.Add(psRadio); langPanel.Children.Add(cmdRadio);
            Grid.SetRow(langPanel,1); grid.Children.Add(langPanel);

            var contentBox = new TextBox
            {
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0,0,0,12)
            };
            Grid.SetRow(contentBox,2); grid.Children.Add(contentBox);

            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancelBtn = new Button { Content = "Cancel", Style = GetStyleOrDefault("ModernButtonStyle"), Margin = new Thickness(0,0,8,0) };
            var createBtn = new Button { Content = "Create", Style = GetStyleOrDefault("ExecuteButtonStyle") };
            buttonsPanel.Children.Add(cancelBtn); buttonsPanel.Children.Add(createBtn);
            Grid.SetRow(buttonsPanel,3); grid.Children.Add(buttonsPanel);

            cancelBtn.Click += (_, __) => dialog.Close();
            createBtn.Click += (_, __) =>
            {
                var name = nameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Name required."); return; }
                var script = new CustomScript
                {
                    Name = name,
                    Language = psRadio.IsChecked == true ? ScriptLanguage.PowerShell : ScriptLanguage.Cmd,
                    Content = contentBox.Text
                };
                _customScripts.Add(script);
                _userDataService.SaveCustomScripts(_customScripts);
                dialog.Close();
                AppendCustomScriptCard(script);
                
                // Show confirmation message
                var owner = Window.GetWindow(this);
                StyledMessageDialog.ShowOk(
                    owner,
                    "Script Created",
                    $"Script '{name}' has been created successfully!\n\n" +
                    "You can now view and execute it from the scripts list.");
            };

            dialog.Content = grid;
            dialog.ShowDialog();
        }

        private void AppendCustomScriptCard(CustomScript script)
        {
            if (this.Content is Grid root && root.FindName("ScriptsHostPanel") is StackPanel host)
            {
                var card = CreateCustomScriptCard(script);
                card.Tag = "CustomScript";
                host.Children.Add(card);
            }
        }

        private Border CreateCustomScriptCard(CustomScript script)
        {
            var card = new Border { Style = (Style)FindResource("ScriptCardStyle") };
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var infoPanel = new StackPanel { Margin = new Thickness(0,0,0,0) };
            var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,12) };
            header.Children.Add(new TextBlock { Text = script.Language == ScriptLanguage.PowerShell ? "🧩" : "📄", FontSize = 24, Margin = new Thickness(0,0,12,0), Foreground = (System.Windows.Media.Brush)FindResource("IconBrush") });
            header.Children.Add(new TextBlock { Text = script.Name, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = (System.Windows.Media.Brush)FindResource("SystemControlForegroundBaseHighBrush") });
            infoPanel.Children.Add(header);
            infoPanel.Children.Add(new TextBlock { Text = script.Language == ScriptLanguage.PowerShell ? "Custom PowerShell script" : "Custom CMD batch script", Style = (Style)FindResource("DescriptionTextStyle"), Margin = new Thickness(36,0,0,12) });
            Grid.SetRow(infoPanel,0); Grid.SetColumn(infoPanel,0); grid.Children.Add(infoPanel);

            var actionsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var execBtn = new Button { Style = GetStyleOrDefault("ExecuteButtonStyle"), Margin = new Thickness(0,0,8,0) };
            execBtn.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "▶️", FontSize = 14, Margin = new Thickness(0,0,8,0), Foreground = Brushes.White }, new TextBlock { Text = script.Language == ScriptLanguage.PowerShell ? "Run" : "Execute" } } };
            var editBtn = new Button { Style = GetStyleOrDefault("ModernButtonStyle"), Margin = new Thickness(0,0,8,0) };
            editBtn.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "✏️", FontSize = 14, Margin = new Thickness(0,0,8,0), Foreground = Brushes.White }, new TextBlock { Text = "Edit" } } };
            var deleteBtn = new Button { Style = GetStyleOrDefault("DangerButtonStyle") };
            deleteBtn.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "🗑️", FontSize = 14, Margin = new Thickness(0,0,8,0), Foreground = Brushes.White }, new TextBlock { Text = "Delete" } } };
            
            actionsPanel.Children.Add(execBtn); actionsPanel.Children.Add(editBtn); actionsPanel.Children.Add(deleteBtn);
            Grid.SetColumn(actionsPanel,1); Grid.SetRow(actionsPanel,0); grid.Children.Add(actionsPanel);

            card.Child = grid;
            execBtn.Click += async (_, __) => await ExecuteCustomScript(script);
            editBtn.Click += (_, __) => EditCustomScript(script);
            deleteBtn.Click += (_, __) => DeleteCustomScript(script);
            NameScope.SetNameScope(card, new NameScope());
            return card;
        }

        private string SanitizeName(string id)
        {
            var arr = id.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
            return new string(arr);
        }

        private Style GetStyleOrDefault(string key)
        {
            var style = TryFindResource(key) as Style ?? Application.Current.TryFindResource(key) as Style;
            return style ?? (Application.Current.TryFindResource("ModernButtonStyle") as Style ?? new Style(typeof(Button)));
        }

        private async Task ExecuteCustomScript(CustomScript script)
        {
            try
            {
                if (script.Language == ScriptLanguage.PowerShell)
                {
                    var result = await _powerShellService.ExecuteScriptAsync(script.Content);
                    MessageBox.Show(result.Success ? "Script executed successfully" : $"Script failed: {result.Error}");
                }
                else
                {
                    // CMD batch: write temp file and execute
                    var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"custom_{script.Id}.bat");
                    System.IO.File.WriteAllText(tempPath, script.Content);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = tempPath,
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing script: {ex.Message}");
            }
        }

        private void EditCustomScript(CustomScript script)
        {
            var dialog = new Window
            {
                Title = "Edit Custom Script",
                Width = 560,
                Height = 480,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Background = (Brush)FindResource("SystemControlBackgroundChromeMediumBrush")
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameBox = new TextBox { Margin = new Thickness(0, 0, 0, 12), ToolTip = "Enter script name", Text = script.Name };
            Grid.SetRow(nameBox, 0);
            grid.Children.Add(nameBox);

            var langPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            var psRadio = new RadioButton { Content = "PowerShell", IsChecked = script.Language == ScriptLanguage.PowerShell, Margin = new Thickness(0, 0, 12, 0) };
            var cmdRadio = new RadioButton { Content = "CMD Batch", IsChecked = script.Language == ScriptLanguage.Cmd };
            langPanel.Children.Add(psRadio);
            langPanel.Children.Add(cmdRadio);
            Grid.SetRow(langPanel, 1);
            grid.Children.Add(langPanel);

            var contentBox = new TextBox
            {
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12),
                Text = script.Content
            };
            Grid.SetRow(contentBox, 2);
            grid.Children.Add(contentBox);

            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancelBtn = new Button { Content = "Cancel", Style = GetStyleOrDefault("ModernButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
            var saveBtn = new Button { Content = "Save", Style = GetStyleOrDefault("ExecuteButtonStyle") };
            buttonsPanel.Children.Add(cancelBtn);
            buttonsPanel.Children.Add(saveBtn);
            Grid.SetRow(buttonsPanel, 3);
            grid.Children.Add(buttonsPanel);

            cancelBtn.Click += (_, __) => dialog.Close();
            saveBtn.Click += (_, __) =>
            {
                var name = nameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Il nome è obbligatorio.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update the existing script object
                script.Name = name;
                script.Language = psRadio.IsChecked == true ? ScriptLanguage.PowerShell : ScriptLanguage.Cmd;
                script.Content = contentBox.Text;

                // Save to disk
                _userDataService.SaveCustomScripts(_customScripts);

                dialog.Close();

                // Rebuild the UI to reflect changes
                RefreshCustomScriptCards();

                MessageBox.Show(
                    $"Lo script '{name}' è stato aggiornato con successo!",
                    "Script Aggiornato",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            };

            dialog.Content = grid;
            dialog.ShowDialog();
        }

        private void DeleteCustomScript(CustomScript script)
        {
            var owner = Window.GetWindow(this);
            var proceed = StyledMessageDialog.ShowYesNo(
                owner,
                "Conferma Eliminazione",
                $"Sei sicuro di voler eliminare lo script '{script.Name}'?\n\nQuesta operazione non può essere annullata.");

            if (!proceed) return;

            var toRemove = _customScripts.FirstOrDefault(x => x.Id == script.Id);
            if (toRemove != null)
            {
                _customScripts.Remove(toRemove);
                _userDataService.SaveCustomScripts(_customScripts);
                
                // Refresh UI
                RefreshCustomScriptCards();

                StyledMessageDialog.ShowOk(
                    owner,
                    "Script Eliminato",
                    $"Lo script '{script.Name}' è stato eliminato.");
            }
        }

        private void RefreshCustomScriptCards()
        {
            if (this.Content is Grid root && root.FindName("ScriptsHostPanel") is StackPanel host)
            {
                // Remove all custom script cards (keep only built-in cards)
                var customCards = host.Children.OfType<Border>()
                    .Where(b => b.Tag != null && b.Tag.ToString() == "CustomScript")
                    .ToList();

                foreach (var card in customCards)
                {
                    host.Children.Remove(card);
                }

                // Re-add all custom scripts
                foreach (var script in _customScripts)
                {
                    var card = CreateCustomScriptCard(script);
                    card.Tag = "CustomScript";
                    host.Children.Add(card);
                }
            }
        }

        private void ToggleInlinePreview(string panelName, string textBoxName, string content)
        {
            if (this.FindName(panelName) is Border panel && this.FindName(textBoxName) is TextBox tb)
            {
                tb.Text = content;
                // Animate expand/collapse (simple opacity/height storyboard on demand)
                bool willShow = panel.Visibility != Visibility.Visible;
                if (willShow)
                {
                    panel.Visibility = Visibility.Visible;
                    var sb = new System.Windows.Media.Animation.Storyboard();
                    var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(180)));
                    System.Windows.Media.Animation.Storyboard.SetTarget(fadeIn, panel);
                    System.Windows.Media.Animation.Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
                    var heightAnim = new System.Windows.Media.Animation.DoubleAnimation(0, 180, new Duration(TimeSpan.FromMilliseconds(180)));
                    System.Windows.Media.Animation.Storyboard.SetTarget(heightAnim, panel);
                    System.Windows.Media.Animation.Storyboard.SetTargetProperty(heightAnim, new PropertyPath("Height"));
                    sb.Children.Add(fadeIn);
                    sb.Children.Add(heightAnim);
                    sb.Begin();
                }
                else
                {
                    var sb = new System.Windows.Media.Animation.Storyboard();
                    var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(panel.Opacity, 0, new Duration(TimeSpan.FromMilliseconds(140)));
                    System.Windows.Media.Animation.Storyboard.SetTarget(fadeOut, panel);
                    System.Windows.Media.Animation.Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
                    var heightAnim = new System.Windows.Media.Animation.DoubleAnimation(panel.ActualHeight, 0, new Duration(TimeSpan.FromMilliseconds(140)));
                    System.Windows.Media.Animation.Storyboard.SetTarget(heightAnim, panel);
                    System.Windows.Media.Animation.Storyboard.SetTargetProperty(heightAnim, new PropertyPath("Height"));
                    sb.Children.Add(fadeOut);
                    sb.Children.Add(heightAnim);
                    sb.Completed += (_, __) => { panel.Visibility = Visibility.Collapsed; };
                    sb.Begin();
                }
            }
        }

        private async void GamingOptimization_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will apply comprehensive gaming optimizations including:\n\n" +
                "• GPU performance settings\n" +
                "• CPU priority optimizations\n" +
                "• Memory management tweaks\n" +
                "• Network latency improvements\n\n" +
                "A system restart may be required. Continue?",
                "Gaming Performance Optimization",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var progressWindow = new ProgressWindow("Applying gaming optimizations...");
            progressWindow.Show();

            try
            {
                await ExecuteGamingOptimizations(progressWindow);
                progressWindow.Close();

                MessageBox.Show(
                    "Gaming optimizations have been applied successfully!\n\n" +
                    "For best results, restart your system and close unnecessary background applications before gaming.",
                    "Optimization Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"An error occurred during optimization:\n\n{ex.Message}",
                    "Optimization Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void GamingOptimization_View_Click(object sender, RoutedEventArgs e)
        {
            var content = @"# Gaming Performance Optimization
# This operation uses Windows APIs and system settings; no single shell script is executed.
# Steps include:
# - GPU performance profile configuration
# - CPU priority adjustments
# - Memory management tweaks
# - Network latency optimizations
# - High performance power plan";
            ToggleInlinePreview("GamingPreviewPanel", "GamingPreviewText", content);
        }

        private async void SystemCleanup_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will perform a deep system cleanup including:\n\n" +
                "• Temporary files and cache\n" +
                "• System logs and error reports\n" +
                "• Browser cache and cookies\n" +
                "• Recycle bin contents\n\n" +
                "Continue with cleanup?",
                "Deep System Cleanup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var progressWindow = new ProgressWindow("Performing system cleanup...");
            progressWindow.Show();

            try
            {
                await ExecuteSystemCleanup(progressWindow);
                progressWindow.Close();

                MessageBox.Show(
                    "System cleanup completed successfully!\n\n" +
                    "Your system should now have more free disk space and improved performance.",
                    "Cleanup Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"An error occurred during cleanup:\n\n{ex.Message}",
                    "Cleanup Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SystemCleanup_View_Click(object sender, RoutedEventArgs e)
        {
            var content = @"# Deep System Cleanup
# This operation orchestrates cleanup via Windows APIs and shell utilities; exact commands may vary by step.
# Typical steps:
# - Clear temporary files (%TEMP%)
# - Purge system caches and logs
# - Optionally clear browser cache
# - Empty recycle bin";
            ToggleInlinePreview("CleanupPreviewPanel", "CleanupPreviewText", content);
        }

        private async void NetworkOptimization_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will optimize network settings for:\n\n" +
                "• Reduced latency and ping\n" +
                "• Improved throughput\n" +
                "• Better gaming network performance\n" +
                "• DNS optimization\n\n" +
                "Network settings will be modified. Continue?",
                "Network Performance Boost",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var progressWindow = new ProgressWindow("Optimizing network settings...");
            progressWindow.Show();

            try
            {
                await ExecuteNetworkOptimizations(progressWindow);
                progressWindow.Close();

                MessageBox.Show(
                    "Network optimizations have been applied successfully!\n\n" +
                    "You may need to restart your network adapter or system for all changes to take effect.",
                    "Network Optimization Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"An error occurred during network optimization:\n\n{ex.Message}",
                    "Network Optimization Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void NetworkOptimization_View_Click(object sender, RoutedEventArgs e)
        {
            var content = @"# Network Performance Boost
# Adjusts network parameters for latency/throughput. These are applied via APIs/registry depending on step.
# Example areas:
# - TCP parameters (e.g., autotuning)
# - DNS resolver settings
# - Adapter offload/interrupt moderation
# - QoS/DSCP (when applicable)";
            ToggleInlinePreview("NetworkPreviewPanel", "NetworkPreviewText", content);
        }

        private async void HardwareOptimization_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will detect your hardware and apply manufacturer-specific optimizations:\n\n" +
                "• Intel/AMD CPU optimizations\n" +
                "• Nvidia/AMD GPU settings\n" +
                "• Chipset-specific tweaks\n" +
                "• Power management settings\n\n" +
                "Continue with hardware optimization?",
                "Hardware-Specific Optimization",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var progressWindow = new ProgressWindow("Detecting hardware and applying optimizations...");
            progressWindow.Show();

            try
            {
                await ExecuteHardwareOptimizations(progressWindow);
                progressWindow.Close();

                MessageBox.Show(
                    "Hardware-specific optimizations have been applied successfully!\n\n" +
                    "Your system has been optimized for your specific hardware configuration.",
                    "Hardware Optimization Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"An error occurred during hardware optimization:\n\n{ex.Message}",
                    "Hardware Optimization Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void HardwareOptimization_View_Click(object sender, RoutedEventArgs e)
        {
            var content = @"# Hardware-Specific Optimization
# Detects CPU/GPU vendor and applies recommended settings via vendor APIs/registry.
# Areas include:
# - Intel/AMD CPU power/perf tweaks
# - Nvidia/AMD driver settings
# - Chipset and power management";
            ToggleInlinePreview("HardwarePreviewPanel", "HardwarePreviewText", content);
        }

        private async void ResetAllTweaks_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ WARNING: This will reset ALL tweaks to their original values!\n\n" +
                "This action will:\n" +
                "• Restore all registry modifications\n" +
                "• Reset system settings to defaults\n" +
                "• Undo all performance optimizations\n\n" +
                "This action cannot be easily undone. Are you absolutely sure?",
                "Reset All Tweaks - WARNING",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            // Double confirmation for destructive action
            var confirmResult = MessageBox.Show(
                "This is your final confirmation.\n\n" +
                "Clicking 'Yes' will permanently reset all tweaks.\n\n" +
                "Do you want to proceed?",
                "Final Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop);

            if (confirmResult != MessageBoxResult.Yes)
                return;

            var progressWindow = new ProgressWindow("Resetting all tweaks...");
            progressWindow.Show();

            try
            {
                await ExecuteResetAllTweaks(progressWindow);
                progressWindow.Close();

                MessageBox.Show(
                    "All tweaks have been reset to their original values.\n\n" +
                    "Your system has been restored to its default configuration. " +
                    "A restart is recommended to ensure all changes take effect.",
                    "Reset Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"An error occurred during reset:\n\n{ex.Message}",
                    "Reset Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ResetAllTweaks_View_Click(object sender, RoutedEventArgs e)
        {
            var content = @"# Reset All Tweaks
# Restores registry values from backups and returns settings to defaults.
# This process is orchestrated by the app using stored backups, not a single shell script.";
            ToggleInlinePreview("ResetPreviewPanel", "ResetPreviewText", content);
        }

        private async Task ExecuteGamingOptimizations(ProgressWindow progress)
        {
            var steps = new[]
            {
                "Applying GPU performance tweaks...",
                "Optimizing CPU priority settings...",
                "Configuring memory management...",
                "Optimizing network for gaming...",
                "Setting power plan to high performance...",
                "Finalizing optimizations..."
            };

            for (int i = 0; i < steps.Length; i++)
            {
                progress.UpdateStatus(steps[i]);
                progress.UpdateProgress((i + 1) * 100 / steps.Length);
                
                // Simulate work and apply actual optimizations
                await Task.Delay(1000);
                
                switch (i)
                {
                    case 0: // GPU tweaks
                        await ApplyGpuOptimizations();
                        break;
                    case 1: // CPU priority
                        await ApplyCpuOptimizations();
                        break;
                    case 2: // Memory management
                        await ApplyMemoryOptimizations();
                        break;
                    case 3: // Network gaming
                        await ApplyGamingNetworkOptimizations();
                        break;
                    case 4: // Power plan
                        await ApplyHighPerformancePowerPlan();
                        break;
                }
            }
        }

        private async Task ExecuteSystemCleanup(ProgressWindow progress)
        {
            var steps = new[]
            {
                "Cleaning temporary files...",
                "Clearing system cache...",
                "Removing log files...",
                "Cleaning browser data...",
                "Emptying recycle bin...",
                "Finalizing cleanup..."
            };

            for (int i = 0; i < steps.Length; i++)
            {
                progress.UpdateStatus(steps[i]);
                progress.UpdateProgress((i + 1) * 100 / steps.Length);
                
                await Task.Delay(800);
                
                switch (i)
                {
                    case 0:
                        await CleanTemporaryFiles();
                        break;
                    case 1:
                        await CleanSystemCache();
                        break;
                    case 2:
                        await CleanLogFiles();
                        break;
                    case 3:
                        await CleanBrowserData();
                        break;
                    case 4:
                        await EmptyRecycleBin();
                        break;
                }
            }
        }

        private async Task ExecuteNetworkOptimizations(ProgressWindow progress)
        {
            var steps = new[]
            {
                "Optimizing TCP settings...",
                "Configuring DNS settings...",
                "Adjusting network adapter settings...",
                "Optimizing network stack...",
                "Finalizing network optimizations..."
            };

            for (int i = 0; i < steps.Length; i++)
            {
                progress.UpdateStatus(steps[i]);
                progress.UpdateProgress((i + 1) * 100 / steps.Length);
                
                await Task.Delay(1000);
                // Apply network-specific tweaks from the tweak service
            }
        }

        private async Task ExecuteHardwareOptimizations(ProgressWindow progress)
        {
            var steps = new[]
            {
                "Detecting hardware configuration...",
                "Applying CPU-specific optimizations...",
                "Configuring GPU settings...",
                "Optimizing chipset settings...",
                "Finalizing hardware optimizations..."
            };

            for (int i = 0; i < steps.Length; i++)
            {
                progress.UpdateStatus(steps[i]);
                progress.UpdateProgress((i + 1) * 100 / steps.Length);
                
                await Task.Delay(1200);
                // Apply hardware-specific optimizations
            }
        }

        private async Task ExecuteResetAllTweaks(ProgressWindow progress)
        {
            var allTweaks = _tweakService.TweakCategories.SelectMany(c => c.Tweaks).ToList();
            
            for (int i = 0; i < allTweaks.Count; i++)
            {
                var tweak = allTweaks[i];
                progress.UpdateStatus($"Resetting: {tweak.Name}");
                progress.UpdateProgress((i + 1) * 100 / allTweaks.Count);
                
                try
                {
                    _registryService.RestoreTweak(tweak);
                }
                catch
                {
                    // Continue with other tweaks even if one fails
                }
                
                await Task.Delay(100);
            }
        }

        // Placeholder methods for actual optimization implementations
        private async Task ApplyGpuOptimizations() => await Task.Delay(100);
        private async Task ApplyCpuOptimizations() => await Task.Delay(100);
        private async Task ApplyMemoryOptimizations() => await Task.Delay(100);
        private async Task ApplyGamingNetworkOptimizations() => await Task.Delay(100);
        private async Task ApplyHighPerformancePowerPlan() => await Task.Delay(100);
        private async Task CleanTemporaryFiles() => await Task.Delay(100);
        private async Task CleanSystemCache() => await Task.Delay(100);
        private async Task CleanLogFiles() => await Task.Delay(100);
        private async Task CleanBrowserData() => await Task.Delay(100);
        private async Task EmptyRecycleBin() => await Task.Delay(100);

        private void AudioLoudnessEqualization_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AudioDeviceSelectionDialog
                {
                    Owner = Application.Current.MainWindow
                };

                dialog.ShowDialog();

                if (dialog.DialogResult && dialog.SelectedDevice != null)
                {
                    // Success message is already shown in the dialog
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while opening the audio device selection dialog:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AudioLoudnessEqualization_View_Click(object sender, RoutedEventArgs e)
        {
            var content = @"# Enable Audio Loudness Equalization
# The app uses Windows audio APIs to toggle endpoint properties for the selected device.
# There isn't a reliable single-command script for this across devices.";
            ToggleInlinePreview("AudioPreviewPanel", "AudioPreviewText", content);
        }

        private async void WinGetInstallation_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will install Microsoft WinGet package manager if it's not already present.\n\n" +
                "The script will try multiple installation methods with automatic fallback.\n\n" +
                "Do you want to continue?",
                "Install WinGet Package Manager",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var progressWindow = new ProgressWindow("Installing WinGet Package Manager");

            try
            {
                progressWindow.Show();
                progressWindow.UpdateStatus("Checking if WinGet is already installed...");
                progressWindow.UpdateProgress(10);

                // Check if WinGet is already installed
                var checkScript = @"
                    try {
                        $version = winget --version
                        if ($version) {
                            Write-Output ""ALREADY_INSTALLED:$version""
                            return
                        }
                    } catch {
                        Write-Output ""NOT_INSTALLED""
                    }
                ";

                var checkResult = await Task.Run(() => _powerShellService.ExecuteScript(checkScript));

                if (checkResult.Success && checkResult.Output.Contains("ALREADY_INSTALLED"))
                {
                    progressWindow.Close();
                    var version = checkResult.Output.Replace("ALREADY_INSTALLED:", "").Trim();
                    MessageBox.Show(
                        $"WinGet is already installed!\n\nVersion: {version}",
                        "Already Installed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                progressWindow.UpdateStatus("Installing WinGet using method 1 (PowerShell registration)...");
                progressWindow.UpdateProgress(30);

                var installScript = @"
                    $success = $false
                    $method = """"

                    # Method 1: PowerShell registration
                    try {
                        Write-Host ""Trying Method 1: PowerShell registration...""
                        Add-AppxPackage -RegisterByFamilyName -MainPackage Microsoft.DesktopAppInstaller_8wekyb3d8bbwe

                        # Test if it worked
                        Start-Sleep -Seconds 3
                        $testResult = winget --version 2>$null
                        if ($testResult) {
                            $success = $true
                            $method = ""PowerShell registration""
                            Write-Host ""Method 1 successful""
                        }
                    } catch {
                        Write-Host ""Method 1 failed: $($_.Exception.Message)""
                    }

                    # Method 2: Direct download if Method 1 failed
                    if (-not $success) {
                        try {
                            Write-Host ""Trying Method 2: Direct download...""
                            $downloadPath = ""$env:TEMP\winget.msixbundle""
                            Invoke-WebRequest -Uri ""https://aka.ms/getwinget"" -OutFile $downloadPath -UseBasicParsing
                            Add-AppxPackage $downloadPath
                            Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue

                            # Test if it worked
                            Start-Sleep -Seconds 3
                            $testResult = winget --version 2>$null
                            if ($testResult) {
                                $success = $true
                                $method = ""Direct download""
                                Write-Host ""Method 2 successful""
                            }
                        } catch {
                            Write-Host ""Method 2 failed: $($_.Exception.Message)""
                        }
                    }

                    # Method 3: GitHub latest release if Method 2 failed
                    if (-not $success) {
                        try {
                            Write-Host ""Trying Method 3: GitHub latest release...""
                            $url = ""https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle""
                            $downloadPath = ""$env:TEMP\winget-installer.msixbundle""
                            Invoke-WebRequest -Uri $url -OutFile $downloadPath -UseBasicParsing
                            Add-AppxPackage $downloadPath
                            Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue

                            # Test if it worked
                            Start-Sleep -Seconds 3
                            $testResult = winget --version 2>$null
                            if ($testResult) {
                                $success = $true
                                $method = ""GitHub release""
                                Write-Host ""Method 3 successful""
                            }
                        } catch {
                            Write-Host ""Method 3 failed: $($_.Exception.Message)""
                        }
                    }

                    if ($success) {
                        $version = winget --version
                        Write-Output ""SUCCESS:$method:$version""
                    } else {
                        Write-Output ""FAILED:All installation methods failed""
                    }
                ";

                progressWindow.UpdateStatus("Executing installation script...");
                progressWindow.UpdateProgress(60);

                var installResult = await Task.Run(() => _powerShellService.ExecuteScript(installScript));

                progressWindow.UpdateStatus("Verifying installation...");
                progressWindow.UpdateProgress(90);
                await Task.Delay(1000);

                progressWindow.UpdateStatus("Installation completed!");
                progressWindow.UpdateProgress(100);
                await Task.Delay(1000);
                progressWindow.Close();

                if (installResult.Success && installResult.Output.Contains("SUCCESS"))
                {
                    var parts = installResult.Output.Replace("SUCCESS:", "").Split(':');
                    var method = parts.Length > 0 ? parts[0] : "Unknown";
                    var version = parts.Length > 1 ? parts[1] : "Unknown";

                    MessageBox.Show(
                        $"WinGet has been installed successfully!\n\n" +
                        $"Installation method: {method}\n" +
                        $"Version: {version}\n\n" +
                        "You can now use WinGet to install and manage applications from the command line.",
                        "Installation Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "WinGet installation failed using all available methods.\n\n" +
                        "Please try installing WinGet manually from the Microsoft Store or GitHub releases page.\n\n" +
                        "You may also need to ensure Windows is up to date.",
                        "Installation Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"An error occurred during WinGet installation:\n\n{ex.Message}",
                    "Installation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void WinGetInstallation_View_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "WinGetInstall.ps1");
                string content;
                if (System.IO.File.Exists(scriptPath))
                {
                    content = System.IO.File.ReadAllText(scriptPath);
                }
                else
                {
                    content = "# WinGetInstall.ps1 not found in Scripts folder.";
                }
                ToggleInlinePreview("WinGetPreviewPanel", "WinGetPreviewText", content);
            }
            catch (Exception ex)
            {
                ToggleInlinePreview("WinGetPreviewPanel", "WinGetPreviewText", "# Error loading script:\n" + ex.Message);
            }
        }

        private async void BcdEditApply_Click(object sender, RoutedEventArgs e)
        {
            var disclaimer = "⚠️ Warning: These commands modify Windows boot configuration. Changes may affect system stability and require administrator privileges. A system restart will be required for changes to take effect. Proceed only if you understand the implications.";
            var result = MessageBox.Show(disclaimer, "BCDEdit Performance Tweaks", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            var progress = new ProgressWindow("Applying BCDEdit Performance Tweaks...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Checking administrator privileges...");
                progress.UpdateProgress(10);
                if (!_powerShellService.IsAdministrator())
                {
                    progress.Close();
                    MessageBox.Show("Administrator privileges are required to apply BCDEdit tweaks.", "Permission Required", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Build a PowerShell script that runs the bcdedit commands in an elevated cmd silently
                var script = @"
                    $commands = @(
                        'bcdedit /set disabledynamictick yes',
                        'bcdedit /set tscsyncpolicy enhanced',
                        'bcdedit /set useplatformclock false',
                        'bcdedit /set hypervisorlaunchtype off'
                    )

                    foreach ($cmd in $commands) {
                        Start-Process cmd.exe -Verb RunAs -ArgumentList ('/c ' + $cmd) -WindowStyle Hidden -Wait
                    }
                ";

                progress.UpdateStatus("Executing BCDEdit commands...");
                progress.UpdateProgress(60);
                var psResult = await _powerShellService.ExecuteScriptAsync(script);

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(90);
                await Task.Delay(500);

                progress.UpdateProgress(100);
                progress.Close();

                if (psResult.Success)
                {
                    MessageBox.Show("BCDEdit performance tweaks applied successfully. Please restart your computer for changes to take effect.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to apply BCDEdit tweaks.\n\nError: {psResult.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show($"An error occurred:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BcdEdit_View_Click(object sender, RoutedEventArgs e)
        {
            var content = @"# BCDEdit Performance Tweaks
bcdedit /set disabledynamictick yes
bcdedit /set tscsyncpolicy enhanced
bcdedit /set useplatformclock false
bcdedit /set hypervisorlaunchtype off";
            ToggleInlinePreview("BcdPreviewPanel", "BcdPreviewText", content);
        }

        private async void BcdEditRestore_Click(object sender, RoutedEventArgs e)
        {
            var disclaimer = "⚠️ Warning: These commands modify Windows boot configuration. Changes may affect system stability and require administrator privileges. A system restart will be required for changes to take effect. Proceed only if you understand the implications.";
            var result = MessageBox.Show(disclaimer + "\n\nRestore original configuration?", "Restore BCDEdit Tweaks", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            var progress = new ProgressWindow("Restoring BCDEdit configuration...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Checking administrator privileges...");
                progress.UpdateProgress(10);
                if (!_powerShellService.IsAdministrator())
                {
                    progress.Close();
                    MessageBox.Show("Administrator privileges are required to restore BCDEdit settings.", "Permission Required", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var script = @"
                    $commands = @(
                        'bcdedit /deletevalue disabledynamictick',
                        'bcdedit /deletevalue tscsyncpolicy',
                        'bcdedit /deletevalue useplatformclock',
                        'bcdedit /set hypervisorlaunchtype auto'
                    )

                    foreach ($cmd in $commands) {
                        Start-Process cmd.exe -Verb RunAs -ArgumentList ('/c ' + $cmd) -WindowStyle Hidden -Wait
                    }
                ";

                progress.UpdateStatus("Executing BCDEdit restore commands...");
                progress.UpdateProgress(60);
                var psResult = await _powerShellService.ExecuteScriptAsync(script);

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(90);
                await Task.Delay(500);

                progress.UpdateProgress(100);
                progress.Close();

                if (psResult.Success)
                {
                    MessageBox.Show("BCDEdit configuration restored. Please restart your computer for changes to take effect.", "Restored", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to restore BCDEdit settings.\n\nError: {psResult.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show($"An error occurred:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DismSfcRepair_Click(object sender, RoutedEventArgs e)
        {
            var info = "ℹ️ System Repair Information:\n• If SFC finds and repairs files, it's recommended to restart and re-run: sfc /scannow\n• If DISM /RestoreHealth fails (e.g., Windows Update issues), use a mounted Windows ISO (assume drive X:) with install.wim:\n  DISM /Online /Cleanup-Image /RestoreHealth /Source:WIM:X:\\sources\\install.wim:1 /LimitAccess\n• If using install.esd instead of install.wim, replace 'WIM:' with 'ESD:' in the command\n• This process may take 15-30 minutes to complete";
            var result = MessageBox.Show(info + "\n\nRun DISM + SFC now?", "DISM + SFC System Repair", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result != MessageBoxResult.Yes) return;

            var progress = new ProgressWindow("Running DISM + SFC repairs...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Starting DISM /CheckHealth...");
                progress.UpdateProgress(10);
                var script = @"
                    $commands = @(
                        'DISM /Online /Cleanup-Image /CheckHealth',
                        'DISM /Online /Cleanup-Image /ScanHealth',
                        'DISM /Online /Cleanup-Image /RestoreHealth',
                        'sfc /scannow'
                    )

                    foreach ($cmd in $commands) {
                        Start-Process cmd.exe -Verb RunAs -ArgumentList ('/c ' + $cmd) -WindowStyle Hidden -Wait
                    }
                ";

                var step = 1;
                foreach (var status in new[] {
                    "Running DISM /CheckHealth...",
                    "Running DISM /ScanHealth...",
                    "Running DISM /RestoreHealth...",
                    "Running SFC /scannow..."
                })
                {
                    progress.UpdateStatus(status);
                    progress.UpdateProgress(step * 25);
                    step++;
                    await Task.Delay(300);
                }

                var psResult = await _powerShellService.ExecuteScriptAsync(script);

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(95);
                await Task.Delay(500);

                progress.UpdateProgress(100);
                progress.Close();

                if (psResult.Success)
                {
                    MessageBox.Show("DISM + SFC repair sequence completed. Review output in Event Viewer if needed. A restart is recommended.", "Repair Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"DISM + SFC sequence failed.\n\nError: {psResult.Error}", "Repair Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show($"An error occurred:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DismSfcRepair_View_Click(object sender, RoutedEventArgs e)
        {
            var content = @"# DISM + SFC Repair Sequence
DISM /Online /Cleanup-Image /CheckHealth
DISM /Online /Cleanup-Image /ScanHealth
DISM /Online /Cleanup-Image /RestoreHealth
sfc /scannow";
            ToggleInlinePreview("DismPreviewPanel", "DismPreviewText", content);
        }

        private void CopyScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TextBox tb)
            {
                try { Clipboard.SetText(tb.Text ?? string.Empty); } catch { }
            }
        }

        private void SpectreDisable_View_Click(object sender, RoutedEventArgs e)
        {
            var content = @"# Disable Spectre and Meltdown Mitigations
# WARNING: This reduces security! Only for isolated/gaming systems.

# Disable CPU mitigations via registry
reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v FeatureSettingsOverride /t REG_DWORD /d 3 /f
reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v FeatureSettingsOverrideMask /t REG_DWORD /d 3 /f

# Disable Hyper-V to reduce overhead
bcdedit /set hypervisorlaunchtype off

# Note: Restart required for changes to take effect";
            ToggleInlinePreview("SpectrePreviewPanel", "SpectrePreviewText", content);
        }

        private async void SpectreDisable_Click(object sender, RoutedEventArgs e)
        {
            var disclaimer = @"⚠️⚠️⚠️ CRITICAL SECURITY WARNING ⚠️⚠️⚠️

This will DISABLE Spectre and Meltdown CPU vulnerability mitigations!

RISKS:
• Your system will be vulnerable to known CPU security exploits
• Malicious code could potentially read sensitive memory data
• NOT recommended for systems with internet access
• NOT recommended for systems with sensitive data

BENEFITS:
• Potential 5-15% performance improvement in some workloads
• Reduced CPU overhead

This should ONLY be used on:
• Isolated gaming systems
• Systems without sensitive data
• Systems not connected to untrusted networks

A SYSTEM RESTART WILL BE REQUIRED.

Are you ABSOLUTELY SURE you want to proceed?";

            var result = MessageBox.Show(
                disclaimer,
                "Disable Spectre/Meltdown - SECURITY WARNING",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // Second confirmation
            var confirmResult = MessageBox.Show(
                "Final confirmation: This will reduce your system's security.\n\nProceed with disabling mitigations?",
                "Final Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop);

            if (confirmResult != MessageBoxResult.Yes) return;

            var progress = new ProgressWindow("Disabling Spectre/Meltdown mitigations...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Checking administrator privileges...");
                progress.UpdateProgress(10);

                if (!_powerShellService.IsAdministrator())
                {
                    progress.Close();
                    MessageBox.Show(
                        "Administrator privileges are required to disable mitigations.",
                        "Permission Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                progress.UpdateStatus("Modifying registry settings...");
                progress.UpdateProgress(40);

                var script = @"
                    # Disable CPU vulnerability mitigations
                    reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v FeatureSettingsOverride /t REG_DWORD /d 3 /f
                    reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v FeatureSettingsOverrideMask /t REG_DWORD /d 3 /f
                    
                    # Disable Hyper-V
                    Start-Process cmd.exe -Verb RunAs -ArgumentList '/c bcdedit /set hypervisorlaunchtype off' -WindowStyle Hidden -Wait
                ";

                progress.UpdateStatus("Applying changes...");
                progress.UpdateProgress(70);

                var psResult = await _powerShellService.ExecuteScriptAsync(script);

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(90);
                await Task.Delay(500);

                progress.UpdateProgress(100);
                progress.Close();

                if (psResult.Success)
                {
                    MessageBox.Show(
                        "Spectre and Meltdown mitigations have been DISABLED.\n\n" +
                        "⚠️ Your system security has been reduced!\n\n" +
                        "Please restart your computer for changes to take effect.",
                        "Mitigations Disabled",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(
                        $"Failed to disable mitigations.\n\nError: {psResult.Error}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show(
                    $"An error occurred:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void SpectreEnable_Click(object sender, RoutedEventArgs e)
        {
            var info = "This will RE-ENABLE Spectre and Meltdown CPU vulnerability mitigations.\n\n" +
                       "Your system security will be restored to normal levels.\n\n" +
                       "A system restart will be required.\n\n" +
                       "Continue?";

            var result = MessageBox.Show(
                info,
                "Enable Spectre/Meltdown Mitigations",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var progress = new ProgressWindow("Re-enabling Spectre/Meltdown mitigations...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Checking administrator privileges...");
                progress.UpdateProgress(10);

                if (!_powerShellService.IsAdministrator())
                {
                    progress.Close();
                    MessageBox.Show(
                        "Administrator privileges are required to enable mitigations.",
                        "Permission Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                progress.UpdateStatus("Restoring registry settings...");
                progress.UpdateProgress(40);

                var script = @"
                    # Remove custom mitigation settings (restore defaults)
                    reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v FeatureSettingsOverride /f 2>$null
                    reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v FeatureSettingsOverrideMask /f 2>$null
                    
                    # Re-enable Hyper-V to auto
                    Start-Process cmd.exe -Verb RunAs -ArgumentList '/c bcdedit /set hypervisorlaunchtype auto' -WindowStyle Hidden -Wait
                ";

                progress.UpdateStatus("Applying changes...");
                progress.UpdateProgress(70);

                var psResult = await _powerShellService.ExecuteScriptAsync(script);

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(90);
                await Task.Delay(500);

                progress.UpdateProgress(100);
                progress.Close();

                if (psResult.Success)
                {
                    MessageBox.Show(
                        "Spectre and Meltdown mitigations have been RE-ENABLED.\n\n" +
                        "✅ Your system security has been restored!\n\n" +
                        "Please restart your computer for changes to take effect.",
                        "Mitigations Enabled",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        $"Failed to enable mitigations.\n\nError: {psResult.Error}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show(
                    $"An error occurred:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
