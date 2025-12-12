using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;
using Windows.Storage.Streams;

using BluetoothControlPanel.Core.Bluetooth.Event;

namespace BluetoothControlPanel.Core.Bluetooth;

/// <summary>
/// Basic Bluetooth LE scanner using Windows.Devices.Bluetooth APIs (no third-party packages).
/// </summary>
public static class Scanner
{
    public static bool IsInitialized { get; private set; }

    public static bool IsScanning { get; private set; }

    public static bool IsSupported { get; private set; } = true;

    public static bool IsEnabled { get; private set; }

    public static string? LastError { get; private set; }

    private static readonly BluetoothLEAdvertisementWatcher _watcher = new()
    {
        ScanningMode = BluetoothLEScanningMode.Active
    };
    private static Radio? _bluetoothRadio;
    private static bool _disposed;

    public static event EventHandler<DeviceInfo>? DeviceDiscovered;

    public static event EventHandler<ScanningStateChangedEventArgs>? ScanningStateChanged;

    public static async Task InitAsync()
    {
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

            _watcher.Received += OnAdvertisementReceived;
            _watcher.Stopped += OnWatcherStopped;
            _bluetoothRadio.StateChanged += OnRadioStateChanged;
            UpdateRadioState(_bluetoothRadio);
        }
        catch (Exception ex)
        {
            IsSupported = false;
            LastError = ex.Message;
        }
    }

    public static void Start()
    {
        ThrowIfDisposed();

        if (IsScanning)
        {
            return;
        }

        if (!IsSupported)
        {
            LastError = "Bluetooth not supported on this device.";
            ScanningStateChanged?.Invoke(_bluetoothRadio, new ScanningStateChangedEventArgs(false, LastError));
            return;
        }

        if (!IsEnabled)
        {
            LastError = "Bluetooth is turned off.";
            ScanningStateChanged?.Invoke(_bluetoothRadio, new ScanningStateChangedEventArgs(false, LastError));
            return;
        }

        _watcher.Start();
        IsScanning = true;
        ScanningStateChanged?.Invoke(_bluetoothRadio, new ScanningStateChangedEventArgs(true));
    }

    public static void Stop()
    {
        ThrowIfDisposed();

        if (!IsScanning)
        {
            return;
        }

        _watcher.Stop();
        // Stopped event will update IsScanning and notify listeners.
    }

    private static void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        var manufacturerData = new List<ManufacturerData>();
        foreach (var data in args.Advertisement.ManufacturerData)
        {
            manufacturerData.Add(new ManufacturerData(data.CompanyId, ToByteArray(data.Data)));
        }

        var info = new DeviceInfo(
            Address: FormatBluetoothAddress(args.BluetoothAddress),
            Name: string.IsNullOrWhiteSpace(args.Advertisement.LocalName) ? null : args.Advertisement.LocalName,
            Rssi: args.RawSignalStrengthInDBm,
            LastSeen: args.Timestamp,
            ManufacturerData: manufacturerData);

        DeviceDiscovered?.Invoke(sender, info);
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
                Stop();
            }
            ScanningStateChanged?.Invoke(sender, new ScanningStateChangedEventArgs(false, LastError));
        }
        else
        {
            LastError = null;
            ScanningStateChanged?.Invoke(sender, new ScanningStateChangedEventArgs(IsScanning));
        }
    }

    private static void OnWatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
    {
        IsScanning = false;
        var reason = args.Error.ToString();
        ScanningStateChanged?.Invoke(sender, new ScanningStateChangedEventArgs(false, reason));
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

    private static void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(Scanner));
    }

    public static void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _watcher.Received -= OnAdvertisementReceived;
        _watcher.Stopped -= OnWatcherStopped;
        _watcher.Stop();
        if (_bluetoothRadio is not null)
        {
            _bluetoothRadio.StateChanged -= OnRadioStateChanged;
        }
    }
}
