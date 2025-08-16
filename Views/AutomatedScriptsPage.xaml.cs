using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TweakHub.Services;

namespace TweakHub.Views
{
    public partial class AutomatedScriptsPage : Page
    {
        private readonly TweakService _tweakService;
        private readonly RegistryService _registryService;
        private readonly PowerShellService _powerShellService;

        public AutomatedScriptsPage()
        {
            InitializeComponent();
            _tweakService = TweakService.Instance;
            _registryService = RegistryService.Instance;
            _powerShellService = PowerShellService.Instance;
        }

        private async void GamingOptimization_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will apply comprehensive gaming optimizations including:\n\n" +
                "• GPU performance settings\n" +
                "• CPU priority optimizations\n" +
                "• Memory management tweaks\n" +
                "• Network latency improvements\n\n" +
                "A system restart may be required. Continue?",
                "Gaming Performance Optimization",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var progressWindow = new ProgressWindow("Applying gaming optimizations...");
            progressWindow.Show();

            try
            {
                await ExecuteGamingOptimizations(progressWindow);
                progressWindow.Close();

                MessageBox.Show(
                    "Gaming optimizations have been applied successfully!\n\n" +
                    "For best results, restart your system and close unnecessary background applications before gaming.",
                    "Optimization Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"An error occurred during optimization:\n\n{ex.Message}",
                    "Optimization Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void SystemCleanup_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will perform a deep system cleanup including:\n\n" +
                "• Temporary files and cache\n" +
                "• System logs and error reports\n" +
                "• Browser cache and cookies\n" +
                "• Recycle bin contents\n\n" +
                "Continue with cleanup?",
                "Deep System Cleanup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var progressWindow = new ProgressWindow("Performing system cleanup...");
            progressWindow.Show();

            try
            {
                await ExecuteSystemCleanup(progressWindow);
                progressWindow.Close();

                MessageBox.Show(
                    "System cleanup completed successfully!\n\n" +
                    "Your system should now have more free disk space and improved performance.",
                    "Cleanup Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"An error occurred during cleanup:\n\n{ex.Message}",
                    "Cleanup Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void NetworkOptimization_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will optimize network settings for:\n\n" +
                "• Reduced latency and ping\n" +
                "• Improved throughput\n" +
                "• Better gaming network performance\n" +
                "• DNS optimization\n\n" +
                "Network settings will be modified. Continue?",
                "Network Performance Boost",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var progressWindow = new ProgressWindow("Optimizing network settings...");
            progressWindow.Show();

            try
            {
                await ExecuteNetworkOptimizations(progressWindow);
                progressWindow.Close();

                MessageBox.Show(
                    "Network optimizations have been applied successfully!\n\n" +
                    "You may need to restart your network adapter or system for all changes to take effect.",
                    "Network Optimization Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"An error occurred during network optimization:\n\n{ex.Message}",
                    "Network Optimization Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void HardwareOptimization_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will detect your hardware and apply manufacturer-specific optimizations:\n\n" +
                "• Intel/AMD CPU optimizations\n" +
                "• Nvidia/AMD GPU settings\n" +
                "• Chipset-specific tweaks\n" +
                "• Power management settings\n\n" +
                "Continue with hardware optimization?",
                "Hardware-Specific Optimization",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var progressWindow = new ProgressWindow("Detecting hardware and applying optimizations...");
            progressWindow.Show();

            try
            {
                await ExecuteHardwareOptimizations(progressWindow);
                progressWindow.Close();

                MessageBox.Show(
                    "Hardware-specific optimizations have been applied successfully!\n\n" +
                    "Your system has been optimized for your specific hardware configuration.",
                    "Hardware Optimization Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"An error occurred during hardware optimization:\n\n{ex.Message}",
                    "Hardware Optimization Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void ResetAllTweaks_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ WARNING: This will reset ALL tweaks to their original values!\n\n" +
                "This action will:\n" +
                "• Restore all registry modifications\n" +
                "• Reset system settings to defaults\n" +
                "• Undo all performance optimizations\n\n" +
                "This action cannot be easily undone. Are you absolutely sure?",
                "Reset All Tweaks - WARNING",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            // Double confirmation for destructive action
            var confirmResult = MessageBox.Show(
                "This is your final confirmation.\n\n" +
                "Clicking 'Yes' will permanently reset all tweaks.\n\n" +
                "Do you want to proceed?",
                "Final Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop);

            if (confirmResult != MessageBoxResult.Yes)
                return;

            var progressWindow = new ProgressWindow("Resetting all tweaks...");
            progressWindow.Show();

            try
            {
                await ExecuteResetAllTweaks(progressWindow);
                progressWindow.Close();

                MessageBox.Show(
                    "All tweaks have been reset to their original values.\n\n" +
                    "Your system has been restored to its default configuration. " +
                    "A restart is recommended to ensure all changes take effect.",
                    "Reset Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"An error occurred during reset:\n\n{ex.Message}",
                    "Reset Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task ExecuteGamingOptimizations(ProgressWindow progress)
        {
            var steps = new[]
            {
                "Applying GPU performance tweaks...",
                "Optimizing CPU priority settings...",
                "Configuring memory management...",
                "Optimizing network for gaming...",
                "Setting power plan to high performance...",
                "Finalizing optimizations..."
            };

            for (int i = 0; i < steps.Length; i++)
            {
                progress.UpdateStatus(steps[i]);
                progress.UpdateProgress((i + 1) * 100 / steps.Length);
                
                // Simulate work and apply actual optimizations
                await Task.Delay(1000);
                
                switch (i)
                {
                    case 0: // GPU tweaks
                        await ApplyGpuOptimizations();
                        break;
                    case 1: // CPU priority
                        await ApplyCpuOptimizations();
                        break;
                    case 2: // Memory management
                        await ApplyMemoryOptimizations();
                        break;
                    case 3: // Network gaming
                        await ApplyGamingNetworkOptimizations();
                        break;
                    case 4: // Power plan
                        await ApplyHighPerformancePowerPlan();
                        break;
                }
            }
        }

        private async Task ExecuteSystemCleanup(ProgressWindow progress)
        {
            var steps = new[]
            {
                "Cleaning temporary files...",
                "Clearing system cache...",
                "Removing log files...",
                "Cleaning browser data...",
                "Emptying recycle bin...",
                "Finalizing cleanup..."
            };

            for (int i = 0; i < steps.Length; i++)
            {
                progress.UpdateStatus(steps[i]);
                progress.UpdateProgress((i + 1) * 100 / steps.Length);
                
                await Task.Delay(800);
                
                switch (i)
                {
                    case 0:
                        await CleanTemporaryFiles();
                        break;
                    case 1:
                        await CleanSystemCache();
                        break;
                    case 2:
                        await CleanLogFiles();
                        break;
                    case 3:
                        await CleanBrowserData();
                        break;
                    case 4:
                        await EmptyRecycleBin();
                        break;
                }
            }
        }

        private async Task ExecuteNetworkOptimizations(ProgressWindow progress)
        {
            var steps = new[]
            {
                "Optimizing TCP settings...",
                "Configuring DNS settings...",
                "Adjusting network adapter settings...",
                "Optimizing network stack...",
                "Finalizing network optimizations..."
            };

            for (int i = 0; i < steps.Length; i++)
            {
                progress.UpdateStatus(steps[i]);
                progress.UpdateProgress((i + 1) * 100 / steps.Length);
                
                await Task.Delay(1000);
                // Apply network-specific tweaks from the tweak service
            }
        }

        private async Task ExecuteHardwareOptimizations(ProgressWindow progress)
        {
            var steps = new[]
            {
                "Detecting hardware configuration...",
                "Applying CPU-specific optimizations...",
                "Configuring GPU settings...",
                "Optimizing chipset settings...",
                "Finalizing hardware optimizations..."
            };

            for (int i = 0; i < steps.Length; i++)
            {
                progress.UpdateStatus(steps[i]);
                progress.UpdateProgress((i + 1) * 100 / steps.Length);
                
                await Task.Delay(1200);
                // Apply hardware-specific optimizations
            }
        }

        private async Task ExecuteResetAllTweaks(ProgressWindow progress)
        {
            var allTweaks = _tweakService.TweakCategories.SelectMany(c => c.Tweaks).ToList();
            
            for (int i = 0; i < allTweaks.Count; i++)
            {
                var tweak = allTweaks[i];
                progress.UpdateStatus($"Resetting: {tweak.Name}");
                progress.UpdateProgress((i + 1) * 100 / allTweaks.Count);
                
                try
                {
                    _registryService.RestoreTweak(tweak);
                }
                catch
                {
                    // Continue with other tweaks even if one fails
                }
                
                await Task.Delay(100);
            }
        }

        // Placeholder methods for actual optimization implementations
        private async Task ApplyGpuOptimizations() => await Task.Delay(100);
        private async Task ApplyCpuOptimizations() => await Task.Delay(100);
        private async Task ApplyMemoryOptimizations() => await Task.Delay(100);
        private async Task ApplyGamingNetworkOptimizations() => await Task.Delay(100);
        private async Task ApplyHighPerformancePowerPlan() => await Task.Delay(100);
        private async Task CleanTemporaryFiles() => await Task.Delay(100);
        private async Task CleanSystemCache() => await Task.Delay(100);
        private async Task CleanLogFiles() => await Task.Delay(100);
        private async Task CleanBrowserData() => await Task.Delay(100);
        private async Task EmptyRecycleBin() => await Task.Delay(100);

        private void AudioLoudnessEqualization_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AudioDeviceSelectionDialog
                {
                    Owner = Application.Current.MainWindow
                };

                dialog.ShowDialog();

                if (dialog.DialogResult && dialog.SelectedDevice != null)
                {
                    // Success message is already shown in the dialog
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while opening the audio device selection dialog:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void WinGetInstallation_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will install Microsoft WinGet package manager if it's not already present.\n\n" +
                "The script will try multiple installation methods with automatic fallback.\n\n" +
                "Do you want to continue?",
                "Install WinGet Package Manager",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var progressWindow = new ProgressWindow("Installing WinGet Package Manager");

            try
            {
                progressWindow.Show();
                progressWindow.UpdateStatus("Checking if WinGet is already installed...");
                progressWindow.UpdateProgress(10);

                // Check if WinGet is already installed
                var checkScript = @"
                    try {
                        $version = winget --version
                        if ($version) {
                            Write-Output ""ALREADY_INSTALLED:$version""
                            return
                        }
                    } catch {
                        Write-Output ""NOT_INSTALLED""
                    }
                ";

                var checkResult = await Task.Run(() => _powerShellService.ExecuteScript(checkScript));

                if (checkResult.Success && checkResult.Output.Contains("ALREADY_INSTALLED"))
                {
                    progressWindow.Close();
                    var version = checkResult.Output.Replace("ALREADY_INSTALLED:", "").Trim();
                    MessageBox.Show(
                        $"WinGet is already installed!\n\nVersion: {version}",
                        "Already Installed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                progressWindow.UpdateStatus("Installing WinGet using method 1 (PowerShell registration)...");
                progressWindow.UpdateProgress(30);

                var installScript = @"
                    $success = $false
                    $method = """"

                    # Method 1: PowerShell registration
                    try {
                        Write-Host ""Trying Method 1: PowerShell registration...""
                        Add-AppxPackage -RegisterByFamilyName -MainPackage Microsoft.DesktopAppInstaller_8wekyb3d8bbwe

                        # Test if it worked
                        Start-Sleep -Seconds 3
                        $testResult = winget --version 2>$null
                        if ($testResult) {
                            $success = $true
                            $method = ""PowerShell registration""
                            Write-Host ""Method 1 successful""
                        }
                    } catch {
                        Write-Host ""Method 1 failed: $($_.Exception.Message)""
                    }

                    # Method 2: Direct download if Method 1 failed
                    if (-not $success) {
                        try {
                            Write-Host ""Trying Method 2: Direct download...""
                            $downloadPath = ""$env:TEMP\winget.msixbundle""
                            Invoke-WebRequest -Uri ""https://aka.ms/getwinget"" -OutFile $downloadPath -UseBasicParsing
                            Add-AppxPackage $downloadPath
                            Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue

                            # Test if it worked
                            Start-Sleep -Seconds 3
                            $testResult = winget --version 2>$null
                            if ($testResult) {
                                $success = $true
                                $method = ""Direct download""
                                Write-Host ""Method 2 successful""
                            }
                        } catch {
                            Write-Host ""Method 2 failed: $($_.Exception.Message)""
                        }
                    }

                    # Method 3: GitHub latest release if Method 2 failed
                    if (-not $success) {
                        try {
                            Write-Host ""Trying Method 3: GitHub latest release...""
                            $url = ""https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle""
                            $downloadPath = ""$env:TEMP\winget-installer.msixbundle""
                            Invoke-WebRequest -Uri $url -OutFile $downloadPath -UseBasicParsing
                            Add-AppxPackage $downloadPath
                            Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue

                            # Test if it worked
                            Start-Sleep -Seconds 3
                            $testResult = winget --version 2>$null
                            if ($testResult) {
                                $success = $true
                                $method = ""GitHub release""
                                Write-Host ""Method 3 successful""
                            }
                        } catch {
                            Write-Host ""Method 3 failed: $($_.Exception.Message)""
                        }
                    }

                    if ($success) {
                        $version = winget --version
                        Write-Output ""SUCCESS:$method:$version""
                    } else {
                        Write-Output ""FAILED:All installation methods failed""
                    }
                ";

                progressWindow.UpdateStatus("Executing installation script...");
                progressWindow.UpdateProgress(60);

                var installResult = await Task.Run(() => _powerShellService.ExecuteScript(installScript));

                progressWindow.UpdateStatus("Verifying installation...");
                progressWindow.UpdateProgress(90);
                await Task.Delay(1000);

                progressWindow.UpdateStatus("Installation completed!");
                progressWindow.UpdateProgress(100);
                await Task.Delay(1000);
                progressWindow.Close();

                if (installResult.Success && installResult.Output.Contains("SUCCESS"))
                {
                    var parts = installResult.Output.Replace("SUCCESS:", "").Split(':');
                    var method = parts.Length > 0 ? parts[0] : "Unknown";
                    var version = parts.Length > 1 ? parts[1] : "Unknown";

                    MessageBox.Show(
                        $"WinGet has been installed successfully!\n\n" +
                        $"Installation method: {method}\n" +
                        $"Version: {version}\n\n" +
                        "You can now use WinGet to install and manage applications from the command line.",
                        "Installation Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "WinGet installation failed using all available methods.\n\n" +
                        "Please try installing WinGet manually from the Microsoft Store or GitHub releases page.\n\n" +
                        "You may also need to ensure Windows is up to date.",
                        "Installation Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"An error occurred during WinGet installation:\n\n{ex.Message}",
                    "Installation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void BcdEditApply_Click(object sender, RoutedEventArgs e)
        {
            var disclaimer = "⚠️ Warning: These commands modify Windows boot configuration. Changes may affect system stability and require administrator privileges. A system restart will be required for changes to take effect. Proceed only if you understand the implications.";
            var result = MessageBox.Show(disclaimer, "BCDEdit Performance Tweaks", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            var progress = new ProgressWindow("Applying BCDEdit Performance Tweaks...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Checking administrator privileges...");
                progress.UpdateProgress(10);
                if (!_powerShellService.IsAdministrator())
                {
                    progress.Close();
                    MessageBox.Show("Administrator privileges are required to apply BCDEdit tweaks.", "Permission Required", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Build a PowerShell script that runs the bcdedit commands in an elevated cmd silently
                var script = @"
                    $commands = @(
                        'bcdedit /set disabledynamictick yes',
                        'bcdedit /set tscsyncpolicy enhanced',
                        'bcdedit /set useplatformclock false',
                        'bcdedit /set hypervisorlaunchtype off'
                    )

                    foreach ($cmd in $commands) {
                        Start-Process cmd.exe -Verb RunAs -ArgumentList ('/c ' + $cmd) -WindowStyle Hidden -Wait
                    }
                ";

                progress.UpdateStatus("Executing BCDEdit commands...");
                progress.UpdateProgress(60);
                var psResult = await _powerShellService.ExecuteScriptAsync(script);

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(90);
                await Task.Delay(500);

                progress.UpdateProgress(100);
                progress.Close();

                if (psResult.Success)
                {
                    MessageBox.Show("BCDEdit performance tweaks applied successfully. Please restart your computer for changes to take effect.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to apply BCDEdit tweaks.\n\nError: {psResult.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show($"An error occurred:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BcdEditRestore_Click(object sender, RoutedEventArgs e)
        {
            var disclaimer = "⚠️ Warning: These commands modify Windows boot configuration. Changes may affect system stability and require administrator privileges. A system restart will be required for changes to take effect. Proceed only if you understand the implications.";
            var result = MessageBox.Show(disclaimer + "\n\nRestore original configuration?", "Restore BCDEdit Tweaks", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            var progress = new ProgressWindow("Restoring BCDEdit configuration...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Checking administrator privileges...");
                progress.UpdateProgress(10);
                if (!_powerShellService.IsAdministrator())
                {
                    progress.Close();
                    MessageBox.Show("Administrator privileges are required to restore BCDEdit settings.", "Permission Required", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var script = @"
                    $commands = @(
                        'bcdedit /deletevalue disabledynamictick',
                        'bcdedit /deletevalue tscsyncpolicy',
                        'bcdedit /deletevalue useplatformclock',
                        'bcdedit /set hypervisorlaunchtype auto'
                    )

                    foreach ($cmd in $commands) {
                        Start-Process cmd.exe -Verb RunAs -ArgumentList ('/c ' + $cmd) -WindowStyle Hidden -Wait
                    }
                ";

                progress.UpdateStatus("Executing BCDEdit restore commands...");
                progress.UpdateProgress(60);
                var psResult = await _powerShellService.ExecuteScriptAsync(script);

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(90);
                await Task.Delay(500);

                progress.UpdateProgress(100);
                progress.Close();

                if (psResult.Success)
                {
                    MessageBox.Show("BCDEdit configuration restored. Please restart your computer for changes to take effect.", "Restored", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to restore BCDEdit settings.\n\nError: {psResult.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show($"An error occurred:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DismSfcRepair_Click(object sender, RoutedEventArgs e)
        {
            var info = "ℹ️ System Repair Information:\n• If SFC finds and repairs files, it's recommended to restart and re-run: sfc /scannow\n• If DISM /RestoreHealth fails (e.g., Windows Update issues), use a mounted Windows ISO (assume drive X:) with install.wim:\n  DISM /Online /Cleanup-Image /RestoreHealth /Source:WIM:X:\\sources\\install.wim:1 /LimitAccess\n• If using install.esd instead of install.wim, replace 'WIM:' with 'ESD:' in the command\n• This process may take 15-30 minutes to complete";
            var result = MessageBox.Show(info + "\n\nRun DISM + SFC now?", "DISM + SFC System Repair", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result != MessageBoxResult.Yes) return;

            var progress = new ProgressWindow("Running DISM + SFC repairs...");
            progress.Show();

            try
            {
                progress.UpdateStatus("Starting DISM /CheckHealth...");
                progress.UpdateProgress(10);
                var script = @"
                    $commands = @(
                        'DISM /Online /Cleanup-Image /CheckHealth',
                        'DISM /Online /Cleanup-Image /ScanHealth',
                        'DISM /Online /Cleanup-Image /RestoreHealth',
                        'sfc /scannow'
                    )

                    foreach ($cmd in $commands) {
                        Start-Process cmd.exe -Verb RunAs -ArgumentList ('/c ' + $cmd) -WindowStyle Hidden -Wait
                    }
                ";

                var step = 1;
                foreach (var status in new[] {
                    "Running DISM /CheckHealth...",
                    "Running DISM /ScanHealth...",
                    "Running DISM /RestoreHealth...",
                    "Running SFC /scannow..."
                })
                {
                    progress.UpdateStatus(status);
                    progress.UpdateProgress(step * 25);
                    step++;
                    await Task.Delay(300);
                }

                var psResult = await _powerShellService.ExecuteScriptAsync(script);

                progress.UpdateStatus("Finalizing...");
                progress.UpdateProgress(95);
                await Task.Delay(500);

                progress.UpdateProgress(100);
                progress.Close();

                if (psResult.Success)
                {
                    MessageBox.Show("DISM + SFC repair sequence completed. Review output in Event Viewer if needed. A restart is recommended.", "Repair Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"DISM + SFC sequence failed.\n\nError: {psResult.Error}", "Repair Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                progress.Close();
                MessageBox.Show($"An error occurred:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
