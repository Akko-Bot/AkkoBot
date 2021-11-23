using AkkoCore.Commands.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AkkoCore.Commands.Attributes;

/// <summary>
/// Marks a class as a command service and defines its lifespan.
/// </summary>
/// <inheritdoc/>
public sealed class CommandServiceAttribute : CommandServiceAttributeBase
{
    public CommandServiceAttribute(ServiceLifetime lifespan) : base(lifespan)
    {
    }

    protected override void RegisterSingleton(IServiceCollection ioc, Type implementationType)
        => ioc.AddSingleton(implementationType);

    protected override void RegisterScoped(IServiceCollection ioc, Type implementationType)
        => ioc.AddScoped(implementationType);

    protected override void RegisterTransient(IServiceCollection ioc, Type implementationType)
        => ioc.AddTransient(implementationType);
}