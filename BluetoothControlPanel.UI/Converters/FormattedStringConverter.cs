using System;
using System.Globalization;
using System.Windows.Data;

namespace BluetoothControlPanel.UI.Converters;

/// <summary>
/// Formats a value with optional ToString format, formatter string, and fallback.
/// Expected MultiBinding values: [0] value, [1] toStringFormat (string, optional), [2] formatter (string), [3] fallback.
/// </summary>
public class FormattedStringConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var source = values is { Length: > 0 } ? values[0] : null;
        var toStringFormat = values is { Length: > 1 } ? values[1] as string : null;
        var formatter = values is { Length: > 2 } ? values[2] as string : null;
        var fallback = values is { Length: > 3 } ? values[3] : null;

        var text = string.Empty;
        if (source is IFormattable formattable && !string.IsNullOrWhiteSpace(toStringFormat))
        {
            text = formattable.ToString(toStringFormat, culture);
        }
        else if (source is not null)
        {
            text = source.ToString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(text))
        {
            text = fallback?.ToString() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(formatter))
        {
            return text;
        }

        return string.Format(culture, formatter, text);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => [];
}
