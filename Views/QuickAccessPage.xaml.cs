using System.Windows;
using System.Windows.Controls;
using TweakHub.Localization;
using TweakHub.Models;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views
{
    public partial class QuickAccessPage : Page
    {
        private sealed record ShortcutGroup(
            string Name,
            string Icon,
            IReadOnlyList<SystemShortcut> Shortcuts);

        private sealed record QuickTile(SystemShortcut Shortcut, string Glyph);

        // Fixed picks, not usage-tracked: command -> Segoe Fluent Icons glyph.
        private static readonly (string Command, string Glyph)[] FrequentCommands =
        {
            ("devmgmt.msc", "\uE772"),
            ("services.msc", "\uE713"),
            ("taskschd.msc", "\uE787"),
            ("eventvwr.msc", "\uE9D9"),
        };

        private readonly ShortcutService _shortcutService;
        private List<ShortcutGroup> _groups = new();

        public QuickAccessPage()
        {
            InitializeComponent();
            _shortcutService = ShortcutService.Instance;
            Loaded += QuickAccessPage_Loaded;
        }

        private void QuickAccessPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_shortcutService.SystemShortcuts.Count == 0)
                    _shortcutService.LoadSystemShortcuts();
                LoadShortcuts();
                Loaded -= QuickAccessPage_Loaded;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"QuickAccessPage load failed: {ex}");
                ShowLoadError();
            }
        }

        private void LoadShortcuts()
        {
            LoadErrorText.Visibility = Visibility.Collapsed;
            var byCommand = _shortcutService.SystemShortcuts.GroupBy(s => s.Command).ToDictionary(g => g.Key, g => g.First());
            FrequentControl.ItemsSource = FrequentCommands
                .Where(item => byCommand.ContainsKey(item.Command))
                .Select(item => new QuickTile(byCommand[item.Command], item.Glyph))
                .ToList();
            _groups = _shortcutService.SystemShortcuts
                .GroupBy(shortcut => shortcut.Category)
                .OrderBy(group => ShortcutService.CategoryOrder(group.Key))
                .Select(group => new ShortcutGroup(
                    ShortcutService.LocalizeCategory(group.Key),
                    ShortcutService.CategoryIcon(group.Key),
                    group.OrderBy(shortcut => shortcut.Name).ToList()))
                .ToList();
            ApplyFilter();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            var query = SearchBox.Text.Trim();
            var searching = query.Length > 0;
            FrequentPanel.Visibility = searching ? Visibility.Collapsed : Visibility.Visible;
            var shown = searching
                ? _groups
                    .Select(group => group with
                    {
                        Shortcuts = group.Shortcuts
                            .Where(s => s.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                                || s.Description.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                            .ToList()
                    })
                    .Where(group => group.Shortcuts.Count > 0)
                    .ToList()
                : _groups;
            ShortcutsControl.ItemsSource = shown;
            NoResultsText.Visibility = shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is QuickTile tile)
                ExecuteShortcut(tile.Shortcut);
        }

        private void Shortcut_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is SystemShortcut shortcut)
                ExecuteShortcut(shortcut);
        }

        private async void ExecuteShortcut(SystemShortcut shortcut)
        {
            try
            {
                if (!_shortcutService.ExecuteShortcut(shortcut))
                {
                    await AppDialog.ShowWarningAsync(
                        Window.GetWindow(this),
                        L.Get("Tools:ShortcutExecutionFailed"),
                        L.Format("Tools:ShortcutExecutionFailedMessage", shortcut.Name));
                }
            }
            catch (Exception ex)
            {
                await AppDialog.ShowErrorAsync(
                    Window.GetWindow(this),
                    L.Get("Tools:ShortcutError"),
                    L.Format("Tools:ShortcutErrorMessage", ex.Message));
            }
        }

        private void ShowLoadError() => LoadErrorText.Visibility = Visibility.Visible;
    }
}
