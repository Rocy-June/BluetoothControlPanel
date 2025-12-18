using System.Windows;
using System.Windows.Controls;
using BluetoothControlPanel.Domain.Bluetooth;

namespace BluetoothControlPanel.UI.Controls;

public partial class DeviceItem : Button
{
    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register(
        nameof(Device),
        typeof(DeviceInfo),
        typeof(DeviceItem),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty IsPairedProperty = DependencyProperty.Register(
        nameof(IsPaired),
        typeof(bool),
        typeof(DeviceItem),
        new PropertyMetadata(false)
    );

    public DeviceInfo? Device
    {
        get => (DeviceInfo?)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public bool IsPaired
    {
        get => (bool)GetValue(IsPairedProperty);
        set => SetValue(IsPairedProperty, value);
    }

    public DeviceItem()
    {
        InitializeComponent();
    }
}
