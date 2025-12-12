using System;
using System.Collections.Generic;

namespace BluetoothControlPanel.Core.Bluetooth;

public sealed record ManufacturerData(ushort CompanyId, byte[] Data);

public sealed record DeviceInfo(
    string Address,
    string? Name,
    int Rssi,
    DateTimeOffset LastSeen,
    IReadOnlyList<ManufacturerData> ManufacturerData);
