using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using BluetoothControlPanel.Core.Bluetooth;
using BluetoothControlPanel.Core.Bluetooth.Event;
using BluetoothControlPanel.UI.Services.Configuration;
using BluetoothControlPanel.UI.Services.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BluetoothControlPanel.UI.ViewModels;

[SingletonService]
public partial class DebugViewModel : ViewModelBase
{
    private readonly IAppConfigService _configService;

    public DebugViewModel(IAppConfigService configService)
    {
        _configService = configService;

        Scanner.DeviceDiscovered += OnDeviceDiscovered;
        Scanner.ScanningStateChanged += OnScanningStateChanged;

        AddLog($"Config loaded from {_configService.ConfigPath}");
    }

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private ObservableCollection<DeviceInfo> devices = [];

    private readonly HashSet<string> _seenAddresses = new(StringComparer.OrdinalIgnoreCase);

    [RelayCommand]
    private async Task RefreshStatusAsync()
    {
        StatusMessage = "Scanning...";
        _seenAddresses.Clear();
        Devices.Clear();
        AddLog($"Scan started.");

        try
        {
            if (!Scanner.IsSupported)
            {
                StatusMessage = "Bluetooth not supported";
                AddLog(Scanner.LastError ?? "Bluetooth not supported.");
                return;
            }

            if (!Scanner.IsEnabled)
            {
                StatusMessage = "Bluetooth is off";
                AddLog(Scanner.LastError ?? "Please turn on Bluetooth.");
                return;
            }

            Scanner.Start();
            await Task.Delay(TimeSpan.FromSeconds(10));
            Scanner.Stop();

            StatusMessage = $"Found {Devices.Count} device(s)";
            AddLog($"Scan completed.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Scan failed";
            AddLog($"Scan error: {ex.Message}");
        }
    }

    private void OnDeviceDiscovered(object? sender, DeviceInfo info)
    {
        void AddDevice()
        {
            if (_seenAddresses.Add(info.Address))
            {
                Devices.Add(info);
                AddLog($"Device: {info.Name ?? "<unknown>"} ({info.Address}) RSSI {info.Rssi}");
            }
        }

        if (Application.Current?.Dispatcher?.CheckAccess() == true)
        {
            AddDevice();
        }
        else
        {
            Application.Current?.Dispatcher?.Invoke(AddDevice);
        }
    }

    private void OnScanningStateChanged(object? sender, ScanningStateChangedEventArgs e)
    {
        var message = e.IsScanning
            ? "Scanning..."
            : $"Stopped scanning ({e.Reason ?? "completed"})";
        if (Application.Current?.Dispatcher?.CheckAccess() == true)
        {
            StatusMessage = message;
            AddLog(message);
        }
        else
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                StatusMessage = message;
                AddLog(message);
            });
        }
    }
}
