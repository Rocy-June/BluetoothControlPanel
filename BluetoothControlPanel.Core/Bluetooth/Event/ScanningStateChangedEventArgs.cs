using System;
using System.Collections.Generic;
using System.Text;

namespace BluetoothControlPanel.Core.Bluetooth.Event;

public sealed class ScanningStateChangedEventArgs(bool isScanning, string? reason = null)
    : EventArgs
{
    public bool IsScanning { get; } = isScanning;

    public string? Reason { get; } = reason;
}
