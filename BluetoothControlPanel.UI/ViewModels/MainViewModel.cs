using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BluetoothControlPanel.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string applicationTitle = "Bluetooth Control Panel";

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private ObservableCollection<string> logEntries = new();

    [RelayCommand]
    private void RefreshStatus()
    {
        StatusMessage = "Refreshing...";

        // Record the refresh attempt with a timestamp.
        LogEntries.Insert(0, $"Refresh requested at {DateTime.Now:T}");

        StatusMessage = "Ready";
    }
}
