using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
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

    public ObservableCollection<SystemColorSwatch> SystemColorSwatches { get; } = new();
    public ObservableCollection<DeviceInfo> Devices { get; } = new();

    public DebugViewModel(IAppConfigService configService)
    {
        _configService = configService;

        Driver.ScanningStateChanged += OnScanningStateChanged;
        Driver.DevicesChanged += OnDevicesChanged;

        AddLog($"Config loaded from {_configService.ConfigPath}");
        LoadSystemColors();
        SyncFromDriver();
    }

    [ObservableProperty]
    private string statusMessage = "Ready";

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

            StatusMessage = $"Found {Devices.Count} device(s)";
            AddLog($"Scan completed.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Scan failed";
            AddLog($"Scan error: {ex.Message}");
        }
    }

    private void OnDevicesChanged(object? sender, EventArgs e)
    {
        if (Application.Current?.Dispatcher?.CheckAccess() == true)
        {
            SyncFromDriver();
        }
        else
        {
            Application.Current?.Dispatcher?.Invoke(SyncFromDriver);
        }
    }

    private void SyncFromDriver()
    {
        Devices.Clear();
        foreach (var item in Driver.GetAvailableDevicesSnapshot())
        {
            Devices.Add(item);
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

    private void LoadSystemColors()
    {
        SystemColorSwatches.Clear();

        var type = typeof(SystemColors);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            try
            {
                var name = prop.Name;
                if (!seen.Add(name))
                {
                    continue;
                }

                if (prop.PropertyType == typeof(Color))
                {
                    var color = (Color)prop.GetValue(null)!;
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    SystemColorSwatches.Add(new SystemColorSwatch(name, brush));
                }
            }
            catch
            {
                // Ignore problematic properties.
            }
        }
    }
}

public sealed record SystemColorSwatch(string Name, Brush Brush);
