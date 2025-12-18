using System.Windows;

namespace BluetoothControlPanel.Application.Services.Windows;

public interface IWindowManager
{
    bool IsDebugWindowVisible { get; }

    void ShowMainWindow();

    void FocusMainWindow();

    void ShowDebugWindow();
}
