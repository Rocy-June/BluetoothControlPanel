using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace BluetoothControlPanel.UI.Converters;

public class BrightnessBrushFilterConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var brush = values is { Length: > 0 } && values[0] is SolidColorBrush c ? c : new SolidColorBrush(Colors.Transparent);
        var brightness = values is { Length: > 1 } && values[1] is double b ? b : 1d;

        var color = brush.Color;

        color.R = (byte)Math.Clamp(brush.Color.R * brightness, 0, 255);
        color.G = (byte)Math.Clamp(brush.Color.G * brightness, 0, 255);
        color.B = (byte)Math.Clamp(brush.Color.B * brightness, 0, 255);

        return new SolidColorBrush(color);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => [];
}
