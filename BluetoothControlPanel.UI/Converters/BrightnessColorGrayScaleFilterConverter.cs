using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace BluetoothControlPanel.UI.Converters;

public class BrightnessColorGrayScaleFilterConverter : IMultiValueConverter
{
    private static readonly double[] WEIGHTED = [0.299, 0.587, 0.114];

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var color = values is { Length: > 0 } && values[0] is Color c ? c : Colors.Transparent;
        var brightness = values is { Length: > 1 } && values[1] is double b ? b : 1d;

        var currentBrightness = color.R * WEIGHTED[0] + color.G * WEIGHTED[1] + color.B * WEIGHTED[2];
        var targetBrightness = currentBrightness * brightness;
        var delta = targetBrightness - currentBrightness;

        color.R = (byte)Math.Clamp(color.R + delta * WEIGHTED[0], 0, 255);
        color.G = (byte)Math.Clamp(color.G + delta * WEIGHTED[1], 0, 255);
        color.B = (byte)Math.Clamp(color.B + delta * WEIGHTED[2], 0, 255);

        return color;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => [];
}
