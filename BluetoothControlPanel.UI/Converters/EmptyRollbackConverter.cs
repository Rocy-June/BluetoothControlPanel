using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace BluetoothControlPanel.UI.Converters;

/// <summary>
/// Returns the first non-null/non-whitespace string from the supplied values.
/// ConverterParameter (if string) is used as a final fallback.
/// </summary>
public class EmptyRollbackConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var firstNonEmpty = values
            .Select(v => v as string)
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

        if (!string.IsNullOrWhiteSpace(firstNonEmpty))
        {
            return firstNonEmpty!;
        }

        if (parameter is string fallback && !string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        return string.Empty;
    }

    public object[] ConvertBack(
        object value,
        Type[] targetTypes,
        object parameter,
        CultureInfo culture
    ) => [.. targetTypes.Select(_ => Binding.DoNothing)];
}
