using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BluetoothControlPanel.Application.DependencyInjection;
using BluetoothControlPanel.Application.Services.Logging;
using BluetoothControlPanel.Application.Services.Windows;
using BluetoothControlPanel.Application.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

using App = System.Windows.Application;

namespace BluetoothControlPanel.Infrastructure.Services.Taskbar;

[SingletonService]
public sealed class TrayIconService : IDisposable
{
    private readonly TaskbarIcon _taskbarIcon;

    private readonly ILogService _logService;
    private readonly IWindowManager _windowManager;
    private readonly MainViewModel _mainViewModel;

    private bool _ignoreOnce = false;

    public TrayIconService(
        ILogService logService,
        IWindowManager windowManager,
        MainViewModel mainViewModel
    )
    {
        _logService = logService;
        _windowManager = windowManager;
        _mainViewModel = mainViewModel;
        _taskbarIcon = new()
        {
            ToolTipText = "Bluetooth Device",
            Icon = LoadTrayIcon() ?? SystemIcons.Application,
            Visibility = Visibility.Visible,
            ContextMenu = BuildContextMenu(),
        };
        _taskbarIcon.TrayLeftMouseDown += TaskbarIcon_TrayLeftMouseDown;
        _taskbarIcon.TrayLeftMouseUp += TaskbarIcon_TrayLeftMouseUp;
    }

    private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
    {
        _logService.Add("Tray icon left mouse downed.");

        if (_mainViewModel.IsWindowShowing)
        {
            _ignoreOnce = true;
        }
    }

    private void TaskbarIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
    {
        _logService.Add("Tray icon clicked.");

        if (_ignoreOnce)
        {
            _ignoreOnce = false;
            return;
        }

        _mainViewModel.IsWindowOpen = !_mainViewModel.IsWindowOpen;
        _windowManager.FocusMainWindow();
    }

    private static Icon? LoadTrayIcon()
    {
        try
        {
            var iconUri = new Uri("pack://application:,,,/Assets/Images/Logo.ico");
            var streamInfo = App.GetResourceStream(iconUri);
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
        var window = App.Current?.MainWindow;
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

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(CreateMenuItem("Allow device connections", OnAllowDeviceConnections));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Send file", OnSendFile));
        menu.Items.Add(CreateMenuItem("Receive file", OnReceiveFile));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Join personal area network", OnJoinPersonalAreaNetwork));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Open settings", OnOpenSettings));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Exit", OnExit));
        
        return menu;
    }

    private static MenuItem CreateMenuItem(string header, RoutedEventHandler handler)
    {
        var item = new MenuItem { Header = header };
        item.Click += handler;
        return item;
    }

    private void OnAllowDeviceConnections(object sender, RoutedEventArgs e)
    {
        _logService.Add("Allow device connections clicked.", nameof(TrayIconService));
    }

    private void OnSendFile(object sender, RoutedEventArgs e)
    {
        _logService.Add("Send file clicked.", nameof(TrayIconService));
    }

    private void OnReceiveFile(object sender, RoutedEventArgs e)
    {
        _logService.Add("Receive file clicked.", nameof(TrayIconService));
    }

    private void OnJoinPersonalAreaNetwork(object sender, RoutedEventArgs e)
    {
        _logService.Add("Join personal area network clicked.", nameof(TrayIconService));
    }

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        _mainViewModel.IsWindowOpen = true;
        _windowManager.FocusMainWindow();
    }

    private static void OnExit(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current?.Shutdown();
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
