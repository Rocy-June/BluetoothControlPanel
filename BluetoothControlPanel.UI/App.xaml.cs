using System.Reflection;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;

using BluetoothControlPanel.Application.DependencyInjection;
using BluetoothControlPanel.Application.Services.Config;
using BluetoothControlPanel.Application.Services.Theme;
using BluetoothControlPanel.Application.Services.Windows;
using BluetoothControlPanel.Application.ViewModels;
using BluetoothControlPanel.Application.Services.Bluetooth;
using BluetoothControlPanel.Infrastructure.Services.Monitors;
using BluetoothControlPanel.UI.Views;
using BluetoothControlPanel.Infrastructure.Services.Taskbar;

namespace BluetoothControlPanel.UI;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;
    private TrayIconService? _trayIconService;
    private IBluetoothDriverService? _driver;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ThemeService.Initialize(this);

        var services = new ServiceCollection();
        services.AddAttributedServices(
            Assembly.GetExecutingAssembly(),
            typeof(MainViewModel).Assembly,
            typeof(MonitorService).Assembly);

        _serviceProvider = services.BuildServiceProvider();

        var configService = _serviceProvider.GetRequiredService<IAppConfigService>();
        await configService.LoadAsync();

        _driver = _serviceProvider.GetRequiredService<IBluetoothDriverService>();
        await _driver.InitAsync();
        await _driver.LoadPairedDevicesAsync();

        _trayIconService = _serviceProvider.GetRequiredService<TrayIconService>();

        var windowManager = _serviceProvider.GetRequiredService<IWindowManager>();
        windowManager.ShowMainWindow();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();
        _trayIconService = null;

        if (_driver is not null && _driver.IsInitialized)
        {
            _driver.Dispose();
        }

        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
