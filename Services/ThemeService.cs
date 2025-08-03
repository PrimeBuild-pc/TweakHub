using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TweakHub.Services
{
    public enum AppTheme
    {
        Light,
        Dark,
        System
    }

    public class ThemeService : INotifyPropertyChanged
    {
        private static ThemeService? _instance;
        private AppTheme _currentTheme = AppTheme.Dark;

        public static ThemeService Instance => _instance ??= new ThemeService();

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    OnPropertyChanged();
                    ApplyTheme();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private ThemeService()
        {
            LoadThemeFromSettings();
            // Apply the theme immediately after loading to ensure proper initialization
            ApplyTheme();
        }

        private void LoadThemeFromSettings()
        {
            // Load theme preference from registry or config file
            try
            {
                var savedTheme = Properties.Settings.Default.Theme;
                if (Enum.TryParse<AppTheme>(savedTheme, out var theme))
                {
                    _currentTheme = theme;
                }
            }
            catch
            {
                _currentTheme = AppTheme.Dark;
            }
        }

        public void SaveThemeToSettings()
        {
            try
            {
                Properties.Settings.Default.Theme = _currentTheme.ToString();
                Properties.Settings.Default.Save();
            }
            catch
            {
                // Handle save error silently
            }
        }

        private void ApplyTheme()
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            var actualTheme = GetActualTheme();
            var isDark = actualTheme == AppTheme.Dark;

            // Don't clear all resources, just update the theme-specific ones
            // Remove existing ModernWPF theme resources
            var toRemove = app.Resources.MergedDictionaries
                .Where(d => d.Source?.ToString().Contains("ModernWpf") == true)
                .ToList();

            foreach (var dict in toRemove)
            {
                app.Resources.MergedDictionaries.Remove(dict);
            }

            // Add ModernWPF theme resources
            app.Resources.MergedDictionaries.Insert(0, new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/ModernWpf;component/ThemeResources/Light.xaml")
            });

            var themeUri = isDark
                ? "pack://application:,,,/ModernWpf;component/ThemeResources/Dark.xaml"
                : "pack://application:,,,/ModernWpf;component/ThemeResources/Light.xaml";

            app.Resources.MergedDictionaries.Insert(1, new System.Windows.ResourceDictionary
            {
                Source = new Uri(themeUri)
            });

            // Apply comprehensive theme overrides
            ApplyThemeOverrides(isDark);

            SaveThemeToSettings();
        }

        private void ApplyThemeOverrides(bool isDark)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            if (isDark)
            {
                // Background colors
                app.Resources["SystemControlBackgroundBaseLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26)); // #1A1A1A
                app.Resources["SystemControlBackgroundChromeMediumLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)); // #2D2D2D
                app.Resources["SystemControlBackgroundChromeMediumBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 58, 58)); // #3A3A3A
                app.Resources["SystemControlBackgroundListLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 58, 58)); // #3A3A3A
                app.Resources["SystemControlBackgroundListMediumBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)); // #2D2D2D

                // Text colors
                app.Resources["SystemControlForegroundBaseHighBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                app.Resources["SystemControlForegroundBaseMediumBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(176, 176, 176)); // #B0B0B0
                app.Resources["SystemControlForegroundBaseLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74)); // #4A4A4A

                // Border colors
                app.Resources["SystemControlBorderBaseLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74)); // #4A4A4A

                // Icon colors for dark theme - Tutte le icone bianche
                app.Resources["IconBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                app.Resources["IconSecondaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(176, 176, 176)); // #B0B0B0

                // Additional system colors that might be used
                app.Resources["ApplicationPageBackgroundThemeBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
                app.Resources["SystemControlPageBackgroundBaseLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
                app.Resources["SystemControlPageBackgroundChromeLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
            }
            else
            {
                // Light theme colors
                app.Resources["SystemControlBackgroundBaseLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                app.Resources["SystemControlBackgroundChromeMediumLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250)); // #F8F9FA
                app.Resources["SystemControlBackgroundChromeMediumBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                app.Resources["SystemControlBackgroundListLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                app.Resources["SystemControlBackgroundListMediumBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250));

                // Text colors
                app.Resources["SystemControlForegroundBaseHighBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 37, 41)); // #212529
                app.Resources["SystemControlForegroundBaseMediumBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 117, 125)); // #6C757D
                app.Resources["SystemControlForegroundBaseLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(222, 226, 230)); // #DEE2E6

                // Border colors
                app.Resources["SystemControlBorderBaseLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(222, 226, 230));

                // Icon colors for light theme - Tutte le icone scure
                app.Resources["IconBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 37, 41)); // #212529
                app.Resources["IconSecondaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 117, 125)); // #6C757D

                // Additional system colors
                app.Resources["ApplicationPageBackgroundThemeBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                app.Resources["SystemControlPageBackgroundBaseLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                app.Resources["SystemControlPageBackgroundChromeLowBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250));
            }
        }

        private AppTheme GetActualTheme()
        {
            if (_currentTheme == AppTheme.System)
            {
                return IsSystemDarkTheme() ? AppTheme.Dark : AppTheme.Light;
            }
            return _currentTheme;
        }

        private bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
