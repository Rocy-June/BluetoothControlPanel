using System;
using System.Collections.Generic;
using System.Text;

namespace BluetoothControlPanel.UI.Services.Monitors
{
    public enum TaskBarPosition : int
    {
        Bottom = 0b0001,
        Top = 0b0011,
        Left = 0b0100,
        Right = 0b1100
    }
}
