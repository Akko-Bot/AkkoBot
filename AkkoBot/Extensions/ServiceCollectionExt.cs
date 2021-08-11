using AkkoBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AkkoBot.Extensions
{
    public static class ServiceCollectionExt
    {
        /// <summary>
        /// Adds several singleton services of the type specified in <paramref name="abstraction"/> to this <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="dependencies">This collection of services.</param>
        /// <param name="abstraction">The type implemented by all services.</param>
        /// <returns>This <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddSingletonServices(this IServiceCollection dependencies, Type abstraction)
        {
            var modules = GeneralService.GetConcreteTypesOf(abstraction);

            foreach (var module in modules)
                dependencies.AddSingleton(module);

            return dependencies;
        }

        /// <summary>
        /// Adds several singleton services provided in <paramref name="implementations"/> to this <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="dependencies">This collection of services.</param>
        /// <param name="implementations">Objects to be injected as singletons.</param>
        /// <returns>This <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddSingletonServices(this IServiceCollection dependencies, params object[] implementations)
        {
            foreach (var module in implementations)
                dependencies.AddSingleton(module.GetType(), module);

            return dependencies;
        }

        /// <summary>
        /// Adds several scoped services of the type specified in <paramref name="abstraction"/> to this <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="dependencies">This collection of services.</param>
        /// <param name="abstraction">The type implemented by all services</param>
        /// <returns>This <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddScopedServices(this IServiceCollection dependencies, Type abstraction)
        {
            var modules = GeneralService.GetConcreteTypesOf(abstraction);

            foreach (var module in modules)
                dependencies.AddScoped(abstraction, module.GetType());

            return dependencies;
        }

        /// <summary>
        /// Adds several transient services of the type specified in <paramref name="abstraction"/> to this <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="dependencies">This collection of services.</param>
        /// <param name="abstraction">The type implemented by all services</param>
        /// <returns>This <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddTransientServices(this IServiceCollection dependencies, Type abstraction)
        {
            var modules = GeneralService.GetConcreteTypesOf(abstraction);

            foreach (var module in modules)
                dependencies.AddTransient(abstraction, module.GetType());

            return dependencies;
        }
    }
}