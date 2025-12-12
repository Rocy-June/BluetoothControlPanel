using System.Windows;

namespace BluetoothControlPanel.UI.Services.Monitors;

public sealed record MonitorInfo(
    string DeviceName,
    Rect MonitorArea,
    Rect WorkArea,
    bool IsPrimary);
