using System.Windows;
using System.Windows.Controls;

namespace TweakHub.Views.Controls;

public partial class LoadingOverlay : UserControl
{
    public LoadingOverlay() => InitializeComponent();

    public void Show(string message, string? detail = null)
    {
        MessageText.Text = message;
        DetailText.Text = detail ?? string.Empty;
        DetailText.Visibility = string.IsNullOrEmpty(detail) ? Visibility.Collapsed : Visibility.Visible;
        Ring.IsActive = true;
        Visibility = Visibility.Visible;
    }

    public void Hide()
    {
        Ring.IsActive = false;
        Visibility = Visibility.Collapsed;
    }
}
