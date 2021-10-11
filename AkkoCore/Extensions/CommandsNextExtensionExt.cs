using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AkkoCore.Extensions
{
    public static class CommandsNextExtensionExt
    {
        /// <summary>
        /// Registers an argument converter of the <paramref name="concreteType"/> type.
        /// </summary>
        /// <param name="cmdHandler">This command handler.</param>
        /// <param name="concreteType">The type of the converter to be registered.</param>
        /// <exception cref="ArgumentException">Occurs when <paramref name="concreteType"/> does not implement <see cref="IArgumentConverter{T}"/>.</exception>
        /// <exception cref="ArgumentNullException">Occurs when <paramref name="concreteType"/> is <see langword="null"/>.</exception>
        public static void RegisterConverter(this CommandsNextExtension cmdHandler, Type concreteType)
        {
            if (concreteType is null)
                throw new ArgumentNullException(nameof(concreteType), "Type of argument converter cannot be null.");
            else if (!concreteType.IsAssignableTo(typeof(IArgumentConverter)))
                throw new ArgumentException($"Type \"{concreteType.Name}\" must implement \"IArgumentConverter<T>\"", nameof(concreteType));

            // TODO: figure out a way to do this without dynamic
            dynamic instance = Activator.CreateInstance(concreteType);
            cmdHandler.RegisterConverter(instance);
        }

        /// <summary>
        /// Gets all commands registered in this command handler.
        /// </summary>
        /// <param name="cmdHandler">This command handler.</param>
        /// <remarks>Includes all command overloads.</remarks>
        /// <returns>The collection of all registered commands.</returns>
        public static IEnumerable<Command> GetAllCommands(this CommandsNextExtension cmdHandler)
        {
            return cmdHandler.RegisteredCommands.Values
                .Concat(
                    cmdHandler.RegisteredCommands.Values
                        .Where(x => x is CommandGroup)
                        .SelectMany(x => (x as CommandGroup).Children)
                );
        }

        /// <summary>
        /// Gets all commands registered in this command handler whose qualified name contains <paramref name="keyword"/>.
        /// </summary>
        /// <param name="cmdHandler">This command handler.</param>
        /// <param name="keyword">The keyword to search for.</param>
        /// <remarks>Includes all command overloads.</remarks>
        /// <returns>The collection of registered commands</returns>
        public static IEnumerable<Command> GetAllCommands(this CommandsNextExtension cmdHandler, string keyword)
        {
            return cmdHandler.GetAllCommands()
                .Where(
                    x => x.QualifiedName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        || x.Aliases.Any(x => x.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                );
        }
    }
}