using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Hardcodet.Wpf.TaskbarNotification;

using BluetoothControlPanel.Application.DependencyInjection;
using BluetoothControlPanel.Application.Services.Logging;
using BluetoothControlPanel.Application.ViewModels;
using BluetoothControlPanel.Application.Services.Windows;

namespace BluetoothControlPanel.Infrastructure.Services.Taskbar;

[SingletonService]
public sealed class TrayIconService : IDisposable
{
    private readonly TaskbarIcon _taskbarIcon;

    private readonly ILogService _logService;
    private readonly IWindowManager _windowManager;
    private readonly MainViewModel _mainViewModel;

    public TrayIconService(ILogService logService, IWindowManager windowManager, MainViewModel mainViewModel)
    {
        _logService = logService;
        _windowManager = windowManager;
        _mainViewModel = mainViewModel;
        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "Bluetooth Device",
            Icon = LoadTrayIcon() ?? SystemIcons.Application,
            Visibility = Visibility.Visible
        };
        _taskbarIcon.TrayLeftMouseUp += TaskbarIcon_TrayLeftMouseUp;
    }

    private void TaskbarIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
    {
        _logService.Add("Tray icon clicked.");

        _mainViewModel.IsWindowOpen = true;
        _windowManager.FocusMainWindow();
    }

    private static Icon? LoadTrayIcon()
    {
        try
        {
            var iconUri = new Uri("pack://application:,,,/Assets/Images/ApplicationIcon.ico");
            var streamInfo = System.Windows.Application.GetResourceStream(iconUri);
            if (streamInfo == null)
            {
                return null;
            }

            var desiredSize = GetTrayIconSize();
            return new Icon(streamInfo.Stream, new System.Drawing.Size(desiredSize, desiredSize));
        }
        catch
        {
            return null;
        }
    }

    private static int GetTrayIconSize()
    {
        const int defaultSize = 16;
        var window = System.Windows.Application.Current?.MainWindow;
        if (window == null)
        {
            return defaultSize;
        }

        try
        {
            var dpi = VisualTreeHelper.GetDpi(window);
            return Math.Max(defaultSize, (int)Math.Round(defaultSize * dpi.DpiScaleX));
        }
        catch
        {
            return defaultSize;
        }
    }

    public void Dispose()
    {
        _taskbarIcon.Visibility = Visibility.Collapsed;
        _taskbarIcon.Dispose();
    }

    public void TestLog(string message = "Tray icon test log")
    {
        _logService.Add(message, nameof(TrayIconService));
    }
}
