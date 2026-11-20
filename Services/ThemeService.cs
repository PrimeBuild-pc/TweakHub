using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using ModernWpf;
using TweakHub.Localization;
using TweakHub.Models;

namespace TweakHub.Services;

public sealed class ThemeService : INotifyPropertyChanged
{
    private string _themeMode = "System";
    private string _customAccent = string.Empty;
    private bool _transparencyEnabled = true;
    private bool _isDark;

    public static ThemeService Instance { get; } = new();
    public string ThemeMode => _themeMode;
    public string CustomAccent => _customAccent;
    public bool UseSystemAccent => string.IsNullOrEmpty(_customAccent);
    public bool TransparencyEnabled => _transparencyEnabled;
    public bool IsDark => _isDark;
    public string StatusText => L.Format("UI:AppearanceStatus", L.Get($"UI:Theme{_themeMode}"), UseSystemAccent ? L.Get("UI:SystemColor") : _customAccent);

    public event PropertyChangedEventHandler? PropertyChanged;

    private ThemeService()
    {
        var appearance = UserDataService.Instance.LoadAppearance();
        _themeMode = NormalizeTheme(appearance.Theme);
        _customAccent = appearance.AccentColor ?? string.Empty;
        _transparencyEnabled = appearance.Transparency;
        ApplyTheme();
    }

    public bool SetPreferences(string themeMode, bool useSystemAccent, string customAccent, bool transparency, string language, out string error)
    {
        themeMode = NormalizeTheme(themeMode);
        customAccent = useSystemAccent ? string.Empty : customAccent.Trim();
        language = L.Normalize(language);
        if (!useSystemAccent && !TryParseColor(customAccent, out _))
        {
            error = L.Get("UI:InvalidAccentColor");
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
                Transparency = _transparencyEnabled,
                Language = language
            });
            error = string.Empty;
            return true;
        }
        catch
        {
            error = L.Get("UI:AppearanceSaveFailed");
            return false;
        }
    }

    public void RefreshSystemThemeIfNeeded() => ApplyTheme();

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

        _isDark = SystemParameters.HighContrast
            ? SystemColors.WindowColor.R + SystemColors.WindowColor.G + SystemColors.WindowColor.B < 384
            : _themeMode switch
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

        if (SystemParameters.HighContrast)
        {
            var window = SystemColors.WindowColor;
            var windowText = SystemColors.WindowTextColor;
            var highlight = SystemColors.HighlightColor;
            var highlightText = SystemColors.HighlightTextColor;
            ApplyFluentAccent(app, highlight, highlightText, SystemColors.HotTrackColor);
            ApplyTableSelection(app, window, window, highlight);
            SetBrush(app, "ComboBoxDropDownBackground", window);
            ApplyDropDownSurface(app, window, windowText);
            SetBrush(app, "NavigationViewItemBackgroundSelected", window);
            SetBrush(app, "NavigationViewItemBackgroundSelectedPointerOver", window);
            ApplyTextInputSurface(app, window);
            // Drop the accent focus line so the system high-contrast focus colours apply again.
            app.Resources.Remove("TextControlBorderBrushFocused");
            app.Resources.Remove("TextControlBorderThemeThicknessFocused");
            SetBrush(app, "SuccessBrush", highlight);
            SetBrush(app, "SuccessForegroundBrush", highlightText);
            SetBrush(app, "WarningBrush", windowText);
            SetBrush(app, "DangerBrush", highlight);
            SetBrush(app, "DangerForegroundBrush", highlightText);
            SetBrush(app, "WindowBackgroundBrush", window);
            SetBrush(app, "NavigationBrush", window);
            SetBrush(app, "CardBrush", window);
            SetBrush(app, "CardHoverBrush", window);
            SetBrush(app, "CardPressedBrush", window);
            SetBrush(app, "NeutralActionFillBrush", window);
            SetBrush(app, "NeutralActionHoverBrush", window);
            SetBrush(app, "NeutralActionPressedBrush", window);
            SetBrush(app, "SelectionBrush", highlight);
            SetBrush(app, "SelectionForegroundBrush", highlightText);
            SetBrush(app, "SubtleFillBrush", highlight);
            SetBrush(app, "SystemControlBackgroundBaseLowBrush", window);
            SetBrush(app, "SystemControlBackgroundChromeMediumLowBrush", window);
            SetBrush(app, "SystemControlBackgroundChromeMediumBrush", window);
            SetBrush(app, "SystemControlBackgroundListLowBrush", highlight);
            SetBrush(app, "SystemControlBackgroundListMediumBrush", highlight);
            SetBrush(app, "SystemControlForegroundBaseHighBrush", windowText);
            SetBrush(app, "SystemControlForegroundBaseMediumBrush", SystemColors.GrayTextColor);
            SetBrush(app, "SystemControlForegroundBaseLowBrush", windowText);
            SetBrush(app, "SystemControlBorderBaseLowBrush", windowText);
            SetBrush(app, "SystemControlBorderBaseMediumBrush", windowText);
            SetBrush(app, "IconBrush", windowText);
            SetBrush(app, "IconSecondaryBrush", windowText);
        }
        else
        {
            var useCustomAccent = TryParseColor(_customAccent, out var selectedAccent);
            var success = _isDark ? Color.FromRgb(108, 203, 95) : Color.FromRgb(15, 123, 15);
            var warning = _isDark ? Color.FromRgb(252, 225, 0) : Color.FromRgb(157, 93, 0);
            var danger = _isDark ? Color.FromRgb(255, 153, 164) : Color.FromRgb(196, 43, 28);
            var background = _isDark ? Color.FromRgb(32, 32, 32) : Color.FromRgb(243, 243, 243);
            var surface = _isDark ? Color.FromRgb(44, 44, 44) : Colors.White;
            var hover = _isDark ? Color.FromArgb(15, 255, 255, 255) : Color.FromArgb(9, 0, 0, 0);
            var border = _isDark ? Color.FromArgb(24, 255, 255, 255) : Color.FromArgb(15, 0, 0, 0);
            var primary = _isDark ? Colors.White : Color.FromRgb(27, 27, 27);
            var secondary = _isDark ? Color.FromRgb(200, 200, 200) : Color.FromRgb(96, 96, 96);

            var fluentAccent = useCustomAccent
                ? Blend(selectedAccent, _isDark ? Colors.White : Colors.Black, 0.28)
                : _isDark ? SystemColors.AccentColorLight2 : SystemColors.AccentColorDark1;
            var fluentAccentText = useCustomAccent
                ? Blend(selectedAccent, _isDark ? Colors.White : Colors.Black, 0.45)
                : _isDark ? SystemColors.AccentColorLight3 : SystemColors.AccentColorDark2;
            ApplyFluentAccent(app, fluentAccent, GetContrastingForeground(fluentAccent), fluentAccentText);
            ApplyTableSelection(app, Blend(background, primary, 0.08), Blend(background, primary, 0.12), Colors.Transparent);
            SetBrush(app, "ComboBoxDropDownBackground", _isDark ? Color.FromRgb(43, 43, 43) : Color.FromRgb(249, 249, 249));
            ApplyDropDownSurface(app, _isDark ? Color.FromRgb(56, 56, 56) : Colors.White, border);
            SetBrush(app, "NavigationViewItemBackgroundSelected", _isDark ? Blend(background, primary, 0.06) : surface);
            SetBrush(app, "NavigationViewItemBackgroundSelectedPointerOver", _isDark ? Blend(background, primary, 0.09) : Blend(surface, primary, 0.025));

            ApplyTextInputSurface(app, _isDark ? Color.FromRgb(48, 48, 48) : Colors.White);
            ApplyTextInputFocus(app, fluentAccent, border);
            SetBrush(app, "SuccessBrush", success);
            SetBrush(app, "SuccessForegroundBrush", GetContrastingForeground(success));
            SetBrush(app, "WarningBrush", warning);
            SetBrush(app, "DangerBrush", danger);
            SetBrush(app, "DangerForegroundBrush", GetContrastingForeground(danger));
            SetBrush(app, "WindowBackgroundBrush", WithAlpha(background, (byte)(_transparencyEnabled ? 235 : 255)));
            SetBrush(app, "NavigationBrush", WithAlpha(background, (byte)(_transparencyEnabled ? 210 : 255)));
            SetBrush(app, "CardBrush", WithAlpha(surface, (byte)(_transparencyEnabled ? 224 : 255)));
            SetBrush(app, "CardHoverBrush", Blend(surface, primary, 0.06));
            SetBrush(app, "CardPressedBrush", Blend(surface, primary, 0.10));
            SetBrush(app, "NeutralActionFillBrush", _isDark ? Color.FromRgb(55, 55, 55) : Color.FromRgb(251, 251, 251));
            SetBrush(app, "NeutralActionHoverBrush", _isDark ? Color.FromRgb(65, 65, 65) : Color.FromRgb(244, 244, 244));
            SetBrush(app, "NeutralActionPressedBrush", _isDark ? Color.FromRgb(50, 50, 50) : Color.FromRgb(237, 237, 237));
            SetBrush(app, "TabViewItemHeaderBackgroundSelected", Blend(surface, primary, 0.06));
            SetBrush(app, "TabViewItemHeaderForegroundSelected", primary);
            SetBrush(app, "SelectionBrush", surface);
            SetBrush(app, "SelectionForegroundBrush", primary);
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
        }

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

    private static void ApplyDropDownSurface(Application app, Color background, Color border)
    {
        var fill = new SolidColorBrush(background);
        var stroke = new SolidColorBrush(border);
        app.Resources["ComboBoxBackground"] = fill;
        app.Resources["ComboBoxBackgroundPointerOver"] = fill;
        app.Resources["ComboBoxBorderBrush"] = stroke;
        app.Resources["ComboBoxBorderBrushPointerOver"] = stroke;
    }

    private static void ApplyTextInputSurface(Application app, Color background)
    {
        // Keep the fill steady as focus moves. ModernWpf still supplies the focus border.
        var fill = new SolidColorBrush(background);
        app.Resources["TextControlBackground"] = fill;
        app.Resources["TextControlBackgroundPointerOver"] = fill;
        app.Resources["TextControlBackgroundFocused"] = fill;
    }

    private static void ApplyTextInputFocus(Application app, Color accent, Color border)
    {
        // Windows 11 marks a focused field with a 2px accent line on the bottom edge only; sides and top stay subtle.
        // Absolute mapping keeps the line 2px for the 32px field; taller fields use FieldMultilineStyle.
        var focus = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 32),
        };
        focus.GradientStops.Add(new GradientStop(border, 0));
        focus.GradientStops.Add(new GradientStop(border, 0.93));
        focus.GradientStops.Add(new GradientStop(accent, 0.93));
        focus.GradientStops.Add(new GradientStop(accent, 1));
        app.Resources["TextControlBorderBrushFocused"] = focus;
        app.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(1, 1, 1, 2);
    }

    private static void ApplyFluentAccent(Application app, Color accent, Color foreground, Color text)
    {
        // The ModernWpf templates reference the individual brushes so loaded controls update when the theme changes.
        var fill = new SolidColorBrush(accent);
        var hover = new SolidColorBrush(accent) { Opacity = 0.9 };
        var pressed = new SolidColorBrush(accent) { Opacity = 0.8 };
        var onAccent = new SolidColorBrush(foreground);
        app.Resources["FluentAccentColor"] = accent;
        app.Resources["FluentAccentBrush"] = fill;
        app.Resources["FluentAccentForegroundBrush"] = onAccent;
        app.Resources["AccentBrush"] = fill;
        app.Resources["AccentPressedBrush"] = pressed;
        app.Resources["AccentForegroundBrush"] = onAccent;
        app.Resources["NavigationViewSelectionIndicatorForeground"] = fill;
        app.Resources["AccentButtonBackgroundPointerOver"] = hover;
        app.Resources["AccentButtonBackgroundPressed"] = pressed;
        app.Resources["AccentButtonForegroundPointerOver"] = onAccent;
        app.Resources["AccentButtonForegroundPressed"] = onAccent;
        app.Resources["ToggleSwitchFillOn"] = fill;
        app.Resources["ToggleSwitchFillOnPointerOver"] = hover;
        app.Resources["ToggleSwitchFillOnPressed"] = pressed;
        foreach (var state in new[] { "Checked", "Indeterminate" })
        {
            app.Resources["CheckBoxCheckBackgroundFill" + state] = fill;
            app.Resources["CheckBoxCheckBackgroundFill" + state + "PointerOver"] = SystemParameters.HighContrast ? fill : hover;
            app.Resources["CheckBoxCheckBackgroundFill" + state + "Pressed"] = SystemParameters.HighContrast ? fill : pressed;
            app.Resources["CheckBoxCheckGlyphForeground" + state] = onAccent;
            app.Resources["CheckBoxCheckGlyphForeground" + state + "PointerOver"] = onAccent;
            app.Resources["CheckBoxCheckGlyphForeground" + state + "Pressed"] = onAccent;
        }
        app.Resources["ToggleSwitchStrokeOnPointerOver"] = hover;
        app.Resources["ToggleSwitchStrokeOnPressed"] = pressed;
        app.Resources["ToggleSwitchKnobFillOn"] = onAccent;
        app.Resources["ToggleSwitchKnobFillOnPointerOver"] = onAccent;
        app.Resources["ToggleSwitchKnobFillOnPressed"] = onAccent;
        SetBrush(app, "FluentAccentTextBrush", text);
    }

    private static void ApplyTableSelection(Application app, Color selected, Color hovered, Color indication)
    {
        SetBrush(app, "DataGridRowSelectedBackground", selected);
        SetBrush(app, "DataGridRowSelectedUnfocusedBackground", selected);
        SetBrush(app, "DataGridRowSelectedHoveredBackground", hovered);
        SetBrush(app, "DataGridRowSelectedHoveredUnfocusedBackground", hovered);
        SetBrush(app, "DataGridCurrencyVisualPrimaryBrush", indication);
        app.Resources["DataGridRowSelectedBackgroundOpacity"] = 1d;
        app.Resources["DataGridRowSelectedUnfocusedBackgroundOpacity"] = 1d;
        app.Resources["DataGridRowSelectedHoveredBackgroundOpacity"] = 1d;
        app.Resources["DataGridRowSelectedHoveredUnfocusedBackgroundOpacity"] = 1d;
    }

    internal static Color GetContrastingForeground(Color color)
    {
        static double Linear(byte channel)
        {
            var value = channel / 255d;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        var luminance = 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        return luminance > 0.20 ? Colors.Black : Colors.White;
    }

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
