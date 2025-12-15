using System;
using System.Windows;
using System.Windows.Media;
using BluetoothControlPanel.UI.Services.DependencyInjection;
using BluetoothControlPanel.UI.Services.Monitors;

namespace BluetoothControlPanel.UI.Views;

[SingletonService]
public partial class MainWindow : Window
{
    private readonly IMonitorService _monitorService;

    public MainWindow(IMonitorService monitorService, BluetoothControlPanel.UI.ViewModels.MainViewModel viewModel)
    {
        _monitorService = monitorService;

        InitializeComponent();
        DataContext = viewModel;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ResetPosition();
    }

    private void ResetPosition()
    {
        // Ensure layout is measured to avoid zero ActualWidth/ActualHeight.
        if (double.IsNaN(ActualWidth) || ActualWidth <= 0 || double.IsNaN(ActualHeight) || ActualHeight <= 0)
        {
            UpdateLayout();
        }

        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;

        var primary = _monitorService.GetPrimaryMonitor();
        if (primary is null)
        {
            return;
        }

        var taskBarPosition = GetTaskBarPosition(primary);

        var topLeft = new Point(primary.WorkArea.Left, primary.WorkArea.Top);
        var bottomRight = new Point(primary.WorkArea.Right, primary.WorkArea.Bottom);

        double targetLeft;
        double targetTop;

        switch (taskBarPosition)
        {
            case TaskBarPosition.Top:
                targetLeft = Math.Max(topLeft.X, bottomRight.X - width);
                targetTop = topLeft.Y;
                break;
            case TaskBarPosition.Left:
                targetLeft = topLeft.X;
                targetTop = Math.Max(topLeft.Y, bottomRight.Y - height);
                break;
            default:
                targetLeft = Math.Max(topLeft.X, bottomRight.X - width);
                targetTop = Math.Max(topLeft.Y, bottomRight.Y - height);
                break;
        }

        var marginObj = TryFindResource("WindowBorderMarginSize");
        var margin = marginObj is double marginSize ? marginSize : 10d;
        var newMargin = new Thickness(margin);
        switch (taskBarPosition)
        {
            case TaskBarPosition.Bottom:
                newMargin.Bottom = -1;
                break;
            case TaskBarPosition.Top:
                newMargin.Top = -1;
                break;
            case TaskBarPosition.Left:
                newMargin.Left = -1;
                break;
            case TaskBarPosition.Right:
                newMargin.Right = -1;
                break;
        }

        Left = targetLeft;
        Top = targetTop;
        Root.Margin = newMargin;
    }

    private TaskBarPosition GetTaskBarPosition(MonitorInfo monitorInfo)
    {
        var monitor = monitorInfo.MonitorArea;
        var work = monitorInfo.WorkArea;

        if (work.Bottom < monitor.Bottom)
        {
            return TaskBarPosition.Bottom;
        }
        else if (work.Top > monitor.Top)
        {
            return TaskBarPosition.Top;
        }
        else if (work.Right < monitor.Right)
        {
            return TaskBarPosition.Right;
        }

        return TaskBarPosition.Left;
    }
}
