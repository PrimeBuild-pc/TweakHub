using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TweakHub.Services;
using TweakHub.Models;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views
{
    public partial class AutomatedScriptsPage : Page
    {
        private readonly PowerShellService _powerShellService;
        private readonly UserDataService _userDataService;
        private readonly ObservableCollection<CustomScript> _customScripts = new();

        public AutomatedScriptsPage()
        {
            InitializeComponent();
            _powerShellService = PowerShellService.Instance;
            _userDataService = UserDataService.Instance;
            LoadCustomScripts();
            Loaded += (_, _) => RefreshCustomScriptCards();
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


        private Style GetStyleOrDefault(string key)
        {
            var style = TryFindResource(key) as Style ?? Application.Current.TryFindResource(key) as Style;
            return style ?? (Application.Current.TryFindResource("ModernButtonStyle") as Style ?? new Style(typeof(Button)));
        }

        private async Task ExecuteCustomScript(CustomScript script)
        {
            try
            {
                var result = script.Language == ScriptLanguage.PowerShell
                    ? await _powerShellService.ExecuteScriptAsync(script.Content)
                    : await ExecuteCmdScript(script);
                var details = string.Join("\n", new[] { result.Output.Trim(), result.Error.Trim() }.Where(s => s.Length > 0));

                MessageBox.Show(
                    result.Success ? $"Script completed successfully.\n\n{details}".Trim() : $"Script failed (exit {result.ExitCode}).\n\n{details}".Trim(),
                    script.Name,
                    MessageBoxButton.OK,
                    result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing script: {ex.Message}");
            }
        }

        private static async Task<PowerShellResult> ExecuteCmdScript(CustomScript script)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"TweakHub-{script.Id}.cmd");
            try
            {
                await System.IO.File.WriteAllTextAsync(path, script.Content);
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                process.StartInfo.ArgumentList.Add("/d");
                process.StartInfo.ArgumentList.Add("/c");
                process.StartInfo.ArgumentList.Add(path);
                process.Start();
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                return new PowerShellResult
                {
                    Success = process.ExitCode == 0,
                    ExitCode = process.ExitCode,
                    Output = await output,
                    Error = await error
                };
            }
            finally
            {
                try { System.IO.File.Delete(path); } catch { }
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

        private async void WinGetInstallation_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "Install Microsoft WinGet if it is not already available?",
                    "Install WinGet Package Manager",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "WinGetInstall.ps1");
            if (!System.IO.File.Exists(scriptPath))
            {
                MessageBox.Show("WinGetInstall.ps1 was not found.", "Installation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var progress = new ProgressWindow("Installing WinGet Package Manager");
            progress.Show();

            try
            {
                progress.UpdateStatus("Running the WinGet installation script...");
                var result = await _powerShellService.ExecuteScriptAsync(System.IO.File.ReadAllText(scriptPath));
                var success = result.Success &&
                              (result.Output.Contains("SUCCESS:") || result.Output.Contains("ALREADY_INSTALLED:"));

                MessageBox.Show(
                    success ? result.Output.Trim() : $"WinGet installation failed.\n\n{result.Error}\n{result.Output}".Trim(),
                    success ? "Installation Complete" : "Installation Failed",
                    MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Installation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                progress.Close();
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

        private void MouseTweaks_View_Click(object sender, RoutedEventArgs e)
        {
            var content = @"# 8K Mouse Tweaks
# Optimizes mouse polling and USB settings for reduced input latency

# BCDEdit timing tweaks
bcdedit /set useplatformtick yes
bcdedit /set disabledynamictick yes
bcdedit /set useplatformclock true

# Disable USB selective suspend
REG ADD ""HKLM\SYSTEM\CurrentControlSet\Services\USB"" /v DisableSelectiveSuspend /t REG_DWORD /d 1 /f

# USB XHCI idle settings
REG ADD ""HKLM\SYSTEM\CurrentControlSet\Services\USBXHCI\Parameters"" /v IdleInterval /t REG_DWORD /d 0 /f
REG ADD ""HKLM\SYSTEM\CurrentControlSet\Services\USBXHCI\Parameters"" /v IdleTimeout /t REG_DWORD /d 0 /f

# HID USB idle timeout
REG ADD ""HKLM\SYSTEM\CurrentControlSet\Services\HidUsb\Parameters"" /v IdleUsbSelectiveSuspendTimeout /t REG_DWORD /d 0 /f

# Note: Restart required for changes to take effect";
            ToggleInlinePreview("MouseTweaksPreviewPanel", "MouseTweaksPreviewText", content);
        }

        private async void MouseTweaksApply_Click(object sender, RoutedEventArgs e)
        {
            var disclaimer = "This will apply 8K Mouse Tweaks to optimize mouse polling and USB settings for reduced input latency.\n\nChanges include:\n• BCDEdit timing optimizations\n• USB selective suspend disabled\n• USB idle timeouts set to zero\n\nA system restart will be required for changes to take effect.\n\nContinue?";
            var result = MessageBox.Show(disclaimer, "8K Mouse Tweaks", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var progress = new ProgressWindow("Applying 8K Mouse Tweaks...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Checking administrator privileges...");
                progress.UpdateProgress(10);
                if (!_powerShellService.IsAdministrator())
                {
                    progress.Close();
                    MessageBox.Show("Administrator privileges are required to apply mouse tweaks.", "Permission Required", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var script = @"
                    $commands = @(
                        'bcdedit /set useplatformtick yes',
                        'bcdedit /set disabledynamictick yes',
                        'bcdedit /set useplatformclock true'
                    )

                    foreach ($cmd in $commands) {
                        Start-Process cmd.exe -Verb RunAs -ArgumentList ('/c ' + $cmd) -WindowStyle Hidden -Wait
                    }

                    # Registry tweaks
                    reg add ""HKLM\SYSTEM\CurrentControlSet\Services\USB"" /v DisableSelectiveSuspend /t REG_DWORD /d 1 /f
                    reg add ""HKLM\SYSTEM\CurrentControlSet\Services\USBXHCI\Parameters"" /v IdleInterval /t REG_DWORD /d 0 /f
                    reg add ""HKLM\SYSTEM\CurrentControlSet\Services\USBXHCI\Parameters"" /v IdleTimeout /t REG_DWORD /d 0 /f
                    reg add ""HKLM\SYSTEM\CurrentControlSet\Services\HidUsb\Parameters"" /v IdleUsbSelectiveSuspendTimeout /t REG_DWORD /d 0 /f
                ";

                progress.UpdateStatus("Applying BCDEdit commands...");
                progress.UpdateProgress(40);
                await Task.Delay(300);

                progress.UpdateStatus("Applying registry tweaks...");
                progress.UpdateProgress(70);
                var psResult = await _powerShellService.ExecuteScriptAsync(script);

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(90);
                await Task.Delay(500);

                progress.UpdateProgress(100);
                progress.Close();

                if (psResult.Success)
                {
                    MessageBox.Show("8K Mouse Tweaks applied successfully!\n\nPlease restart your computer for changes to take effect.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to apply mouse tweaks.\n\nError: {psResult.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show($"An error occurred:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MouseTweaksRestore_Click(object sender, RoutedEventArgs e)
        {
            var disclaimer = "This will restore the original mouse and USB settings.\n\nA system restart will be required for changes to take effect.\n\nContinue?";
            var result = MessageBox.Show(disclaimer, "Restore Mouse Tweaks", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var progress = new ProgressWindow("Restoring mouse settings...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Checking administrator privileges...");
                progress.UpdateProgress(10);
                if (!_powerShellService.IsAdministrator())
                {
                    progress.Close();
                    MessageBox.Show("Administrator privileges are required to restore mouse settings.", "Permission Required", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var script = @"
                    $commands = @(
                        'bcdedit /deletevalue useplatformtick',
                        'bcdedit /deletevalue disabledynamictick',
                        'bcdedit /deletevalue useplatformclock'
                    )

                    foreach ($cmd in $commands) {
                        Start-Process cmd.exe -Verb RunAs -ArgumentList ('/c ' + $cmd) -WindowStyle Hidden -Wait
                    }

                    # Remove registry tweaks
                    reg delete ""HKLM\SYSTEM\CurrentControlSet\Services\USB"" /v DisableSelectiveSuspend /f 2>$null
                    reg delete ""HKLM\SYSTEM\CurrentControlSet\Services\USBXHCI\Parameters"" /v IdleInterval /f 2>$null
                    reg delete ""HKLM\SYSTEM\CurrentControlSet\Services\USBXHCI\Parameters"" /v IdleTimeout /f 2>$null
                    reg delete ""HKLM\SYSTEM\CurrentControlSet\Services\HidUsb\Parameters"" /v IdleUsbSelectiveSuspendTimeout /f 2>$null
                ";

                progress.UpdateStatus("Restoring BCDEdit settings...");
                progress.UpdateProgress(40);
                await Task.Delay(300);

                progress.UpdateStatus("Removing registry tweaks...");
                progress.UpdateProgress(70);
                var psResult = await _powerShellService.ExecuteScriptAsync(script);

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(90);
                await Task.Delay(500);

                progress.UpdateProgress(100);
                progress.Close();

                if (psResult.Success)
                {
                    MessageBox.Show("Mouse settings restored successfully!\n\nPlease restart your computer for changes to take effect.", "Restored", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to restore mouse settings.\n\nError: {psResult.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show($"An error occurred:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
