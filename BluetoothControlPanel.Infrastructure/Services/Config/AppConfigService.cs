using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BluetoothControlPanel.Application.DependencyInjection;
using BluetoothControlPanel.Application.Services.Config;
using BluetoothControlPanel.Domain.Model;

namespace BluetoothControlPanel.Infrastructure.Services.Config;

[SingletonService(ServiceType = typeof(IAppConfigService))]
public class AppConfigService : IAppConfigService
{
    private readonly JsonSerializerOptions _jsonOptions =
        new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AppConfigService()
    {
        ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.dat");
    }

    public string ConfigPath { get; }

    public AppConfig Current { get; private set; } = AppConfig.Default;

    public async Task<AppConfig> LoadAsync()
    {
        if (File.Exists(ConfigPath))
        {
            await using var stream = File.OpenRead(ConfigPath);
            var config = await JsonSerializer.DeserializeAsync<AppConfig>(stream, _jsonOptions);
            if (config is not null)
            {
                Current = config;
                return Current;
            }
        }

        Current = AppConfig.Default;
        await SaveAsync();
        return Current;
    }

    public async Task SaveAsync()
    {
        var directory = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(ConfigPath);
        await JsonSerializer.SerializeAsync(stream, Current, _jsonOptions);
        await stream.FlushAsync();
    }
}
