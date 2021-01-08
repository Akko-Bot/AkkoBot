using AkkoBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AkkoBot.Extensions
{
    public static class ServiceCollectionExt
    {
        /// <summary>
        /// Adds several singleton services of the type specified in serviceType to this IoC container.
        /// </summary>
        /// <param name="dependencies">This collection of injectables.</param>
        /// <param name="cmdModule">The type implemented by all classes.</param>
        /// <returns>A collection of default singletons to be injected into a service.</returns>
        public static IServiceCollection AddSingletonServices(this IServiceCollection dependencies, Type cmdModule)
        {
            var modules = GeneralService.GetImplementables(cmdModule);

            foreach (var module in modules)
                dependencies.AddSingleton(module);

            return dependencies;
        }

        /// <summary>
        /// Adds several singleton services provided in implementations to this IoC container.
        /// </summary>
        /// <param name="dependencies">This collection of injectables.</param>
        /// <param name="implementations">Objects to be injected as singletons.</param>
        /// <returns>A collection of singletons to be injected into a service.</returns>
        public static IServiceCollection AddSingletonServices(this IServiceCollection dependencies, params object[] implementations)
        {
            foreach (var module in implementations)
                dependencies.AddSingleton(module.GetType(), module);

            return dependencies;
        }
    }
}