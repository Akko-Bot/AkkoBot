using Microsoft.Extensions.DependencyInjection;
using System;

namespace AkkoBot.Extensions
{
    public static class IServiceProviderExt
    {
        /// <summary>
        /// Get a scoped service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type of the service.</typeparam>
        /// <param name="ioc">This service provider.</param>
        /// <param name="service">The requested scoped service. It will be <see langword="null"/> if <typeparamref name="T"/> is not registered.</param>
        /// <remarks>This method is heavy on performance. Use sparingly.</remarks>
        /// <returns>An <see cref="IServiceScope"/> to be disposed of after use.</returns>
        public static IServiceScope GetScopedService<T>(this IServiceProvider ioc, out T service)
        {
            var scope = ioc.CreateScope();
            service = scope.ServiceProvider.GetService<T>();

            return scope;
        }
    }
}