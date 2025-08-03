using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TweakHub.Services;

namespace TweakHub.Views
{
    public partial class AudioDeviceSelectionDialog : Window
    {
        private List<AudioDevice> _audioDevices = new();
        private AudioDevice? _selectedDevice;

        public AudioDevice? SelectedDevice => _selectedDevice;
        public new bool DialogResult { get; private set; }

        public AudioDeviceSelectionDialog()
        {
            InitializeComponent();
            Loaded += AudioDeviceSelectionDialog_Loaded;
        }

        private async void AudioDeviceSelectionDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAudioDevices();
        }

        private async Task LoadAudioDevices()
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                DeviceListBox.Visibility = Visibility.Collapsed;

                // Load audio devices in background
                await Task.Run(() =>
                {
                    _audioDevices = AudioDeviceService.Instance.GetAudioOutputDevices();
                });

                // Update UI on main thread
                Dispatcher.Invoke(() =>
                {
                    DeviceListBox.ItemsSource = _audioDevices;
                    
                    // Select default device if available
                    var defaultDevice = _audioDevices.FirstOrDefault(d => d.IsDefault);
                    if (defaultDevice != null)
                    {
                        DeviceListBox.SelectedItem = defaultDevice;
                        _selectedDevice = defaultDevice;
                        OkButton.IsEnabled = true;
                    }

                    LoadingPanel.Visibility = Visibility.Collapsed;
                    DeviceListBox.Visibility = Visibility.Visible;

                    if (!_audioDevices.Any())
                    {
                        ShowNoDevicesMessage();
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    ShowErrorMessage($"Failed to load audio devices: {ex.Message}");
                });
            }
        }

        private void ShowNoDevicesMessage()
        {
            var messagePanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconText = new TextBlock
            {
                Text = "🔇",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            iconText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");

            var messageText = new TextBlock
            {
                Text = "No audio output devices found.\nPlease check your audio drivers and try again.",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            messageText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");

            messagePanel.Children.Add(iconText);
            messagePanel.Children.Add(messageText);

            var parentGrid = (Grid)DeviceListBox.Parent;
            parentGrid.Children.Remove(DeviceListBox);
            parentGrid.Children.Add(messagePanel);
            Grid.SetRow(messagePanel, 1);
        }

        private void ShowErrorMessage(string message)
        {
            var errorPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconText = new TextBlock
            {
                Text = "❌",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };

            var messageText = new TextBlock
            {
                Text = message,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 400
            };
            messageText.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlForegroundBaseMediumBrush");

            errorPanel.Children.Add(iconText);
            errorPanel.Children.Add(messageText);

            var parentGrid = (Grid)DeviceListBox.Parent;
            parentGrid.Children.Remove(DeviceListBox);
            parentGrid.Children.Add(errorPanel);
            Grid.SetRow(errorPanel, 1);
        }

        private void DeviceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDevice = DeviceListBox.SelectedItem as AudioDevice;
            OkButton.IsEnabled = _selectedDevice != null;
        }

        private async void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice == null)
            {
                MessageBox.Show("Please select an audio device.", "No Device Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Show progress
                OkButton.IsEnabled = false;
                OkButton.Content = "Processing...";
                CancelButton.IsEnabled = false;

                // Create progress window
                var progressWindow = new ProgressWindow
                {
                    Owner = this,
                    Title = "Enabling Loudness Equalization"
                };

                progressWindow.Show();
                progressWindow.UpdateStatus("Downloading and executing loudness equalization script...");
                progressWindow.UpdateProgress(50);

                // Execute the loudness equalization script
                var success = await Task.Run(() => 
                    AudioDeviceService.Instance.EnableLoudnessEqualization(_selectedDevice.Name));

                progressWindow.Close();

                if (success)
                {
                    progressWindow.UpdateStatus("Loudness equalization enabled successfully!");
                    progressWindow.UpdateProgress(100);
                    
                    MessageBox.Show(
                        $"Loudness equalization has been enabled for:\n{_selectedDevice.DisplayName}\n\n" +
                        "You may need to restart audio applications for the changes to take effect.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(
                        "Failed to enable loudness equalization. Please try running TweakHub as administrator and ensure the selected device is available.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // Reset button state
                    OkButton.IsEnabled = true;
                    OkButton.Content = "Enable Loudness Equalization";
                    CancelButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Reset button state
                OkButton.IsEnabled = true;
                OkButton.Content = "Enable Loudness Equalization";
                CancelButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
