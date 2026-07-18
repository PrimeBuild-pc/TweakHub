using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Views
{
    public partial class ExternalToolsPage : Page
    {
        private readonly ShortcutService _shortcutService;
        private readonly ToolDownloadService _downloadService;
        private string _searchQuery = string.Empty;
        private bool _favoritesOnly;

        public ExternalToolsPage()
        {
            this.InitializeComponent();
            _shortcutService = ShortcutService.Instance;
            _downloadService = ToolDownloadService.Instance;

            Loaded += ExternalToolsPage_Loaded;
            Unloaded += ExternalToolsPage_Unloaded;
            SizeChanged += (_, _) => UpdateGridColumns();
        }

        private void ExternalToolsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _downloadService.DownloadProgress -= OnDownloadProgress;
            _downloadService.DownloadCompleted -= OnDownloadCompleted;
        }

        private void ExternalToolsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _downloadService.DownloadProgress -= OnDownloadProgress;
            _downloadService.DownloadCompleted -= OnDownloadCompleted;
            _downloadService.DownloadProgress += OnDownloadProgress;
            _downloadService.DownloadCompleted += OnDownloadCompleted;

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
                    || t.Category.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            // Group tools by category
            var groupedTools = toolSource
                .OrderByDescending(t => t.IsFavorite)
                .ThenBy(t => t.Name)
                .GroupBy(t => t.Category)
                .OrderBy(g => GetCategoryOrder(g.Key));

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

        private static int GetCategoryOrder(string category) => category switch
        {
            "System Utilities" => 1,
            "CPU & Memory" => 2,
            "Firmware & Power" => 3,
            "Monitoring & Diagnostics" => 4,
            "GPU & Display" => 5,
            "Gaming & Input" => 6,
            "Storage & USB" => 7,
            "Network" => 8,
            "Audio" => 9,
            "Benchmarks & Stability" => 10,
            "AI Tools" => 11,
            _ => 99
        };

        private StackPanel CreateCategoryHeader(string categoryName, int toolCount)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var iconText = new TextBlock
            {
                Text = GetCategoryIcon(categoryName),
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 18,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconText.SetResourceReference(TextBlock.ForegroundProperty, "IconBrush");

            var nameText = new TextBlock
            {
                Text = categoryName,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            nameText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseHighBrush");

            var countText = new TextBlock
            {
                Text = $"({toolCount} tools)",
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
                Text = "Favorites",
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

        private static string GetCategoryIcon(string category) => category switch
        {
            "System Utilities" => "\uE713",
            "CPU & Memory" => "\uE950",
            "Firmware & Power" => "\uE945",
            "Monitoring & Diagnostics" => "\uE9D9",
            "GPU & Display" => "\uE7F4",
            "Gaming & Input" => "\uE7FC",
            "Storage & USB" => "\uEDA2",
            "Network" => "\uE968",
            "Audio" => "\uE767",
            "Benchmarks & Stability" => "\uE9D2",
            "AI Tools" => "\uE99A",
            _ => "\uE90F"
        };

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
                    await _downloadService.DownloadOrOpenTool(tool);
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
                Text = GetCategoryIcon(tool.Category),
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
                ToolTip = tool.IsFavorite ? "Unfavorite" : "Favorite",
                Cursor = System.Windows.Input.Cursors.Hand
            };
            AutomationProperties.SetName(favBtn, tool.IsFavorite ? "Remove from favorites" : "Add to favorites");
            favBtn.Click += (s, e) =>
            {
                e.Handled = true; // Don't trigger card click
                tool.IsFavorite = !tool.IsFavorite;
                ((TextBlock)favBtn.Content).Text = tool.IsFavorite ? "★" : "☆";
                favBtn.ToolTip = tool.IsFavorite ? "Unfavorite" : "Favorite";
                AutomationProperties.SetName(favBtn, tool.IsFavorite ? "Remove from favorites" : "Add to favorites");
                PersistFavorites();
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
                    ToolTip = "Edit custom tool",
                    Padding = new Thickness(4, 0, 4, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                AutomationProperties.SetName(editButton, $"Edit {tool.Name}");
                editButton.Click += (_, e) =>
                {
                    e.Handled = true;
                    EditCustomTool(tool);
                };
                var deleteButton = new Button
                {
                    Content = new TextBlock { Text = "\uE74D", FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = 12 },
                    ToolTip = "Delete custom tool",
                    Padding = new Thickness(4, 0, 4, 0),
                    Margin = new Thickness(4, 0, 0, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                AutomationProperties.SetName(deleteButton, $"Delete {tool.Name}");
                deleteButton.Click += (_, e) =>
                {
                    e.Handled = true;
                    if (MessageBox.Show($"Delete {tool.Name}?", "Delete Custom Tool", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
                    _shortcutService.DeleteCustomTool(tool);
                    PersistFavorites();
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
                    ToolTip = "Uninstall",
                    Padding = new Thickness(4, 0, 4, 0),
                    Margin = new Thickness(6, 0, 0, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                AutomationProperties.SetName(uninstallBtn, $"Uninstall {tool.Name}");
                uninstallBtn.Click += async (s, e) =>
                {
                    e.Handled = true;
                    var result = MessageBox.Show($"Are you sure you want to uninstall {tool.Name}?", "Confirm Uninstall", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        card.IsEnabled = false;
                        card.Opacity = 0.7;
                        try
                        {
                            await _downloadService.UninstallWithWinget(tool);
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

        private void PersistFavorites()
        {
            try
            {
                var favs = _shortcutService.ExternalTools.Where(t => t.IsFavorite).Select(ShortcutService.FavoriteKey).ToList();
                UserDataService.Instance.SaveFavoriteTools(favs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to save favorites", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void EditCustomTool(ExternalTool? tool)
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
                MessageBox.Show(ex.Message, "Unable to Save Tool", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string GetDownloadIcon(ExternalTool tool) =>
            !string.IsNullOrWhiteSpace(tool.WingetId) ? "\uE896" : "\uE774";

        private string GetActionText(ExternalTool tool)
        {
            if (!string.IsNullOrWhiteSpace(tool.WingetId))
                return "Install";
            if (!string.IsNullOrEmpty(tool.DownloadUrl) && tool.DownloadUrl.Contains("github.com"))
                return "GitHub";
            if (!string.IsNullOrEmpty(tool.DownloadUrl))
                return "Website";
            return "Run";
        }

        private void OnDownloadProgress(object? sender, DownloadProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                this.ProgressPanel.Visibility = Visibility.Visible;
                this.ProgressText.Text = $"{e.ToolName}: {e.StatusMessage}";
                if (e.ProgressPercentage >= 0)
                    this.ProgressBar.Value = e.ProgressPercentage;
            });
        }

        private void OnDownloadCompleted(object? sender, DownloadCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Success)
                {
                    this.ProgressText.Text = e.Message;
                    this.ProgressBar.Value = 100;


                    // Hide progress panel after 3 seconds
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(3)
                    };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        this.ProgressPanel.Visibility = Visibility.Collapsed;
                        this.ProgressBar.Value = 0;
                    };
                    timer.Start();
                }
                else
                {
                    this.ProgressText.Text = e.Message;
                    this.ProgressBar.Value = 0;

                    // Hide progress panel after 5 seconds for errors
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(5)
                    };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        this.ProgressPanel.Visibility = Visibility.Collapsed;
                    };
                    timer.Start();
                }
            });
        }
        private void ShowLoadError()
        {
            Dispatcher.Invoke(() =>
            {
                this.ToolsContainer.Children.Clear();
                var errorText = new TextBlock
                {
                    Text = "Failed to load external tools. Please restart TweakHub.",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.Red
                };
                this.ToolsContainer.Children.Add(errorText);
            });
        }

    }
}
