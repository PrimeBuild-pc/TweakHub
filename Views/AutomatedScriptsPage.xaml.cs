using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
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
            private bool _isFavorite;
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
            public bool IsFavorite
            {
                get => _isFavorite;
                private set { _isFavorite = value; PropertyChanged?.Invoke(this, new(nameof(IsFavorite))); }
            }
            public void SetFavorite(bool value) => IsFavorite = value;
            public void SetCompletion(DateTimeOffset? completedAt)
            {
                IsCompleted = completedAt.HasValue;
                CompletionToolTip = completedAt.HasValue ? L.Format("Scripts:CompletedOnThisPc", completedAt.Value.ToLocalTime().ToString("g", L.Culture)) : string.Empty;
            }
        }

        private readonly PowerShellService _powerShellService;
        private readonly UserDataService _userDataService;
        private readonly ObservableCollection<CustomScript> _customScripts = new();
        private readonly ObservableCollection<Playbook> _playbooks = new();
        private readonly Dictionary<string, CancellationTokenSource> _runningScripts = new();
        private HashSet<string> _favoriteScriptKeys = [];
        private readonly BuiltInScriptCard[] _builtInScripts =
        [
            new("winget", "\uE7B8", L.Get("Scripts:winget_Name"),
                L.Get("Scripts:winget_Description"),
                L.Get("Scripts:winget_Category"), L.Get("Scripts:winget_Execute"), false, 15,
                L.Get("Scripts:winget_Confirmation")),
            new("gaming-runtimes", "\uE7FC", L.Get("Scripts:gaming_runtimes_Name"),
                L.Get("Scripts:gaming_runtimes_Description"),
                L.Get("Scripts:gaming_runtimes_Category"), L.Get("Scripts:Install"), false, 45,
                L.Get("Scripts:gaming_runtimes_Confirmation")),
            new("dotnet-developer-setup", "\uE943", L.Get("Scripts:dotnet_developer_setup_Name"),
                L.Get("Scripts:dotnet_developer_setup_Description"),
                L.Get("Scripts:dotnet_developer_setup_Category"), L.Get("Scripts:Install"), false, 120,
                L.Get("Scripts:dotnet_developer_setup_Confirmation")),
            new("ctt-winutil", "\uE756", L.Get("Scripts:ctt_winutil_Name"),
                L.Get("Scripts:ctt_winutil_Description"),
                L.Get("Scripts:ctt_winutil_Category"), L.Get("Scripts:ctt_winutil_Execute"), true, 30,
                L.Get("Scripts:ctt_winutil_Confirmation")),
            new("change-local-password", "\uE72E", L.Get("Scripts:change_local_password_Name"),
                L.Get("Scripts:change_local_password_Description"),
                L.Get("Scripts:CurrentUser"), L.Get("Scripts:Run"), true, 10,
                L.Get("Scripts:change_local_password_Confirmation")),
            new("dism-sfc-chkdsk", "\uE90F", L.Get("Scripts:dism_sfc_chkdsk_Name"),
                L.Get("Scripts:dism_sfc_chkdsk_Description"),
                L.Get("Scripts:dism_sfc_chkdsk_Category"), L.Get("Scripts:dism_sfc_chkdsk_Execute"), true, 90,
                L.Get("Scripts:dism_sfc_chkdsk_Confirmation"),
                L.Get("Scripts:dism_sfc_chkdsk_CompletionNote")),
            new("component-cleanup", "\uE74D", L.Get("Scripts:component_cleanup_Name"),
                L.Get("Scripts:component_cleanup_Description"),
                L.Get("Scripts:component_cleanup_Category"), L.Get("Scripts:component_cleanup_Execute"), true, 60,
                L.Get("Scripts:component_cleanup_Confirmation")),
            new("temp-cleanup", "", L.Get("Scripts:temp_cleanup_Name"),
                L.Get("Scripts:temp_cleanup_Description"),
                L.Get("Scripts:temp_cleanup_Category"), L.Get("Scripts:temp_cleanup_Execute"), true, 30,
                L.Get("Scripts:temp_cleanup_Confirmation")),
            new("network-reset", "\uE968", L.Get("Scripts:network_reset_Name"),
                L.Get("Scripts:network_reset_Description"),
                L.Get("Scripts:network_reset_Category"), L.Get("Scripts:network_reset_Execute"), true, 15,
                L.Get("Scripts:network_reset_Confirmation"),
                L.Get("Scripts:network_reset_CompletionNote")),
            new("windows-update-reset", "\uE895", L.Get("Scripts:windows_update_reset_Name"),
                L.Get("Scripts:windows_update_reset_Description"),
                L.Get("Scripts:windows_update_reset_Category"), L.Get("Scripts:windows_update_reset_Execute"), true, 20,
                L.Get("Scripts:windows_update_reset_Confirmation")),
            new("microsoft-store-repair", "\uE719", L.Get("Scripts:microsoft_store_repair_Name"),
                L.Get("Scripts:microsoft_store_repair_Description"),
                L.Get("Scripts:dism_sfc_chkdsk_Category"), L.Get("Scripts:dism_sfc_chkdsk_Execute"), false, 20,
                L.Get("Scripts:microsoft_store_repair_Confirmation")),
            new("gaming-services-repair", "\uE7FC", L.Get("Scripts:gaming_services_repair_Name"),
                L.Get("Scripts:gaming_services_repair_Description"),
                L.Get("Scripts:gaming_runtimes_Category"), L.Get("Scripts:dism_sfc_chkdsk_Execute"), true, 30,
                L.Get("Scripts:gaming_services_repair_Confirmation")),
            new("print-spooler-repair", "\uE749", L.Get("Scripts:print_spooler_repair_Name"),
                L.Get("Scripts:print_spooler_repair_Description"),
                L.Get("Scripts:dism_sfc_chkdsk_Category"), L.Get("Scripts:dism_sfc_chkdsk_Execute"), true, 10,
                L.Get("Scripts:print_spooler_repair_Confirmation")),
            new("onedrive-repair", "\uE753", L.Get("Scripts:onedrive_repair_Name"),
                L.Get("Scripts:onedrive_repair_Description"),
                L.Get("Scripts:dism_sfc_chkdsk_Category"), L.Get("Scripts:dism_sfc_chkdsk_Execute"), false, 15,
                L.Get("Scripts:onedrive_repair_Confirmation")),
            new("wsl-setup", "\uE756", L.Get("Scripts:wsl_setup_Name"),
                L.Get("Scripts:wsl_setup_Description"),
                L.Get("Scripts:dotnet_developer_setup_Category"), L.Get("Scripts:Install"), true, 45,
                L.Get("Scripts:wsl_setup_Confirmation"), L.Get("Scripts:wsl_setup_CompletionNote")),
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
            PlaybooksControl.ItemsSource = _playbooks;
            try { AppDataPath.EnsureAppsDirectory(); } catch { }
            LoadCustomScripts();
            LoadFavoriteScripts();
            LoadPlaybooks();
            LoadScriptHistory();
            Loaded += async (_, _) =>
            {
                // Started again as administrator to run this playbook with a single UAC prompt.
                if (App.StartupPlaybookId is not { } id) return;
                App.StartupPlaybookId = null;
                if (_playbooks.FirstOrDefault(playbook => playbook.Id == id) is { } playbook) await RunPlaybookAsync(playbook, confirmed: true);
            };
        }

        private void LoadCustomScripts()
        {
            var loaded = _userDataService.LoadCustomScripts();
            _customScripts.Clear();
            foreach (var s in loaded) _customScripts.Add(s);
        }

        private void LoadFavoriteScripts()
        {
            _favoriteScriptKeys = _userDataService.LoadFavoriteScripts();
            foreach (var script in _builtInScripts)
                script.SetFavorite(_favoriteScriptKeys.Contains(FavoriteKey(script)));
            foreach (var script in _customScripts)
                script.IsFavorite = _favoriteScriptKeys.Contains(FavoriteKey(script));
        }

        private void FavoriteScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            string? key = null;
            bool favorite;
            if (button.DataContext is BuiltInScriptCard builtIn)
            {
                favorite = !builtIn.IsFavorite;
                builtIn.SetFavorite(favorite);
                key = FavoriteKey(builtIn);
            }
            else if (button.DataContext is CustomScript custom)
            {
                favorite = !custom.IsFavorite;
                custom.IsFavorite = favorite;
                key = FavoriteKey(custom);
                CustomScriptsControl.Items.Refresh();
            }
            else return;

            if (favorite) _favoriteScriptKeys.Add(key); else _favoriteScriptKeys.Remove(key);
            _userDataService.SaveFavoriteScripts(_favoriteScriptKeys);
        }

        private static string FavoriteKey(BuiltInScriptCard script) => $"builtin:{script.Id}";
        private static string FavoriteKey(CustomScript script) => $"custom:{script.Id}";

        private void LoadPlaybooks()
        {
            _playbooks.Clear();
            foreach (var playbook in _userDataService.LoadPlaybooks()) _playbooks.Add(playbook);
        }

        private void OpenAppsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppDataPath.EnsureAppsDirectory();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppDataPath.AppsPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _ = AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Scripts:AppsFolder"), ex.Message);
            }
        }

        private void NewPlaybook_Click(object sender, RoutedEventArgs e) => EditPlaybook(null, detect: false);

        private void CapturePlaybook_Click(object sender, RoutedEventArgs e) => EditPlaybook(null, detect: true);

        private void EditPlaybook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: Playbook playbook }) EditPlaybook(playbook, detect: false);
        }

        private async void DeletePlaybook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Playbook playbook }
                || !await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Scripts:DeletePlaybook"),
                    L.Format("Scripts:DeletePlaybookMessage", playbook.Name))) return;
            try
            {
                _userDataService.SavePlaybooks(_playbooks.Where(item => item != playbook));
                _playbooks.Remove(playbook);
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Scripts:DeletePlaybook"), ex.Message);
            }
        }

        private async void ExportPlaybook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Playbook playbook }) return;
            var dialog = new SaveFileDialog
            {
                Filter = L.Get("Scripts:PlaybookFileFilter"),
                FileName = string.Concat(playbook.Name.Split(System.IO.Path.GetInvalidFileNameChars())) + ".tweakhub-playbook.json"
            };
            if (dialog.ShowDialog() != true) return;
            try { PlaybookService.Instance.Export(playbook, dialog.FileName); }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Scripts:ExportPlaybook"), ex.Message);
            }
        }

        private async void ImportPlaybook_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = L.Get("Scripts:PlaybookFileFilter") };
            if (dialog.ShowDialog() != true) return;
            try
            {
                var result = PlaybookService.Instance.Import(dialog.FileName);
                LoadPlaybooks();
                LoadCustomScripts();
                await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Scripts:ImportPlaybook"),
                    L.Format("Scripts:PlaybookImported", result.Playbook.Name, result.Playbook.Steps.Count, result.CustomTweaks, result.CustomScripts));
            }
            catch (Exception ex) when (ex is System.IO.InvalidDataException or System.IO.IOException or UnauthorizedAccessException)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Scripts:ImportPlaybook"), ex.Message);
            }
        }

        private async void RunPlaybook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: Playbook playbook }) await RunPlaybookAsync(playbook, confirmed: false);
        }

        /// <param name="confirmed">Already confirmed before TweakHub restarted as administrator.</param>
        private async Task RunPlaybookAsync(Playbook playbook, bool confirmed)
        {
            PlaybookPreflight preflight;
            try { preflight = PlaybookService.Instance.Preflight(playbook); }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Scripts:PlaybookCannotRun"), ex.Message);
                return;
            }
            if (!preflight.CanRun)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Scripts:PlaybookCannotRun"),
                    string.Join(Environment.NewLine, preflight.Errors));
                return;
            }
            if (!confirmed)
            {
                var preview = string.Join(Environment.NewLine, preflight.Lines);
                if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), playbook.Name,
                        L.Format("Scripts:PlaybookRunConfirmation", preview), L.Get("Scripts:Run"), L.Get("Scripts:Cancel"))) return;
                // Several administrator steps would each ask for UAC: one elevated restart asks once.
                if (preflight.AdministratorSteps > 1 && Elevation.CanRestartElevated
                    && await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Scripts:PlaybookElevateTitle"),
                        L.Format("Scripts:PlaybookElevateMessage", preflight.AdministratorSteps),
                        L.Get("Scripts:PlaybookElevateRestart"), L.Get("Scripts:PlaybookElevateContinue"), DialogKind.Shield)
                    && App.RestartElevated($"--run-playbook {playbook.Id}"))
                    return;
            }

            var progressWindow = new ProgressWindow(playbook.Name);
            progressWindow.Show();
            var progress = new Progress<string>(message => progressWindow.UpdateStatus(message));
            try
            {
                var result = await PlaybookService.Instance.ExecuteAsync(playbook, progress);
                progressWindow.Close();
                var message = L.Format("Scripts:PlaybookRunResult", result.Completed, playbook.Steps.Count, result.LogPath);
                if (result.Success)
                    await AppDialog.ShowAsync(Window.GetWindow(this), playbook.Name, message);
                else
                {
                    var details = result.Details.Length > 4000 ? result.Details[^4000..] : result.Details;
                    await AppDialog.ShowErrorAsync(Window.GetWindow(this), playbook.Name, message + Environment.NewLine + details);
                }
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), playbook.Name, ex.Message);
            }
        }

        private void EditPlaybook(Playbook? existing, bool detect)
        {
            var dialog = new PlaybookDialog(existing, detect) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true || dialog.Result is not { } saved) return;
            try
            {
                var playbooks = _playbooks.Select(playbook => playbook == existing ? saved : playbook).ToList();
                if (existing == null) playbooks.Add(saved);
                _userDataService.SavePlaybooks(playbooks);
                LoadPlaybooks();
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            {
                _ = AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Scripts:PlaybookInvalid"), ex.Message);
            }
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

        private async void ImportScriptsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = L.Get("Scripts:ImportFilter"),
                Multiselect = true
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var imported = dialog.FileNames.Select(path =>
                {
                    var extension = System.IO.Path.GetExtension(path);
                    if (extension.ToLowerInvariant() is not (".ps1" or ".cmd" or ".bat"))
                        throw new InvalidDataException(L.Format("Scripts:ScriptFileTypeUnsupported", System.IO.Path.GetFileName(path)));
                    if (new System.IO.FileInfo(path).Length > 1024 * 1024)
                        throw new InvalidDataException(L.Format("Scripts:ScriptFileTooLarge", System.IO.Path.GetFileName(path)));
                    var name = System.IO.Path.GetFileNameWithoutExtension(path);
                    return new CustomScript
                    {
                        // Profile import accepts at most 100 characters; keep file-imported scripts exportable.
                        Name = name.Length > 100 ? name[..100] : name,
                        Language = extension.Equals(".ps1", StringComparison.OrdinalIgnoreCase)
                            ? ScriptLanguage.PowerShell
                            : ScriptLanguage.Cmd,
                        Content = System.IO.File.ReadAllText(path)
                    };
                }).ToList();
                _userDataService.SaveCustomScripts(_customScripts.Concat(imported));
                foreach (var script in imported) _customScripts.Add(script);
                await AppDialog.ShowAsync(Window.GetWindow(this), L.Get("Scripts:ScriptFilesImported"),
                    L.Format("Scripts:ScriptFilesImportedMessage", imported.Count));
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Scripts:ScriptImportFailed"), ex.Message);
            }
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


        private async Task ExecuteCustomScript(CustomScript script, CancellationToken cancellationToken)
        {
            if (script.RequiresAdministrator && !await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    L.Get("Scripts:AdministratorScript"),
                    L.Format("Scripts:AdministratorScriptMessage", script.Name),
                    L.Get("Scripts:Run"),
                    L.Get("Scripts:Cancel"))) return;

            var result = await _powerShellService.ExecuteCustomScriptAsync(script, cancellationToken);
            var details = string.Join("\n", new[] { result.Output.Trim(), result.Error.Trim() }.Where(s => s.Length > 0));
            var summary = result.Success
                ? L.Format("Scripts:CustomScriptCompleted", result.Duration.TotalSeconds)
                : L.Format("Scripts:CustomScriptFailed", result.ExitCode, result.Duration.TotalSeconds);

            if (result.Success)
                await AppDialog.ShowAsync(Window.GetWindow(this), script.Name, $"{summary}\n\n{details}".Trim());
            else
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), script.Name, $"{summary}\n\n{details}".Trim());
        }

        private async void EditCustomScript(CustomScript script, bool isNew = false)
        {
            if (new ScriptEditorDialog(script, isNew) { Owner = Window.GetWindow(this) }.ShowDialog() != true) return;
            if (isNew) _customScripts.Add(script);
            _userDataService.SaveCustomScripts(_customScripts);
            CustomScriptsControl.Items.Refresh();
            await AppDialog.ShowAsync(
                Window.GetWindow(this),
                L.Get(isNew ? "Scripts:ScriptCreated" : "Scripts:ScriptUpdated"),
                L.Format(isNew ? "Scripts:ScriptCreatedMessage" : "Scripts:ScriptUpdatedMessage", script.Name));
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
                _favoriteScriptKeys.Remove(FavoriteKey(toRemove));
                _userDataService.SaveFavoriteScripts(_favoriteScriptKeys);

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
            "gaming-runtimes" => @"
$packages = @(
    'Microsoft.VCRedist.2005.x86', 'Microsoft.VCRedist.2005.x64',
    'Microsoft.VCRedist.2008.x86', 'Microsoft.VCRedist.2008.x64',
    'Microsoft.VCRedist.2010.x86', 'Microsoft.VCRedist.2010.x64',
    'Microsoft.VCRedist.2012.x86', 'Microsoft.VCRedist.2012.x64',
    'Microsoft.VCRedist.2013.x86', 'Microsoft.VCRedist.2013.x64',
    'Microsoft.VCRedist.2015+.x86', 'Microsoft.VCRedist.2015+.x64',
    'Microsoft.DirectX', 'CreativeTechnology.OpenAL', 'Microsoft.XNARedist',
    'Nvidia.PhysX', 'Nvidia.PhysXLegacy'
)
foreach ($package in $packages) {
    Write-Output ""Installing $package...""
    & winget.exe install --id $package --exact --silent --accept-source-agreements --accept-package-agreements --disable-interactivity
    if ($LASTEXITCODE -ne 0) { throw ""$package failed with exit code $LASTEXITCODE"" }
}
",
            "dotnet-developer-setup" => @"
$packages = 'Git.Git', 'Microsoft.DotNet.SDK.10'
foreach ($package in $packages) {
    Write-Output ""Installing $package...""
    & winget.exe install --id $package --exact --silent --accept-source-agreements --accept-package-agreements --disable-interactivity
    if ($LASTEXITCODE -ne 0) { throw ""$package failed with exit code $LASTEXITCODE"" }
}
Write-Output 'Installing Visual Studio Community with the .NET desktop workload...'
& winget.exe install --id Microsoft.VisualStudio.2022.Community --exact --silent --accept-source-agreements --accept-package-agreements --disable-interactivity --override '--wait --quiet --add Microsoft.VisualStudio.Workload.ManagedDesktop --includeRecommended'
if ($LASTEXITCODE -ne 0) { throw ""Visual Studio failed with exit code $LASTEXITCODE"" }
",
            "change-local-password" => @"
try {
    $targetUser = if ($env:TWEAKHUB_TARGET_USER) { $env:TWEAKHUB_TARGET_USER } else { $env:USERNAME }
    $account = Get-LocalUser -Name $targetUser -ErrorAction Stop
    if ($account.PrincipalSource -ne 'Local') { throw 'The current account is not a local Windows account.' }
    & net.exe user $targetUser '*'
    if ($LASTEXITCODE -ne 0) { throw ""net user failed with exit code $LASTEXITCODE"" }
    Write-Host '`nPassword changed successfully.' -ForegroundColor Green
} catch {
    Write-Host ""`n$($_.Exception.Message)"" -ForegroundColor Red
    Read-Host 'Press Enter to close'
    exit 1
}
Read-Host 'Press Enter to close'
",
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
            "temp-cleanup" => @"
$ErrorActionPreference = 'Stop'
# Only files untouched for a day, so running installers and apps keep their current temp files.
$cutoff = (Get-Date).AddDays(-1)
$script:freed = 0
function Clear-Folder([string]$path) {
    foreach ($item in Get-ChildItem -LiteralPath $path -Force -ErrorAction SilentlyContinue) {
        # Never follow junctions or symbolic links out of the temp folder.
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        if ($item.PSIsContainer) {
            Clear-Folder $item.FullName
            try { [IO.Directory]::Delete($item.FullName) } catch { }
        } elseif ($item.LastWriteTime -lt $cutoff) {
            try { Remove-Item -LiteralPath $item.FullName -Force -ErrorAction Stop; $script:freed += $item.Length } catch { }
        }
    }
}
foreach ($folder in @([IO.Path]::GetTempPath(), (Join-Path $env:SystemRoot 'Temp')) | Select-Object -Unique) {
    if (Test-Path -LiteralPath $folder) { Clear-Folder $folder }
}
Delete-DeliveryOptimizationCache -Force -ErrorAction SilentlyContinue
'Freed {0:N1} MB of temporary files. Files in use or newer than one day were kept.' -f ($script:freed / 1MB)
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
            "microsoft-store-repair" => @"
# Source: https://support.microsoft.com/account-billing/microsoft-store-doesn-t-open-126a875d-8b72-def1-0af6-d325276a058b
function Test-Store {
    $package = Get-AppxPackage Microsoft.WindowsStore -ErrorAction SilentlyContinue
    if (-not $package -or $package.Status -ne 'Ok' -or -not (Test-Path (Join-Path $package.InstallLocation 'AppxManifest.xml'))) { return $false }
    Start-Process 'ms-windows-store:'
    Start-Sleep -Seconds 5
    return [bool](Get-Process WinStore.App -ErrorAction SilentlyContinue)
}
if (Test-Store) { Write-Output 'VERIFIED:Microsoft Store is registered and opens.'; return }
& wsreset.exe
if (Test-Store) { Write-Output 'VERIFIED:Microsoft Store cache was reset and the app opens.'; return }
$package = Get-AppxPackage Microsoft.WindowsStore -ErrorAction SilentlyContinue
if ($package) { $package | Reset-AppxPackage }
if (Test-Store) { Write-Output 'VERIFIED:Microsoft Store was reset and opens.'; return }
if ($package) { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $package.InstallLocation 'AppxManifest.xml') }
if (-not (Test-Store)) { throw 'Microsoft Store was reset and re-registered, but it still does not open. Run the DISM + SFC repair next.' }
Write-Output 'VERIFIED:Microsoft Store was re-registered and opens.'
",
            "gaming-services-repair" => @"
# Source: https://support.xbox.com/help/games-apps/troubleshooting/gaming-services-repair-tool
function Test-GamingServices {
    $package = Get-AppxPackage Microsoft.GamingServices -ErrorAction SilentlyContinue
    $services = @(Get-Service GamingServices,GamingServicesNet -ErrorAction SilentlyContinue)
    return $package -and $package.Status -eq 'Ok' -and $services.Count -eq 2
}
if (Test-GamingServices) { Write-Output 'VERIFIED:Xbox Gaming Services package and services are registered.'; return }
$tool = Join-Path $env:TEMP 'TweakHub-GamingRepairTool.exe'
try {
    Invoke-WebRequest 'https://aka.ms/GamingRepairTool' -OutFile $tool -UseBasicParsing
    $signature = Get-AuthenticodeSignature $tool
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft') {
        throw 'The Gaming Services Repair Tool does not have the expected valid Microsoft signature.'
    }
    $process = Start-Process $tool -Wait -PassThru
    if ($process.ExitCode -notin 0, 3010) { throw ""Gaming Services Repair Tool failed with exit code $($process.ExitCode)."" }
    if (Test-GamingServices) { Write-Output 'VERIFIED:Xbox Gaming Services package and services were repaired.'; return }
    'Microsoft.GamingServices','Microsoft.GamingApp' | ForEach-Object {
        Get-AppxPackage $_ -ErrorAction SilentlyContinue | Reset-AppxPackage
    }
    $process = Start-Process $tool -Wait -PassThru
    if ($process.ExitCode -notin 0, 3010) { throw ""Gaming Services Repair Tool retry failed with exit code $($process.ExitCode)."" }
    if (-not (Test-GamingServices)) { throw 'Gaming Services is still unavailable. Restart Windows, then run this fix again.' }
    Write-Output 'VERIFIED:Xbox Gaming Services packages were reset and both services are registered.'
} finally {
    Remove-Item $tool -Force -ErrorAction SilentlyContinue
}
",
            "print-spooler-repair" => @"
# Source: https://support.microsoft.com/windows/hardware/printer/fix-printer-connection-and-printing-problems-in-windows
Get-Service Spooler -ErrorAction Stop | Out-Null
Stop-Service Spooler -Force
$queue = Join-Path $env:WINDIR 'System32\spool\PRINTERS'
Get-ChildItem $queue -Force -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction Stop
Set-Service Spooler -StartupType Automatic
Start-Service Spooler
if ((Get-Service Spooler).Status -ne 'Running' -or (Get-ChildItem $queue -Force -ErrorAction SilentlyContinue)) {
    throw 'Print Spooler did not return to a running state with an empty queue.'
}
Write-Output 'VERIFIED:Print Spooler is running and the print queue is empty.'
",
            "onedrive-repair" => @"
# Source: https://support.microsoft.com/onedrive/reset-onedrive
$paths = @(
    (Join-Path $env:LOCALAPPDATA 'Microsoft\OneDrive\OneDrive.exe'),
    (Join-Path $env:ProgramFiles 'Microsoft OneDrive\OneDrive.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Microsoft OneDrive\OneDrive.exe')
)
$client = $paths | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $client) { throw 'OneDrive is not installed.' }
Stop-Process -Name OneDrive -Force -ErrorAction SilentlyContinue
$reset = Start-Process $client -ArgumentList '/reset' -Wait -PassThru
if ($reset.ExitCode -ne 0) { throw ""OneDrive reset failed with exit code $($reset.ExitCode)."" }
Start-Process $client
Start-Sleep -Seconds 8
if (-not (Get-Process OneDrive -ErrorAction SilentlyContinue)) { throw 'OneDrive was reset but did not restart.' }
Write-Output 'VERIFIED:OneDrive was reset and restarted. Sync connections will be rebuilt.'
",
            "wsl-setup" => @"
# Source: https://learn.microsoft.com/windows/wsl/install
$features = 'Microsoft-Windows-Subsystem-Linux', 'VirtualMachinePlatform'
$restartRequired = $false
foreach ($feature in $features) {
    $state = (Get-WindowsOptionalFeature -Online -FeatureName $feature).State
    if ($state -ne 'Enabled') {
        $result = Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart
        $restartRequired = $restartRequired -or $result.RestartNeeded
    }
}
& wsl.exe --update
if ($LASTEXITCODE -ne 0 -and -not $restartRequired) { throw ""WSL update failed with exit code $LASTEXITCODE."" }
& wsl.exe --set-default-version 2
if ($LASTEXITCODE -ne 0 -and -not $restartRequired) { throw ""Setting WSL 2 as default failed with exit code $LASTEXITCODE."" }
$distros = @(& wsl.exe --list --quiet) -replace ""`0"", ''
if (-not ($distros -match '^Ubuntu')) {
    & wsl.exe --install --distribution Ubuntu --no-launch
    if ($LASTEXITCODE -ne 0 -and -not $restartRequired) { throw ""Ubuntu installation failed with exit code $LASTEXITCODE."" }
}
if ($restartRequired) { Write-Output 'VERIFIED:WSL features were enabled. Restart Windows to finish setup, then launch Ubuntu once.'; return }
& wsl.exe --status | Out-Null
if ($LASTEXITCODE -ne 0 -or -not ((@(& wsl.exe --list --quiet) -replace ""`0"", '') -match '^Ubuntu')) {
    throw 'WSL commands completed, but WSL 2 or Ubuntu could not be verified.'
}
Write-Output 'VERIFIED:WSL 2 is available and Ubuntu is registered.'
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
            else if (script.Id == "change-local-password") ExecuteInteractiveBuiltInScript(script);
            else ExecuteBuiltInScript(script);
        }

        private async void ExecuteInteractiveBuiltInScript(BuiltInScriptCard script)
        {
            if (!await AppDialog.ConfirmAsync(
                    Window.GetWindow(this), script.Name, script.Confirmation,
                    script.ExecuteText, L.Get("Scripts:Cancel"))) return;

            try
            {
                var command = $"$env:TWEAKHUB_TARGET_USER = {PowerShellService.Literal(Environment.UserName)}{Environment.NewLine}{GetBuiltInScript(script.Id)}";
                using var process = Process.Start(CreateInteractivePowerShellStartInfo(
                    command, script.RequiresAdministrator))
                    ?? throw new InvalidOperationException("Failed to start PowerShell.");
                await process.WaitForExitAsync();
                if (process.ExitCode == 0) MarkScriptCompleted(script);
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(
                    Window.GetWindow(this), L.Format("Scripts:NamedScriptFailed", script.Name), ex.Message);
            }
        }

        internal static ProcessStartInfo CreateInteractivePowerShellStartInfo(string script, bool requireAdministrator) => new()
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {Convert.ToBase64String(Encoding.Unicode.GetBytes(script))}",
            UseShellExecute = true,
            Verb = requireAdministrator ? "runas" : string.Empty,
            WindowStyle = ProcessWindowStyle.Normal
        };

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
                var result = await _powerShellService.ExecuteScriptAsync(System.IO.File.ReadAllText(scriptPath), true, TimeSpan.FromMinutes(20));
                var success = result.Success &&
                              result.Output.Contains("VERIFIED:");
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
