using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using ModernWpf;
using TweakHub.Models;

namespace TweakHub.Services;

public sealed class ThemeService : INotifyPropertyChanged
{
    private static ThemeService? _instance;
    private string _themeMode = "System";
    private string _customAccent = string.Empty;
    private bool _transparencyEnabled = true;
    private bool _isDark;

    public static ThemeService Instance => _instance ??= new ThemeService();
    public string ThemeMode => _themeMode;
    public string CustomAccent => _customAccent;
    public bool UseSystemAccent => string.IsNullOrEmpty(_customAccent);
    public bool TransparencyEnabled => _transparencyEnabled;
    public bool IsDark => _isDark;
    public string StatusText => $"{(_themeMode == "System" ? "Synced with Windows" : _themeMode + " mode")} • {(UseSystemAccent ? "System color" : _customAccent)}";

    public event PropertyChangedEventHandler? PropertyChanged;

    private ThemeService()
    {
        var appearance = UserDataService.Instance.LoadAppearance();
        _themeMode = NormalizeTheme(appearance.Theme);
        _customAccent = appearance.AccentColor ?? string.Empty;
        _transparencyEnabled = appearance.Transparency;
        ApplyTheme();
    }

    public bool SetPreferences(string themeMode, bool useSystemAccent, string customAccent, bool transparency, out string error)
    {
        themeMode = NormalizeTheme(themeMode);
        customAccent = useSystemAccent ? string.Empty : customAccent.Trim();
        if (!useSystemAccent && !TryParseColor(customAccent, out _))
        {
            error = "Enter a color in #RRGGBB format.";
            return false;
        }

        _themeMode = themeMode;
        _customAccent = customAccent;
        _transparencyEnabled = transparency;
        ApplyTheme();
        try
        {
            UserDataService.Instance.SaveAppearance(new AppearanceSettings
            {
                Theme = _themeMode,
                AccentColor = _customAccent,
                Transparency = _transparencyEnabled
            });
            error = string.Empty;
            return true;
        }
        catch
        {
            error = "The appearance was applied for this session but could not be saved.";
            return false;
        }
    }

    public void RefreshSystemThemeIfNeeded()
    {
        if (_themeMode == "System" || UseSystemAccent) ApplyTheme();
    }

    public void ImportAppearance(AppearanceSettings appearance)
    {
        _themeMode = NormalizeTheme(appearance.Theme);
        _customAccent = appearance.AccentColor ?? string.Empty;
        _transparencyEnabled = appearance.Transparency;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var app = Application.Current;
        if (app == null) return;

        _isDark = _themeMode switch
        {
            "Dark" => true,
            "Light" => false,
            _ => IsSystemDarkTheme()
        };
        ThemeManager.Current.ApplicationTheme = _themeMode switch
        {
            "Dark" => ApplicationTheme.Dark,
            "Light" => ApplicationTheme.Light,
            _ => null
        };

        var accent = UseSystemAccent || !TryParseColor(_customAccent, out var selectedAccent)
            ? GetSystemAccentColor()
            : selectedAccent;
        SetBrush(app, "AccentBrush", accent);
        SetBrush(app, "AccentHoverBrush", Blend(accent, _isDark ? Colors.White : Colors.Black, 0.12));
        SetBrush(app, "AccentPressedBrush", Blend(accent, Colors.Black, 0.18));
        SetBrush(app, "SuccessBrush", _isDark ? Color.FromRgb(108, 203, 95) : Color.FromRgb(15, 123, 15));
        SetBrush(app, "WarningBrush", _isDark ? Color.FromRgb(252, 225, 0) : Color.FromRgb(157, 93, 0));
        SetBrush(app, "DangerBrush", _isDark ? Color.FromRgb(255, 153, 164) : Color.FromRgb(196, 43, 28));

        var background = _isDark ? Color.FromRgb(32, 32, 32) : Color.FromRgb(243, 243, 243);
        var surface = _isDark ? Color.FromRgb(44, 44, 44) : Color.FromRgb(255, 255, 255);
        var hover = _isDark ? Color.FromArgb(15, 255, 255, 255) : Color.FromArgb(9, 0, 0, 0);
        var border = _isDark ? Color.FromArgb(24, 255, 255, 255) : Color.FromArgb(15, 0, 0, 0);
        var primary = _isDark ? Colors.White : Color.FromRgb(27, 27, 27);
        var secondary = _isDark ? Color.FromRgb(200, 200, 200) : Color.FromRgb(96, 96, 96);

        SetBrush(app, "WindowBackgroundBrush", WithAlpha(background, (byte)(_transparencyEnabled ? 235 : 255)));
        SetBrush(app, "NavigationBrush", WithAlpha(background, (byte)(_transparencyEnabled ? 210 : 255)));
        SetBrush(app, "CardBrush", WithAlpha(surface, (byte)(_transparencyEnabled ? 224 : 255)));
        SetBrush(app, "SubtleFillBrush", hover);
        SetBrush(app, "SystemControlBackgroundBaseLowBrush", background);
        SetBrush(app, "SystemControlBackgroundChromeMediumLowBrush", surface);
        SetBrush(app, "SystemControlBackgroundChromeMediumBrush", surface);
        SetBrush(app, "SystemControlBackgroundListLowBrush", hover);
        SetBrush(app, "SystemControlBackgroundListMediumBrush", Color.FromArgb((byte)(hover.A * 2), hover.R, hover.G, hover.B));
        SetBrush(app, "SystemControlForegroundBaseHighBrush", primary);
        SetBrush(app, "SystemControlForegroundBaseMediumBrush", secondary);
        SetBrush(app, "SystemControlForegroundBaseLowBrush", border);
        SetBrush(app, "SystemControlBorderBaseLowBrush", border);
        SetBrush(app, "SystemControlBorderBaseMediumBrush", Color.FromArgb((byte)(border.A * 2), border.R, border.G, border.B));
        SetBrush(app, "IconBrush", primary);
        SetBrush(app, "IconSecondaryBrush", secondary);

        OnPropertyChanged(string.Empty);
        OnPropertyChanged(nameof(StatusText));
    }

    private static string NormalizeTheme(string? theme) => theme is "Light" or "Dark" ? theme : "System";

    private static bool TryParseColor(string value, out Color color)
    {
        try
        {
            color = (Color)ColorConverter.ConvertFromString(value);
            return value.Length == 7 && value[0] == '#';
        }
        catch
        {
            color = default;
            return false;
        }
    }

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);
    private static void SetBrush(Application app, string key, Color color) => app.Resources[key] = new SolidColorBrush(color);
    private static Color Blend(Color color, Color target, double amount) => Color.FromArgb(
        color.A,
        (byte)(color.R + (target.R - color.R) * amount),
        (byte)(color.G + (target.G - color.G) * amount),
        (byte)(color.B + (target.B - color.B) * amount));

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    private static Color GetSystemAccentColor()
    {
        if (DwmGetColorizationColor(out var value, out _) == 0)
            return Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
        return Color.FromRgb(0, 120, 212);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetColorizationColor(out uint colorizationColor, out bool opaqueBlend);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
