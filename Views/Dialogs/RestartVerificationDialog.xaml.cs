using System.Windows;
using System.Windows.Controls;
using TweakHub.Localization;
using TweakHub.Services;

namespace TweakHub.Views.Dialogs;

public partial class RestartVerificationDialog : Window
{
    private sealed record Item(RestartVerificationResult Result)
    {
        public string Title => $"{Result.Name} — " + (Result.Kind == RestartOperationKind.WindowsUpdate
            ? L.Get($"Tweaks:WindowsUpdatePreset{Result.TargetPreset}")
            : L.Get(Result.TargetEnabled ? "Scripts:PlaybookEnable" : "Scripts:PlaybookRestore"));
        public string StatusText => L.Get($"Tweaks:RestartVerification{Result.Status}");
        public bool CanRetry => Result.Status is RestartVerificationStatus.Failed or RestartVerificationStatus.Partial;
        public bool CanRestore => CanRetry && RestartVerificationService.Instance.CanRestore(Result);
    }

    public RestartVerificationDialog(IEnumerable<RestartVerificationResult> results)
    {
        InitializeComponent();
        ResultsList.ItemsSource = results.Select(result => new Item(result)).ToList();
    }

    private async void Retry_Click(object sender, RoutedEventArgs e) =>
        await RunAsync(sender, RestartVerificationService.Instance.RetryAsync, "Tweaks:RestartVerificationRetryStarted");

    private async void Restore_Click(object sender, RoutedEventArgs e) =>
        await RunAsync(sender, RestartVerificationService.Instance.RestoreOriginalAsync, "Tweaks:RestartVerificationRestored");

    private async Task RunAsync(object sender, Func<RestartVerificationResult, Task<bool>> action, string successKey)
    {
        if (sender is not Button { DataContext: Item item } button) return;
        button.IsEnabled = false;
        var success = await action(item.Result);
        await AppDialog.ShowAsync(this, item.Result.Name, L.Get(success ? successKey : "Tweaks:RestartVerificationActionFailed"));
        if (success) Close(); else button.IsEnabled = true;
    }

    private void Acknowledge_Click(object sender, RoutedEventArgs e)
    {
        RestartVerificationService.Instance.AcknowledgeResults();
        Close();
    }
}
