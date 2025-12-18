using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Storage.Streams;

using BluetoothControlPanel.Application.DependencyInjection;
using BluetoothControlPanel.Application.Services.Bluetooth;
using BluetoothControlPanel.Domain.Bluetooth;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BluetoothControlPanel.Infrastructure.Services.Bluetooth;

/// <summary>
/// Bluetooth driver service: initializes the radio, scans for devices, and manages basic connect/disconnect lifecycles.
/// </summary>
[SingletonService(ServiceType = typeof(IBluetoothDriverService))]
public partial class BluetoothDriverService : ObservableObject, IBluetoothDriverService, IDisposable
{
    [ObservableProperty]
    private bool isInitialized;
    [ObservableProperty]
    private bool isScanning;
    [ObservableProperty]
    private bool isSupported = true;
    [ObservableProperty]
    private bool isEnabled;
    [ObservableProperty]
    private string? lastError;

    public ReadOnlyObservableCollection<DeviceInfo> PairedDevices { get; }
    public ReadOnlyObservableCollection<DeviceInfo> AvailableDevices { get; }

    private readonly SemaphoreSlim _initGate = new(1, 1);
    private readonly SemaphoreSlim _connectGate = new(1, 1);
    private readonly CancellationTokenSource _cts = new();

    private BluetoothAdapter? _adapter;
    private Radio? _bluetoothRadio;
    private bool _disposed;

    private readonly BluetoothLEAdvertisementWatcher _watcher = CreateWatcher();

    private readonly ObservableCollection<DeviceInfo> _pairedDevicesList = [];
    private readonly ObservableCollection<DeviceInfo> _availableDevices = [];
    private readonly List<DeviceInfo> _scanningDevicesList = [];
    private readonly Lock _devicesLock = new();

    private readonly Dictionary<string, DeviceInfo> _deviceCache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BluetoothLEDevice> _connections =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GattCharacteristic> _batterySubscriptions =
        new(StringComparer.OrdinalIgnoreCase);

    public BluetoothDriverService()
    {
        PairedDevices = new ReadOnlyObservableCollection<DeviceInfo>(_pairedDevicesList);
        AvailableDevices = new ReadOnlyObservableCollection<DeviceInfo>(_availableDevices);
    }

    public async Task InitAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(BluetoothDriverService));

        if (IsInitialized || _disposed)
        {
            return;
        }

        await _initGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (IsInitialized || _disposed)
            {
                return;
            }

            _adapter = await BluetoothAdapter.GetDefaultAsync();
            if (_adapter is null)
            {
                IsSupported = false;
                LastError = "Bluetooth adapter not found.";
                return;
            }

            _bluetoothRadio = await _adapter.GetRadioAsync();
            if (_bluetoothRadio is null)
            {
                IsSupported = false;
                LastError = "Bluetooth radio not found.";
                return;
            }

            _watcher.Received += OnAdvertisementReceived;
            _watcher.Stopped += OnWatcherStopped;
            _bluetoothRadio.StateChanged += OnRadioStateChanged;

            UpdateRadioState(_bluetoothRadio);

            IsInitialized = true;
            LastError = null;
        }
        catch (Exception ex)
        {
            IsSupported = false;
            LastError = ex.Message;
        }
        finally
        {
            _initGate.Release();
        }
    }

    public void StartScan()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(BluetoothDriverService));

        if (IsScanning)
        {
            return;
        }

        if (!IsInitialized)
        {
            LastError = "Bluetooth driver is not initialized.";
            return;
        }

        if (!IsSupported)
        {
            LastError = "Bluetooth not supported on this device.";
            return;
        }

        if (!IsEnabled)
        {
            LastError = "Bluetooth is turned off.";
            return;
        }

        _watcher.Start();
        IsScanning = true;
        LastError = null;
    }

    public void StopScan()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(BluetoothDriverService));

        if (!IsScanning)
        {
            return;
        }

        _watcher.Stop();
    }

    public async Task<bool> ConnectAsync(DeviceInfo? device)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(BluetoothDriverService));
        ArgumentNullException.ThrowIfNull(device);

        _deviceCache[device.Address] = device;

        await _connectGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var bluetoothAddress = ParseBluetoothAddress(device.Address);
            var bleDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
            if (bleDevice is null)
            {
                UpdateConnectionState(device, BluetoothConnectionStatus.Disconnected);
                LastError = "Unable to open Bluetooth device.";
                return false;
            }

            if (_connections.TryGetValue(device.Address, out var existing))
            {
                existing.ConnectionStatusChanged -= OnDeviceConnectionStatusChanged;
                existing.Dispose();
            }

            _connections[device.Address] = bleDevice;
            bleDevice.ConnectionStatusChanged += OnDeviceConnectionStatusChanged;

            UpdateDeviceFromBluetoothLe(device, bleDevice);
            _ = RefreshBatteryLevelAsync(device.Address, bleDevice);
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            UpdateConnectionState(device, BluetoothConnectionStatus.Disconnected);
            return false;
        }
        finally
        {
            _connectGate.Release();
        }
    }

    public void Disconnect(DeviceInfo device)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(BluetoothDriverService));
        ArgumentNullException.ThrowIfNull(device);

        if (_connections.Remove(device.Address, out var bleDevice))
        {
            bleDevice.ConnectionStatusChanged -= OnDeviceConnectionStatusChanged;
            bleDevice.Dispose();
        }

        ClearBatterySubscription(device.Address, device);
        UpdateConnectionState(device, BluetoothConnectionStatus.Disconnected);
    }

    public bool TryGetKnownDevice(string address, out DeviceInfo device)
    {
        return _deviceCache.TryGetValue(address, out device!);
    }

    public async Task LoadPairedDevicesAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(BluetoothDriverService));

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await EnumeratePairedLeDevicesAsync(seen).ConfigureAwait(false);
        await EnumeratePairedClassicDevicesAsync(seen).ConfigureAwait(false);
    }

    public void ClearDiscoveryLists()
    {
        RunOnDispatcher(() =>
        {
            lock (_devicesLock)
            {
                _availableDevices.Clear();
                _scanningDevicesList.Clear();
            }
        });

    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        _cts.Cancel();

        _watcher.Received -= OnAdvertisementReceived;
        _watcher.Stopped -= OnWatcherStopped;
        _watcher.Stop();
        if (_bluetoothRadio is not null)
        {
            _bluetoothRadio.StateChanged -= OnRadioStateChanged;
        }

        foreach (var subscription in _batterySubscriptions.Values)
        {
            subscription.ValueChanged -= OnBatteryLevelChanged;
            subscription.Service?.Dispose();
        }
        _batterySubscriptions.Clear();

        foreach (var connection in _connections.Values)
        {
            connection.ConnectionStatusChanged -= OnDeviceConnectionStatusChanged;
            connection.Dispose();
        }

        _connections.Clear();

        _cts.Dispose();
    }

    private void OnAdvertisementReceived(
        BluetoothLEAdvertisementWatcher sender,
        BluetoothLEAdvertisementReceivedEventArgs args
    )
    {
        if (_disposed || _cts.IsCancellationRequested)
        {
            return;
        }

        var device = UpdateDeviceFromAdvertisement(args);
        if (!IsAddressInList(device.Address, _availableDevices))
        {
            AddOrUpdateDevice(device, _availableDevices);
        }
        else
        {
            UpdateCachedDevice(device);
        }

        AddOrUpdateDevice(device, _scanningDevicesList);

    }

    private void OnRadioStateChanged(Radio sender, object args)
    {
        if (_disposed || _cts.IsCancellationRequested)
        {
            return;
        }

        UpdateRadioState(sender);
    }

    private void UpdateRadioState(Radio sender)
    {
        IsEnabled = sender.State == RadioState.On;

        if (!IsEnabled)
        {
            LastError = "Bluetooth is turned off.";
            if (IsScanning)
            {
                StopScan();
            }
        }
        else
        {
            LastError = null;
        }
    }

    private void OnWatcherStopped(
        BluetoothLEAdvertisementWatcher sender,
        BluetoothLEAdvertisementWatcherStoppedEventArgs args
    )
    {
        if (_disposed || _cts.IsCancellationRequested)
        {
            return;
        }

        IsScanning = false;
        RemoveAndClearDevicesWhenStop();
    }

    private void RemoveAndClearDevicesWhenStop() 
    {
        RunOnDispatcher(() =>
        {
            lock (_devicesLock)
            {
                for (var i = _availableDevices.Count - 1; i >= 0; i--)
                {
                    var device = _availableDevices[i];
                    if (!_scanningDevicesList.Contains(device))
                    {
                        _availableDevices.RemoveAt(i);
                    }
                }
            }
            _scanningDevicesList.Clear();
        });
    }

    private void OnDeviceConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        if (_disposed || _cts.IsCancellationRequested)
        {
            return;
        }

        var address = FormatBluetoothAddress(sender.BluetoothAddress);
        if (_deviceCache.TryGetValue(address, out var device))
        {
            UpdateConnectionState(device, sender.ConnectionStatus);
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                _ = RefreshBatteryLevelAsync(address, sender);
            }
            else
            {
                ClearBatterySubscription(address, device);
            }
        }
    }

    private void UpdateDeviceFromBluetoothLe(DeviceInfo device, BluetoothLEDevice bleDevice)
    {
        device.Name = string.IsNullOrWhiteSpace(bleDevice.Name) ? device.Name : bleDevice.Name;
        device.LastSeen = DateTimeOffset.Now;
        UpdateConnectionState(device, bleDevice.ConnectionStatus);
    }

    private void UpdateDeviceFromClassicDevice(DeviceInfo device, BluetoothDevice classic)
    {
        device.Name = string.IsNullOrWhiteSpace(classic.Name) ? device.Name : classic.Name;
        device.ConnectionState = classic.ConnectionStatus;
        device.ClassOfDevice = classic.ClassOfDevice?.RawValue;
        device.DeviceType = DeviceConverter.FromClassOfDevice(classic.ClassOfDevice?.RawValue);
        device.LastSeen = DateTimeOffset.Now;
    }

    private void UpdateConnectionState(DeviceInfo device, BluetoothConnectionStatus status)
    {
        device.ConnectionState = status;
    }

    private async Task EnumeratePairedLeDevicesAsync(HashSet<string> seen)
    {
        var leSelector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
        var leInfos = await DeviceInformation.FindAllAsync(leSelector);
        foreach (var info in leInfos)
        {
            BluetoothLEDevice? ble = null;
            try
            {
                if (_cts.IsCancellationRequested)
                {
                    return;
                }

                ble = await BluetoothLEDevice.FromIdAsync(info.Id);
                if (ble is null)
                {
                    continue;
                }

                var address = FormatBluetoothAddress(ble.BluetoothAddress);
                if (!seen.Add(address))
                {
                    continue;
                }

                var device = GetOrCreateDevice(address);
                UpdateDeviceFromBluetoothLe(device, ble);
                AddOrUpdateDevice(device, _pairedDevicesList);
            }
            catch
            {
                // Ignore individual failures while enumerating paired devices.
            }
            finally
            {
                ble?.Dispose();
            }
        }
    }

    private async Task EnumeratePairedClassicDevicesAsync(HashSet<string> seen)
    {
        var classicSelector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var classicInfos = await DeviceInformation.FindAllAsync(classicSelector);
        foreach (var info in classicInfos)
        {
            BluetoothDevice? classic = null;
            try
            {
                if (_cts.IsCancellationRequested)
                {
                    return;
                }

                classic = await BluetoothDevice.FromIdAsync(info.Id);
                if (classic is null)
                {
                    continue;
                }

                var address = FormatBluetoothAddress(classic.BluetoothAddress);
                if (!seen.Add(address))
                {
                    continue;
                }

                var device = GetOrCreateDevice(address);
                UpdateDeviceFromClassicDevice(device, classic);

                AddOrUpdateDevice(device, _pairedDevicesList);
            }
            catch
            {
                // Ignore individual failures while enumerating paired devices.
            }
            finally
            {
                classic?.Dispose();
            }
        }
    }

    private DeviceInfo UpdateDeviceFromAdvertisement(
        BluetoothLEAdvertisementReceivedEventArgs args
    )
    {
        var address = FormatBluetoothAddress(args.BluetoothAddress);
        var manufacturerData = new List<ManufacturerData>();
        foreach (var data in args.Advertisement.ManufacturerData)
        {
            manufacturerData.Add(new ManufacturerData(data.CompanyId, ToByteArray(data.Data)));
        }

        var device = GetOrCreateDevice(address);
        device.Name = string.IsNullOrWhiteSpace(args.Advertisement.LocalName)
            ? device.Name
            : args.Advertisement.LocalName;
        device.Rssi = args.RawSignalStrengthInDBm;
        device.LastSeen = args.Timestamp;
        device.ManufacturerData = manufacturerData;
        return device;
    }

    private DeviceInfo GetOrCreateDevice(string address)
    {
        if (_deviceCache.TryGetValue(address, out var existing))
        {
            return existing;
        }

        var device = new DeviceInfo(
            address: address,
            name: null,
            rssi: null,
            lastSeen: DateTimeOffset.MinValue,
            manufacturerData: []
        );

        _deviceCache[address] = device;
        return device;
    }

    private void UpdateCachedDevice(DeviceInfo updated)
    {
        if (_deviceCache.TryGetValue(updated.Address, out var existing))
        {
            existing.Name = updated.Name ?? existing.Name;
            existing.Rssi = updated.Rssi;
            existing.LastSeen = updated.LastSeen;
            existing.ManufacturerData = updated.ManufacturerData;
            existing.ClassOfDevice = updated.ClassOfDevice;
            existing.DeviceType = updated.DeviceType;
            existing.BatteryLevel = updated.BatteryLevel;
        }
    }

    private bool IsAddressInList(string address, ICollection<DeviceInfo> list) =>
        list.Any(d => string.Equals(d.Address, address, StringComparison.OrdinalIgnoreCase));

    private void AddOrUpdateDevice(DeviceInfo device, IList<DeviceInfo> target)
    {
        RunOnDispatcher(() =>
        {
            lock (_devicesLock)
            {
                _deviceCache[device.Address] = device;
                var existing = target.FirstOrDefault(d =>
                    string.Equals(d.Address, device.Address, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    target.Add(device);
                }
            }
        });
    }

    private static string FormatBluetoothAddress(ulong bluetoothAddress)
    {
        Span<byte> bytes = stackalloc byte[6];
        for (var i = 0; i < 6; i++)
        {
            bytes[5 - i] = (byte)(bluetoothAddress >> (8 * i) & 0xFF);
        }

        return string.Join(":", bytes.ToArray().Select(b => b.ToString("X2")));
    }

    private static ulong ParseBluetoothAddress(string address)
    {
        var parts = address.Split(':');
        if (parts.Length != 6)
        {
            throw new FormatException(
                "Bluetooth address must have 6 octets (e.g. 01:23:45:67:89:AB)."
            );
        }

        ulong value = 0;
        foreach (var part in parts)
        {
            value = (value << 8) + Convert.ToUInt64(part, 16);
        }

        return value;
    }

    private static BluetoothLEAdvertisementWatcher CreateWatcher()
    {
        var watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };

        watcher.SignalStrengthFilter.SamplingInterval = TimeSpan.FromMilliseconds(500);
        return watcher;
    }

    private static byte[] ToByteArray(IBuffer buffer)
    {
        if (buffer.Length == 0)
        {
            return [];
        }

        var data = new byte[buffer.Length];
        using var reader = DataReader.FromBuffer(buffer);
        reader.ReadBytes(data);
        return data;
    }

    private async Task RefreshBatteryLevelAsync(string address, BluetoothLEDevice device)
    {
        if (_cts.IsCancellationRequested)
        {
            return;
        }

        try
        {
            var servicesResult = await device.GetGattServicesForUuidAsync(GattServiceUuids.Battery);
            if (servicesResult.Status != GattCommunicationStatus.Success)
            {
                return;
            }
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            var service = servicesResult.Services.FirstOrDefault();
            if (service is null)
            {
                return;
            }

            var characteristicsResult = await service.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel);
            if (characteristicsResult.Status != GattCommunicationStatus.Success)
            {
                service.Dispose();
                return;
            }
            if (_cts.IsCancellationRequested)
            {
                service.Dispose();
                return;
            }

            var characteristic = characteristicsResult.Characteristics.FirstOrDefault();
            if (characteristic is null)
            {
                service.Dispose();
                return;
            }

            characteristic.ValueChanged -= OnBatteryLevelChanged;
            characteristic.ValueChanged += OnBatteryLevelChanged;

            var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify
            );
            if (status != GattCommunicationStatus.Success)
            {
                characteristic.ValueChanged -= OnBatteryLevelChanged;
                service.Dispose();
                return;
            }
            if (_cts.IsCancellationRequested)
            {
                characteristic.ValueChanged -= OnBatteryLevelChanged;
                service.Dispose();
                return;
            }

            lock (_devicesLock)
            {
                _batterySubscriptions[address] = characteristic;
            }

            var readResult = await characteristic.ReadValueAsync();
            if (readResult.Status == GattCommunicationStatus.Success)
            {
                UpdateBatteryLevel(address, readResult.Value);
            }
        }
        catch
        {
            // ignore battery update failures
        }
    }

    private void OnBatteryLevelChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        if (_disposed || _cts.IsCancellationRequested)
        {
            return;
        }

        var service = sender.Service;
        if (service?.Device is null)
        {
            return;
        }

        var address = FormatBluetoothAddress(service.Device.BluetoothAddress);
        UpdateBatteryLevel(address, args.CharacteristicValue);
    }

    private void UpdateBatteryLevel(string address, IBuffer buffer)
    {
        if (_cts.IsCancellationRequested)
        {
            return;
        }

        if (!_deviceCache.TryGetValue(address, out var device))
        {
            return;
        }

        try
        {
            var level = ToBatteryPercentage(buffer);
            device.BatteryLevel = level;
        }
        catch
        {
            // ignore invalid battery payloads
        }
    }

    private static double? ToBatteryPercentage(IBuffer buffer)
    {
        if (buffer.Length < 1)
        {
            return null;
        }

        using var reader = DataReader.FromBuffer(buffer);
        var raw = reader.ReadByte();
        return raw <= 100 ? raw : null;
    }

    private void ClearBatterySubscription(string address, DeviceInfo device)
    {
        GattCharacteristic? characteristic = null;
        lock (_devicesLock)
        {
            if (_batterySubscriptions.Remove(address, out characteristic))
            {
                // fall through
            }
        }

        if (characteristic is not null)
        {
            characteristic.ValueChanged -= OnBatteryLevelChanged;
            characteristic.Service?.Dispose();
        }

        device.BatteryLevel = null;
    }

    private void RunOnDispatcher(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }
}
