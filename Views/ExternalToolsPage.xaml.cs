using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TweakHub.Localization;
using TweakHub.Models;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views
{
    public partial class ExternalToolsPage : Page
    {
        private readonly ShortcutService _shortcutService;
        private readonly ToolDownloadService _downloadService;
        private readonly IProgress<ToolProgress> _progress;
        private string _searchQuery = string.Empty;
        private bool _favoritesOnly;

        public ExternalToolsPage()
        {
            this.InitializeComponent();
            _shortcutService = ShortcutService.Instance;
            _downloadService = ToolDownloadService.Instance;
            _progress = new Progress<ToolProgress>(OnDownloadProgress);

            Loaded += ExternalToolsPage_Loaded;
            SizeChanged += (_, _) => UpdateGridColumns();
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
            this.ToolsContainer.Children.Clear();

            // Favorites pseudo-category at top
            var favorites = _shortcutService.ExternalTools.Where(t => t.IsFavorite).OrderBy(t => t.Name).ToList();
            if (!_favoritesOnly && favorites.Any())
            {
                var favExpander = new Expander
                {
                    Header = CreateFavoritesHeader(favorites.Count),
                    IsExpanded = true,
                    Margin = new Thickness(0, 0, 0, 16),
                    Style = (Style)FindResource("CategoryExpanderStyle")
                };
                var favGrid = new System.Windows.Controls.Primitives.UniformGrid { Columns = GetToolColumnCount(), Margin = new Thickness(0, 16, 0, 0) };
                foreach (var tool in favorites)
                {
                    favGrid.Children.Add(CreateToolCard(tool));
                }
                favExpander.Content = favGrid;
                this.ToolsContainer.Children.Add(favExpander);
            }

            IEnumerable<ExternalTool> toolSource = _shortcutService.ExternalTools;
            if (_favoritesOnly) toolSource = toolSource.Where(tool => tool.IsFavorite);
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                toolSource = toolSource.Where(t => t.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)
                    || t.Description.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)
                    || t.Category.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)
                    || ShortcutService.LocalizeCategory(t.Category).Contains(_searchQuery, StringComparison.CurrentCultureIgnoreCase));
            }

            // Group tools by category
            var groupedTools = toolSource
                .OrderByDescending(t => t.IsFavorite)
                .ThenBy(t => t.Name)
                .GroupBy(t => t.Category)
                .OrderBy(g => ShortcutService.CategoryOrder(g.Key));

            foreach (var group in groupedTools)
            {
                // Create collapsible category section
                var categoryExpander = new Expander
                {
                    Header = CreateCategoryHeader(group.Key, group.Count()),
                    IsExpanded = true,
                    Margin = new Thickness(0, 0, 0, 16),
                    Style = (Style)FindResource("CategoryExpanderStyle")
                };

                var toolsGrid = new System.Windows.Controls.Primitives.UniformGrid
                {
                    Columns = GetToolColumnCount(),
                    Margin = new Thickness(0, 16, 0, 0)
                };

                foreach (var tool in group.OrderByDescending(t => t.IsFavorite).ThenBy(t => t.Name))
                {
                    var toolCard = CreateToolCard(tool);
                    toolsGrid.Children.Add(toolCard);
                }

                categoryExpander.Content = toolsGrid;
                this.ToolsContainer.Children.Add(categoryExpander);
            }
        }

        private int GetToolColumnCount() => ActualWidth >= 1000 ? 3 : ActualWidth >= 650 ? 2 : 1;

        private void UpdateGridColumns()
        {
            var columns = GetToolColumnCount();
            foreach (var expander in ToolsContainer.Children.OfType<Expander>())
                if (expander.Content is System.Windows.Controls.Primitives.UniformGrid grid)
                    grid.Columns = columns;
        }

        private StackPanel CreateCategoryHeader(string categoryName, int toolCount)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var iconText = new TextBlock
            {
                Text = ShortcutService.CategoryIcon(categoryName),
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 18,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconText.SetResourceReference(TextBlock.ForegroundProperty, "IconBrush");

            var nameText = new TextBlock
            {
                Text = ShortcutService.LocalizeCategory(categoryName),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            nameText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseHighBrush");

            var countText = new TextBlock
            {
                Text = L.Format("Tools:ToolCount", toolCount),
                FontSize = 12,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            countText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");

            headerPanel.Children.Add(iconText);
            headerPanel.Children.Add(nameText);
            headerPanel.Children.Add(countText);

            return headerPanel;
        }

        private StackPanel CreateFavoritesHeader(int count)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var icon = new TextBlock
            {
                Text = "\uE734",
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 18,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.SetResourceReference(TextBlock.ForegroundProperty, "IconBrush");
            var title = new TextBlock
            {
                Text = L.Get("Tools:Favorites"),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            title.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseHighBrush");
            var countText = new TextBlock
            {
                Text = $"({count})",
                FontSize = 12,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            countText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");
            panel.Children.Add(icon);
            panel.Children.Add(title);
            panel.Children.Add(countText);
            return panel;
        }

        private Border CreateToolCard(ExternalTool tool)
        {
            var card = new Border
            {
                Style = (Style)FindResource("ToolCardStyle"),
                Tag = tool,
                ToolTip = GetActionText(tool),
                Cursor = Cursors.Hand
            };
            AutomationProperties.SetName(card, $"{tool.Name}. {GetActionText(tool)}");

            async Task RunAction()
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
                    card.Opacity = 1.0;
                }
            }

            card.MouseLeftButtonUp += async (_, _) => await RunAction();
            card.KeyDown += async (_, e) =>
            {
                if (e.Key is not (Key.Enter or Key.Space)) return;
                e.Handled = true;
                await RunAction();
            };

            var stackPanel = new StackPanel();

            // Tool icon and name
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var iconText = new TextBlock
            {
                Text = ShortcutService.CategoryIcon(tool.Category),
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 18,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconText.SetResourceReference(TextBlock.ForegroundProperty, "IconBrush");

            var nameText = new TextBlock
            {
                Text = tool.Name,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            nameText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseHighBrush");

            headerPanel.Children.Add(iconText);
            headerPanel.Children.Add(nameText);

            // Favorite star button
            var favBtn = new Button
            {
                Content = new TextBlock { Text = tool.IsFavorite ? "★" : "☆", FontSize = 16 },
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                ToolTip = L.Get(tool.IsFavorite ? "Tools:Unfavorite" : "Tools:Favorite"),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            AutomationProperties.SetName(favBtn, L.Get(tool.IsFavorite ? "Tools:RemoveFromFavorites" : "Tools:AddToFavorites"));
            favBtn.Click += async (s, e) =>
            {
                e.Handled = true; // Don't trigger card click
                tool.IsFavorite = !tool.IsFavorite;
                ((TextBlock)favBtn.Content).Text = tool.IsFavorite ? "★" : "☆";
                favBtn.ToolTip = L.Get(tool.IsFavorite ? "Tools:Unfavorite" : "Tools:Favorite");
                AutomationProperties.SetName(favBtn, L.Get(tool.IsFavorite ? "Tools:RemoveFromFavorites" : "Tools:AddToFavorites"));
                await PersistFavoritesAsync();
                LoadExternalTools();
            };

            headerGrid.Children.Add(headerPanel);
            Grid.SetColumn(headerPanel, 0);
            headerGrid.Children.Add(favBtn);
            Grid.SetColumn(favBtn, 1);

            // Description
            var descriptionText = new TextBlock
            {
                Text = tool.Description,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 14,
                Margin = new Thickness(0, 0, 0, 12),
                MaxHeight = 42, // Limit to ~3 lines
                TextTrimming = TextTrimming.WordEllipsis
            };
            descriptionText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");

            // Footer with install/uninstall icons
            var footerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Install/Open indicator
            var actionIcon = new TextBlock
            {
                Text = GetDownloadIcon(tool),
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            actionIcon.SetResourceReference(TextBlock.ForegroundProperty, "IconBrush");

            var actionText = new TextBlock
            {
                Text = GetActionText(tool),
                FontSize = 10,
                Margin = new Thickness(4, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            actionText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");

            footerPanel.Children.Add(actionIcon);
            footerPanel.Children.Add(actionText);

            if (tool.IsCustom)
            {
                var editButton = new Button
                {
                    Content = new TextBlock { Text = "\uE70F", FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = 12 },
                    ToolTip = L.Get("Tools:EditCustomTool"),
                    Padding = new Thickness(4, 0, 4, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                AutomationProperties.SetName(editButton, L.Format("Tools:EditToolNamed", tool.Name));
                editButton.Click += (_, e) =>
                {
                    e.Handled = true;
                    EditCustomTool(tool);
                };
                var deleteButton = new Button
                {
                    Content = new TextBlock { Text = "\uE74D", FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = 12 },
                    ToolTip = L.Get("Tools:DeleteCustomTool"),
                    Padding = new Thickness(4, 0, 4, 0),
                    Margin = new Thickness(4, 0, 0, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                AutomationProperties.SetName(deleteButton, L.Format("Tools:DeleteToolNamed", tool.Name));
                deleteButton.Click += async (_, e) =>
                {
                    e.Handled = true;
                    if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Tools:DeleteCustomTool"), L.Format("Tools:DeleteToolMessage", tool.Name), L.Get("Tools:Delete"), L.Get("Tools:Cancel"))) return;
                    _shortcutService.DeleteCustomTool(tool);
                    await PersistFavoritesAsync();
                    LoadExternalTools();
                };
                footerPanel.Children.Add(editButton);
                footerPanel.Children.Add(deleteButton);
            }

            // Uninstall button for Winget tools
            if (!string.IsNullOrWhiteSpace(tool.WingetId))
            {
                var uninstallBtn = new Button
                {
                    Content = new TextBlock { Text = "\uE74D", FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = 12 },
                    ToolTip = L.Get("Tools:Uninstall"),
                    Padding = new Thickness(4, 0, 4, 0),
                    Margin = new Thickness(6, 0, 0, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                AutomationProperties.SetName(uninstallBtn, L.Format("Tools:UninstallNamed", tool.Name));
                uninstallBtn.Click += async (s, e) =>
                {
                    e.Handled = true;
                    if (await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Tools:ConfirmUninstall"),
                            L.Format("Tools:ConfirmUninstallMessage", tool.Name), L.Get("Tools:Uninstall"), L.Get("Tools:Cancel")))
                    {
                        card.IsEnabled = false;
                        card.Opacity = 0.7;
                        try
                        {
                            await _downloadService.UninstallWithWinget(tool, _progress);
                        }
                        finally
                        {
                            card.IsEnabled = true;
                            card.Opacity = 1.0;
                        }
                    }
                };

                footerPanel.Children.Add(uninstallBtn);
            }

            stackPanel.Children.Add(headerGrid);
            stackPanel.Children.Add(descriptionText);
            stackPanel.Children.Add(footerPanel);

            card.Child = stackPanel;

            return card;
        }

        private async Task PersistFavoritesAsync()
        {
            try
            {
                var favs = _shortcutService.ExternalTools.Where(t => t.IsFavorite).Select(ShortcutService.FavoriteKey).ToList();
                UserDataService.Instance.SaveFavoriteTools(favs);
            }
            catch (Exception ex)
            {
                await AppDialog.ShowWarningAsync(Window.GetWindow(this), L.Get("Tools:UnableSaveFavorites"), ex.Message);
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
            var dialog = new TweakHub.Views.Dialogs.CustomToolDialog(_shortcutService.GetToolCategories(), tool)
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
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Tools:UnableSaveTool"), ex.Message);
            }
        }

        private static string GetDownloadIcon(ExternalTool tool) =>
            !string.IsNullOrWhiteSpace(tool.WingetId) ? "\uE896" : "\uE774";

        private string GetActionText(ExternalTool tool)
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
            if (progress.IsCompleted) HideProgressAfter(TimeSpan.FromSeconds(progress.Success ? 3 : 5));
        }

        private void HideProgressAfter(TimeSpan delay)
        {
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = delay };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                this.ProgressPanel.Visibility = Visibility.Collapsed;
                this.ProgressBar.Value = 0;
            };
            timer.Start();
        }

        private void ShowLoadError()
        {
            Dispatcher.Invoke(() =>
            {
                this.ToolsContainer.Children.Clear();
                var errorText = new TextBlock
                {
                    Text = L.Get("Tools:LoadToolsFailed"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.Red
                };
                this.ToolsContainer.Children.Add(errorText);
            });
        }

    }
}
