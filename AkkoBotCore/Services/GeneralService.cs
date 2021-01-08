using System;
using System.Collections.Generic;
using System.Linq;

namespace AkkoBot.Services
{
    public class GeneralService
    {
        /// <summary>
        /// Gets a collection of all classes of the specified type in the AkkoBot namespace.
        /// </summary>
        /// <param name="abstraction">The type implemented by all classes.</param>
        /// <returns>A collection of types.</returns>
        public static IEnumerable<Type> GetImplementables(Type abstraction)
        {
            return AppDomain.CurrentDomain.GetAssemblies()          // Get all assemblies associated with the project
                .SelectMany(assemblies => assemblies.GetTypes())    // Get all the types in those assemblies
                .Where(types => abstraction.IsAssignableFrom(types) // Filter to find any type that can be assigned to the specified abstraction
                && !types.IsInterface
                && !types.IsAbstract
                && types.Namespace.Contains("AkkoBot")
            );
        }
    }
}