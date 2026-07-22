using System.Windows;
using System.Windows.Controls;
using ModernWpf.Controls;
using TweakHub.Localization;

namespace TweakHub.Views.Dialogs;

public static class AppDialog
{
    public static async Task ShowAsync(Window? owner, string title, string message, string? buttonText = null) =>
        await Create(owner, title, message, buttonText ?? L.Get("UI:OK")).ShowAsync();

    public static async Task ShowWarningAsync(Window? owner, string title, string message, string? buttonText = null) =>
        await Create(owner, title, message, buttonText ?? L.Get("UI:OK"), kind: "Warning").ShowAsync();

    public static async Task ShowErrorAsync(Window? owner, string title, string message, string? buttonText = null) =>
        await Create(owner, title, message, buttonText ?? L.Get("UI:OK"), kind: "Error").ShowAsync();

    public static async Task<bool> ConfirmAsync(
        Window? owner,
        string title,
        string message,
        string? primaryButtonText = null,
        string? secondaryButtonText = null) =>
        await Create(owner, title, message, primaryButtonText ?? L.Get("UI:Yes"), secondaryButtonText ?? L.Get("UI:No")).ShowAsync()
            == ContentDialogResult.Primary;

    public static Task ShowRestartRequiredAsync(Window? owner, string message) => ShowAsync(
        owner,
        L.Get("UI:RestartRequired"),
        L.Format("UI:RestartRequiredMessage", message));

    public static Task ShowDisclaimerAsync(Window? owner) => ShowAsync(
        owner,
        L.Get("UI:DisclaimerTitle"),
        L.Get("UI:DisclaimerMessage"),
        L.Get("UI:IUnderstand"));

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
