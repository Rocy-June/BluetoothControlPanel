using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BluetoothControlPanel.Core.Bluetooth.Event;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Storage.Streams;

namespace BluetoothControlPanel.Core.Bluetooth;

/// <summary>
/// Bluetooth driver: initializes the radio, scans for devices, and manages basic connect/disconnect lifecycles.
/// </summary>
public static class Driver
{
    public static bool IsInitialized { get; private set; }

    public static bool IsScanning { get; private set; }

    public static bool IsSupported { get; private set; } = true;

    public static bool IsEnabled { get; private set; }

    public static string? LastError { get; private set; }

    private static readonly BluetoothLEAdvertisementWatcher Watcher =
        new() { ScanningMode = BluetoothLEScanningMode.Active };

    private static readonly List<DeviceInfo> PairedDevicesList = [];
    private static readonly List<DeviceInfo> NewDevicesList = [];
    private static readonly List<DeviceInfo> ScanningDevicesList = [];
    private static readonly List<DeviceInfo> AvailableDevicesList = [];
    private static readonly Lock DevicesLock = new();

    private static readonly Dictionary<string, DeviceInfo> DeviceCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, BluetoothLEDevice> Connections =
        new(StringComparer.OrdinalIgnoreCase);

    private static Radio? _bluetoothRadio;
    private static bool _disposed;

    public static event EventHandler<DeviceInfo>? DeviceDiscovered;
    public static event EventHandler<ScanningStateChangedEventArgs>? ScanningStateChanged;
    public static event EventHandler<DeviceInfo>? DeviceConnectionChanged;
    public static event EventHandler? DevicesChanged;

    public static async Task InitAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        try
        {
            var radios = await Radio.GetRadiosAsync();
            _bluetoothRadio = radios.FirstOrDefault(r => r.Kind == RadioKind.Bluetooth);
            if (_bluetoothRadio is null)
            {
                IsSupported = false;
                LastError = "Bluetooth radio not found.";
                return;
            }

            Watcher.Received += OnAdvertisementReceived;
            Watcher.Stopped += OnWatcherStopped;
            _bluetoothRadio.StateChanged += OnRadioStateChanged;

            UpdateRadioState(_bluetoothRadio);

            IsInitialized = true;
        }
        catch (Exception ex)
        {
            IsSupported = false;
            LastError = ex.Message;
        }
    }

    public static void StartScan()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(Driver));

        if (IsScanning)
        {
            return;
        }

        if (!IsSupported)
        {
            LastError = "Bluetooth not supported on this device.";
            ScanningStateChanged?.Invoke(
                _bluetoothRadio,
                new ScanningStateChangedEventArgs(false, LastError)
            );
            return;
        }

        if (!IsEnabled)
        {
            LastError = "Bluetooth is turned off.";
            ScanningStateChanged?.Invoke(
                _bluetoothRadio,
                new ScanningStateChangedEventArgs(false, LastError)
            );
            return;
        }

        Watcher.Start();
        IsScanning = true;
        ScanningStateChanged?.Invoke(_bluetoothRadio, new ScanningStateChangedEventArgs(true));
    }

    public static void StopScan()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(Driver));

        if (!IsScanning)
        {
            return;
        }

        Watcher.Stop();
        // Stopped event will update IsScanning and notify listeners.
    }

    public static async Task<bool> ConnectAsync(DeviceInfo? device)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(Driver));
        ArgumentNullException.ThrowIfNull(device);

        DeviceCache[device.Address] = device;
        var address = device.Address;

        try
        {
            var bluetoothAddress = ParseBluetoothAddress(address);
            var bleDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
            if (bleDevice is null)
            {
                device.ConnectionState = BluetoothConnectionStatus.Disconnected;
                LastError = "Unable to open Bluetooth device.";
                return false;
            }

            if (Connections.TryGetValue(address, out var existing))
            {
                existing.ConnectionStatusChanged -= OnDeviceConnectionStatusChanged;
                existing.Dispose();
            }

            Connections[address] = bleDevice;
            bleDevice.ConnectionStatusChanged += OnDeviceConnectionStatusChanged;
            UpdateConnectionState(device, bleDevice.ConnectionStatus);
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            device.ConnectionState = BluetoothConnectionStatus.Disconnected;
            DeviceConnectionChanged?.Invoke(null, device);
            return false;
        }
    }

    public static void Disconnect(DeviceInfo device)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(Driver));
        ArgumentNullException.ThrowIfNull(device);

        if (Connections.Remove(device.Address, out var bleDevice))
        {
            bleDevice.ConnectionStatusChanged -= OnDeviceConnectionStatusChanged;
            bleDevice.Dispose();
        }

        device.ConnectionState = BluetoothConnectionStatus.Disconnected;
        DeviceConnectionChanged?.Invoke(null, device);
    }

    public static bool TryGetKnownDevice(string address, out DeviceInfo device)
    {
        return DeviceCache.TryGetValue(address, out device!);
    }

    private static void OnAdvertisementReceived(
        BluetoothLEAdvertisementWatcher sender,
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
        AddOrUpdateDevice(device, ScanningDevicesList, updateAvailable: true);

        DeviceDiscovered?.Invoke(sender, device);
    }

    public static async Task LoadPairedDevicesAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(Driver));

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var leSelector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
        var leInfos = await DeviceInformation.FindAllAsync(leSelector);
        foreach (var info in leInfos)
        {
            try
            {
                var ble = await BluetoothLEDevice.FromIdAsync(info.Id);
                if (ble is null)
                {
                    return;
                }

                var address = FormatBluetoothAddress(ble.BluetoothAddress);
                if (!seen.Add(address))
                {
                    return;
                }

                var device = GetOrCreateDevice(address);
                device.Name = string.IsNullOrWhiteSpace(ble.Name) ? device.Name : ble.Name;
                device.ConnectionState = ble.ConnectionStatus;
                device.LastSeen = DateTimeOffset.Now;
                AddOrUpdateDevice(device, PairedDevicesList, updateAvailable: false);
                DeviceDiscovered?.Invoke(null, device);
            }
            catch
            {
                // Ignore individual failures while enumerating paired devices.
            }
        }

        var classicSelector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var classicInfos = await DeviceInformation.FindAllAsync(classicSelector);
        foreach (var info in classicInfos)
        {
            try
            {
                var classic = await BluetoothDevice.FromIdAsync(info.Id);
                if (classic is null)
                {
                    return;
                }

                var address = FormatBluetoothAddress(classic.BluetoothAddress);
                if (!seen.Add(address))
                {
                    return;
                }

                var device = GetOrCreateDevice(address);
                device.Name = string.IsNullOrWhiteSpace(classic.Name) ? device.Name : classic.Name;
                device.ConnectionState = classic.ConnectionStatus;
                device.ClassOfDevice = classic.ClassOfDevice?.RawValue;
                device.DeviceType = DeviceConverter.FromClassOfDevice(
                    classic.ClassOfDevice?.RawValue
                );
                device.LastSeen = DateTimeOffset.Now;
                AddOrUpdateDevice(device, PairedDevicesList, updateAvailable: false);
                DeviceDiscovered?.Invoke(null, device);
            }
            catch
            {
                // Ignore individual failures while enumerating paired devices.
            }
        }
    }

    private static void OnRadioStateChanged(Radio sender, object args)
    {
        UpdateRadioState(sender);
    }

    private static void UpdateRadioState(Radio sender)
    {
        IsEnabled = sender.State == RadioState.On;

        if (!IsEnabled)
        {
            LastError = "Bluetooth is turned off.";
            if (IsScanning)
            {
                StopScan();
            }
            ScanningStateChanged?.Invoke(
                sender,
                new ScanningStateChangedEventArgs(false, LastError)
            );
        }
        else
        {
            LastError = null;
            ScanningStateChanged?.Invoke(sender, new ScanningStateChangedEventArgs(IsScanning));
        }
    }

    private static void OnWatcherStopped(
        BluetoothLEAdvertisementWatcher sender,
        BluetoothLEAdvertisementWatcherStoppedEventArgs args
    )
    {
        IsScanning = false;
        var reason = args.Error.ToString();
        PromoteScanResults();
        ScanningStateChanged?.Invoke(sender, new ScanningStateChangedEventArgs(false, reason));
    }

    private static void OnDeviceConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        var address = FormatBluetoothAddress(sender.BluetoothAddress);
        if (DeviceCache.TryGetValue(address, out var device))
        {
            UpdateConnectionState(device, sender.ConnectionStatus);
        }
    }

    private static void UpdateConnectionState(DeviceInfo device, BluetoothConnectionStatus status)
    {
        device.ConnectionState = status;

        DeviceConnectionChanged?.Invoke(null, device);
    }

    private static DeviceInfo GetOrCreateDevice(string address)
    {
        if (DeviceCache.TryGetValue(address, out var existing))
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

        DeviceCache[address] = device;
        return device;
    }

    public static IReadOnlyList<DeviceInfo> GetPairedDevicesSnapshot()
    {
        lock (DevicesLock)
        {
            return PairedDevicesList.ToList();
        }
    }

    public static IReadOnlyList<DeviceInfo> GetAvailableDevicesSnapshot()
    {
        lock (DevicesLock)
        {
            return AvailableDevicesList.ToList();
        }
    }

    public static IReadOnlyList<DeviceInfo> GetNewDevicesSnapshot()
    {
        lock (DevicesLock)
        {
            return NewDevicesList.ToList();
        }
    }

    public static IReadOnlyList<DeviceInfo> GetScanningDevicesSnapshot()
    {
        lock (DevicesLock)
        {
            return ScanningDevicesList.ToList();
        }
    }

    private static void AddOrUpdateDevice(
        DeviceInfo device,
        IList<DeviceInfo> target,
        bool updateAvailable,
        bool raiseEvent = true
    )
    {
        lock (DevicesLock)
        {
            DeviceCache[device.Address] = device;
            if (!target.Contains(device))
            {
                target.Add(device);
            }

            if (updateAvailable)
            {
                UpdateAvailableDevicesLocked();
            }
        }

        if (raiseEvent)
        {
            DevicesChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    private static void UpdateAvailableDevicesLocked()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AvailableDevicesList.Clear();

        foreach (var device in ScanningDevicesList)
        {
            if (seen.Add(device.Address))
            {
                AvailableDevicesList.Add(device);
            }
        }

        foreach (var device in NewDevicesList)
        {
            if (seen.Add(device.Address))
            {
                AvailableDevicesList.Add(device);
            }
        }
    }

    private static void PromoteScanResults()
    {
        lock (DevicesLock)
        {
            NewDevicesList.Clear();
            foreach (var device in ScanningDevicesList)
            {
                AddOrUpdateDevice(
                    device,
                    NewDevicesList,
                    updateAvailable: false,
                    raiseEvent: false
                );
            }

            ScanningDevicesList.Clear();
            UpdateAvailableDevicesLocked();
        }

        DevicesChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void ClearDiscoveryLists()
    {
        lock (DevicesLock)
        {
            NewDevicesList.Clear();
            ScanningDevicesList.Clear();
            UpdateAvailableDevicesLocked();
        }

        DevicesChanged?.Invoke(null, EventArgs.Empty);
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

    public static void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Watcher.Received -= OnAdvertisementReceived;
        Watcher.Stopped -= OnWatcherStopped;
        Watcher.Stop();
        if (_bluetoothRadio is not null)
        {
            _bluetoothRadio.StateChanged -= OnRadioStateChanged;
        }

        foreach (var connection in Connections.Values)
        {
            connection.ConnectionStatusChanged -= OnDeviceConnectionStatusChanged;
            connection.Dispose();
        }

        Connections.Clear();
    }
}
