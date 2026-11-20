using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using TweakHub.Localization;
using TweakHub.Models;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views
{
    public partial class ExternalToolsPage : Page
    {
        private sealed class ToolItem : INotifyPropertyChanged
        {
            private readonly Action<ToolItem> _selectionChanged;
            private bool _isInstalled;
            private bool _isSelected;

            public ToolItem(ExternalTool tool, Action<ToolItem> selectionChanged, bool isInstalled, bool isSelected)
            {
                Tool = tool;
                _selectionChanged = selectionChanged;
                _isInstalled = isInstalled;
                _isSelected = isSelected && CanSelect;
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            public ExternalTool Tool { get; }
            public bool IsWinget => !string.IsNullOrWhiteSpace(Tool.WingetId);
            public bool CanSelect => IsWinget && !_isInstalled;
            public bool CanRunAction => !(IsWinget && _isInstalled);
            public bool CanUninstall => IsWinget && _isInstalled;
            public string ActionText => CanRunAction ? GetActionText(Tool) : L.Get("Tools:Installed");
            public string ActionIcon => CanRunAction ? GetDownloadIcon(Tool) : "\uE73E";
            public string ToolIcon => ShortcutService.CategoryIcon(Tool.Category);
            public string AutomationName => $"{Tool.Name}. {ActionText}";
            public string SelectAutomationName => L.Format("Tools:SelectToolNamed", Tool.Name);
            public string EditAutomationName => L.Format("Tools:EditToolNamed", Tool.Name);
            public string DeleteAutomationName => L.Format("Tools:DeleteToolNamed", Tool.Name);
            public string UninstallAutomationName => L.Format("Tools:UninstallNamed", Tool.Name);

            public bool IsInstalled
            {
                get => _isInstalled;
                set
                {
                    if (_isInstalled == value) return;
                    _isInstalled = value;
                    if (_isInstalled) IsSelected = false;
                    Raise(nameof(IsInstalled), nameof(CanSelect), nameof(CanRunAction), nameof(CanUninstall), nameof(ActionText), nameof(ActionIcon), nameof(AutomationName));
                }
            }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected == value || (value && !CanSelect)) return;
                    _isSelected = value;
                    Raise(nameof(IsSelected));
                    _selectionChanged(this);
                }
            }

            private void Raise(params string[] names)
            {
                foreach (var name in names)
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        // Winget ids per preset, checked against the catalogue by a test.
        internal static readonly (string NameKey, string[] WingetIds)[] Presets =
        [
            ("Tools:PresetDiagnostics", ["CPUID.CPU-Z", "CPUID.HWMonitor", "TechPowerUp.GPU-Z", "Microsoft.Sysinternals.ProcessExplorer", "Microsoft.Sysinternals.Autoruns"]),
            ("Tools:PresetLatency", ["Resplendence.LatencyMon", "Microsoft.Sysinternals.RAMMap", "Microsoft.Sysinternals.ProcessMonitor", "Intel.PresentMon", "BitSum.ParkControl"]),
            ("Tools:PresetGpu", ["Guru3D.Afterburner", "Guru3D.RTSS", "TechPowerUp.GPU-Z", "Wagnardsoft.DisplayDriverUninstaller"]),
            ("Tools:PresetUtilities", ["Microsoft.PowerToys", "voidtools.Everything", "AntibodySoftware.WizTree", "ShareX.ShareX"]),
        ];

        private sealed record ToolGroup(
            string Name,
            string Icon,
            string CountText,
            IReadOnlyList<ToolItem> Tools)
        {
            public bool IsExpanded { get; set; } = true;
        }

        private readonly ShortcutService _shortcutService;
        private readonly ToolDownloadService _downloadService;
        private readonly IProgress<ToolProgress> _progress;
        private string _searchQuery = string.Empty;
        private bool _favoritesOnly;
        private readonly string? _onlyCategory;
        private readonly Dictionary<string, ToolItem> _items = new();
        private readonly HashSet<string> _selectedKeys = new();
        private HashSet<string>? _installedIds;

        /// <param name="onlyCategory">Shows one catalog category, hosted inside a section page that already has its own heading.</param>
        public ExternalToolsPage(string? onlyCategory = null)
        {
            _onlyCategory = onlyCategory;
            InitializeComponent();
            if (_onlyCategory != null)
            {
                // Hosted inside SecurityPage, which already shows the heading.
                RootGrid.Margin = new Thickness(0);
                HeaderText.Visibility = Visibility.Collapsed;
                AddToolButton.Visibility = Visibility.Collapsed;
            }
            _shortcutService = ShortcutService.Instance;
            _downloadService = ToolDownloadService.Instance;
            _progress = new Progress<ToolProgress>(OnDownloadProgress);
            Loaded += ExternalToolsPage_Loaded;
        }

        private async void ExternalToolsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ToolsControl.ItemsSource is null)
                CatalogLoading.Show(L.Get("UI:ProgressWindowPleaseWait"));
            try
            {
                await Dispatcher.Yield(DispatcherPriority.Background);
                if (!IsLoaded) return;
                _shortcutService.LoadExternalTools();
                LoadExternalTools();
                _ = RefreshInstalledAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExternalToolsPage load failed: {ex}");
                ShowLoadError();
            }
            finally
            {
                CatalogLoading.Hide();
            }
        }

        private void LoadExternalTools()
        {
            LoadErrorText.Visibility = Visibility.Collapsed;
            ToolsControl.ItemsSource = CreateToolGroups();
        }

        private IReadOnlyList<ToolGroup> CreateToolGroups()
        {
            var groups = new List<ToolGroup>();
            var tools = FilterTools(_shortcutService.ExternalTools, _searchQuery, _favoritesOnly, _onlyCategory).ToList();
            var favorites = tools
                .Where(tool => tool.IsFavorite)
                .OrderBy(tool => tool.Name)
                .ToList();

            if (!_favoritesOnly && favorites.Count > 0)
            {
                groups.Add(new(
                    L.Get("Tools:Favorites"),
                    "\uE734",
                    $"({favorites.Count})",
                    favorites.Select(CreateToolItem).ToList()));
            }

            groups.AddRange(tools
                .OrderByDescending(tool => tool.IsFavorite)
                .ThenBy(tool => tool.Name)
                .GroupBy(tool => tool.Category)
                .OrderBy(group => ShortcutService.CategoryOrder(group.Key))
                .Select(group => new ToolGroup(
                    ShortcutService.LocalizeCategory(group.Key),
                    ShortcutService.CategoryIcon(group.Key),
                    L.Format("Tools:ToolCount", group.Count()),
                        group.Select(CreateToolItem).ToList())));

            return groups;
        }

        // One instance per tool, shared by the Favorites group and its category, so state changes show in both.
        private ToolItem CreateToolItem(ExternalTool tool)
        {
            var key = ShortcutService.FavoriteKey(tool);
            if (!_items.TryGetValue(key, out var item) || !ReferenceEquals(item.Tool, tool))
                _items[key] = item = new ToolItem(tool, OnSelectionChanged, IsInstalled(tool), _selectedKeys.Contains(key));
            return item;
        }

        private bool IsInstalled(ExternalTool tool) =>
            !string.IsNullOrWhiteSpace(tool.WingetId) && _installedIds?.Contains(tool.WingetId) == true;

        private async Task RefreshInstalledAsync()
        {
            _installedIds = await _downloadService.GetInstalledWingetIdsAsync();
            if (!IsLoaded) return;
            foreach (var item in _items.Values)
                item.IsInstalled = IsInstalled(item.Tool);
        }

        private void OnSelectionChanged(ToolItem item)
        {
            var key = ShortcutService.FavoriteKey(item.Tool);
            if (item.IsSelected) _selectedKeys.Add(key); else _selectedKeys.Remove(key);
            UpdateSelectionUi();
        }

        private void UpdateSelectionUi()
        {
            var count = _selectedKeys.Count;
            InstallSelectedButton.Visibility = ClearSelectionButton.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            InstallSelectedText.Text = L.Format("Tools:InstallSelected", count);
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var tool in FilterTools(_shortcutService.ExternalTools, _searchQuery, _favoritesOnly, _onlyCategory))
                CreateToolItem(tool).IsSelected = true;
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items.Values.Where(item => item.IsSelected).ToList())
                item.IsSelected = false;
        }

        private void Presets_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = PlacementMode.Bottom };
            foreach (var (nameKey, ids) in Presets)
            {
                var entry = new MenuItem { Header = L.Get(nameKey) };
                entry.Click += (_, _) =>
                {
                    ClearSelection_Click(this, e);
                    foreach (var tool in _shortcutService.ExternalTools.Where(tool => ids.Contains(tool.WingetId, StringComparer.OrdinalIgnoreCase)))
                        CreateToolItem(tool).IsSelected = true;
                };
                menu.Items.Add(entry);
            }
            menu.IsOpen = true;
        }

        private async void InstallSelected_Click(object sender, RoutedEventArgs e)
        {
            var items = _items.Values.Where(item => item.IsSelected && item.CanSelect).OrderBy(item => item.Tool.Name).ToList();
            if (items.Count == 0) return;

            if (!await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    L.Get("Tools:ConfirmInstallSelected"),
                    L.Format("Tools:ConfirmInstallSelectedMessage", items.Count, string.Join(", ", items.Select(item => item.Tool.Name))),
                    L.Get("Tools:Install"),
                    L.Get("Tools:Cancel")))
                return;

            // The installed inventory can finish while the prompt is open: skip what turned out to be installed.
            items = items.Where(item => item.CanSelect).ToList();
            if (items.Count == 0) return;

            SetBusy(true);
            var installed = 0;
            try
            {
                foreach (var item in items)
                {
                    if (!await _downloadService.InstallWithWinget(item.Tool, _progress)) continue;
                    item.IsInstalled = true;
                    installed++;
                }
            }
            finally
            {
                SetBusy(false);
            }

            _progress.Report(new(string.Empty, installed * 100 / items.Count,
                L.Format("Tools:InstallSelectedDone", installed, items.Count), true, installed == items.Count));
            _ = RefreshInstalledAsync();
        }

        private void SetBusy(bool busy) => ToolsControl.IsEnabled = ToolbarPanel.IsEnabled = !busy;

        internal static IEnumerable<ExternalTool> FilterTools(
            IEnumerable<ExternalTool> tools,
            string searchQuery,
            bool favoritesOnly,
            string? onlyCategory = null)
        {
            if (onlyCategory != null)
                tools = tools.Where(tool => tool.Category.Equals(onlyCategory, StringComparison.OrdinalIgnoreCase));

            if (favoritesOnly)
                tools = tools.Where(tool => tool.IsFavorite);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                tools = tools.Where(tool =>
                    tool.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    || tool.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    || tool.Category.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    || ShortcutService.LocalizeCategory(tool.Category)
                        .Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase));
            }

            return tools;
        }

        private async void ToolAction_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is Button { DataContext: ToolItem item, Tag: Border card })
                await RunToolAsync(card, item);
        }

        private async Task RunToolAsync(Border card, ToolItem item)
        {
            card.IsEnabled = false;
            card.Opacity = 0.7;
            try
            {
                var tool = item.Tool;
                if (await _downloadService.DownloadOrOpenTool(tool, _progress) && item.IsWinget && string.IsNullOrWhiteSpace(tool.PowerShellCommand))
                    item.IsInstalled = true;
            }
            finally
            {
                card.IsEnabled = true;
                card.Opacity = 1;
            }
        }

        private async void FavoriteTool_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.DataContext is not ToolItem item) return;

            item.Tool.IsFavorite = !item.Tool.IsFavorite;
            await PersistFavoritesAsync();
            LoadExternalTools();
        }

        private void EditTool_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.DataContext is ToolItem item)
                EditCustomTool(item.Tool);
        }

        private async void DeleteTool_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.DataContext is not ToolItem item) return;

            var tool = item.Tool;
            if (!await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    L.Get("Tools:DeleteCustomTool"),
                    L.Format("Tools:DeleteToolMessage", tool.Name),
                    L.Get("Tools:Delete"),
                    L.Get("Tools:Cancel")))
                return;

            _shortcutService.DeleteCustomTool(tool);
            await PersistFavoritesAsync();
            LoadExternalTools();
        }

        private async void UninstallTool_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is not Button { DataContext: ToolItem item, Tag: Border card }) return;

            var tool = item.Tool;
            if (!await AppDialog.ConfirmAsync(
                    Window.GetWindow(this),
                    L.Get("Tools:ConfirmUninstall"),
                    L.Format("Tools:ConfirmUninstallMessage", tool.Name),
                    L.Get("Tools:Uninstall"),
                    L.Get("Tools:Cancel")))
                return;

            card.IsEnabled = false;
            card.Opacity = 0.7;
            try
            {
                if (await _downloadService.UninstallWithWinget(tool, _progress))
                    item.IsInstalled = false;
            }
            finally
            {
                card.IsEnabled = true;
                card.Opacity = 1;
            }
        }

        private async Task PersistFavoritesAsync()
        {
            try
            {
                var favorites = _shortcutService.ExternalTools
                    .Where(tool => tool.IsFavorite)
                    .Select(ShortcutService.FavoriteKey)
                    .ToList();
                UserDataService.Instance.SaveFavoriteTools(favorites);
            }
            catch (Exception ex)
            {
                await AppDialog.ShowWarningAsync(
                    Window.GetWindow(this),
                    L.Get("Tools:UnableSaveFavorites"),
                    ex.Message);
            }
        }

        // Rebuilding every card takes tens of milliseconds: a held Backspace queued one rebuild per repeat and froze the window.
        private System.Windows.Threading.DispatcherTimer? _searchTimer;

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = (sender as TextBox)?.Text ?? string.Empty;
            if (_searchTimer is null)
            {
                _searchTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                _searchTimer.Tick += (_, _) => { _searchTimer.Stop(); LoadExternalTools(); };
            }
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void FavoritesFilter_Changed(object sender, RoutedEventArgs e)
        {
            _favoritesOnly = FavoritesOnlyButton.IsChecked == true;
            LoadExternalTools();
        }

        private void AddTool_Click(object sender, RoutedEventArgs e) => EditCustomTool(null);

        private async void EditCustomTool(ExternalTool? tool)
        {
            var dialog = new CustomToolDialog(_shortcutService.GetToolCategories(), tool)
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                _shortcutService.SaveCustomTool(dialog.Tool);
                LoadExternalTools();
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(
                    Window.GetWindow(this),
                    L.Get("Tools:UnableSaveTool"),
                    ex.Message);
            }
        }

        private static string GetDownloadIcon(ExternalTool tool) =>
            !string.IsNullOrWhiteSpace(tool.WingetId) ? "\uE896" : "\uE774";

        private static string GetActionText(ExternalTool tool)
        {
            if (!string.IsNullOrWhiteSpace(tool.WingetId))
                return L.Get("Tools:Install");
            if (!string.IsNullOrEmpty(tool.DownloadUrl) && tool.DownloadUrl.Contains("github.com"))
                return "GitHub";
            if (!string.IsNullOrEmpty(tool.DownloadUrl))
                return L.Get("Tools:Website");
            return L.Get("Tools:Run");
        }

        private DispatcherTimer? _progressTimer;

        private void OnDownloadProgress(ToolProgress progress)
        {
            _progressTimer?.Stop();
            ProgressPanel.Visibility = Visibility.Visible;
            ProgressText.Text = progress.IsCompleted ? progress.Message : $"{progress.ToolName}: {progress.Message}";
            ProgressBar.Value = progress.Percentage;
            if (progress.IsCompleted)
                HideProgressAfter(TimeSpan.FromSeconds(progress.Success ? 3 : 5));
        }

        private void HideProgressAfter(TimeSpan delay)
        {
            // One timer: the end of one tool in a batch must not hide the progress of the next.
            _progressTimer?.Stop();
            _progressTimer = new DispatcherTimer { Interval = delay };
            _progressTimer.Tick += (_, _) =>
            {
                _progressTimer.Stop();
                ProgressPanel.Visibility = Visibility.Collapsed;
                ProgressBar.Value = 0;
            };
            _progressTimer.Start();
        }

        private void ShowLoadError() => LoadErrorText.Visibility = Visibility.Visible;
    }
}
