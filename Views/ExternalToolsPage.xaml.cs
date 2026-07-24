using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TweakHub.Localization;
using TweakHub.Models;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views
{
    public partial class ExternalToolsPage : Page
    {
        private sealed record ToolItem(
            ExternalTool Tool,
            string ActionText,
            string ActionIcon)
        {
            public string ToolIcon => ShortcutService.CategoryIcon(Tool.Category);
            public string AutomationName => $"{Tool.Name}. {ActionText}";
            public string EditAutomationName => L.Format("Tools:EditToolNamed", Tool.Name);
            public string DeleteAutomationName => L.Format("Tools:DeleteToolNamed", Tool.Name);
            public string UninstallAutomationName => L.Format("Tools:UninstallNamed", Tool.Name);
        }

        private sealed record ToolGroup(
            string Name,
            string Icon,
            string CountText,
            IReadOnlyList<ToolItem> Tools);

        private readonly ShortcutService _shortcutService;
        private readonly ToolDownloadService _downloadService;
        private readonly IProgress<ToolProgress> _progress;
        private string _searchQuery = string.Empty;
        private bool _favoritesOnly;

        public ExternalToolsPage()
        {
            InitializeComponent();
            _shortcutService = ShortcutService.Instance;
            _downloadService = ToolDownloadService.Instance;
            _progress = new Progress<ToolProgress>(OnDownloadProgress);
            Loaded += ExternalToolsPage_Loaded;
        }

        private void ExternalToolsPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _shortcutService.Initialize();
                LoadExternalTools();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExternalToolsPage load failed: {ex}");
                ShowLoadError();
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
            var favorites = _shortcutService.ExternalTools
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

            groups.AddRange(FilterTools(_shortcutService.ExternalTools, _searchQuery, _favoritesOnly)
                .OrderByDescending(tool => tool.IsFavorite)
                .ThenBy(tool => tool.Name)
                .GroupBy(tool => tool.Category)
                .OrderBy(group => ShortcutService.CategoryOrder(group.Key))
                .Select(group => new ToolGroup(
                    ShortcutService.LocalizeCategory(group.Key),
                    ShortcutService.CategoryIcon(group.Key),
                    L.Format("Tools:ToolCount", group.Count()),
                    group.OrderByDescending(tool => tool.IsFavorite)
                        .ThenBy(tool => tool.Name)
                        .Select(CreateToolItem)
                        .ToList())));

            return groups;
        }

        private ToolItem CreateToolItem(ExternalTool tool) =>
            new(tool, GetActionText(tool), GetDownloadIcon(tool));

        internal static IEnumerable<ExternalTool> FilterTools(
            IEnumerable<ExternalTool> tools,
            string searchQuery,
            bool favoritesOnly)
        {
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

        private async void ToolCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border { DataContext: ToolItem item } card)
                await RunToolAsync(card, item.Tool);
        }

        private async void ToolCard_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is not (Key.Enter or Key.Space)
                || sender is not Border { DataContext: ToolItem item } card)
                return;

            e.Handled = true;
            await RunToolAsync(card, item.Tool);
        }

        private async Task RunToolAsync(Border card, ExternalTool tool)
        {
            card.IsEnabled = false;
            card.Opacity = 0.7;
            try
            {
                await _downloadService.DownloadOrOpenTool(tool, _progress);
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
                await _downloadService.UninstallWithWinget(tool, _progress);
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

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = (sender as TextBox)?.Text ?? string.Empty;
            LoadExternalTools();
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

        private void OnDownloadProgress(ToolProgress progress)
        {
            ProgressPanel.Visibility = Visibility.Visible;
            ProgressText.Text = progress.IsCompleted ? progress.Message : $"{progress.ToolName}: {progress.Message}";
            ProgressBar.Value = progress.Percentage;
            if (progress.IsCompleted)
                HideProgressAfter(TimeSpan.FromSeconds(progress.Success ? 3 : 5));
        }

        private void HideProgressAfter(TimeSpan delay)
        {
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = delay };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                ProgressPanel.Visibility = Visibility.Collapsed;
                ProgressBar.Value = 0;
            };
            timer.Start();
        }

        private void ShowLoadError() => LoadErrorText.Visibility = Visibility.Visible;
    }
}
