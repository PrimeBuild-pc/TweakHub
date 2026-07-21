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
            string Id, string Icon, string Name, string Description, string Category, string ExecuteText,
            bool RequiresAdministrator, int TimeoutMinutes, string Confirmation, string CompletionNote = "")
            : System.ComponentModel.INotifyPropertyChanged
        {
            private bool _isCompleted;
            private string _completionToolTip = string.Empty;
            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            public bool IsCompleted
            {
                get => _isCompleted;
                private set { _isCompleted = value; PropertyChanged?.Invoke(this, new(nameof(IsCompleted))); }
            }
            public string CompletionToolTip
            {
                get => _completionToolTip;
                private set { _completionToolTip = value; PropertyChanged?.Invoke(this, new(nameof(CompletionToolTip))); }
            }
            public void SetCompletion(DateTimeOffset? completedAt)
            {
                IsCompleted = completedAt.HasValue;
                CompletionToolTip = completedAt.HasValue ? $"Completed on this PC: {completedAt.Value.ToLocalTime():g}" : string.Empty;
            }
        }

        private readonly PowerShellService _powerShellService;
        private readonly UserDataService _userDataService;
        private readonly ObservableCollection<CustomScript> _customScripts = new();
        private readonly Dictionary<string, CancellationTokenSource> _runningScripts = new();
        private readonly BuiltInScriptCard[] _builtInScripts =
        [
            new("winget", "\uE7B8", "Install WinGet Package Manager",
                "Install Microsoft WinGet package manager if not present. Includes multiple installation methods with automatic fallback.",
                "Package Manager", "Execute", false, 15,
                "Install Microsoft WinGet if it is not already available?"),
            new("ctt-winutil", "\uE756", "CTT Tool - Winutil",
                "Download and run the current Chris Titus Tech Windows utility script.",
                "Utilities", "Run", true, 30,
                "This downloads and executes a remote script that can change over time. Only continue if you trust christitus.com."),
            new("dism-sfc-chkdsk", "\uE90F", "DISM + SFC + CHKDSK System Repair",
                "Repair the Windows image and system files, then scan the system drive for filesystem errors.",
                "Repair", "Run Repair", true, 90,
                "System repair can take 15–90 minutes. Do not close TweakHub while it is running.",
                "A restart is recommended after repairs."),
            new("component-cleanup", "\uE74D", "Component Store Cleanup",
                "Remove superseded Windows component versions to reclaim disk space.",
                "Maintenance", "Run Cleanup", true, 60,
                "This removes superseded Windows components. Continue?"),
            new("network-reset", "\uE968", "Network Stack Reset",
                "Flush DNS and reset Winsock and TCP/IP to repair persistent network problems.",
                "Network", "Run Reset", true, 15,
                "This can affect static network settings and requires a restart. Continue?",
                "Restart Windows before testing the connection."),
            new("windows-update-reset", "\uE895", "Windows Update Components Reset",
                "Recreate the Windows Update download and catalog caches when updates are stuck.",
                "Repair", "Run Reset", true, 20,
                "This clears cached update downloads and update history shown in Settings. Continue?"),
            new("prevent-device-metadata", "\uE72E", "Prevent Device Companion Apps",
                "Prevent Windows from downloading apps and metadata associated with connected devices.",
                "Privacy", "Apply Policy", true, 10,
                "This policy blocks device companion apps and metadata, but not necessarily every driver from Windows Update. Continue?"),
            new("exclude-wu-drivers", "\uE895", "Exclude Drivers from Windows Update",
                "Enable the Windows policy that excludes packages classified as drivers from quality updates.",
                "Windows Update", "Apply Policy", true, 10,
                "Windows Update will stop offering packages classified as drivers. Continue?"),
            new("empty-standby-list", "\uE950", "Empty Standby List",
                "Use Microsoft RAMMap to empty cached standby memory only when diagnosing a measured memory problem.",
                "Memory", "Run Once", true, 10,
                "Cached RAM is released automatically when applications need it. Emptying it routinely does not improve FPS or PC performance and can make games load more slowly. Continue only for a documented standby-list problem."),
            new("remove-windows-ai", "\uE99A", "Windows AI - Disable and Remove",
                "Disable Windows AI policies and remove available Copilot/CoreAI packages and Recall.",
                "Privacy", "Run", true, 30,
                "This removes packages and disables features. It is partially irreversible and TweakHub cannot guarantee automatic reinstallation. Continue?"),
            new("adobe-hosts-block", "\uE968", "Adobe URL Block List - Enable",
                "Add the maintained Ruddernation Designs Adobe block list to the hosts file inside TweakHub markers.",
                "Privacy", "Enable", true, 10,
                "This downloads a third-party list and blocks matching Adobe hosts system-wide. Continue?"),
            new("adobe-hosts-unblock", "\uE777", "Adobe URL Block List - Remove",
                "Remove only the hosts-file block previously added between TweakHub markers.",
                "Privacy", "Remove", true, 10,
                "Remove the TweakHub Adobe block from the hosts file?" )
        ];

        public AutomatedScriptsPage()
        {
            InitializeComponent();
            _powerShellService = PowerShellService.Instance;
            _userDataService = UserDataService.Instance;
            BuiltInScriptsControl.ItemsSource = _builtInScripts;
            CustomScriptsControl.ItemsSource = _customScripts;
            LoadCustomScripts();
            LoadScriptHistory();
        }

        private void LoadCustomScripts()
        {
            var loaded = _userDataService.LoadCustomScripts();
            _customScripts.Clear();
            foreach (var s in loaded) _customScripts.Add(s);
        }

        private void LoadScriptHistory()
        {
            foreach (var card in _builtInScripts)
                card.SetCompletion(ScriptHistoryService.Instance.TryGetCompletion(card.Id, GetBuiltInScript(card.Id), out var completedAt)
                    ? completedAt
                    : null);
        }

        private void MarkScriptCompleted(BuiltInScriptCard card)
        {
            try
            {
                var script = GetBuiltInScript(card.Id);
                ScriptHistoryService.Instance.MarkCompleted(card.Id, script);
                card.SetCompletion(DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unable to save script history: {ex.Message}");
            }
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
            if (script.RequiresAdministrator && !await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    "Administrator Script",
                    $"Run '{script.Name}' with administrator privileges?",
                    "Run",
                    "Cancel")) return;

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

            if (result.Success)
                await AppDialog.ShowAsync(Window.GetWindow(this), script.Name, $"{summary}\n\n{details}".Trim());
            else
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), script.Name, $"{summary}\n\n{details}".Trim());
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
                    await AppDialog.ShowWarningAsync(dialog, "Invalid Script", "Name required.");
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
            if (_runningScripts.ContainsKey(script.Id))
            {
                await AppDialog.ShowAsync(owner, "Script Running", "Stop the script before deleting it.");
                return;
            }
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
                textBox.Text = GetBuiltInScript(script.Id);
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

        internal static string GetBuiltInScript(string id) => id switch
        {
            "winget" => LoadWinGetScript(),
            "ctt-winutil" => "irm christitus.com/win | iex",
            "dism-sfc-chkdsk" => @"
$commands = @(
    @{ Executable = 'DISM.exe'; Arguments = @('/Online', '/Cleanup-Image', '/CheckHealth') },
    @{ Executable = 'DISM.exe'; Arguments = @('/Online', '/Cleanup-Image', '/ScanHealth') },
    @{ Executable = 'DISM.exe'; Arguments = @('/Online', '/Cleanup-Image', '/RestoreHealth') },
    @{ Executable = 'sfc.exe'; Arguments = @('/scannow') },
    @{ Executable = 'chkdsk.exe'; Arguments = @($env:SystemDrive, '/scan') }
)
foreach ($command in $commands) {
    $executable = $command.Executable
    $arguments = $command.Arguments
    Write-Output ""`n> $executable $arguments""
    & $executable @arguments
    if ($LASTEXITCODE -ne 0) { throw ""$executable failed with exit code $LASTEXITCODE"" }
}
",
            "component-cleanup" => @"
& DISM.exe /Online /Cleanup-Image /StartComponentCleanup
if ($LASTEXITCODE -ne 0) { throw ""DISM failed with exit code $LASTEXITCODE"" }
",
            "network-reset" => @"
& ipconfig.exe /flushdns
if ($LASTEXITCODE -ne 0) { throw ""ipconfig failed with exit code $LASTEXITCODE"" }
& netsh.exe winsock reset
if ($LASTEXITCODE -ne 0) { throw ""Winsock reset failed with exit code $LASTEXITCODE"" }
& netsh.exe int ip reset
if ($LASTEXITCODE -ne 0) { throw ""TCP/IP reset failed with exit code $LASTEXITCODE"" }
",
            "windows-update-reset" => @"
$services = 'bits', 'wuauserv', 'cryptsvc'
$stamp = Get-Date -Format yyyyMMddHHmmss
try {
    Stop-Service $services -Force -ErrorAction Stop
    $caches = (Join-Path $env:SystemRoot 'SoftwareDistribution'), (Join-Path $env:SystemRoot 'System32\catroot2')
    foreach ($cache in $caches) {
        if (Test-Path $cache) {
            $newName = '{0}.tweakhub-{1}' -f [IO.Path]::GetFileName($cache), $stamp
            Rename-Item $cache $newName -ErrorAction Stop
        }
    }
} finally {
    foreach ($service in $services) { Start-Service $service -ErrorAction Continue }
}
Write-Output 'Windows Update caches recreated.'
",
            "prevent-device-metadata" => @"
$ErrorActionPreference = 'Stop'
$path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Device Metadata'
New-Item -Path $path -Force | Out-Null
New-ItemProperty -Path $path -Name 'PreventDeviceMetadataFromNetwork' -PropertyType DWord -Value 1 -Force | Out-Null
& gpupdate.exe /target:computer /force
if ($LASTEXITCODE -ne 0) { throw ""gpupdate failed with exit code $LASTEXITCODE"" }
if ((Get-ItemPropertyValue -Path $path -Name 'PreventDeviceMetadataFromNetwork') -ne 1) {
    throw 'Policy verification failed.'
}
Write-Output 'Prevent Device Companion Apps policy is enabled and verified.'
",
            "exclude-wu-drivers" => @"
$ErrorActionPreference = 'Stop'
$path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'
New-Item -Path $path -Force | Out-Null
New-ItemProperty -Path $path -Name 'ExcludeWUDriversInQualityUpdate' -PropertyType DWord -Value 1 -Force | Out-Null
& gpupdate.exe /target:computer /force
if ($LASTEXITCODE -ne 0) { throw ""gpupdate failed with exit code $LASTEXITCODE"" }
if ((Get-ItemPropertyValue -Path $path -Name 'ExcludeWUDriversInQualityUpdate') -ne 1) {
    throw 'Policy verification failed.'
}
Write-Output 'Driver exclusion policy is enabled and verified.'
",
            "empty-standby-list" => @"
$ErrorActionPreference = 'Stop'
$command = Get-Command RAMMap64.exe, RAMMap.exe -ErrorAction SilentlyContinue | Select-Object -First 1
$executable = $command.Source
if (-not $executable) {
    $packageRoot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages'
    $executable = Get-ChildItem $packageRoot -Filter 'RAMMap*.exe' -Recurse -ErrorAction SilentlyContinue |
        Where-Object Name -Match '^RAMMap(64)?\.exe$' |
        Select-Object -ExpandProperty FullName -First 1
}
if (-not $executable) { throw 'RAMMap was not found. Install Microsoft RAMMap from External Tools first.' }
$process = Start-Process -FilePath $executable -ArgumentList '-Et' -Wait -PassThru
if ($process.ExitCode -ne 0) { throw ""RAMMap failed with exit code $($process.ExitCode)"" }
Write-Output 'Standby list emptied once. Do not schedule this operation.'
",
            "remove-windows-ai" => @"
$ErrorActionPreference = 'Stop'
$policyPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer'
New-Item $policyPath -Force | Out-Null
New-ItemProperty $policyPath -Name SettingsPageVisibility -PropertyType String -Value 'hide:aicomponents' -Force | Out-Null
$notepadPath = 'HKLM:\SOFTWARE\Policies\WindowsNotepad'
New-Item $notepadPath -Force | Out-Null
New-ItemProperty $notepadPath -Name DisableAIFeatures -PropertyType DWord -Value 1 -Force | Out-Null
Get-AppxPackage -AllUsers '*Copilot*' | ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName -AllUsers }
Get-AppxPackage -AllUsers Microsoft.MicrosoftOfficeHub | ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName -AllUsers }
Get-AppxPackage MicrosoftWindows.Client.CoreAI | ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName }
if (Get-Service WSAIFabricSvc -ErrorAction SilentlyContinue) { Set-Service WSAIFabricSvc -StartupType Disabled }
if ((Get-WindowsOptionalFeature -Online -FeatureName Recall -ErrorAction SilentlyContinue).State -eq 'Enabled') {
    Disable-WindowsOptionalFeature -FeatureName Recall -Online -NoRestart | Out-Null
}
if (Get-Command winget.exe -ErrorAction SilentlyContinue) {
    $winget = Start-Process winget.exe -ArgumentList @('uninstall', '-e', '--name', 'Copilot', '--silent', '--force', '--accept-source-agreements') -Wait -PassThru -WindowStyle Hidden
    if ($winget.ExitCode -ne 0) { Write-Warning 'Copilot was not installed through WinGet or could not be removed.' }
}
Write-Output 'Available Windows AI components were disabled or removed. A restart is recommended.'
",
            "adobe-hosts-block" => @"
$ErrorActionPreference = 'Stop'
$hostsPath = Join-Path $env:SystemRoot 'System32\drivers\etc\hosts'
$start = '# TweakHub Adobe block START'
$end = '# TweakHub Adobe block END'
$pattern = '(?ms)^# TweakHub Adobe block START\r?\n.*?^# TweakHub Adobe block END\r?\n?'
$content = [IO.File]::ReadAllText($hostsPath)
$content = [regex]::Replace($content, $pattern, '')
$list = Invoke-RestMethod -Uri 'https://github.com/Ruddernation-Designs/Adobe-URL-Block-List/raw/refs/heads/master/hosts'
if ([string]::IsNullOrWhiteSpace([string]$list)) { throw 'Downloaded hosts list is empty.' }
$block = ""$start`r`n$([string]$list)`r`n$end`r`n""
[IO.File]::WriteAllText($hostsPath, $content.TrimEnd() + ""`r`n"" + $block, [Text.UTF8Encoding]::new($false))
& ipconfig.exe /flushdns | Out-Null
Write-Output 'Adobe block list added between TweakHub markers.'
",
            "adobe-hosts-unblock" => @"
$ErrorActionPreference = 'Stop'
$hostsPath = Join-Path $env:SystemRoot 'System32\drivers\etc\hosts'
$pattern = '(?ms)^# TweakHub Adobe block START\r?\n.*?^# TweakHub Adobe block END\r?\n?'
$content = [IO.File]::ReadAllText($hostsPath)
$updated = [regex]::Replace($content, $pattern, '')
[IO.File]::WriteAllText($hostsPath, $updated, [Text.UTF8Encoding]::new($false))
& ipconfig.exe /flushdns | Out-Null
Write-Output 'TweakHub Adobe block removed.'
",
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown built-in script.")
        };

        private void BuiltInScript_Execute_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: BuiltInScriptCard script }) return;
            if (script.Id == "winget") WinGetInstallation_Click(sender, e);
            else ExecuteBuiltInScript(script);
        }

        private void CopyBuiltInScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: TextBox textBox })
                try { Clipboard.SetText(textBox.Text ?? string.Empty); } catch { }
        }

        private async void WinGetInstallation_Click(object sender, RoutedEventArgs e)
        {
            if (!await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    "Install WinGet Package Manager",
                    "Install Microsoft WinGet if it is not already available?",
                    "Install",
                    "Cancel")) return;

            var scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "WinGetInstall.ps1");
            if (!System.IO.File.Exists(scriptPath))
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), "Installation Error", "WinGetInstall.ps1 was not found.");
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
                if (success && _builtInScripts.FirstOrDefault(card => card.Id == "winget") is { } card)
                    MarkScriptCompleted(card);

                if (success)
                    await AppDialog.ShowAsync(Window.GetWindow(this), "Installation Complete", result.Output.Trim());
                else
                    await AppDialog.ShowErrorAsync(Window.GetWindow(this), "Installation Failed",
                        $"WinGet installation failed.\n\n{result.Error}\n{result.Output}".Trim());
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), "Installation Error", ex.Message);
            }
            finally
            {
                progress.Close();
            }
        }

        private async void ExecuteBuiltInScript(BuiltInScriptCard script)
        {
            if (!await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    script.Name,
                    script.Confirmation,
                    script.ExecuteText,
                    "Cancel")) return;

            var progress = new ProgressWindow(script.Name);
            progress.Show();
            progress.UpdateStatus("Running commands. Do not close TweakHub...");
            progress.UpdateProgress(10);

            try
            {
                var result = await _powerShellService.ExecuteScriptAsync(
                    GetBuiltInScript(script.Id),
                    script.RequiresAdministrator,
                    TimeSpan.FromMinutes(script.TimeoutMinutes));
                if (result.Success) MarkScriptCompleted(script);
                var logDirectory = System.IO.Path.Combine(_userDataService.DataDirectory, "Logs");
                var logPath = System.IO.Path.Combine(logDirectory, $"{script.Id}-last.log");
                try
                {
                    System.IO.Directory.CreateDirectory(logDirectory);
                    await System.IO.File.WriteAllTextAsync(logPath, result.Output + Environment.NewLine + result.Error);
                }
                catch (Exception ex)
                {
                    logPath = $"Log could not be saved: {ex.Message}";
                }
                progress.UpdateProgress(100);

                var details = string.Join("\n", new[] { result.Output.Trim(), result.Error.Trim() }.Where(value => value.Length > 0));
                if (details.Length > 4000) details = details[^4000..];
                var message = $"{(result.Success ? "Completed successfully." : $"Failed with exit code {result.ExitCode}.")}\n" +
                              $"{script.CompletionNote}\n\n{details}\n\nLog: {logPath}".Trim();
                if (result.Success)
                    await AppDialog.ShowAsync(Window.GetWindow(this), "Complete", message);
                else
                    await AppDialog.ShowErrorAsync(Window.GetWindow(this), "Failed", message);
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), $"{script.Name} Failed", ex.Message);
            }
            finally
            {
                progress.Close();
            }
        }


    }
}
