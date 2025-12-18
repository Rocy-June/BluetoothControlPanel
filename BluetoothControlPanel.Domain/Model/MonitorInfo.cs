using System.Windows;

namespace BluetoothControlPanel.Domain.Model;

public sealed record MonitorInfo(
    string DeviceName,
    Rect MonitorArea,
    Rect WorkArea,
    bool IsPrimary);
