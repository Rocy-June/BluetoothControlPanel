using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using BluetoothControlPanel.Domain.Bluetooth;

namespace BluetoothControlPanel.Application.Services.Bluetooth;

public interface IBluetoothDriverService : IDisposable, INotifyPropertyChanged
{
    bool IsInitialized { get; }
    bool IsScanning { get; }
    bool IsSupported { get; }
    bool IsEnabled { get; }
    string? LastError { get; }

    ReadOnlyObservableCollection<DeviceInfo> PairedDevices { get; }
    ReadOnlyObservableCollection<DeviceInfo> AvailableDevices { get; }

    Task InitAsync();
    void StartScan();
    void StopScan();
    Task<bool> ConnectAsync(DeviceInfo? device);
    void Disconnect(DeviceInfo device);
    Task LoadPairedDevicesAsync();
    bool TryGetKnownDevice(string address, out DeviceInfo device);
    void ClearDiscoveryLists();
}
