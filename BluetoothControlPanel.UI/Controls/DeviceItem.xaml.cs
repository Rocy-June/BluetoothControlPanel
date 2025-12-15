using System.Windows;
using System.Windows.Controls;
using BluetoothControlPanel.Core.Bluetooth;

namespace BluetoothControlPanel.UI.Controls;

public partial class DeviceItem : Button
{
    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register(
        nameof(Device),
        typeof(DeviceInfo),
        typeof(DeviceItem),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty ConnectTextProperty = DependencyProperty.Register(
        nameof(ConnectText),
        typeof(string),
        typeof(DeviceItem),
        new PropertyMetadata("Connected")
    );

    public static readonly DependencyProperty DisconnectTextProperty = DependencyProperty.Register(
        nameof(DisconnectText),
        typeof(string),
        typeof(DeviceItem),
        new PropertyMetadata("Disconnected")
    );

    public DeviceInfo? Device
    {
        get => (DeviceInfo?)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public string ConnectText
    {
        get => (string)GetValue(ConnectTextProperty);
        set => SetValue(ConnectTextProperty, value);
    }

    public string DisconnectText
    {
        get => (string)GetValue(DisconnectTextProperty);
        set => SetValue(DisconnectTextProperty, value);
    }

    public DeviceItem()
    {
        InitializeComponent();
    }
}
