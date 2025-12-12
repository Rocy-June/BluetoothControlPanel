using System.Collections.Generic;

namespace BluetoothControlPanel.UI.Services.Monitors;

public interface IMonitorService
{
    IReadOnlyList<MonitorInfo> GetMonitors();

    MonitorInfo? GetPrimaryMonitor();
}
