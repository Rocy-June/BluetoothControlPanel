using System.Threading.Tasks;
using BluetoothControlPanel.Domain.Model;

namespace BluetoothControlPanel.Application.Services.Config;

public interface IAppConfigService
{
    string ConfigPath { get; }

    AppConfig Current { get; }

    Task<AppConfig> LoadAsync();

    Task SaveAsync();
}
