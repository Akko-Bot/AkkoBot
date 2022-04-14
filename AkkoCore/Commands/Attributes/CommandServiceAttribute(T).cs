using AkkoCore.Commands.Abstractions;
using Kotz.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace AkkoCore.Commands.Attributes;

/// <summary>
/// Marks a class as a command service and defines its lifespan and interface.
/// </summary>
/// <typeparam name="T">The interface of this service.</typeparam>
/// <inheritdoc/>
public sealed class CommandServiceAttribute<T> : CommandServiceAttributeBase
{
    /// <summary>
    /// Defines the type this service should be registered as.
    /// </summary>
    public Type AbstractionType { get; }

    public CommandServiceAttribute(ServiceLifetime lifespan) : base(lifespan)
        => AbstractionType = typeof(T);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Occurs when <paramref name="implementationType"/> is not assignable to <see cref="CommandServiceAttribute{T}.AbstractionType"/>.
    /// </exception>
    public override void RegisterService(IServiceCollection ioc, Type implementationType)
    {
        if (!implementationType.IsAssignableTo(AbstractionType))
            throw new InvalidOperationException(GetRegistrationInheritanceError(implementationType));

        if (ioc.Any(x => x.ImplementationType?.EqualsAny(AbstractionType, implementationType) is true || x.ServiceType.EqualsAny(AbstractionType, implementationType)))
            throw new ArgumentException(base.GetRegistrationPresenceError(ioc, implementationType, AbstractionType));

        base.RegisterService(ioc, implementationType);
    }

    protected override void RegisterSingleton(IServiceCollection ioc, Type implementationType)
        => ioc.AddSingleton(AbstractionType, implementationType);

    protected override void RegisterScoped(IServiceCollection ioc, Type implementationType)
        => ioc.AddScoped(AbstractionType, implementationType);

    protected override void RegisterTransient(IServiceCollection ioc, Type implementationType)
        => ioc.AddTransient(AbstractionType, implementationType);

    /// <summary>
    /// Gets the error message for when the service type doesn't match the provided interface.
    /// </summary>
    /// <param name="implementationType">The type of the service to be registered.</param>
    /// <returns>The error message.</returns>
    private string GetRegistrationInheritanceError(Type implementationType)
        => @$"Type ""{implementationType.Name}"" does not {((AbstractionType.IsInterface) ? "implement" : "inherit")} ""{AbstractionType.Name}"".";
}