using System.Windows;
using TweakHub.Localization;

namespace TweakHub.Views.Dialogs;

public static class AppDialog
{
    public static Task ShowAsync(Window? owner, string title, string message, string? buttonText = null) =>
        Task.FromResult(Show(owner, title, message, buttonText ?? L.Get("UI:OK"), null, DialogKind.Info));

    public static Task ShowWarningAsync(Window? owner, string title, string message, string? buttonText = null) =>
        Task.FromResult(Show(owner, title, message, buttonText ?? L.Get("UI:OK"), null, DialogKind.Warning));

    public static Task ShowErrorAsync(Window? owner, string title, string message, string? buttonText = null) =>
        Task.FromResult(Show(owner, title, message, buttonText ?? L.Get("UI:OK"), null, DialogKind.Error));

    public static Task<bool> ConfirmAsync(
        Window? owner,
        string title,
        string message,
        string? primaryButtonText = null,
        string? secondaryButtonText = null,
        DialogKind kind = DialogKind.Info,
        bool topmost = false) =>
        Task.FromResult(Show(owner, title, message, primaryButtonText ?? L.Get("UI:Yes"), secondaryButtonText ?? L.Get("UI:No"), kind, topmost));

    public static Task ShowRestartRequiredAsync(Window? owner, string message) => ShowAsync(owner, L.Get("UI:RestartRequired"), message);

    public static Task ShowDisclaimerAsync(Window? owner) => Task.FromResult(Show(
        owner,
        L.Get("UI:DisclaimerTitle"),
        L.Get("UI:DisclaimerMessage"),
        L.Get("UI:IUnderstand"),
        null,
        DialogKind.Shield));

    /// <summary>Asks for one line of text; null when cancelled or left empty.</summary>
    public static Task<string?> PromptAsync(Window? owner, string title, string message, string initial)
    {
        var dialog = new AppDialogWindow(title, message, L.Get("UI:OK"), L.Get("UI:Cancel"), DialogKind.Info, initial);
        return Task.FromResult<string?>(Show(owner, dialog) && dialog.InputText.Length > 0 ? dialog.InputText : null);
    }

    /// <summary>Lets the user tick one or more choices; null when cancelled or nothing is ticked.</summary>
    public static Task<IReadOnlyList<int>?> ChooseAsync(Window? owner, string title, string message, IEnumerable<(string Label, bool IsChecked)> choices)
    {
        var dialog = new AppDialogWindow(title, message, L.Get("UI:OK"), L.Get("UI:Cancel"), DialogKind.Info);
        dialog.SetChoices(choices);
        return Task.FromResult<IReadOnlyList<int>?>(Show(owner, dialog) && dialog.CheckedChoices.Count > 0 ? dialog.CheckedChoices : null);
    }

    private static bool Show(Window? owner, string title, string message, string primary, string? secondary, DialogKind kind, bool topmost = false)
    {
        var dialog = new AppDialogWindow(title, message, primary, secondary, kind) { Topmost = topmost };
        return Show(owner, dialog);
    }

    private static bool Show(Window? owner, AppDialogWindow dialog)
    {
        owner ??= Application.Current?.MainWindow;
        if (owner is { IsVisible: true }) dialog.Owner = owner;
        else dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        return dialog.ShowDialog() == true;
    }
}
