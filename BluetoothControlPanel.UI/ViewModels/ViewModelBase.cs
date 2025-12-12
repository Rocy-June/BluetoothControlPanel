using System;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BluetoothControlPanel.UI.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    private readonly ObservableCollection<string> log = [];
    private readonly ReadOnlyObservableCollection<string> readonlyLog;
    private readonly Lock locker = new();

    protected ViewModelBase()
    {
        readonlyLog = new ReadOnlyObservableCollection<string>(log);
    }

    public ReadOnlyObservableCollection<string> LogEntries => readonlyLog;

    protected void AddLog(string message)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(AddEntry);
        }
        else
        {
            AddEntry(message);
        }
    }
    private void AddEntry(string message)
    {
        lock (locker)
        {
            if (log.Count > 500)
            {
                log.RemoveAt(log.Count - 1);
            }

            log.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}: {message}");
        }
    }

    protected void ClearLog()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(ClearEntries);
        }
        else
        {
            ClearEntries();
        }
    }
    private void ClearEntries()
    {
        lock (locker)
        {
            log.Clear();
        }
    }
}
