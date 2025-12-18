using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using BluetoothControlPanel.Domain.Bluetooth;
using Windows.Devices.Bluetooth;

namespace BluetoothControlPanel.UI.Converters;

/// <summary>
/// Computes button visibility based on action, connection state, and pairing state.
/// ConverterParameter: "Connect" | "Disconnect" | "Pair" | "Remove".
/// Values: [0] DeviceInfo, [1] IsPaired (bool).
/// </summary>
public class ConnectionActionVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var device = values.OfType<DeviceInfo>().FirstOrDefault();
        var isPaired = values.Length > 1 && values[1] is bool b && b;
        var action = parameter as string;
        var state = device?.ConnectionState ?? BluetoothConnectionStatus.Disconnected;

        var visible = action switch
        {
            "Connect" => isPaired && state != BluetoothConnectionStatus.Connected,
            "Disconnect" => isPaired && state == BluetoothConnectionStatus.Connected,
            "Pair" => !isPaired,
            "Remove" => isPaired,
            _ => false
        };

        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        targetTypes.Select(_ => Binding.DoNothing).ToArray();
}
