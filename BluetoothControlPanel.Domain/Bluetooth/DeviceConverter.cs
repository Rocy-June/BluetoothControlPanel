using System;

namespace BluetoothControlPanel.Domain.Bluetooth;

public static class DeviceConverter
{
    public static DeviceType FromClassOfDevice(uint? classOfDevice)
    {
        if (classOfDevice is null)
        {
            return DeviceType.Unknown;
        }

        var cod = classOfDevice.Value;
        var major = (cod >> 8) & 0x1F;
        var minor = (cod >> 2) & 0x3F;

        return major switch
        {
            0x04 => MapAudioVideo(minor),
            0x05 => MapPeripheral(minor),
            0x02 => DeviceType.Phone,
            0x01 => DeviceType.Computer,
            0x07 => DeviceType.Wearable,
            0x06 => DeviceType.Imaging,
            0x09 => DeviceType.Health,
            0x08 => DeviceType.Toy,
            _ => DeviceType.Unknown
        };
    }

    private static DeviceType MapAudioVideo(uint minor)
    {
        return minor switch
        {
            0x04 => DeviceType.AudioHeadset,
            0x08 => DeviceType.AudioHeadphones,
            0x10 => DeviceType.AudioHandsfree,
            0x0C => DeviceType.AudioSpeaker,
            _ => DeviceType.AudioVideo
        };
    }

    private static DeviceType MapPeripheral(uint minor)
    {
        var hasKeyboard = (minor & 0x10) != 0;
        var hasPointer = (minor & 0x20) != 0;

        if (hasKeyboard && hasPointer)
        {
            return DeviceType.PeripheralCombo;
        }

        if (hasKeyboard)
        {
            return DeviceType.PeripheralKeyboard;
        }

        if (hasPointer)
        {
            return DeviceType.PeripheralMouse;
        }

        return DeviceType.Peripheral;
    }
}
