using CommunityToolkit.Mvvm.ComponentModel;
using BluetoothControlPanel.Application.Services.Logging;

namespace BluetoothControlPanel.Application.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    private static ILogService? _logService;

    protected static void ConfigureLogging(ILogService logService) => _logService = logService;

    public static System.Collections.ObjectModel.ReadOnlyObservableCollection<string> LogEntries =>
        _logService?.Entries ?? new System.Collections.ObjectModel.ReadOnlyObservableCollection<string>([]);

    protected void AddLog(string message) => _logService?.Add(message, GetType().Name);

    protected void ClearLog() => _logService?.Clear();
}
