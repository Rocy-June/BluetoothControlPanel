using System.Reflection;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;

using BluetoothControlPanel.Core.Bluetooth;
using BluetoothControlPanel.UI.Services.Configuration;
using BluetoothControlPanel.UI.Services.DependencyInjection;
using BluetoothControlPanel.UI.Services.Theme;
using BluetoothControlPanel.UI.Services.Taskbar;
using BluetoothControlPanel.UI.Views;

namespace BluetoothControlPanel.UI;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private TrayIconService? _trayIconService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ThemeService.Initialize(this);

        var services = new ServiceCollection();
        services.AddAttributedServices(Assembly.GetExecutingAssembly());

        _serviceProvider = services.BuildServiceProvider();

        var configService = _serviceProvider.GetRequiredService<IAppConfigService>();
        await configService.LoadAsync();

        await Driver.InitAsync();
        await Driver.LoadPairedDevicesAsync();

        _trayIconService = _serviceProvider.GetRequiredService<TrayIconService>();

        var window = _serviceProvider.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();
        _trayIconService = null;

        if (Driver.IsInitialized)
        {
            Driver.Dispose();
        }

        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
