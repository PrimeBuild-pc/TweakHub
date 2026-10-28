using System.Windows;
using System.Windows.Controls;
using TweakHub.Localization;
using TweakHub.Services;
using TweakHub.Views.Dialogs;

namespace TweakHub.Views;

/// <summary>Forced Group Policy values, grouped by area, with a reset to the Windows default that can be undone.</summary>
public partial class PolicyView : UserControl
{
    public PolicyView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        ContentPanel.IsEnabled = false;
        StatusText.Text = L.Get("Devices:Scanning");
        try
        {
            ManagedWarning.Visibility = await Task.Run(PolicyService.IsManagedPc) ? Visibility.Visible : Visibility.Collapsed;
            var categories = await PolicyService.Instance.ScanAsync();
            CategoriesList.ItemsSource = categories;
            StatusText.Text = categories.Count == 0
                ? L.Get("Security:PoliciesNone")
                : L.Format("Security:PoliciesSummary", categories.Sum(category => category.Values.Count), categories.Count,
                    categories.Sum(category => category.Resettable.Count));
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or System.IO.IOException)
        {
            StatusText.Text = ex.Message;
        }
        finally { ContentPanel.IsEnabled = true; }
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PolicyCategory category }) category.IsPreviewVisible = !category.IsPreviewVisible;
    }

    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PolicyCategory category }) return;
        var lines = category.Resettable.Select(value => value.Line).ToList();
        var list = string.Join("\n", lines.Take(20)) + (lines.Count > 20 ? $"\n… (+{lines.Count - 20})" : string.Empty);
        if (!await AppDialog.ConfirmAsync(Window.GetWindow(this), L.Get("Security:PoliciesResetTitle"),
                L.Format("Security:PoliciesResetConfirm", category.Name, lines.Count, list),
                L.Get("Security:PoliciesReset"), L.Get("UI:Cancel"))) return;
        await RunAsync(() => PolicyService.Instance.ResetAsync(category));
    }

    private async void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PolicyCategory category }) await RunAsync(() => PolicyService.Instance.UndoAsync(category));
    }

    private async Task RunAsync(Func<Task<bool>> action)
    {
        ContentPanel.IsEnabled = false;
        try
        {
            if (!await action())
                await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), L.Get("Security:PoliciesFailed"));
        }
        catch (Exception ex)
        {
            await AppDialog.ShowErrorAsync(Window.GetWindow(this), L.Get("Devices:ApplyFailedTitle"), ex.Message);
        }
        finally { await RefreshAsync(); }
    }
}
