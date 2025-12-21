using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

using App = System.Windows.Application;

namespace BluetoothControlPanel.Infrastructure.Services.Theme;

public static class ThemeService
{
    private const int BASE_IDX = 1;

    private const string AccentBrushKey = "AccentAccentDark1Brush";
    private static SolidColorBrush? _accentBrush;
    private static bool _initialized;
    private static ResourceDictionary? _windowsModeDictionary;
    private static ResourceDictionary? _windowsMainThemeDictionary;
    private static ResourceDictionary? _themeModeDictionary;

    public static void Initialize(App app)
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
            App.Current?.Dispatcher?.Invoke(() =>
            {
                if (App.Current is not null)
                {
                    UpdateAccentBrush(App.Current);
                }
            });
        }
    }

    private static void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General ||
            e.Category == UserPreferenceCategory.Color)
        {
            App.Current?.Dispatcher?.Invoke(() =>
            {
                if (App.Current is not null)
                {
                    ApplyThemeDictionaries(App.Current);
                }
            });
        }
    }

    private static void UpdateAccentBrush(App app)
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

    private static SolidColorBrush ResolveOrCreateBrush(App app, Color color)
    {
        if (app.Resources[AccentBrushKey] is SolidColorBrush existing && !existing.IsFrozen)
        {
            existing.Color = color;
            return existing;
        }

        return new SolidColorBrush(color);
    }

    private static void ApplyThemeDictionaries(App app)
    {
        var windowsModeUri = GetWindowsModeUri();
        var windowsMainThemeUri = GetWindowsMainThemeUri();
        var themeModeUri = GetThemeModeUri();

        var idx = BASE_IDX;

        _windowsModeDictionary = ReplaceDictionary(app, idx++, _windowsModeDictionary, windowsModeUri);
        _windowsMainThemeDictionary = ReplaceDictionary(app, idx++, _windowsMainThemeDictionary, windowsMainThemeUri);
        _themeModeDictionary = ReplaceDictionary(app, idx++, _themeModeDictionary, themeModeUri);
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

    private static ResourceDictionary ReplaceDictionary(App app, int idx, ResourceDictionary? current, Uri source)
    {
        var dict = new ResourceDictionary { Source = source };
        if (current is null)
        {
            app.Resources.MergedDictionaries.Insert(idx, dict);
            return dict;
        }

        app.Resources.MergedDictionaries[idx] = dict;
        return dict;
    }
}
