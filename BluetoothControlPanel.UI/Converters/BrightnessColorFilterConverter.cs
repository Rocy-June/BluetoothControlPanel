using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace BluetoothControlPanel.UI.Converters;

public class BrightnessColorFilterConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var color = values is { Length: > 0 } && values[0] is Color c ? c : Colors.Transparent;
        var brightness = values is { Length: > 1 } && values[1] is double b ? b : 1d;
        
        color.R = (byte)Math.Clamp(color.R * brightness, 0, 255);
        color.G = (byte)Math.Clamp(color.G * brightness, 0, 255);
        color.B = (byte)Math.Clamp(color.B * brightness, 0, 255);

        return color;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => [];
}
