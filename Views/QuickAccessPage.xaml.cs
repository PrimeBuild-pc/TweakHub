using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TweakHub.Models;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views
{
    public partial class QuickAccessPage : Page
    {
        private readonly ShortcutService _shortcutService;

        public QuickAccessPage()
        {
            InitializeComponent();
            _shortcutService = ShortcutService.Instance;

            Loaded += QuickAccessPage_Loaded;
            SizeChanged += (_, _) => UpdateGridColumns();
        }

        private void QuickAccessPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure service is initialized before loading
                if (_shortcutService.SystemShortcuts.Count == 0)
                {
                    _shortcutService.Initialize();
                }
                LoadShortcuts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"QuickAccessPage load failed: {ex}");
                ShowLoadError();
            }
        }

        private void LoadShortcuts()
        {
            ShortcutsContainer.Children.Clear();

            // Group shortcuts by category
            var groupedShortcuts = _shortcutService.SystemShortcuts
                .GroupBy(s => s.Category)
                .OrderBy(g => GetCategoryOrder(g.Key));

            foreach (var group in groupedShortcuts)
            {
                // Category header
                var categoryHeader = new TextBlock
                {
                    Text = group.Key,
                    Style = (Style)FindResource("CategoryHeaderStyle")
                };
                ShortcutsContainer.Children.Add(categoryHeader);

                // Create grid for shortcuts in this category
                var shortcutsGrid = new UniformGrid
                {
                    Columns = GetColumnCount(),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                foreach (var shortcut in group.OrderBy(s => s.Name))
                {
                    var button = CreateShortcutButton(shortcut);
                    shortcutsGrid.Children.Add(button);
                }

                ShortcutsContainer.Children.Add(shortcutsGrid);
            }
        }

        private int GetColumnCount() => ActualWidth >= 1100 ? 3 : ActualWidth >= 700 ? 2 : 1;

        private void UpdateGridColumns()
        {
            var columns = GetColumnCount();
            foreach (var grid in ShortcutsContainer.Children.OfType<UniformGrid>())
                grid.Columns = columns;
        }

        private Button CreateShortcutButton(SystemShortcut shortcut)
        {
            var button = new Button
            {
                Style = (Style)FindResource("ShortcutButtonStyle"),
                Tag = shortcut
            };

            button.Click += (s, e) => ExecuteShortcut(shortcut);

            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0)
            };

            // Icon
            var iconText = new TextBlock
            {
                Text = GetCategoryIcon(shortcut.Category),
                FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"),
                FontSize = 20,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            iconText.SetResourceReference(TextBlock.ForegroundProperty, "IconBrush");

            // Content panel with proper sizing
            var contentPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var nameText = new TextBlock
            {
                Text = shortcut.Name,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 3),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320
            };
            nameText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseHighBrush");

            var descriptionText = new TextBlock
            {
                Text = shortcut.Description,
                FontSize = 12,
                LineHeight = 16,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320,
                Margin = new Thickness(0, 0, 0, 0)
            };
            descriptionText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");

            contentPanel.Children.Add(nameText);
            contentPanel.Children.Add(descriptionText);

            mainPanel.Children.Add(iconText);
            mainPanel.Children.Add(contentPanel);

            button.Content = mainPanel;

            return button;
        }

        private static string GetCategoryIcon(string category) => category switch
        {
            "System Management" => "\uE713",
            "System Information" => "\uE946",
            "Advanced Tools" => "\uE90F",
            "Performance" => "\uE9D9",
            "Power Management" => "\uE945",
            "Network" => "\uE968",
            "Audio" => "\uE767",
            "Display" => "\uE7F4",
            "Maintenance" => "\uE74D",
            _ => "\uE8F1"
        };

        private async void ExecuteShortcut(SystemShortcut shortcut)
        {
            try
            {
                var success = _shortcutService.ExecuteShortcut(shortcut);

                if (!success)
                {
                    await AppDialog.ShowWarningAsync(
                        Window.GetWindow(this),
                        "Shortcut Execution Failed",
                        $"Failed to execute shortcut: {shortcut.Name}\n\n" +
                        "This may be due to insufficient permissions or the target application not being available.");
                }
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(
                    Window.GetWindow(this),
                    "Shortcut Error",
                    $"An error occurred while executing the shortcut:\n\n{ex.Message}");
            }
        }

        private int GetCategoryOrder(string category)
        {
            return category switch
            {
                "System Management" => 1,
                "Performance" => 2,
                "System Information" => 3,
                "Network" => 4,
                "Audio" => 5,
                "Display" => 6,
                "Power Management" => 7,
                "Maintenance" => 8,
                "Advanced Tools" => 9,
                _ => 10
            };
        }

        private void ShowLoadError()
        {
            Dispatcher.Invoke(() =>
            {
                ShortcutsContainer.Children.Clear();
                var errorText = new TextBlock
                {
                    Text = "Failed to load shortcuts. Please restart TweakHub.",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush")
                };
                ShortcutsContainer.Children.Add(errorText);
            });
        }
    }
}
