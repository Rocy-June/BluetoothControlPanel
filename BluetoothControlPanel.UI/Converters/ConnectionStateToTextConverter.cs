using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using BluetoothControlPanel.Domain.Bluetooth;
using Windows.Devices.Bluetooth;

namespace BluetoothControlPanel.UI.Converters;

public class ConnectionStateToTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var device = values.OfType<DeviceInfo>().FirstOrDefault();
        var isPaired = values is { Length: > 1 } && values[1] is bool b && b;

        if (device is null) return string.Empty;

        if (isPaired)
        {
            return device.ConnectionState switch
            {
                BluetoothConnectionStatus.Disconnected => "Paired",
                BluetoothConnectionStatus.Connected => "Connected",
                _ => "Unknown",
            };
        }
        else 
        {
            return string.Empty;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        targetTypes.Select(_ => Binding.DoNothing).ToArray();
}
