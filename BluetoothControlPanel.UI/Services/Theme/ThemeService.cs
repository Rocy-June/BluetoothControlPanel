using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace BluetoothControlPanel.UI.Services.Theme;

public static class ThemeService
{
    private const string AccentBrushKey = "AccentBackgroundBrush";
    private static SolidColorBrush? _accentBrush;
    private static bool _initialized;
    private static ResourceDictionary? _windowsModeDictionary;
    private static ResourceDictionary? _windowsMainThemeDictionary;
    private static ResourceDictionary? _themeModeDictionary;

    public static void Initialize(Application app)
    {
        if (_initialized)
        {
            return;
        }

        UpdateAccentBrush(app);
        ApplyThemeDictionaries(app);

        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        _initialized = true;
    }

    private static void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(SystemParameters.WindowGlassColor), StringComparison.Ordinal))
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (Application.Current is not null)
                {
                    UpdateAccentBrush(Application.Current);
                }
            });
        }
    }

    private static void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General ||
            e.Category == UserPreferenceCategory.Color)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (Application.Current is not null)
                {
                    ApplyThemeDictionaries(Application.Current);
                }
            });
        }
    }

    private static void UpdateAccentBrush(Application app)
    {
        var color = SystemParameters.WindowGlassColor;

        if (_accentBrush is null)
        {
            _accentBrush = ResolveOrCreateBrush(app, color);
            app.Resources[AccentBrushKey] = _accentBrush;
        }
        else
        {
            if (_accentBrush.IsFrozen)
            {
                _accentBrush = new SolidColorBrush(color);
                app.Resources[AccentBrushKey] = _accentBrush;
            }
            else
            {
                _accentBrush.Color = color;
            }
        }
    }

    private static SolidColorBrush ResolveOrCreateBrush(Application app, Color color)
    {
        if (app.Resources[AccentBrushKey] is SolidColorBrush existing && !existing.IsFrozen)
        {
            existing.Color = color;
            return existing;
        }

        return new SolidColorBrush(color);
    }

    private static void ApplyThemeDictionaries(Application app)
    {
        var windowsModeUri = GetWindowsModeUri();
        var windowsMainThemeUri = GetWindowsMainThemeUri();
        var themeModeUri = GetThemeModeUri();

        _windowsModeDictionary = ReplaceDictionary(app, _windowsModeDictionary, windowsModeUri);
        _windowsMainThemeDictionary = ReplaceDictionary(app, _windowsMainThemeDictionary, windowsMainThemeUri);
        _themeModeDictionary = ReplaceDictionary(app, _themeModeDictionary, themeModeUri);
    }

    private static Uri GetWindowsModeUri()
    {
        var isLight = ReadPersonalizeFlag("SystemUsesLightTheme", defaultValue: true);
        return new Uri($"Styles/WindowsMode/{(isLight ? "Light" : "Dark")}.xaml", UriKind.Relative);
    }

    private static Uri GetWindowsMainThemeUri()
    {
        var isLight = ReadPersonalizeFlag("SystemUsesLightTheme", defaultValue: true);
        var accentOnMain = ReadPersonalizeFlag("ColorPrevalence", defaultValue: false);
        var folder = accentOnMain ? "EnableMainTheme" : "DisableMainTheme";
        return new Uri($"Styles/WindowsMode/{folder}/{(isLight ? "Light" : "Dark")}.xaml", UriKind.Relative);
    }

    private static Uri GetThemeModeUri()
    {
        var isLight = ReadPersonalizeFlag("AppsUseLightTheme", defaultValue: true);
        return new Uri($"Styles/ThemeMode/{(isLight ? "Light" : "Dark")}.xaml", UriKind.Relative);
    }

    private static bool ReadPersonalizeFlag(string valueName, bool defaultValue)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue(valueName) is int raw)
            {
                return raw > 0;
            }
        }
        catch
        {
            // ignore and use default
        }

        return defaultValue;
    }

    private static ResourceDictionary ReplaceDictionary(Application app, ResourceDictionary? current, Uri source)
    {
        if (current is not null)
        {
            app.Resources.MergedDictionaries.Remove(current);
        }

        var dict = new ResourceDictionary { Source = source };
        app.Resources.MergedDictionaries.Add(dict);
        return dict;
    }
}
