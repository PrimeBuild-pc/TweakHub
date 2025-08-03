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
            if (_currentStep < _loadingSteps.Length)
            {
                LoadingText.Text = _loadingSteps[_currentStep];
                LoadingProgress.Value = (double)(_currentStep + 1) / _loadingSteps.Length * 100;
                
                // Simulate actual loading work
                await Task.Delay(200);
                
                switch (_currentStep)
                {
                    case 1: // Reading system specifications
                        await InitializeSystemInfo();
                        break;
                    case 2: // Scanning registry entries
                        await InitializeRegistryService();
                        break;
                    case 3: // Loading performance tweaks
                        await InitializeTweakData();
                        break;
                    case 4: // Preparing user interface
                        await InitializeServices();
                        break;
                }
                
                _currentStep++;
            }
            else
            {
                _loadingTimer.Stop();
                await Task.Delay(500); // Brief pause before showing main window
                
                // Show main window
                var mainWindow = new MainWindow();
                mainWindow.Show();
                
                // Close loading window
                Close();
            }
        }

        private async Task InitializeSystemInfo()
        {
            await Task.Run(() =>
            {
                // Initialize system monitoring service
                var systemService = SystemMonitoringService.Instance;
                systemService.Initialize();
            });
        }

        private async Task InitializeRegistryService()
        {
            await Task.Run(() =>
            {
                // Initialize registry service
                var registryService = RegistryService.Instance;
                registryService.Initialize();
            });
        }

        private async Task InitializeTweakData()
        {
            await Task.Run(() =>
            {
                // Load tweak definitions
                var tweakService = TweakService.Instance;
                tweakService.LoadTweaks();
            });
        }

        private async Task InitializeServices()
        {
            await Task.Run(() =>
            {
                // Initialize other services
                var shortcutService = ShortcutService.Instance;
                shortcutService.Initialize();
            });
        }
    }
}
