using Microsoft.Extensions.DependencyInjection;
using System;

namespace AkkoCore.Extensions;

public static class DependencyInjectionExt
{
    /// <summary>
    /// Gets a scoped service of type <typeparamref name="T"/> from this <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of the service.</typeparam>
    /// <param name="ioc">This service provider.</param>
    /// <param name="service">The requested scoped service. It will be <see langword="null"/> if <typeparamref name="T"/> is not registered.</param>
    /// <returns>An <see cref="IServiceScope"/> to be disposed after use.</returns>
    public static IServiceScope GetScopedService<T>(this IServiceProvider ioc, out T? service)
    {
        var scope = ioc.CreateScope();
        service = scope.ServiceProvider.GetService<T>();

        return scope;
    }

    /// <summary>
    /// Gets a scoped service of type <typeparamref name="T"/> from this <see cref="IServiceScopeFactory"/>.
    /// </summary>
    /// <typeparam name="T">The type of the service.</typeparam>
    /// <param name="scopeFactory">The IOC's scope factory.</param>
    /// <param name="service">The requested scoped service. It will be <see langword="null"/> if <typeparamref name="T"/> is not registered.</param>
    /// <returns>An <see cref="IServiceScope"/> to be disposed after use.</returns>
    public static IServiceScope GetScopedService<T>(this IServiceScopeFactory scopeFactory, out T? service)
    {
        var scope = scopeFactory.CreateScope();
        service = scope.ServiceProvider.GetService<T>();

        return scope;
    }

    /// <summary>
    /// Gets a scoped service of type <typeparamref name="T"/> from this <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of the service.</typeparam>
    /// <param name="ioc">This service provider.</param>
    /// <param name="service">The requested scoped service. It will be <see langword="null"/> if <typeparamref name="T"/> is not registered.</param>
    /// <returns>An <see cref="IServiceScope"/> to be disposed after use.</returns>
    /// <exception cref="InvalidOperationException">Occurs when the service of type <typeparamref name="T"/> is not found.</exception>
    public static IServiceScope GetRequiredScopedService<T>(this IServiceProvider ioc, out T service) where T : notnull
    {
        var scope = ioc.CreateScope();
        service = scope.ServiceProvider.GetRequiredService<T>();

        return scope;
    }

    /// <summary>
    /// Gets a scoped service of type <typeparamref name="T"/> from this <see cref="IServiceScopeFactory"/>.
    /// </summary>
    /// <typeparam name="T">The type of the service.</typeparam>
    /// <param name="scopeFactory">The IOC's scope factory.</param>
    /// <param name="service">The requested scoped service. It will be <see langword="null"/> if <typeparamref name="T"/> is not registered.</param>
    /// <returns>An <see cref="IServiceScope"/> to be disposed after use.</returns>
    /// <exception cref="InvalidOperationException">Occurs when the service of type <typeparamref name="T"/> is not found.</exception>
    public static IServiceScope GetRequiredScopedService<T>(this IServiceScopeFactory scopeFactory, out T service) where T : notnull
    {
        var scope = scopeFactory.CreateScope();
        service = scope.ServiceProvider.GetRequiredService<T>();

        return scope;
    }
}