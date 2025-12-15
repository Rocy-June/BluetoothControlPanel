using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using BluetoothControlPanel.Core.Bluetooth;
using BluetoothControlPanel.Core.Bluetooth.Event;
using BluetoothControlPanel.UI.Services.DependencyInjection;
using BluetoothControlPanel.UI.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BluetoothControlPanel.UI.ViewModels;

[SingletonService]
public partial class MainViewModel : ViewModelBase
{
    private readonly DebugWindow _debugWindow;

    public ObservableCollection<DeviceInfo> PairedDevices { get; } = new();
    public ObservableCollection<DeviceInfo> AvailableDevices { get; } = new();

    [ObservableProperty]
    private string statusMessage = "Ready";

    public MainViewModel(DebugWindow debugWindow)
    {
        _debugWindow = debugWindow;

        Driver.DevicesChanged += OnDevicesChanged;
        Driver.ScanningStateChanged += OnScanningStateChanged;

        SyncFromDriver();
    }

    [RelayCommand]
    private async Task RefreshStatusAsync()
    {
        StatusMessage = "Scanning...";
        Driver.ClearDiscoveryLists();
        AddLog($"Scan started.");

        try
        {
            if (!Driver.IsSupported)
            {
                StatusMessage = "Bluetooth not supported";
                AddLog(Driver.LastError ?? "Bluetooth not supported.");
                return;
            }

            if (!Driver.IsEnabled)
            {
                StatusMessage = "Bluetooth is off";
                AddLog(Driver.LastError ?? "Please turn on Bluetooth.");
                return;
            }

            Driver.StartScan();
            await Task.Delay(TimeSpan.FromSeconds(10));
            Driver.StopScan();

            StatusMessage = $"Paired: {PairedDevices.Count}, Nearby: {AvailableDevices.Count}";
            AddLog($"Scan completed.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Scan failed";
            AddLog($"Scan error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenDebugWindow()
    {
        if (_debugWindow.IsVisible)
        {
            _debugWindow.Activate();
        }
        else
        {
            _debugWindow.Show();
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

    private void OnDevicesChanged(object? sender, EventArgs e)
    {
        void Update()
        {
            SyncFromDriver();
            StatusMessage = $"Paired: {PairedDevices.Count}, Nearby: {AvailableDevices.Count}";
        }

        if (Application.Current?.Dispatcher?.CheckAccess() == true)
        {
            Update();
        }
        else
        {
            Application.Current?.Dispatcher?.Invoke(Update);
        }
    }

    private void SyncFromDriver()
    {
        UpdateCollection(PairedDevices, Driver.GetPairedDevicesSnapshot());
        UpdateCollection(AvailableDevices, Driver.GetAvailableDevicesSnapshot());
    }

    private static void UpdateCollection(ObservableCollection<DeviceInfo> target, IReadOnlyList<DeviceInfo> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
