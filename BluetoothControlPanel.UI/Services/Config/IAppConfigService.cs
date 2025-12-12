using System.Threading.Tasks;

namespace BluetoothControlPanel.UI.Services.Configuration;

public interface IAppConfigService
{
    string ConfigPath { get; }

    AppConfig Current { get; }

    Task<AppConfig> LoadAsync();

    Task SaveAsync();
}
