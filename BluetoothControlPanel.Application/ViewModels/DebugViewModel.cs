using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using BluetoothControlPanel.Application.DependencyInjection;
using BluetoothControlPanel.Application.Services.Bluetooth;
using BluetoothControlPanel.Application.Services.Config;
using BluetoothControlPanel.Application.Services.Logging;
using BluetoothControlPanel.Domain.Bluetooth;

namespace BluetoothControlPanel.Application.ViewModels;

[SingletonService]
public partial class DebugViewModel : ViewModelBase
{
    private readonly IBluetoothDriverService _driver;
    private readonly IAppConfigService _configService;
    public DebugViewModel(ILogService logService, IBluetoothDriverService driver, IAppConfigService configService)
    {
        ConfigureLogging(logService);
        _driver = driver;
        _configService = configService;

        _driver.PropertyChanged += OnDriverPropertyChanged;

        AddLog($"Config loaded from {_configService.ConfigPath}");
    }

    [ObservableProperty]
    private string statusMessage = "Ready";

    [RelayCommand]
    private async Task RefreshStatusAsync()
    {
        StatusMessage = "Scanning...";
        _driver.ClearDiscoveryLists();
        AddLog("Scan started.");

        try
        {
            if (!_driver.IsSupported)
            {
                StatusMessage = "Bluetooth not supported";
                AddLog(_driver.LastError ?? "Bluetooth not supported.");
                return;
            }

            if (!_driver.IsEnabled)
            {
                StatusMessage = "Bluetooth is off";
                AddLog(_driver.LastError ?? "Please turn on Bluetooth.");
                return;
            }

            _driver.StartScan();
            await Task.Delay(TimeSpan.FromSeconds(10));
            _driver.StopScan();

            StatusMessage = $"Found {_driver.AvailableDevices.Count} device(s)";
            AddLog("Scan completed.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Scan failed";
            AddLog($"Scan error: {ex.Message}");
        }
    }

    private void OnDriverPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IBluetoothDriverService.IsScanning))
        {
            var message = _driver.IsScanning
                ? "Scanning..."
                : $"Stopped scanning ({_driver.LastError ?? "completed"})";

            StatusMessage = message;

            AddLog(message);
        }
    }
}
