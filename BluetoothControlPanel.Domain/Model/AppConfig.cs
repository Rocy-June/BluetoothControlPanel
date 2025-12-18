namespace BluetoothControlPanel.Domain.Model;

public sealed class AppConfig
{
    public string ApplicationTitle { get; set; } = "Bluetooth Control Panel";

    public string LastDeviceId { get; set; } = string.Empty;

    public bool AutoReconnect { get; set; } = true;

    public static AppConfig Default => new();
}
