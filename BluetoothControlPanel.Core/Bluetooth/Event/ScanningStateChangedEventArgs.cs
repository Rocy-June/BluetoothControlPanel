using System;
using System.Collections.Generic;
using System.Text;

namespace BluetoothControlPanel.Core.Bluetooth.Event;

public sealed class ScanningStateChangedEventArgs : EventArgs
{
    public ScanningStateChangedEventArgs(bool isScanning, string? reason = null)
    {
        IsScanning = isScanning;
        Reason = reason;
    }

    public bool IsScanning { get; }

    public string? Reason { get; }
}

