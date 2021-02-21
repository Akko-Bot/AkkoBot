using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Extensions
{
    public static class CommandsNextExtensionExt
    {
        /// <summary>
        /// Registers an argument converter of the <paramref name="concreteType"/> type.
        /// </summary>
        /// <param name="cmdHandler">This command handler.</param>
        /// <param name="concreteType">The type of the converter to be registered.</param>
        /// <remarks>
        /// You must be absolutely sure that <paramref name="concreteType"/> implements
        /// <see cref="IArgumentConverter{T}"/>, otherwise you're going to get runtime errors.
        /// </remarks>
        public static void RegisterConverter(this CommandsNextExtension cmdHandler, Type concreteType)
        {
            // TODO: figure out a way to do this without dynamic
            dynamic instance = Activator.CreateInstance(concreteType);
            cmdHandler.RegisterConverter(instance);
        }
    }
}