using System.Windows;
using System.Windows.Threading;
using TweakHub.Services;

namespace TweakHub.Views
{
    public partial class LoadingWindow : Window
    {
        private readonly DispatcherTimer _loadingTimer;
        private int _currentStep = 0;
        private readonly string[] _loadingSteps = new[]
        {
            "Initializing system analysis...",
            "Reading system specifications...",
            "Scanning registry entries...",
            "Loading performance tweaks...",
            "Preparing user interface...",
            "Finalizing setup..."
        };

        // Track initialization state
        private bool _initializationFailed = false;
        private readonly object _initLock = new object();

        public LoadingWindow()
        {
            InitializeComponent();

            _loadingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(800)
            };
            _loadingTimer.Tick += LoadingTimer_Tick;

            Loaded += LoadingWindow_Loaded;
        }

        private void LoadingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _loadingTimer.Start();
        }

        private async void LoadingTimer_Tick(object? sender, EventArgs e)
        {
            if (_initializationFailed)
            {
                _loadingTimer.Stop();
                ShowErrorAndExit();
                return;
            }

            if (_currentStep < _loadingSteps.Length)
            {
                LoadingText.Text = _loadingSteps[_currentStep];
                LoadingProgress.Value = (double)(_currentStep + 1) / _loadingSteps.Length * 100;

                try
                {
                    switch (_currentStep)
                    {
                        case 1: // Reading system specifications
                            await InitializeSystemInfoSafe();
                            break;
                        case 2: // Scanning registry entries
                            await InitializeRegistryServiceSafe();
                            break;
                        case 3: // Loading performance tweaks
                            await InitializeTweakDataSafe();
                            break;
                        case 4: // Preparing user interface
                            await InitializeServicesSafe();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Initialization failed at step {_currentStep}: {ex}");
                    _initializationFailed = true;
                    return;
                }

                _currentStep++;
            }
            else
            {
                _loadingTimer.Stop();
                await CompleteInitialization();
            }
        }

        private async Task InitializeSystemInfoSafe()
        {
            await Task.Run(() =>
            {
                lock (_initLock)
                {
                    try
                    {
                        var systemService = SystemMonitoringService.Instance;
                        systemService.Initialize();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SystemMonitoringService init failed: {ex}");
                        throw;
                    }
                }
            });
        }

        private async Task InitializeRegistryServiceSafe()
        {
            await Task.Run(() =>
            {
                lock (_initLock)
                {
                    try
                    {
                        var registryService = RegistryService.Instance;
                        registryService.Initialize();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"RegistryService init failed: {ex}");
                        throw;
                    }
                }
            });
        }

        private async Task InitializeTweakDataSafe()
        {
            await Task.Run(() =>
            {
                lock (_initLock)
                {
                    try
                    {
                        var tweakService = TweakService.Instance;
                        tweakService.LoadTweaks();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"TweakService init failed: {ex}");
                        throw;
                    }
                }
            });
        }

        private async Task InitializeServicesSafe()
        {
            await Task.Run(() =>
            {
                lock (_initLock)
                {
                    try
                    {
                        var shortcutService = ShortcutService.Instance;
                        shortcutService.Initialize();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ShortcutService init failed: {ex}");
                        throw;
                    }
                }
            });
        }

        private async Task CompleteInitialization()
        {
            await Task.Delay(500);

            // Ensure main window creation happens on UI thread
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow creation failed: {ex}");
                    ShowErrorAndExit();
                }
            });
        }

        private void ShowErrorAndExit()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    "TweakHub failed to initialize properly. Please restart the application.\n\n" +
                    "If the problem persists, try running as administrator.",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown();
            });
        }
    }
}
