using System.Windows;
using BluetoothControlPanel.UI.ViewModels;
using BluetoothControlPanel.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BluetoothControlPanel.UI;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
    }

    private void OnStartup(object sender, StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
