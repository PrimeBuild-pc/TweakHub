using System.Windows;
using System.Windows.Controls;
using ModernWpf.Controls;

namespace TweakHub.Views.Dialogs;

public static class AppDialog
{
    public static async Task ShowAsync(Window? owner, string title, string message, string buttonText = "OK") =>
        await Create(owner, title, message, buttonText).ShowAsync();

    public static async Task ShowWarningAsync(Window? owner, string title, string message, string buttonText = "OK") =>
        await Create(owner, title, message, buttonText, kind: "Warning").ShowAsync();

    public static async Task ShowErrorAsync(Window? owner, string title, string message, string buttonText = "OK") =>
        await Create(owner, title, message, buttonText, kind: "Error").ShowAsync();

    public static async Task<bool> ConfirmAsync(
        Window? owner,
        string title,
        string message,
        string primaryButtonText = "Yes",
        string secondaryButtonText = "No") =>
        await Create(owner, title, message, primaryButtonText, secondaryButtonText).ShowAsync()
            == ContentDialogResult.Primary;

    public static Task ShowRestartRequiredAsync(Window? owner, string message) => ShowAsync(
        owner,
        "Restart Required",
        $"{message}\n\nThis message is shown once per session.");

    public static Task ShowDisclaimerAsync(Window? owner) => ShowAsync(
        owner,
        "TweakHub Disclaimer",
        "TweakHub can make changes to Windows settings, the registry, boot configuration, and other system components. " +
        "Create a system restore point before proceeding.\n\n" +
        "You proceed at your own risk. The author assumes no responsibility for any damage, data loss, or system instability that may occur.\n\n" +
        "By continuing, you acknowledge and accept these conditions.",
        "I Understand");

    private static ContentDialog Create(
        Window? owner,
        string title,
        string message,
        string primaryButtonText,
        string? secondaryButtonText = null,
        string? kind = null)
    {
        var text = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 520 };
        text.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseHighBrush");
        FrameworkElement content = text;
        if (kind != null)
        {
            var icon = new TextBlock
            {
                Text = kind == "Error" ? "\uEA39" : "\uE7BA",
                FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"),
                FontSize = 20,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            icon.SetResourceReference(TextBlock.ForegroundProperty, kind == "Error" ? "DangerBrush" : "WarningBrush");
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(icon);
            panel.Children.Add(text);
            content = panel;
        }

        return new ContentDialog
        {
            Owner = owner ?? Application.Current.MainWindow,
            Title = title,
            Content = content,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText ?? string.Empty,
            DefaultButton = ContentDialogButton.Primary
        };
    }
}
