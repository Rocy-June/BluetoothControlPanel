using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Devices.Bluetooth;

namespace BluetoothControlPanel.Core.Bluetooth;

public class DeviceInfo(
    string address,
    string? name,
    int? rssi,
    DateTimeOffset lastSeen,
    IReadOnlyList<ManufacturerData> manufacturerData,
    BluetoothConnectionStatus connectionState = BluetoothConnectionStatus.Disconnected,
    uint? classOfDevice = null,
    double? batteryLevel = null,
    DeviceType? deviceType = DeviceType.Unknown
) : INotifyPropertyChanged
{
    private IReadOnlyList<ManufacturerData> _manufacturerData =
        manufacturerData ?? throw new ArgumentNullException(nameof(manufacturerData));

    public string Address 
    {
        get => address;
        set => SetField(ref address, value, nameof(Address));
    }

    public string? Name
    {
        get => name;
        set => SetField(ref name, value, nameof(Name));
    }

    public int? Rssi
    {
        get => rssi;
        set => SetField(ref rssi, value, nameof(Rssi));
    }

    public DateTimeOffset LastSeen
    {
        get => lastSeen;
        set => SetField(ref lastSeen, value, nameof(LastSeen));
    }

    public IReadOnlyList<ManufacturerData> ManufacturerData
    {
        get => _manufacturerData;
        set => SetField(ref _manufacturerData, value, nameof(ManufacturerData));
    }

    public BluetoothConnectionStatus ConnectionState
    {
        get => connectionState;
        set => SetField(ref connectionState, value, nameof(ConnectionState));
    }

    /// <summary>
    /// Bluetooth Class of Device (CoD) raw value (24-bit). Null if unknown.
    /// </summary>
    public uint? ClassOfDevice
    {
        get => classOfDevice;
        set => SetField(ref classOfDevice, value, nameof(ClassOfDevice));
    }

    /// <summary>
    /// Battery remaining percentage (0-100), if known.
    /// </summary>
    public double? BatteryLevel
    {
        get => batteryLevel;
        set => SetField(ref batteryLevel, value, nameof(BatteryLevel));
    }

    public DeviceType? DeviceType
    {
        get => deviceType;
        set => SetField(ref deviceType, value, nameof(DeviceType));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
