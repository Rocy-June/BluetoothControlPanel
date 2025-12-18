using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace BluetoothControlPanel.UI.Converters;

public class BrightnessBrushGrayScaleFilterConverter : IMultiValueConverter
{
    private static readonly double[] WEIGHTED = [0.299, 0.587, 0.114];

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var brush = values is { Length: > 0 } && values[0] is SolidColorBrush c ? c : new SolidColorBrush(Colors.Transparent);
        var brightness = values is { Length: > 1 } && values[1] is double b ? b : 1d;

        var currentBrightness = brush.Color.R * WEIGHTED[0] + brush.Color.G * WEIGHTED[1] + brush.Color.B * WEIGHTED[2];
        var targetBrightness = currentBrightness * brightness;
        var delta = targetBrightness - currentBrightness;

        var color = brush.Color;

        color.R = (byte)Math.Clamp(brush.Color.R + delta * WEIGHTED[0], 0, 255);
        color.G = (byte)Math.Clamp(brush.Color.G + delta * WEIGHTED[1], 0, 255);
        color.B = (byte)Math.Clamp(brush.Color.B + delta * WEIGHTED[2], 0, 255);

        return new SolidColorBrush(color);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => [];
}
