using System;

namespace BluetoothControlPanel.Application.Bluetooth.Event;

public sealed class ScanningStateChangedEventArgs(bool isScanning, string? reason = null)
    : EventArgs
{
    public bool IsScanning { get; } = isScanning;

    public string? Reason { get; } = reason;
}
