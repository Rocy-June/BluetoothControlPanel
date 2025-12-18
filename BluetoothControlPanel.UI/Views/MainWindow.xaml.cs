using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

using BluetoothControlPanel.Application.DependencyInjection;
using BluetoothControlPanel.Application.Services.Monitors;
using BluetoothControlPanel.Application.ViewModels;
using BluetoothControlPanel.Domain.Model;

namespace BluetoothControlPanel.UI.Views;

[SingletonService]
public partial class MainWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private readonly IMonitorService _monitorService;
    private readonly MainViewModel _viewModel;

    public MainWindow(IMonitorService monitorService, MainViewModel viewModel)
    {
        _monitorService = monitorService;
        _viewModel = viewModel;

        InitializeComponent();
        DataContext = viewModel;

        Loaded += OnLoaded;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetNotShowInTaskList();
        ResetPosition();
        ApplyWindowOpenState(_viewModel.IsWindowOpen);
    }

    private void SetNotShowInTaskList()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        _ = SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
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

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsWindowOpen))
        {
            ApplyWindowOpenState(_viewModel.IsWindowOpen);
        }
    }

    private void ApplyWindowOpenState(bool isOpen)
    {
        if (isOpen)
        {
            if (FindResource("ShowWindowStoryboard") is Storyboard storyboard)
            {
                Show();
                storyboard.Begin(this, true);
            }
        }
        else 
        {
            if (FindResource("HideWindowStoryboard") is Storyboard storyboard)
            {
                storyboard.Completed += (s, e) => { Hide(); };
                storyboard.Begin(this, true);
            }
        }
    }
}
