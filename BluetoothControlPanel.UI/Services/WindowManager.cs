using System;
using System.Windows;
using BluetoothControlPanel.Application.DependencyInjection;
using BluetoothControlPanel.Application.Services.Windows;
using BluetoothControlPanel.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BluetoothControlPanel.UI.Services;

[SingletonService(ServiceType = typeof(IWindowManager))]
public sealed class WindowManager(IServiceProvider serviceProvider) : IWindowManager
{
    private MainWindow? _mainWindow;
    private DebugWindow? _debugWindow;

    public bool IsDebugWindowVisible => EnsureDebugWindow().IsVisible;

    public void ShowMainWindow()
    {
        DispatchToUi(() =>
        {
            var window = EnsureMainWindow();
            if (!window.IsVisible)
            {
                window.Show();
            }

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Activate();
        });
    }

    public void FocusMainWindow() 
    {
        _mainWindow?.Activate();
    }

    public void ShowDebugWindow()
    {
        DispatchToUi(() =>
        {
            var window = EnsureDebugWindow();
            if (window.IsVisible)
            {
                if (window.WindowState == WindowState.Minimized)
                {
                    window.WindowState = WindowState.Normal;
                }

                window.Activate();
                return;
            }

            window.Show();
            window.Activate();
        });
    }

    private MainWindow EnsureMainWindow() =>
        _mainWindow ??= serviceProvider.GetRequiredService<MainWindow>();

    private DebugWindow EnsureDebugWindow() =>
        _debugWindow ??= serviceProvider.GetRequiredService<DebugWindow>();

    private static void DispatchToUi(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            action();
            return;
        }

        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }
}
