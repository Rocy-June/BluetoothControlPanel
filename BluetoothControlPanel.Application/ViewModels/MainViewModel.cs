using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BluetoothControlPanel.Application.DependencyInjection;
using BluetoothControlPanel.Application.Services.Bluetooth;
using BluetoothControlPanel.Application.Services.Logging;
using BluetoothControlPanel.Application.Services.Windows;
using BluetoothControlPanel.Domain.Bluetooth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BluetoothControlPanel.Application.ViewModels;

[SingletonService]
public partial class MainViewModel : ViewModelBase
{
    private readonly IBluetoothDriverService _driver;
    private readonly IWindowManager _windowManager;
    private CancellationTokenSource? _scanCancellationToken;

    public ReadOnlyObservableCollection<DeviceInfo> PairedDevices => _driver.PairedDevices;
    public ReadOnlyObservableCollection<DeviceInfo> AvailableDevices => _driver.AvailableDevices;

    [ObservableProperty]
    private bool isWindowShowing = false;

    [ObservableProperty]
    private bool isWindowOpen = false;

    public MainViewModel(ILogService logService, IBluetoothDriverService driver, IWindowManager windowManager)
    {
        ConfigureLogging(logService);
        _driver = driver;
        _windowManager = windowManager;

        AutoRefreshAsync();
    }

    [RelayCommand]
    private void Shown()
    {
        //AutoRefreshAsync();

        AddLog("Window shown");
    }

    [RelayCommand]
    private void Hide()
    {
        _scanCancellationToken?.Cancel();
        IsWindowOpen = false;

        AddLog("Window hidden");
    }

    [RelayCommand]
    private void OpenDebugWindow()
    {
        _windowManager.ShowDebugWindow();
    }

    private async void AutoRefreshAsync()
    {
        _scanCancellationToken?.Cancel();
        _scanCancellationToken = new();

        bool flag;
        do
        {
            flag = await RefreshOnceAsync();
        } 
        while (flag && !_scanCancellationToken.IsCancellationRequested);
    }

    private async Task<bool> RefreshOnceAsync()
    {
        AddLog("Scan started.");

        try
        {
            if (!_driver.IsSupported)
            {
                AddLog(_driver.LastError ?? "Bluetooth not supported.");
                return false;
            }

            if (!_driver.IsEnabled)
            {
                AddLog(_driver.LastError ?? "Please turn on Bluetooth.");
                return false;
            }

            _driver.StartScan();
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(true);
            _driver.StopScan();

            AddLog("Scan completed.");
        }
        catch (Exception ex)
        {
            AddLog($"Scan error: {ex.Message}");

            return false;
        }

        return true;
    }
}
