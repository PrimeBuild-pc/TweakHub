using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Views
{
    public partial class ExternalToolsPage : Page
    {
        private readonly ShortcutService _shortcutService;
        private readonly ToolDownloadService _downloadService;

        public ExternalToolsPage()
        {
            InitializeComponent();
            _shortcutService = ShortcutService.Instance;
            _downloadService = ToolDownloadService.Instance;

            // Subscribe to download events
            _downloadService.DownloadProgress += OnDownloadProgress;
            _downloadService.DownloadCompleted += OnDownloadCompleted;

            Loaded += ExternalToolsPage_Loaded;
        }

        private void ExternalToolsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadExternalTools();
        }

        private void LoadExternalTools()
        {
            ToolsContainer.Children.Clear();

            // Group tools by category
            var groupedTools = _shortcutService.ExternalTools
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

                var toolsGrid = new UniformGrid
                {
                    Columns = 3,
                    Margin = new Thickness(0, 16, 0, 0)
                };

                foreach (var tool in group.OrderBy(t => t.Name))
                {
                    var toolCard = CreateToolCard(tool);
                    toolsGrid.Children.Add(toolCard);
                }

                categoryExpander.Content = toolsGrid;
                ToolsContainer.Children.Add(categoryExpander);
            }
        }

        private int GetCategoryOrder(string category)
        {
            // Define category display order
            return category switch
            {
                "DLSS and Graphics Tools" => 1,
                "System and Optimization Tools" => 2,
                "Monitoring and Latency Tools" => 3,
                "Overclocking and Hardware Control" => 4,
                "Hardware Monitoring" => 5,
                "Benchmarking and Testing" => 6,
                "Storage and Memory" => 7,
                "Driver and GPU Utilities" => 8,
                "Display and Audio Utilities" => 9,
                "Runtime and Libraries" => 10,
                _ => 99
            };
        }

        private StackPanel CreateCategoryHeader(string categoryName, int toolCount)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var iconText = new TextBlock
            {
                Text = GetCategoryIcon(categoryName),
                FontSize = 20,
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

        private string GetCategoryIcon(string category)
        {
            return category switch
            {
                "DLSS and Graphics Tools" => "🎮",
                "System and Optimization Tools" => "⚙️",
                "Monitoring and Latency Tools" => "⏱️",
                "Overclocking and Hardware Control" => "🔥",
                "Hardware Monitoring" => "📊",
                "Benchmarking and Testing" => "🏁",
                "Storage and Memory" => "💾",
                "Driver and GPU Utilities" => "🔧",
                "Display and Audio Utilities" => "🖥️",
                "Runtime and Libraries" => "📦",
                _ => "🔧"
            };
        }

        private Border CreateToolCard(ExternalTool tool)
        {
            var card = new Border
            {
                Style = (Style)FindResource("ToolCardStyle"),
                Tag = tool,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            card.MouseLeftButtonUp += async (s, e) =>
            {
                // Disable the card during download
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
            };

            var stackPanel = new StackPanel();

            // Tool icon and name
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var iconText = new TextBlock
            {
                Text = tool.Icon,
                FontSize = 20,
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

            // Footer with download indicator
            var footerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var downloadIcon = new TextBlock
            {
                Text = GetDownloadIcon(tool),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            downloadIcon.SetResourceReference(TextBlock.ForegroundProperty, "IconBrush");

            var actionText = new TextBlock
            {
                Text = GetActionText(tool),
                FontSize = 10,
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            actionText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");

            footerPanel.Children.Add(downloadIcon);
            footerPanel.Children.Add(actionText);

            stackPanel.Children.Add(headerPanel);
            stackPanel.Children.Add(descriptionText);
            stackPanel.Children.Add(footerPanel);

            card.Child = stackPanel;

            return card;
        }

        private string GetDownloadIcon(ExternalTool tool)
        {
            if (tool.DownloadUrl.Contains("github.com"))
                return "📦"; // GitHub package
            else if (tool.DownloadUrl.Contains("microsoft.com") || tool.DownloadUrl.Contains("apps.microsoft.com"))
                return "🏪"; // Microsoft Store
            else
                return "⬇️"; // Generic download
        }

        private string GetActionText(ExternalTool tool)
        {
            if (tool.DownloadUrl.Contains("github.com"))
                return "GitHub";
            else if (tool.DownloadUrl.Contains("apps.microsoft.com"))
                return "Store";
            else
                return "Download";
        }

        private void OnDownloadProgress(object? sender, DownloadProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressPanel.Visibility = Visibility.Visible;
                ProgressText.Text = $"Downloading {e.ToolName}... {e.StatusMessage}";
                ProgressBar.Value = e.ProgressPercentage;
            });
        }

        private void OnDownloadCompleted(object? sender, DownloadCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Success)
                {
                    ProgressText.Text = $"✅ {e.ToolName} downloaded successfully!";
                    ProgressBar.Value = 100;

                    // Hide progress panel after 3 seconds
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(3)
                    };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        ProgressPanel.Visibility = Visibility.Collapsed;
                        ProgressBar.Value = 0;
                    };
                    timer.Start();
                }
                else
                {
                    ProgressText.Text = $"❌ Failed to download {e.ToolName}";
                    ProgressBar.Value = 0;

                    // Hide progress panel after 5 seconds for errors
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(5)
                    };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        ProgressPanel.Visibility = Visibility.Collapsed;
                    };
                    timer.Start();
                }
            });
        }
    }
}
