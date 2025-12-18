using System.Collections.Generic;
using BluetoothControlPanel.Domain.Model;

namespace BluetoothControlPanel.Application.Services.Monitors;

public interface IMonitorService
{
    IReadOnlyList<MonitorInfo> GetMonitors();

    MonitorInfo? GetPrimaryMonitor();
}
