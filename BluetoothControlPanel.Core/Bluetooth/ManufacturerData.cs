namespace BluetoothControlPanel.Core.Bluetooth;

public class ManufacturerData(ushort companyId, byte[] data)
{
    public ushort CompanyId { get; } = companyId;
    public byte[] Data { get; } = data;
}
