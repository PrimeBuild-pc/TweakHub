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
            string Confirmation, string CompletionNote = "")
            : System.ComponentModel.INotifyPropertyChanged
        {
            public bool RequiresAdministrator => BuiltInScripts.All[Id].Administrator;
            public int TimeoutMinutes => BuiltInScripts.All[Id].Minutes;
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
                L.Get("Scripts:winget_Category"), L.Get("Scripts:winget_Execute"),
                L.Get("Scripts:winget_Confirmation")),
            new("gaming-runtimes", "\uE7FC", L.Get("Scripts:gaming_runtimes_Name"),
                L.Get("Scripts:gaming_runtimes_Description"),
                L.Get("Scripts:gaming_runtimes_Category"), L.Get("Scripts:Install"),
                L.Get("Scripts:gaming_runtimes_Confirmation")),
            new("dotnet-developer-setup", "\uE943", L.Get("Scripts:dotnet_developer_setup_Name"),
                L.Get("Scripts:dotnet_developer_setup_Description"),
                L.Get("Scripts:dotnet_developer_setup_Category"), L.Get("Scripts:Install"),
                L.Get("Scripts:dotnet_developer_setup_Confirmation")),
            new("ctt-winutil", "\uE756", L.Get("Scripts:ctt_winutil_Name"),
                L.Get("Scripts:ctt_winutil_Description"),
                L.Get("Scripts:ctt_winutil_Category"), L.Get("Scripts:ctt_winutil_Execute"),
                L.Get("Scripts:ctt_winutil_Confirmation")),
            new("change-local-password", "\uE72E", L.Get("Scripts:change_local_password_Name"),
                L.Get("Scripts:change_local_password_Description"),
                L.Get("Scripts:CurrentUser"), L.Get("Scripts:Run"),
                L.Get("Scripts:change_local_password_Confirmation")),
            new("dism-sfc-chkdsk", "\uE90F", L.Get("Scripts:dism_sfc_chkdsk_Name"),
                L.Get("Scripts:dism_sfc_chkdsk_Description"),
                L.Get("Scripts:dism_sfc_chkdsk_Category"), L.Get("Scripts:dism_sfc_chkdsk_Execute"),
                L.Get("Scripts:dism_sfc_chkdsk_Confirmation"),
                L.Get("Scripts:dism_sfc_chkdsk_CompletionNote")),
            new("component-cleanup", "\uE74D", L.Get("Scripts:component_cleanup_Name"),
                L.Get("Scripts:component_cleanup_Description"),
                L.Get("Scripts:component_cleanup_Category"), L.Get("Scripts:component_cleanup_Execute"),
                L.Get("Scripts:component_cleanup_Confirmation")),
            new("temp-cleanup", "", L.Get("Scripts:temp_cleanup_Name"),
                L.Get("Scripts:temp_cleanup_Description"),
                L.Get("Scripts:temp_cleanup_Category"), L.Get("Scripts:temp_cleanup_Execute"),
                L.Get("Scripts:temp_cleanup_Confirmation")),
            new("shader-cache-cleanup", "\uE7F4", L.Get("Scripts:shader_cache_cleanup_Name"),
                L.Get("Scripts:shader_cache_cleanup_Description"),
                L.Get("Scripts:temp_cleanup_Category"), L.Get("Scripts:temp_cleanup_Execute"),
                L.Get("Scripts:shader_cache_cleanup_Confirmation")),
            new("driver-backup", "\uE74E", L.Get("Scripts:driver_backup_Name"),
                L.Get("Scripts:driver_backup_Description"),
                L.Get("Scripts:temp_cleanup_Category"), L.Get("Scripts:Run"),
                L.Get("Scripts:driver_backup_Confirmation"), L.Get("Scripts:driver_backup_CompletionNote")),
            new("driver-restore", "", L.Get("Scripts:driver_restore_Name"),
                L.Get("Scripts:driver_restore_Description"),
                L.Get("Scripts:temp_cleanup_Category"), L.Get("Scripts:driver_restore_Execute"),
                L.Get("Scripts:driver_restore_Confirmation"), L.Get("Scripts:driver_restore_CompletionNote")),
            new("network-reset", "\uE968", L.Get("Scripts:network_reset_Name"),
                L.Get("Scripts:network_reset_Description"),
                L.Get("Scripts:network_reset_Category"), L.Get("Scripts:network_reset_Execute"),
                L.Get("Scripts:network_reset_Confirmation"),
                L.Get("Scripts:network_reset_CompletionNote")),
            new("windows-update-reset", "\uE895", L.Get("Scripts:windows_update_reset_Name"),
                L.Get("Scripts:windows_update_reset_Description"),
                L.Get("Scripts:windows_update_reset_Category"), L.Get("Scripts:windows_update_reset_Execute"),
                L.Get("Scripts:windows_update_reset_Confirmation")),
            new("microsoft-store-repair", "\uE719", L.Get("Scripts:microsoft_store_repair_Name"),
                L.Get("Scripts:microsoft_store_repair_Description"),
                L.Get("Scripts:dism_sfc_chkdsk_Category"), L.Get("Scripts:dism_sfc_chkdsk_Execute"),
                L.Get("Scripts:microsoft_store_repair_Confirmation")),
            new("gaming-services-repair", "\uE7FC", L.Get("Scripts:gaming_services_repair_Name"),
                L.Get("Scripts:gaming_services_repair_Description"),
                L.Get("Scripts:gaming_runtimes_Category"), L.Get("Scripts:dism_sfc_chkdsk_Execute"),
                L.Get("Scripts:gaming_services_repair_Confirmation")),
            new("print-spooler-repair", "\uE749", L.Get("Scripts:print_spooler_repair_Name"),
                L.Get("Scripts:print_spooler_repair_Description"),
                L.Get("Scripts:dism_sfc_chkdsk_Category"), L.Get("Scripts:dism_sfc_chkdsk_Execute"),
                L.Get("Scripts:print_spooler_repair_Confirmation")),
            new("onedrive-repair", "\uE753", L.Get("Scripts:onedrive_repair_Name"),
                L.Get("Scripts:onedrive_repair_Description"),
                L.Get("Scripts:dism_sfc_chkdsk_Category"), L.Get("Scripts:dism_sfc_chkdsk_Execute"),
                L.Get("Scripts:onedrive_repair_Confirmation")),
            new("wsl-setup", "\uE756", L.Get("Scripts:wsl_setup_Name"),
                L.Get("Scripts:wsl_setup_Description"),
                L.Get("Scripts:dotnet_developer_setup_Category"), L.Get("Scripts:Install"),
                L.Get("Scripts:wsl_setup_Confirmation"), L.Get("Scripts:wsl_setup_CompletionNote")),
            new("empty-standby-list", "\uE950", L.Get("Scripts:empty_standby_list_Name"),
                L.Get("Scripts:empty_standby_list_Description"),
                L.Get("Scripts:empty_standby_list_Category"), L.Get("Scripts:empty_standby_list_Execute"),
                L.Get("Scripts:empty_standby_list_Confirmation")),
            new("remove-windows-ai", "\uE99A", L.Get("Scripts:remove_windows_ai_Name"),
                L.Get("Scripts:remove_windows_ai_Description"),
                L.Get("Scripts:remove_windows_ai_Category"), L.Get("Scripts:remove_windows_ai_Execute"),
                L.Get("Scripts:remove_windows_ai_Confirmation")),
            new("adobe-hosts-block", "\uE968", L.Get("Scripts:adobe_hosts_block_Name"),
                L.Get("Scripts:adobe_hosts_block_Description"),
                L.Get("Scripts:adobe_hosts_block_Category"), L.Get("Scripts:adobe_hosts_block_Execute"),
                L.Get("Scripts:adobe_hosts_block_Confirmation")),
            new("adobe-hosts-unblock", "\uE777", L.Get("Scripts:adobe_hosts_unblock_Name"),
                L.Get("Scripts:adobe_hosts_unblock_Description"),
                L.Get("Scripts:adobe_hosts_unblock_Category"), L.Get("Scripts:adobe_hosts_unblock_Execute"),
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
                // playbooks.json can change while UAC is open: run unasked only what the user confirmed.
                if (_playbooks.FirstOrDefault(playbook => playbook.Id == id) is { } playbook)
                    await RunPlaybookAsync(playbook, confirmed: PlaybookService.Fingerprint(playbook) == App.StartupPlaybookFingerprint);
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
                var package = PlaybookService.Instance.Inspect(dialog.FileName);
                var code = PlaybookService.DescribeNewCode(package);
                if (code.Length > 0 && !await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Scripts:ImportPlaybook"),
                        L.Format("Scripts:PlaybookImportReview", package.Playbook.Name, code),
                        L.Get("Scripts:ImportPlaybook"), L.Get("Scripts:Cancel"), DialogKind.Warning)) return;
                var result = PlaybookService.Instance.Import(package);
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
                    && App.RestartElevated($"--run-playbook {playbook.Id} {PlaybookService.Fingerprint(playbook)}"))
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
                card.SetCompletion(ScriptHistoryService.Instance.TryGetCompletion(card.Id, BuiltInScripts.Body(card.Id), out var completedAt)
                    ? completedAt
                    : null);
        }

        private void MarkScriptCompleted(BuiltInScriptCard card)
        {
            try
            {
                var script = BuiltInScripts.Body(card.Id);
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
                textBox.Text = BuiltInScripts.Body(script.Id);
            }
            catch (Exception ex)
            {
                textBox.Text = L.Format("Scripts:ScriptLoadError", ex.Message);
            }
            preview.Visibility = preview.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BuiltInScript_Execute_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: BuiltInScriptCard script }) return;
            if (script.Id == "winget") WinGetInstallation_Click(sender, e);
            else if (script.Id == "change-local-password") ExecuteInteractiveBuiltInScript(script);
            else if (script.Id == "driver-restore") RestoreDrivers(script);
            else ExecuteBuiltInScript(script);
        }

        private async void RestoreDrivers(BuiltInScriptCard script)
        {
            var dialog = new OpenFolderDialog { Title = script.Name };
            if (Directory.Exists(BuiltInScripts.DriverBackupPath)) dialog.InitialDirectory = BuiltInScripts.DriverBackupPath;
            if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
            int packages;
            try { packages = Directory.EnumerateFiles(dialog.FolderName, "*.inf", SearchOption.AllDirectories).Count(); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), script.Name, ex.Message);
                return;
            }
            if (packages == 0)
            {
                await AppDialog.ShowWarningAsync(Window.GetWindow(this), script.Name, L.Format("Scripts:driver_restore_NoPackages", dialog.FolderName));
                return;
            }
            ExecuteBuiltInScript(script, BuiltInScripts.DriverRestoreScript(dialog.FolderName),
                L.Format("Scripts:driver_restore_ConfirmFolder", packages, dialog.FolderName) + "\n\n" + script.Confirmation);
        }

        private async void ExecuteInteractiveBuiltInScript(BuiltInScriptCard script)
        {
            if (!await AppDialog.ConfirmAsync(
                    Window.GetWindow(this), script.Name, script.Confirmation,
                    script.ExecuteText, L.Get("Scripts:Cancel"))) return;

            try
            {
                var command = string.Join(Environment.NewLine, PowerShellService.SystemFolders, PowerShellService.SystemModulesOnly,
                    $"$env:TWEAKHUB_TARGET_USER = {PowerShellService.Literal(Environment.UserName)}", BuiltInScripts.Body(script.Id));
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
            FileName = SystemTools.PowerShell,
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

        /// <param name="content">Script to run instead of the card's own (driver restore passes the chosen folder).</param>
        private async void ExecuteBuiltInScript(BuiltInScriptCard script, string? content = null, string? confirmation = null)
        {
            if (!await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    script.Name,
                    confirmation ?? script.Confirmation,
                    script.ExecuteText,
                    L.Get("Scripts:Cancel"))) return;

            var progress = new ProgressWindow(script.Name);
            progress.Show();
            progress.UpdateStatus(L.Get("Scripts:RunningCommands"));
            progress.UpdateProgress(10);

            try
            {
                var result = await _powerShellService.ExecuteScriptAsync(
                    content ?? BuiltInScripts.Body(script.Id),
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
