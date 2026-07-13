using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using ModernWpf;

namespace TweakHub.Services;

public sealed class ThemeService : INotifyPropertyChanged
{
    private static ThemeService? _instance;
    private bool _isDark;

    public static ThemeService Instance => _instance ??= new ThemeService();
    public bool IsDark => _isDark;

    public event PropertyChangedEventHandler? PropertyChanged;

    private ThemeService() => ApplySystemTheme();

    public void RefreshSystemThemeIfNeeded() => ApplySystemTheme();

    private void ApplySystemTheme()
    {
        var app = Application.Current;
        if (app == null) return;

        _isDark = IsSystemDarkTheme();
        ThemeManager.Current.ApplicationTheme = null;

        var accent = GetSystemAccentColor();
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

        SetBrush(app, "WindowBackgroundBrush", Color.FromArgb(235, background.R, background.G, background.B));
        SetBrush(app, "NavigationBrush", Color.FromArgb(210, background.R, background.G, background.B));
        SetBrush(app, "CardBrush", Color.FromArgb(224, surface.R, surface.G, surface.B));
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

        OnPropertyChanged(nameof(IsDark));
    }

    private static void SetBrush(Application app, string key, Color color) =>
        app.Resources[key] = new SolidColorBrush(color);

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
