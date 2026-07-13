using System;
using System.Collections.ObjectModel;
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
        private readonly PowerShellService _powerShellService;
        private readonly UserDataService _userDataService;
        private readonly ObservableCollection<CustomScript> _customScripts = new();
        private readonly Dictionary<string, CancellationTokenSource> _runningScripts = new();

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

        private void ImportScriptsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Scripts (*.ps1;*.cmd;*.bat)|*.ps1;*.cmd;*.bat",
                Multiselect = true
            };
            if (dialog.ShowDialog() != true) return;

            foreach (var path in dialog.FileNames)
            {
                _customScripts.Add(new CustomScript
                {
                    Name = System.IO.Path.GetFileNameWithoutExtension(path),
                    Language = System.IO.Path.GetExtension(path).Equals(".ps1", StringComparison.OrdinalIgnoreCase)
                        ? ScriptLanguage.PowerShell
                        : ScriptLanguage.Cmd,
                    Content = System.IO.File.ReadAllText(path)
                });
            }
            _userDataService.SaveCustomScripts(_customScripts);
            RefreshCustomScriptCards();
        }

        private void ExportScript(CustomScript script)
        {
            var extension = script.Language == ScriptLanguage.PowerShell ? ".ps1" : ".cmd";
            var dialog = new SaveFileDialog
            {
                Filter = script.Language == ScriptLanguage.PowerShell ? "PowerShell (*.ps1)|*.ps1" : "CMD (*.cmd)|*.cmd",
                FileName = script.Name + extension,
                DefaultExt = extension
            };
            if (dialog.ShowDialog() == true) System.IO.File.WriteAllText(dialog.FileName, script.Content);
        }

        private void NewScriptButton_Click(object sender, RoutedEventArgs e) =>
            EditCustomScript(new CustomScript(), isNew: true);

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
            header.Children.Add(new TextBlock { Text = script.Language == ScriptLanguage.PowerShell ? "\uE8B7" : "\uE8A5", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"), FontSize = 22, Margin = new Thickness(0,0,12,0), Foreground = (System.Windows.Media.Brush)FindResource("IconBrush") });
            header.Children.Add(new TextBlock { Text = script.Name, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = (System.Windows.Media.Brush)FindResource("SystemControlForegroundBaseHighBrush") });
            infoPanel.Children.Add(header);
            infoPanel.Children.Add(new TextBlock { Text = $"{(script.Language == ScriptLanguage.PowerShell ? "PowerShell" : "CMD")} • {(script.RequiresAdministrator ? "Administrator" : "Current user")}", Style = (Style)FindResource("DescriptionTextStyle"), Margin = new Thickness(36,0,0,12) });
            Grid.SetRow(infoPanel,0); Grid.SetColumn(infoPanel,0); grid.Children.Add(infoPanel);

            var actionsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var execBtn = new Button { Style = GetStyleOrDefault("ExecuteButtonStyle"), Margin = new Thickness(0,0,8,0) };
            execBtn.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "\uE768", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"), FontSize = 13, Margin = new Thickness(0,0,8,0), Foreground = Brushes.White }, new TextBlock { Text = script.Language == ScriptLanguage.PowerShell ? "Run" : "Execute" } } };
            var cancelBtn = new Button { Content = "Stop", Style = GetStyleOrDefault("DangerButtonStyle"), Margin = new Thickness(0,0,8,0), Visibility = Visibility.Collapsed };
            var exportBtn = new Button { Content = "Export", Style = GetStyleOrDefault("SecondaryButtonStyle"), Margin = new Thickness(0,0,8,0) };
            var editBtn = new Button { Style = GetStyleOrDefault("SecondaryButtonStyle"), Margin = new Thickness(0,0,8,0) };
            editBtn.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "\uE70F", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"), FontSize = 13, Margin = new Thickness(0,0,8,0) }, new TextBlock { Text = "Edit" } } };
            var deleteBtn = new Button { Style = GetStyleOrDefault("DangerButtonStyle") };
            deleteBtn.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "\uE74D", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"), FontSize = 13, Margin = new Thickness(0,0,8,0) }, new TextBlock { Text = "Delete" } } };
            
            actionsPanel.Children.Add(execBtn); actionsPanel.Children.Add(cancelBtn); actionsPanel.Children.Add(exportBtn); actionsPanel.Children.Add(editBtn); actionsPanel.Children.Add(deleteBtn);
            Grid.SetColumn(actionsPanel,1); Grid.SetRow(actionsPanel,0); grid.Children.Add(actionsPanel);

            card.Child = grid;
            execBtn.Click += async (_, __) =>
            {
                if (_runningScripts.ContainsKey(script.Id)) return;
                using var cancellation = new CancellationTokenSource();
                _runningScripts[script.Id] = cancellation;
                execBtn.IsEnabled = false;
                cancelBtn.Visibility = script.RequiresAdministrator ? Visibility.Collapsed : Visibility.Visible;
                try { await ExecuteCustomScript(script, cancellation.Token); }
                finally
                {
                    _runningScripts.Remove(script.Id);
                    execBtn.IsEnabled = true;
                    cancelBtn.Visibility = Visibility.Collapsed;
                }
            };
            cancelBtn.Click += (_, __) => cancellationFor(script.Id)?.Cancel();
            exportBtn.Click += (_, __) => ExportScript(script);
            editBtn.Click += (_, __) => EditCustomScript(script);
            deleteBtn.Click += (_, __) => DeleteCustomScript(script);
            return card;

            CancellationTokenSource? cancellationFor(string id) =>
                _runningScripts.TryGetValue(id, out var value) ? value : null;
        }

        private Style GetStyleOrDefault(string key) => (Style)FindResource(key);

        private async Task ExecuteCustomScript(CustomScript script, CancellationToken cancellationToken)
        {
            if (script.RequiresAdministrator && MessageBox.Show(
                    $"Run '{script.Name}' with administrator privileges?",
                    "Administrator Script",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            var result = script.Language == ScriptLanguage.PowerShell
                ? await _powerShellService.ExecuteScriptAsync(
                    script.Content,
                    script.RequiresAdministrator,
                    TimeSpan.FromMinutes(15),
                    cancellationToken)
                : await ExecuteCmdScript(script, cancellationToken);
            var details = string.Join("\n", new[] { result.Output.Trim(), result.Error.Trim() }.Where(s => s.Length > 0));
            var summary = result.Success
                ? $"Script completed in {result.Duration.TotalSeconds:F1}s."
                : $"Script failed (exit {result.ExitCode}) after {result.Duration.TotalSeconds:F1}s.";

            MessageBox.Show(
                $"{summary}\n\n{details}".Trim(),
                script.Name,
                MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
        }

        private async Task<PowerShellResult> ExecuteCmdScript(CustomScript script, CancellationToken cancellationToken)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"TweakHub-{script.Id}.cmd");
            try
            {
                await System.IO.File.WriteAllTextAsync(path, script.Content, cancellationToken);
                var escapedPath = path.Replace("'", "''");
                return await _powerShellService.ExecuteScriptAsync(
                    $"& cmd.exe /d /c '{escapedPath}'\nexit $LASTEXITCODE",
                    script.RequiresAdministrator,
                    TimeSpan.FromMinutes(15),
                    cancellationToken);
            }
            finally
            {
                try { System.IO.File.Delete(path); } catch { }
            }
        }

        private void EditCustomScript(CustomScript script, bool isNew = false)
        {
            var dialog = new Window
            {
                Title = isNew ? "Create Custom Script" : "Edit Custom Script",
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
            var cmdRadio = new RadioButton { Content = "CMD Batch", IsChecked = script.Language == ScriptLanguage.Cmd, Margin = new Thickness(0,0,20,0) };
            var adminCheck = new CheckBox { Content = "Requires administrator", IsChecked = script.RequiresAdministrator, VerticalAlignment = VerticalAlignment.Center };
            langPanel.Children.Add(psRadio);
            langPanel.Children.Add(cmdRadio);
            langPanel.Children.Add(adminCheck);
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
            var saveBtn = new Button { Content = isNew ? "Create" : "Save", Style = GetStyleOrDefault("ExecuteButtonStyle") };
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
                    MessageBox.Show("Name required.", "Invalid Script", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                script.Name = name;
                script.Language = psRadio.IsChecked == true ? ScriptLanguage.PowerShell : ScriptLanguage.Cmd;
                script.Content = contentBox.Text;
                script.RequiresAdministrator = adminCheck.IsChecked == true;
                if (isNew) _customScripts.Add(script);
                _userDataService.SaveCustomScripts(_customScripts);
                dialog.Close();
                RefreshCustomScriptCards();
                StyledMessageDialog.ShowOk(
                    Window.GetWindow(this),
                    isNew ? "Script Created" : "Script Updated",
                    $"Script '{name}' was {(isNew ? "created" : "updated")} successfully.");
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
            var customCards = ScriptsHostPanel.Children.OfType<Border>()
                .Where(card => Equals(card.Tag, "CustomScript"))
                .ToList();
            foreach (var card in customCards) ScriptsHostPanel.Children.Remove(card);
            foreach (var script in _customScripts)
            {
                var card = CreateCustomScriptCard(script);
                card.Tag = "CustomScript";
                ScriptsHostPanel.Children.Add(card);
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

        private async void DismSfcRepair_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "DISM and SFC can take 15–30 minutes. Run the verified repair sequence now?",
                    "DISM + SFC System Repair",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information) != MessageBoxResult.Yes)
                return;

            var progress = new ProgressWindow("Running DISM + SFC repairs...");
            progress.Show();
            progress.UpdateStatus("Running system repair commands. Do not close TweakHub...");
            progress.UpdateProgress(10);

            try
            {
                var script = @"
                    $commands = @(
                        @('DISM.exe', @('/Online', '/Cleanup-Image', '/CheckHealth')),
                        @('DISM.exe', @('/Online', '/Cleanup-Image', '/ScanHealth')),
                        @('DISM.exe', @('/Online', '/Cleanup-Image', '/RestoreHealth')),
                        @('sfc.exe', @('/scannow'))
                    )
                    foreach ($command in $commands) {
                        $executable = $command[0]
                        $arguments = $command[1]
                        & $executable @arguments
                        if ($LASTEXITCODE -ne 0) { throw ""$executable failed with exit code $LASTEXITCODE"" }
                    }
                ";

                var result = await _powerShellService.ExecuteScriptAsync(
                    script,
                    requireAdministrator: true,
                    timeout: TimeSpan.FromMinutes(90));
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TweakHub",
                    "dism-sfc-last.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                await System.IO.File.WriteAllTextAsync(logPath, result.Output + Environment.NewLine + result.Error);
                progress.UpdateProgress(100);

                MessageBox.Show(
                    result.Success
                        ? $"Repair completed successfully. A restart is recommended.\n\nLog: {logPath}"
                        : $"Repair failed with exit code {result.ExitCode}.\n\n{result.Error}\nLog: {logPath}",
                    result.Success ? "Repair Complete" : "Repair Failed",
                    MessageBoxButton.OK,
                    result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Repair Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                progress.Close();
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

    }
}
