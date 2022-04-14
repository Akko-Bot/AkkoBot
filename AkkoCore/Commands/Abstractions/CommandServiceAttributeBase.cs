using Kotz.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace AkkoCore.Commands.Abstractions;

/// <summary>
/// Defines the base interface for registration of a command service.
/// </summary>
/// <remarks>
/// If you are inheriting from a class that contains this attribute,
/// don't forget to also apply this attribute to the derived class.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public abstract class CommandServiceAttributeBase : Attribute
{
    private readonly Action<IServiceCollection, Type> _serviceRegister;

    /// <summary>
    /// Defines the lifespan of this service.
    /// </summary>
    public ServiceLifetime Lifespan { get; }

    protected CommandServiceAttributeBase(ServiceLifetime lifespan)
    {
        Lifespan = lifespan;

        _serviceRegister = lifespan switch
        {
            ServiceLifetime.Singleton => RegisterSingleton,
            ServiceLifetime.Scoped => RegisterScoped,
            ServiceLifetime.Transient => RegisterTransient,
            _ => throw new NotImplementedException($"There is no registration available for {lifespan} services."),
        };
    }

    /// <summary>
    /// Registers the specified type as a service in the IoC container.
    /// </summary>
    /// <param name="ioc">The IoC container.</param>
    /// <param name="implementationType">The type of the service to be registered.</param>
    /// <exception cref="ArgumentException">Occurs when a service of type <paramref name="implementationType"/> is already registered.</exception>
    public virtual void RegisterService(IServiceCollection ioc, Type implementationType)
    {
        if (ioc.Any(x => x.ImplementationType == implementationType || x.ServiceType == implementationType))
            throw new ArgumentException(GetRegistrationPresenceError(ioc, implementationType));

        _serviceRegister(ioc, implementationType);
    }

    /// <summary>
    /// Registers the specified type as a singleton service.
    /// </summary>
    /// <param name="ioc">The IoC container.</param>
    /// <param name="implementationType">The type of the service to be registered.</param>
    protected abstract void RegisterSingleton(IServiceCollection ioc, Type implementationType);

    /// <summary>
    /// Registers the specified type as a scoped service.
    /// </summary>
    /// <param name="ioc">The IoC container.</param>
    /// <param name="implementationType">The type of the service to be registered.</param>
    protected abstract void RegisterScoped(IServiceCollection ioc, Type implementationType);

    /// <summary>
    /// Registers the specified type as a transient service.
    /// </summary>
    /// <param name="ioc">The IoC container.</param>
    /// <param name="implementationType">The type of the service to be registered.</param>
    protected abstract void RegisterTransient(IServiceCollection ioc, Type implementationType);

    /// <summary>
    /// Gets the formatted error message for a service that has already been registered.
    /// </summary>
    /// <param name="ioc">The IoC container.</param>
    /// <param name="concreteType">The concrete type of the service.</param>
    /// <param name="abstractType">The interface of the service.</param>
    /// <returns>The error message.</returns>
    protected string GetRegistrationPresenceError(IServiceCollection ioc, Type concreteType, Type? abstractType = default)
    {
        var (serviceName, errorMessage) = GetRegisteredServiceError(ioc, concreteType, abstractType);

        return @$"Service of type ""{serviceName}"" is already registered." + Environment.NewLine +
            $"[Present] {errorMessage}" + Environment.NewLine +
            $"[Attempted] " + ((abstractType is null) ? concreteType.Name : $"{abstractType.Name}: {concreteType.Name}");
    }

    /// <summary>
    /// Gets the error message for a service in the IoC container.
    /// </summary>
    /// <param name="ioc">The IoC container.</param>
    /// <param name="concreteType">The type of the registered service.</param>
    /// <returns>The name of the service and the error message for the specified service.</returns>
    /// <exception cref="InvalidOperationException">
    /// Occurs when <paramref name="concreteType"/> and <paramref name="abstractType"/> have not been registered to the <paramref name="ioc"/>.
    /// </exception>
    private (string, string) GetRegisteredServiceError(IServiceCollection ioc, Type concreteType, Type? abstractType = default)
    {
        var registeredService = ioc.First(x => x.ImplementationType?.EqualsAny(concreteType, abstractType) is true || x.ServiceType.EqualsAny(concreteType, abstractType));
        var registeredConcreteType = (registeredService.ImplementationInstance is null)
            ? registeredService.ImplementationType?.Name
            : registeredService.ImplementationInstance.GetType().Name + " (instance)";

        return (registeredConcreteType ?? registeredService.ServiceType.Name,
            registeredService.ServiceType.Name +
            ((string.IsNullOrWhiteSpace(registeredConcreteType) || registeredService.ServiceType.Name.Equals(registeredConcreteType))
                ? string.Empty
                : ": " + registeredConcreteType));
    }
}