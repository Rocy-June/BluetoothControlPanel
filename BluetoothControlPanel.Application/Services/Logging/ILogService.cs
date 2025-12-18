using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace BluetoothControlPanel.Application.Services.Logging;

public interface ILogService
{
    ReadOnlyObservableCollection<string> Entries { get; }

    void Add(string message, [CallerFilePath] string callerFilePath = "");

    void Clear();
}
