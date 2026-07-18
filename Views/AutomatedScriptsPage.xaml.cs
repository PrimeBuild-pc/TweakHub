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
        private sealed record BuiltInScriptCard(
            string Id, string Icon, string Name, string Description, string Category, string ExecuteText);

        private readonly PowerShellService _powerShellService;
        private readonly UserDataService _userDataService;
        private readonly ObservableCollection<CustomScript> _customScripts = new();
        private readonly Dictionary<string, CancellationTokenSource> _runningScripts = new();
        private readonly BuiltInScriptCard[] _builtInScripts =
        [
            new("winget", "\uE7B8", "Install WinGet Package Manager",
                "Install Microsoft WinGet package manager if not present. Includes multiple installation methods with automatic fallback.",
                "Package Manager", "Execute"),
            new("dism", "\uE90F", "DISM + SFC System Repair",
                "Run comprehensive Windows system file integrity checks and repairs", "Repair", "Run Repair")
        ];

        public AutomatedScriptsPage()
        {
            InitializeComponent();
            _powerShellService = PowerShellService.Instance;
            _userDataService = UserDataService.Instance;
            BuiltInScriptsControl.ItemsSource = _builtInScripts;
            CustomScriptsControl.ItemsSource = _customScripts;
            LoadCustomScripts();
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

        private async void ExecuteCustomScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: CustomScript script, Tag: Button cancelButton } executeButton
                || _runningScripts.ContainsKey(script.Id)) return;

            using var cancellation = new CancellationTokenSource();
            _runningScripts[script.Id] = cancellation;
            executeButton.IsEnabled = false;
            cancelButton.Visibility = script.RequiresAdministrator ? Visibility.Collapsed : Visibility.Visible;
            try
            {
                await ExecuteCustomScript(script, cancellation.Token);
            }
            finally
            {
                _runningScripts.Remove(script.Id);
                executeButton.IsEnabled = true;
                cancelButton.Visibility = Visibility.Collapsed;
            }
        }

        private void CancelCustomScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: CustomScript script }
                && _runningScripts.TryGetValue(script.Id, out var cancellation)) cancellation.Cancel();
        }

        private void ExportCustomScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: CustomScript script }) ExportScript(script);
        }

        private void EditCustomScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: CustomScript script }) EditCustomScript(script);
        }

        private async void DeleteCustomScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: CustomScript script }) await DeleteCustomScript(script);
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
            var cancelBtn = new Button { Content = "Cancel", Style = GetStyleOrDefault("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
            var saveBtn = new Button { Content = isNew ? "Create" : "Save", Style = GetStyleOrDefault("ExecuteButtonStyle") };
            buttonsPanel.Children.Add(cancelBtn);
            buttonsPanel.Children.Add(saveBtn);
            Grid.SetRow(buttonsPanel, 3);
            grid.Children.Add(buttonsPanel);

            cancelBtn.Click += (_, __) => dialog.Close();
            saveBtn.Click += async (_, __) =>
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
                CustomScriptsControl.Items.Refresh();
                await AppDialog.ShowAsync(
                    Window.GetWindow(this),
                    isNew ? "Script Created" : "Script Updated",
                    $"Script '{name}' was {(isNew ? "created" : "updated")} successfully.");
            };

            dialog.Content = grid;
            dialog.ShowDialog();
        }

        private async Task DeleteCustomScript(CustomScript script)
        {
            var owner = Window.GetWindow(this);
            if (!await AppDialog.ConfirmAsync(
                    owner,
                    "Conferma Eliminazione",
                    $"Sei sicuro di voler eliminare lo script '{script.Name}'?\n\nQuesta operazione non può essere annullata.")) return;

            var toRemove = _customScripts.FirstOrDefault(x => x.Id == script.Id);
            if (toRemove != null)
            {
                _customScripts.Remove(toRemove);
                _userDataService.SaveCustomScripts(_customScripts);

                await AppDialog.ShowAsync(
                    owner,
                    "Script Eliminato",
                    $"Lo script '{script.Name}' è stato eliminato.");
            }
        }

        private void BuiltInScript_View_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: BuiltInScriptCard script, Tag: Border preview }
                || preview.Tag is not TextBox textBox) return;

            try
            {
                textBox.Text = script.Id == "winget"
                    ? LoadWinGetScript()
                    : @"# DISM + SFC Repair Sequence
DISM /Online /Cleanup-Image /CheckHealth
DISM /Online /Cleanup-Image /ScanHealth
DISM /Online /Cleanup-Image /RestoreHealth
sfc /scannow";
            }
            catch (Exception ex)
            {
                textBox.Text = "# Error loading script:\n" + ex.Message;
            }
            preview.Visibility = preview.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private static string LoadWinGetScript()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "WinGetInstall.ps1");
            return System.IO.File.Exists(path)
                ? System.IO.File.ReadAllText(path)
                : "# WinGetInstall.ps1 not found in Scripts folder.";
        }

        private void BuiltInScript_Execute_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: BuiltInScriptCard script }) return;
            if (script.Id == "winget") WinGetInstallation_Click(sender, e);
            else DismSfcRepair_Click(sender, e);
        }

        private void CopyBuiltInScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: TextBox textBox })
                try { Clipboard.SetText(textBox.Text ?? string.Empty); } catch { }
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


    }
}
