using Microsoft.Extensions.DependencyInjection;
using System;

namespace AkkoCore.Commands.Attributes
{
    /// <summary>
    /// Marks a class as a command service and defines its lifespan.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public sealed class CommandServiceAttribute : Attribute
    {
        /// <summary>
        /// Defines the lifespan of this service.
        /// </summary>
        public ServiceLifetime Lifespan { get; }

        public CommandServiceAttribute(ServiceLifetime lifespan)
            => Lifespan = lifespan;
    }
}