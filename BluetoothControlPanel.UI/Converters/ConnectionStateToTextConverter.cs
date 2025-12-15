using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Windows.Devices.Bluetooth;

namespace BluetoothControlPanel.UI.Converters;

public class ConnectionStateToTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var value = values is { Length: > 0 } ? values[0] : null;
        var disconnectedString = values is { Length: > 1 } && values[1] is not null ? values[1] : BluetoothConnectionStatus.Disconnected.ToString();
        var connectedString = values is { Length: > 2 } && values[2] is not null ? values[2] : BluetoothConnectionStatus.Connected.ToString();

        return value switch
        {
            BluetoothConnectionStatus.Disconnected => disconnectedString,
            BluetoothConnectionStatus.Connected => connectedString,
            _ => string.Empty,
        };
    }

    public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) =>
        targetType.Select(_ => Binding.DoNothing).ToArray();
}
