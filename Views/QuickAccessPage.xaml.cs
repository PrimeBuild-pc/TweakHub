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

        private readonly ShortcutService _shortcutService;

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
                    _shortcutService.Initialize();
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
            LoadErrorText.Visibility = Visibility.Collapsed;
            ShortcutsControl.ItemsSource = _shortcutService.SystemShortcuts
                .GroupBy(shortcut => shortcut.Category)
                .OrderBy(group => ShortcutService.CategoryOrder(group.Key))
                .Select(group => new ShortcutGroup(
                    ShortcutService.LocalizeCategory(group.Key),
                    ShortcutService.CategoryIcon(group.Key),
                    group.OrderBy(shortcut => shortcut.Name).ToList()))
                .ToList();
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
