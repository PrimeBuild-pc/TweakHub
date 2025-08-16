using System.Windows;
using System.Windows.Controls;
using TweakHub.Models;
using TweakHub.Services;

namespace TweakHub.Views
{
    public partial class RegistryTweaksPage : Page
    {
        private readonly TweakService _tweakService;
        private readonly RegistryService _registryService;

        public RegistryTweaksPage()
        {
            InitializeComponent();
            _tweakService = TweakService.Instance;
            _registryService = RegistryService.Instance;

            Loaded += RegistryTweaksPage_Loaded;
            _tweakService.PropertyChanged += TweakService_PropertyChanged;
        }
        private readonly PowerShellService _powerShellService = PowerShellService.Instance;


        private void TweakService_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TweakService.HasAppliedTweaksThisSession))
            {
                Dispatcher.Invoke(() =>
                {
                    RestoreAllButton.Visibility = _tweakService.HasAppliedTweaksThisSession ? Visibility.Visible : Visibility.Collapsed;
                });
            }
        }

        private void RegistryTweaksPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Show one-time per-session disclaimer on first visit to Registry Tweaks section
            if (!_tweakService.RegistryDisclaimerShown)
            {
                var dialog = new TweakHub.Views.Dialogs.DisclaimerDialog
                {
                    Owner = Window.GetWindow(this)
                };
                dialog.ShowDialog();
                _tweakService.RegistryDisclaimerShown = true;
            }

            LoadTweaks();
        }

        private void LoadTweaks()
        {
            // Load the tweak data first
            _tweakService.LoadTweaks();

            // Then bind to UI and refresh states
            TweakCategoriesControl.ItemsSource = _tweakService.TweakCategories;
            _tweakService.RefreshTweakStates();
        }

        private async void TweakToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is PerformanceTweak tweak)
            {
                // Show confirmation for high-risk tweaks
                if (tweak.RiskLevel >= 3)
                {
                    var result = MessageBox.Show(
                        $"This tweak has a high risk level ({tweak.RiskLevel}/5).\n\n" +
                        $"Description: {tweak.Description}\n\n" +
                        "Are you sure you want to apply this change?",
                        "High Risk Tweak Warning",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                    {
                        // Revert the checkbox state
                        checkBox.IsChecked = !checkBox.IsChecked;
                        return;
                    }



                }

                // Disable the checkbox while processing
                checkBox.IsEnabled = false;

                try
                {
                    var success = await _tweakService.ApplyTweakAsync(tweak);

                    if (!success)
                    {
                        MessageBox.Show(
                            $"Failed to apply tweak: {tweak.Name}\n\n" +
                            "This may be due to insufficient permissions or system restrictions.",
                            "Tweak Application Failed",



                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                        // Revert the checkbox state
                        checkBox.IsChecked = !checkBox.IsChecked;
                    }
                    else if (tweak.RequiresRestart)
                    {
                        MessageBox.Show(
                            $"Tweak '{tweak.Name}' has been applied successfully.\n\n" +
                            "A system restart is required for the changes to take effect.",
                            "Restart Required",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"An error occurred while applying the tweak:\n\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);


                    // Revert the checkbox state
                    checkBox.IsChecked = !checkBox.IsChecked;
                }
                finally
                {
                    checkBox.IsEnabled = true;
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _tweakService.RefreshTweakStates();
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _registryService.SaveBackupsToFile();
                MessageBox.Show(
                    "Registry backup has been created successfully.\n\n" +
                    "Backup location: %AppData%\\TweakHub\\Backups",
                    "Backup Created",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to create backup:\n\n{ex.Message}",
                    "Backup Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void ApplyAllButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will apply all recommended performance tweaks.\n\n" +
                "A backup will be created automatically before applying changes.\n\n" +
                "Do you want to continue?",
                "Apply All Recommended Tweaks",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // Create backup first
            try
            {
                _registryService.SaveBackupsToFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to create backup. Operation cancelled.\n\n{ex.Message}",
                    "Backup Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Apply recommended tweaks (risk level 1-2)
            var recommendedTweaks = _tweakService.TweakCategories
                .SelectMany(c => c.Tweaks)
                .Where(t => t.RiskLevel <= 2 && !t.IsEnabled)
                .ToList();

            if (!recommendedTweaks.Any())
            {
                MessageBox.Show(
                    "All recommended tweaks are already applied.",
                    "No Changes Needed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var progressWindow = new ProgressWindow($"Applying {recommendedTweaks.Count} recommended tweaks...");
            progressWindow.Show();

            int applied = 0;
            int failed = 0;
            bool requiresRestart = false;

            foreach (var tweak in recommendedTweaks)
            {
                try
                {
                    var success = await _tweakService.ApplyTweakAsync(tweak);
                    if (success)
                    {
                        applied++;
                        if (tweak.RequiresRestart)
                            requiresRestart = true;
                    }
                    else
                    {
                        failed++;
                    }
                }
                catch
                {
                    failed++;
                }

                progressWindow.UpdateProgress((applied + failed) * 100 / recommendedTweaks.Count);
            }

            progressWindow.Close();

            var message = $"Applied {applied} tweaks successfully.";
            if (failed > 0)
                message += $"\n{failed} tweaks failed to apply.";
            if (requiresRestart)
                message += "\n\nSome changes require a system restart to take effect.";

            MessageBox.Show(message, "Tweaks Applied", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void CreateRestorePointButton_Click(object sender, RoutedEventArgs e)
        {
            var info = "This will create a System Restore Point named 'TweakHub - Pre Tweaks'.\n\n" +
                       "Note: Creating a restore point may take a minute and requires administrator privileges.";
            var proceed = MessageBox.Show(info + "\n\nProceed?", "Create Restore Point", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (proceed != MessageBoxResult.Yes) return;

            var progress = new ProgressWindow("Creating system restore point...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Requesting restore point...");
                progress.UpdateProgress(30);

                var script = @"
                    try {
                        Checkpoint-Computer -Description 'TweakHub - Pre Tweaks' -RestorePointType 'MODIFY_SETTINGS'
                        Write-Output 'OK'
                    } catch {
                        Write-Error $_.Exception.Message
                    }
                ";

                var result = await _powerShellService.ExecuteScriptAsync(script);

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(90);
                await Task.Delay(500);

                progress.UpdateProgress(100);
                progress.Close();

                if (result.Success && result.Output.Contains("OK"))
                {
                    MessageBox.Show("Restore point created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to create restore point.\n\n{result.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show($"An error occurred:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async void RestoreAllButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "This will attempt to restore all registry tweaks to their original values.\n\nProceed?",
                "Restore All Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
                return;

            var progress = new ProgressWindow("Restoring all registry tweaks...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Restoring...");
                progress.UpdateProgress(20);
                var (restored, failed) = await _tweakService.RestoreAllTweaksAsync();

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(90);
                await Task.Delay(300);

                progress.UpdateProgress(100);
                progress.Close();

                var msg = $"Restored {restored} tweak(s).";
                if (failed > 0) msg += $"\n{failed} tweak(s) failed to restore.";
                MessageBox.Show(msg, "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                // Update visibility of Restore All button
                RestoreAllButton.Visibility = _tweakService.HasAppliedTweaksThisSession ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show($"An error occurred while restoring:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
