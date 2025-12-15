using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Hardcodet.Wpf.TaskbarNotification;

using BluetoothControlPanel.UI.Services.DependencyInjection;

namespace BluetoothControlPanel.UI.Services.Taskbar;

[SingletonService]
public sealed class TrayIconService : IDisposable
{
    private readonly TaskbarIcon _taskbarIcon;

    public TrayIconService()
    {
        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "Bluetooth Control Panel",
            Icon = LoadTrayIcon() ?? SystemIcons.Application,
            Visibility = Visibility.Visible
        };
    }

    private static Icon? LoadTrayIcon()
    {
        try
        {
            var iconUri = new Uri("pack://application:,,,/Assets/Images/ApplicationIcon.ico");
            var streamInfo = Application.GetResourceStream(iconUri);
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
        var window = Application.Current?.MainWindow;
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
}
