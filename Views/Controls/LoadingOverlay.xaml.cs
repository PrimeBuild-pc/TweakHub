using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace TweakHub.Views.Controls;

public partial class LoadingOverlay : UserControl
{
    private AnimationClock? _fade;

    public LoadingOverlay()
    {
        InitializeComponent();
        Unloaded += (_, _) =>
        {
            if (_fade is not null) Collapse();
        };
    }

    public void Show(string message, string? detail = null)
    {
        StopFade();
        IsHitTestVisible = true;
        MessageText.Text = message;
        DetailText.Text = detail ?? string.Empty;
        DetailText.Visibility = string.IsNullOrEmpty(detail) ? Visibility.Collapsed : Visibility.Visible;
        Visibility = Visibility.Visible;
    }

    public void Hide()
    {
        if (Visibility == Visibility.Collapsed || _fade is not null) return;
        IsHitTestVisible = false;
        if (!IsVisible || !SystemParameters.ClientAreaAnimation)
        {
            Collapse();
            return;
        }

        _fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(140)).CreateClock();
        _fade.Completed += FadeCompleted;
        ApplyAnimationClock(OpacityProperty, _fade);
    }

    private void FadeCompleted(object? sender, EventArgs e) => Collapse();

    private void Collapse()
    {
        Visibility = Visibility.Collapsed;
        StopFade();
    }

    private void StopFade()
    {
        if (_fade is null) return;
        _fade.Completed -= FadeCompleted;
        _fade = null;
        ApplyAnimationClock(OpacityProperty, null);
    }
}
