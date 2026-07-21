using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using TweakHub.Localization;
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
                CompletionToolTip = completedAt.HasValue ? L.Format("Scripts:CompletedOnThisPc", completedAt.Value.ToLocalTime().ToString("g", L.Culture)) : string.Empty;
            }
        }

        private readonly PowerShellService _powerShellService;
        private readonly UserDataService _userDataService;
        private readonly ObservableCollection<CustomScript> _customScripts = new();
        private readonly Dictionary<string, CancellationTokenSource> _runningScripts = new();
        private readonly BuiltInScriptCard[] _builtInScripts =
        [
            new("winget", "\uE7B8", L.Get("Scripts:winget_Name"),
                L.Get("Scripts:winget_Description"),
                L.Get("Scripts:winget_Category"), L.Get("Scripts:winget_Execute"), false, 15,
                L.Get("Scripts:winget_Confirmation")),
            new("ctt-winutil", "\uE756", L.Get("Scripts:ctt_winutil_Name"),
                L.Get("Scripts:ctt_winutil_Description"),
                L.Get("Scripts:ctt_winutil_Category"), L.Get("Scripts:ctt_winutil_Execute"), true, 30,
                L.Get("Scripts:ctt_winutil_Confirmation")),
            new("dism-sfc-chkdsk", "\uE90F", L.Get("Scripts:dism_sfc_chkdsk_Name"),
                L.Get("Scripts:dism_sfc_chkdsk_Description"),
                L.Get("Scripts:dism_sfc_chkdsk_Category"), L.Get("Scripts:dism_sfc_chkdsk_Execute"), true, 90,
                L.Get("Scripts:dism_sfc_chkdsk_Confirmation"),
                L.Get("Scripts:dism_sfc_chkdsk_CompletionNote")),
            new("component-cleanup", "\uE74D", L.Get("Scripts:component_cleanup_Name"),
                L.Get("Scripts:component_cleanup_Description"),
                L.Get("Scripts:component_cleanup_Category"), L.Get("Scripts:component_cleanup_Execute"), true, 60,
                L.Get("Scripts:component_cleanup_Confirmation")),
            new("network-reset", "\uE968", L.Get("Scripts:network_reset_Name"),
                L.Get("Scripts:network_reset_Description"),
                L.Get("Scripts:network_reset_Category"), L.Get("Scripts:network_reset_Execute"), true, 15,
                L.Get("Scripts:network_reset_Confirmation"),
                L.Get("Scripts:network_reset_CompletionNote")),
            new("windows-update-reset", "\uE895", L.Get("Scripts:windows_update_reset_Name"),
                L.Get("Scripts:windows_update_reset_Description"),
                L.Get("Scripts:windows_update_reset_Category"), L.Get("Scripts:windows_update_reset_Execute"), true, 20,
                L.Get("Scripts:windows_update_reset_Confirmation")),
            new("prevent-device-metadata", "\uE72E", L.Get("Scripts:prevent_device_metadata_Name"),
                L.Get("Scripts:prevent_device_metadata_Description"),
                L.Get("Scripts:prevent_device_metadata_Category"), L.Get("Scripts:prevent_device_metadata_Execute"), true, 10,
                L.Get("Scripts:prevent_device_metadata_Confirmation")),
            new("exclude-wu-drivers", "\uE895", L.Get("Scripts:exclude_wu_drivers_Name"),
                L.Get("Scripts:exclude_wu_drivers_Description"),
                L.Get("Scripts:exclude_wu_drivers_Category"), L.Get("Scripts:exclude_wu_drivers_Execute"), true, 10,
                L.Get("Scripts:exclude_wu_drivers_Confirmation")),
            new("empty-standby-list", "\uE950", L.Get("Scripts:empty_standby_list_Name"),
                L.Get("Scripts:empty_standby_list_Description"),
                L.Get("Scripts:empty_standby_list_Category"), L.Get("Scripts:empty_standby_list_Execute"), true, 10,
                L.Get("Scripts:empty_standby_list_Confirmation")),
            new("remove-windows-ai", "\uE99A", L.Get("Scripts:remove_windows_ai_Name"),
                L.Get("Scripts:remove_windows_ai_Description"),
                L.Get("Scripts:remove_windows_ai_Category"), L.Get("Scripts:remove_windows_ai_Execute"), true, 30,
                L.Get("Scripts:remove_windows_ai_Confirmation")),
            new("adobe-hosts-block", "\uE968", L.Get("Scripts:adobe_hosts_block_Name"),
                L.Get("Scripts:adobe_hosts_block_Description"),
                L.Get("Scripts:adobe_hosts_block_Category"), L.Get("Scripts:adobe_hosts_block_Execute"), true, 10,
                L.Get("Scripts:adobe_hosts_block_Confirmation")),
            new("adobe-hosts-unblock", "\uE777", L.Get("Scripts:adobe_hosts_unblock_Name"),
                L.Get("Scripts:adobe_hosts_unblock_Description"),
                L.Get("Scripts:adobe_hosts_unblock_Category"), L.Get("Scripts:adobe_hosts_unblock_Execute"), true, 10,
                L.Get("Scripts:adobe_hosts_unblock_Confirmation"))
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
                Filter = L.Get("Scripts:ImportFilter"),
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
                Filter = script.Language == ScriptLanguage.PowerShell ? L.Get("Scripts:PowerShellFilter") : L.Get("Scripts:CmdFilter"),
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
                    L.Get("Scripts:AdministratorScript"),
                    L.Format("Scripts:AdministratorScriptMessage", script.Name),
                    L.Get("Scripts:Run"),
                    L.Get("Scripts:Cancel"))) return;

            var result = script.Language == ScriptLanguage.PowerShell
                ? await _powerShellService.ExecuteScriptAsync(
                    script.Content,
                    script.RequiresAdministrator,
                    TimeSpan.FromMinutes(15),
                    cancellationToken)
                : await ExecuteCmdScript(script, cancellationToken);
            var details = string.Join("\n", new[] { result.Output.Trim(), result.Error.Trim() }.Where(s => s.Length > 0));
            var summary = result.Success
                ? L.Format("Scripts:CustomScriptCompleted", result.Duration.TotalSeconds)
                : L.Format("Scripts:CustomScriptFailed", result.ExitCode, result.Duration.TotalSeconds);

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
                Title = L.Get(isNew ? "Scripts:CreateCustomScript" : "Scripts:EditCustomScript"),
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

            var nameBox = new TextBox { Margin = new Thickness(0, 0, 0, 12), ToolTip = L.Get("Scripts:EnterScriptName"), Text = script.Name };
            Grid.SetRow(nameBox, 0);
            grid.Children.Add(nameBox);

            var langPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            var psRadio = new RadioButton { Content = "PowerShell", IsChecked = script.Language == ScriptLanguage.PowerShell, Margin = new Thickness(0, 0, 12, 0) };
            var cmdRadio = new RadioButton { Content = L.Get("Scripts:CmdBatch"), IsChecked = script.Language == ScriptLanguage.Cmd, Margin = new Thickness(0,0,20,0) };
            var adminCheck = new CheckBox { Content = L.Get("Scripts:RequiresAdministrator"), IsChecked = script.RequiresAdministrator, VerticalAlignment = VerticalAlignment.Center };
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
            var cancelBtn = new Button { Content = L.Get("Scripts:Cancel"), Style = GetStyleOrDefault("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
            var saveBtn = new Button { Content = L.Get(isNew ? "Scripts:Create" : "Scripts:Save"), Style = GetStyleOrDefault("ExecuteButtonStyle") };
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
                    await AppDialog.ShowWarningAsync(dialog, L.Get("Scripts:InvalidScript"), L.Get("Scripts:NameRequired"));
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
                    L.Get(isNew ? "Scripts:ScriptCreated" : "Scripts:ScriptUpdated"),
                    L.Format(isNew ? "Scripts:ScriptCreatedMessage" : "Scripts:ScriptUpdatedMessage", name));
            };

            dialog.Content = grid;
            dialog.ShowDialog();
        }

        private async Task DeleteCustomScript(CustomScript script)
        {
            var owner = Window.GetWindow(this);
            if (_runningScripts.ContainsKey(script.Id))
            {
                await AppDialog.ShowAsync(owner, L.Get("Scripts:ScriptRunning"), L.Get("Scripts:StopBeforeDeleting"));
                return;
            }
            if (!await AppDialog.ConfirmAsync(
                    owner,
                    L.Get("Scripts:ConfirmDelete"),
                    L.Format("Scripts:ConfirmDeleteMessage", script.Name))) return;

            var toRemove = _customScripts.FirstOrDefault(x => x.Id == script.Id);
            if (toRemove != null)
            {
                _customScripts.Remove(toRemove);
                _userDataService.SaveCustomScripts(_customScripts);

                await AppDialog.ShowAsync(
                    owner,
                    L.Get("Scripts:ScriptDeleted"),
                    L.Format("Scripts:ScriptDeletedMessage", script.Name));
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
                textBox.Text = L.Format("Scripts:ScriptLoadError", ex.Message);
            }
            preview.Visibility = preview.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private static string LoadWinGetScript()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "WinGetInstall.ps1");
            return System.IO.File.Exists(path)
                ? System.IO.File.ReadAllText(path)
                : L.Get("Scripts:WingetScriptMissingPreview");
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
                    L.Get("Scripts:winget_Name"),
                    L.Get("Scripts:winget_Confirmation"),
                    L.Get("Scripts:Install"),
                    L.Get("Scripts:Cancel"))) return;

            var scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "WinGetInstall.ps1");
            if (!System.IO.File.Exists(scriptPath))
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Scripts:InstallationError"), L.Get("Scripts:WingetScriptNotFound"));
                return;
            }

            var progress = new ProgressWindow(L.Get("Scripts:InstallingWinget"));
            progress.Show();

            try
            {
                progress.UpdateStatus(L.Get("Scripts:RunningWingetScript"));
                var result = await _powerShellService.ExecuteScriptAsync(System.IO.File.ReadAllText(scriptPath));
                var success = result.Success &&
                              (result.Output.Contains("SUCCESS:") || result.Output.Contains("ALREADY_INSTALLED:"));
                if (success && _builtInScripts.FirstOrDefault(card => card.Id == "winget") is { } card)
                    MarkScriptCompleted(card);

                if (success)
                    await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Scripts:InstallationComplete"), result.Output.Trim());
                else
                    await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Scripts:InstallationFailed"),
                        L.Format("Scripts:WingetInstallationFailed", result.Error, result.Output).Trim());
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Scripts:InstallationError"), ex.Message);
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
                    L.Get("Scripts:Cancel"))) return;

            var progress = new ProgressWindow(script.Name);
            progress.Show();
            progress.UpdateStatus(L.Get("Scripts:RunningCommands"));
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
                    logPath = L.Format("Scripts:LogSaveFailed", ex.Message);
                }
                progress.UpdateProgress(100);

                var details = string.Join("\n", new[] { result.Output.Trim(), result.Error.Trim() }.Where(value => value.Length > 0));
                if (details.Length > 4000) details = details[^4000..];
                var outcome = result.Success ? L.Get("Scripts:CompletedSuccessfully") : L.Format("Scripts:FailedExitCode", result.ExitCode);
                var message = L.Format("Scripts:BuiltInResult", outcome, script.CompletionNote, details, logPath).Trim();
                if (result.Success)
                    await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Scripts:Complete"), message);
                else
                    await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Scripts:Failed"), message);
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Format("Scripts:NamedScriptFailed", script.Name), ex.Message);
            }
            finally
            {
                progress.Close();
            }
        }


    }
}
