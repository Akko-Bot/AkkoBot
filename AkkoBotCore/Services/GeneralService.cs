using AkkoBot.Command.Abstractions;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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

        /// <summary>
        /// Gets a collection of assemblies from the cogs directory
        /// </summary>
        /// <remarks>
        /// This method assumes all assemblies have AkkoBot as a dependency reference and
        /// contain commands that can be registered on CommandsNext.
        /// </remarks>
        /// <returns>A collection of assemblies.</returns>
        public static IEnumerable<Assembly> LoadCogs()
        {
            // Create directory if it doesn't exist already.
            if (!Directory.Exists(AkkoEnvironment.CogsDirectory))
                Directory.CreateDirectory(AkkoEnvironment.CogsDirectory);

            // Get all cogs from the cogs directory
            return Directory.EnumerateFiles(AkkoEnvironment.CogsDirectory)
                .Where(filePath => filePath.EndsWith(".dll"))
                .Select(filePath => Assembly.LoadFrom(filePath));
        }
    }
}