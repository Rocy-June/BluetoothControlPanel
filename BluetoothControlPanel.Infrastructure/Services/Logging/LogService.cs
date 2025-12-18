using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using BluetoothControlPanel.Application.DependencyInjection;
using BluetoothControlPanel.Application.Services.Logging;

namespace BluetoothControlPanel.Infrastructure.Services.Logging;

[SingletonService(ServiceType = typeof(ILogService))]
public sealed class LogService : ILogService
{
    private const int MaxEntries = 500;
    private readonly ObservableCollection<string> _log = [];
    private readonly ReadOnlyObservableCollection<string> _readonly;
    private readonly Lock _lock = new();

    public LogService()
    {
        _readonly = new ReadOnlyObservableCollection<string>(_log);
    }

    public ReadOnlyObservableCollection<string> Entries => _readonly;

    public void Add(string message, [CallerFilePath] string callerFilePath = "")
    {
        RunOnUi(() =>
        {
            lock (_lock)
            {
                if (_log.Count > MaxEntries)
                {
                    _log.RemoveAt(_log.Count - 1);
                }

                var resolvedSource = ResolveSource(callerFilePath) ?? "Unknown";
                _log.Insert(0, $"{DateTime.Now:HH:mm:ss.fff} [{resolvedSource}]: {message}");
            }
        });
    }

    public void Clear()
    {
        RunOnUi(() =>
        {
            lock (_lock)
            {
                _log.Clear();
            }
        });
    }

    private static string? ResolveSource(string callerFilePath)
    {
        if (string.IsNullOrWhiteSpace(callerFilePath))
        {
            return null;
        }

        try
        {
            return Path.GetFileNameWithoutExtension(callerFilePath);
        }
        catch
        {
            return null;
        }
    }

    private static void RunOnUi(Action action)
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
