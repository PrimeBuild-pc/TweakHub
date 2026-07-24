using System.Collections.ObjectModel;
using System.IO;
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
        private sealed record TweakChoice(string ReferenceId, string Name)
        {
            public override string ToString() => Name;
        }

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
            PlaybooksControl.ItemsSource = _playbooks;
            try { AppDataPath.EnsureAppsDirectory(); } catch { }
            LoadCustomScripts();
            LoadFavoriteScripts();
            LoadPlaybooks();
            LoadScriptHistory();
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

        private void NewPlaybook_Click(object sender, RoutedEventArgs e) => EditPlaybook();

        private void EditPlaybook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: Playbook playbook }) EditPlaybook(playbook);
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

        private async void RunPlaybook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Playbook playbook }) return;
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
            var preview = string.Join(Environment.NewLine, preflight.Lines);
            if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), playbook.Name,
                    L.Format("Scripts:PlaybookRunConfirmation", preview), L.Get("Scripts:Run"), L.Get("Scripts:Cancel"))) return;

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

        private void EditPlaybook(Playbook? existing = null)
        {
            var working = new Playbook
            {
                Id = existing?.Id ?? Guid.NewGuid().ToString("N"),
                Name = existing?.Name ?? string.Empty,
                Description = existing?.Description ?? string.Empty,
                Steps = existing?.Steps.Select(CloneStep).ToList() ?? []
            };
            var steps = new ObservableCollection<PlaybookStep>(working.Steps);
            var dialog = CreateEditorWindow(L.Get(existing == null ? "Scripts:CreatePlaybook" : "Scripts:EditPlaybook"), 720, 620);
            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var name = new TextBox { Text = working.Name, Margin = new Thickness(0, 4, 0, 10) };
            System.Windows.Automation.AutomationProperties.SetName(name, L.Get("Scripts:PlaybookName"));
            var namePanel = new StackPanel();
            namePanel.Children.Add(new TextBlock { Text = L.Get("Scripts:PlaybookName") });
            namePanel.Children.Add(name);
            grid.Children.Add(namePanel);
            var description = new TextBox { Text = working.Description, Margin = new Thickness(0, 4, 0, 12) };
            System.Windows.Automation.AutomationProperties.SetName(description, L.Get("Scripts:PlaybookDescription"));
            var descriptionPanel = new StackPanel();
            descriptionPanel.Children.Add(new TextBlock { Text = L.Get("Scripts:PlaybookDescription") });
            descriptionPanel.Children.Add(description);
            Grid.SetRow(descriptionPanel, 1);
            grid.Children.Add(descriptionPanel);
            var list = new ListBox { ItemsSource = steps, DisplayMemberPath = nameof(PlaybookStep.Summary), Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(list, 2);
            grid.Children.Add(list);

            var stepButtons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
            Button AddButton(string text, RoutedEventHandler click)
            {
                var button = new Button { Content = text, Style = GetStyleOrDefault("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
                button.Click += click;
                stepButtons.Children.Add(button);
                return button;
            }
            AddButton(L.Get("Scripts:AddTweakStep"), (_, _) => AddTweakStep(dialog, steps));
            AddButton(L.Get("Scripts:AddApplicationStep"), (_, _) => AddApplicationStep(dialog, steps));
            AddButton(L.Get("Scripts:AddScriptStep"), (_, _) => AddScriptStep(dialog, steps));
            AddButton(L.Get("Scripts:MoveUp"), (_, _) => MoveStep(steps, list.SelectedIndex, -1));
            AddButton(L.Get("Scripts:MoveDown"), (_, _) => MoveStep(steps, list.SelectedIndex, 1));
            AddButton(L.Get("Scripts:RemoveStep"), (_, _) => { if (list.SelectedItem is PlaybookStep step) steps.Remove(step); });
            Grid.SetRow(stepButtons, 3);
            grid.Children.Add(stepButtons);

            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancel = new Button { Content = L.Get("Scripts:Cancel"), Style = GetStyleOrDefault("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
            var save = new Button { Content = L.Get("Scripts:Save"), Style = GetStyleOrDefault("ExecuteButtonStyle") };
            cancel.Click += (_, _) => dialog.Close();
            save.Click += async (_, _) =>
            {
                working.Name = name.Text.Trim();
                working.Description = description.Text.Trim();
                working.Steps = steps.ToList();
                try
                {
                    UserDataService.ValidatePlaybook(working);
                    var saved = _playbooks.Select(playbook => playbook == existing ? working : playbook).ToList();
                    if (existing == null) saved.Add(working);
                    _userDataService.SavePlaybooks(saved);
                    if (existing == null) _playbooks.Add(working);
                    else
                    {
                        existing.Name = working.Name;
                        existing.Description = working.Description;
                        existing.Steps = working.Steps;
                    }
                    PlaybooksControl.Items.Refresh();
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    await AppDialog.ShowWarningAsync(dialog, L.Get("Scripts:PlaybookInvalid"), ex.Message);
                }
            };
            actions.Children.Add(cancel);
            actions.Children.Add(save);
            Grid.SetRow(actions, 4);
            grid.Children.Add(actions);
            dialog.Content = grid;
            dialog.ShowDialog();
        }

        private void AddTweakStep(Window owner, ObservableCollection<PlaybookStep> steps)
        {
            if (TweakService.Instance.TweakCategories.Count == 0) TweakService.Instance.LoadTweaks();
            var choices = TweakService.Instance.TweakCategories.SelectMany(category => category.Tweaks)
                .Select(tweak => new TweakChoice($"builtin:{tweak.Id}", tweak.Name))
                .Concat(_userDataService.LoadCustomTweaks().Select(tweak => new TweakChoice($"custom:{tweak.Id}", tweak.Name)))
                .OrderBy(choice => choice.Name).ToList();
            var dialog = CreateEditorWindow(L.Get("Scripts:AddTweakStep"), 480, 240, owner);
            var panel = new StackPanel { Margin = new Thickness(20) };
            var combo = new ComboBox { ItemsSource = choices, SelectedIndex = choices.Count > 0 ? 0 : -1, Margin = new Thickness(0, 0, 0, 12) };
            System.Windows.Automation.AutomationProperties.SetName(combo, L.Get("Scripts:AddTweakStep"));
            var enabled = new CheckBox { Content = L.Get("Scripts:TweakTargetEnabled"), IsChecked = true, Margin = new Thickness(0, 0, 0, 16) };
            var add = new Button { Content = L.Get("Scripts:Add"), Style = GetStyleOrDefault("ExecuteButtonStyle"), HorizontalAlignment = HorizontalAlignment.Right };
            add.Click += (_, _) =>
            {
                if (combo.SelectedItem is not TweakChoice choice) return;
                steps.Add(new PlaybookStep { Type = PlaybookStepType.Tweak, ReferenceId = choice.ReferenceId, Name = choice.Name, TargetEnabled = enabled.IsChecked == true });
                dialog.Close();
            };
            panel.Children.Add(combo); panel.Children.Add(enabled); panel.Children.Add(add);
            dialog.Content = panel;
            dialog.ShowDialog();
        }

        private void AddApplicationStep(Window owner, ObservableCollection<PlaybookStep> steps)
        {
            if (ShortcutService.Instance.ExternalTools.Count == 0) ShortcutService.Instance.Initialize();
            var available = ShortcutService.Instance.ExternalTools.Where(tool => tool.WingetId.Length > 0)
                .OrderBy(tool => tool.Name).ToList();
            var dialog = CreateEditorWindow(L.Get("Scripts:AddApplicationStep"), 500, 420, owner);
            var panel = new StackPanel { Margin = new Thickness(20) };
            var catalogue = new ComboBox
            {
                ItemsSource = available,
                DisplayMemberPath = nameof(ExternalTool.Name),
                Margin = new Thickness(0, 0, 0, 10),
                ToolTip = L.Get("Scripts:SelectCatalogueApplication")
            };
            var name = new TextBox { Margin = new Thickness(0, 4, 0, 10) };
            var winget = new TextBox { Margin = new Thickness(0, 4, 0, 16) };
            System.Windows.Automation.AutomationProperties.SetName(name, L.Get("Scripts:ApplicationName"));
            System.Windows.Automation.AutomationProperties.SetName(winget, L.Get("Scripts:WingetPackageId"));
            catalogue.SelectionChanged += (_, _) =>
            {
                if (catalogue.SelectedItem is not ExternalTool tool) return;
                name.Text = tool.Name;
                winget.Text = tool.WingetId;
            };
            var add = new Button { Content = L.Get("Scripts:Add"), Style = GetStyleOrDefault("ExecuteButtonStyle"), HorizontalAlignment = HorizontalAlignment.Right };
            add.Click += async (_, _) =>
            {
                try { UserDataService.ValidateWingetId(winget.Text.Trim()); }
                catch (Exception ex) { await AppDialog.ShowWarningAsync(dialog, L.Get("Scripts:PlaybookInvalid"), ex.Message); return; }
                if (string.IsNullOrWhiteSpace(name.Text)) return;
                steps.Add(new PlaybookStep { Type = PlaybookStepType.Winget, Name = name.Text.Trim(), WingetId = winget.Text.Trim() });
                dialog.Close();
            };
            panel.Children.Add(new TextBlock { Text = L.Get("Scripts:SelectCatalogueApplication"), Margin = new Thickness(0, 0, 0, 4) });
            panel.Children.Add(catalogue);
            panel.Children.Add(new TextBlock { Text = L.Get("Scripts:ApplicationName") });
            panel.Children.Add(name);
            panel.Children.Add(new TextBlock { Text = L.Get("Scripts:WingetPackageId") });
            panel.Children.Add(winget);
            panel.Children.Add(add);
            dialog.Content = panel;
            dialog.ShowDialog();
        }

        private void AddScriptStep(Window owner, ObservableCollection<PlaybookStep> steps)
        {
            var scripts = _customScripts.ToList();
            var dialog = CreateEditorWindow(L.Get("Scripts:AddScriptStep"), 480, 210, owner);
            var panel = new StackPanel { Margin = new Thickness(20) };
            var combo = new ComboBox { ItemsSource = scripts, DisplayMemberPath = nameof(CustomScript.Name), SelectedIndex = scripts.Count > 0 ? 0 : -1, Margin = new Thickness(0, 0, 0, 16) };
            System.Windows.Automation.AutomationProperties.SetName(combo, L.Get("Scripts:AddScriptStep"));
            var add = new Button { Content = L.Get("Scripts:Add"), Style = GetStyleOrDefault("ExecuteButtonStyle"), HorizontalAlignment = HorizontalAlignment.Right };
            add.Click += (_, _) =>
            {
                if (combo.SelectedItem is not CustomScript script) return;
                steps.Add(new PlaybookStep { Type = PlaybookStepType.Script, ReferenceId = script.Id, Name = script.Name });
                dialog.Close();
            };
            panel.Children.Add(combo); panel.Children.Add(add);
            dialog.Content = panel;
            dialog.ShowDialog();
        }

        private static void MoveStep(ObservableCollection<PlaybookStep> steps, int index, int direction)
        {
            var target = index + direction;
            if (index < 0 || target < 0 || target >= steps.Count) return;
            steps.Move(index, target);
        }

        private static PlaybookStep CloneStep(PlaybookStep step) => new()
        {
            Id = step.Id,
            Type = step.Type,
            ReferenceId = step.ReferenceId,
            Name = step.Name,
            WingetId = step.WingetId,
            TargetEnabled = step.TargetEnabled
        };

        private static Window CreateEditorWindow(string title, double width, double height, Window? owner = null) => new()
        {
            Title = title,
            Width = width,
            Height = height,
            Owner = owner ?? Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = (Brush)Application.Current.FindResource("WindowBackgroundBrush")
        };

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
                    return new CustomScript
                    {
                        Name = System.IO.Path.GetFileNameWithoutExtension(path),
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

        private Style GetStyleOrDefault(string key) => (Style)FindResource(key);

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
