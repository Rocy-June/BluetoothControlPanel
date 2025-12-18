using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace BluetoothControlPanel.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAttributedServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var exportedTypes = assembly
                .GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract);

            foreach (var type in exportedTypes)
            {
                var registrations = type.GetCustomAttributes<ServiceRegistrationAttribute>(inherit: false);

                foreach (var registration in registrations)
                {
                    var serviceType = registration.ServiceType ?? type;

                    switch (registration.Lifetime)
                    {
                        case ServiceLifetime.Singleton:
                            services.AddSingleton(serviceType, type);
                            break;
                        case ServiceLifetime.Transient:
                            services.AddTransient(serviceType, type);
                            break;
                        default:
                            throw new NotSupportedException($"Unsupported lifetime {registration.Lifetime} on {type.FullName}");
                    }
                }
            }
        }

        return services;
    }
}
