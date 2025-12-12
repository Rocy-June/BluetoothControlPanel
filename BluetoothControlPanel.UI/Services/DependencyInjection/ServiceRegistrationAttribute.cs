using System;
using Microsoft.Extensions.DependencyInjection;

namespace BluetoothControlPanel.UI.Services.DependencyInjection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public abstract class ServiceRegistrationAttribute : Attribute
{
    protected ServiceRegistrationAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }

    public ServiceLifetime Lifetime { get; }

    // Optional service type to register against (defaults to the attributed class).
    public Type? ServiceType { get; init; }
}

public sealed class SingletonServiceAttribute : ServiceRegistrationAttribute
{
    public SingletonServiceAttribute()
        : base(ServiceLifetime.Singleton)
    {
    }
}

public sealed class TransientServiceAttribute : ServiceRegistrationAttribute
{
    public TransientServiceAttribute()
        : base(ServiceLifetime.Transient)
    {
    }
}
