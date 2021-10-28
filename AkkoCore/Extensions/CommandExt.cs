using AkkoCore.Commands.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AkkoCore.Extensions
{
    public static class CommandExt
    {
        /// <summary>
        /// Executes a command and logs its result to the console.
        /// </summary>
        /// <param name="command">This command.</param>
        /// <param name="context">The command context.</param>
        public static async Task ExecuteAndLogAsync(this Command command, CommandContext context)
        {
            var execution = await command.ExecuteAsync(context).ConfigureAwait(false);
            var level = (execution.IsSuccessful) ? LogLevel.Information : LogLevel.Error;

            context.Client.Logger.LogCommand(level, context, string.Empty, execution.Exception);
        }

        /// <summary>
        /// Gets the requirements from a command.
        /// </summary>
        /// <param name="command">The command to get the requirements from.</param>
        /// <returns>A list of attributes with the requirements.</returns>
        public static IEnumerable<CustomAttributeData> GetRequirements(this Command command)
        {
            return command.Module.ModuleType.GetMethods()
                .Where(
                    method => method.CustomAttributes.Any(
                        attribute => (attribute.ConstructorArguments.FirstOrDefault().Value as string)
                            ?.Equals(command.Name, StringComparison.InvariantCultureIgnoreCase) ?? false
                    )
                )
                .SelectMany(method => method.CustomAttributes)
                .Concat(GetCommandAttributeTree(command))
                .Where(
                    attribute => attribute.AttributeType == typeof(BotOwnerAttribute)
                        || attribute.AttributeType == typeof(RequireDirectMessageAttribute)
                        || attribute.AttributeType == typeof(RequireUserPermissionsAttribute)
                        || attribute.AttributeType == typeof(RequirePermissionsAttribute)
                )
                .DistinctBy(attribute => attribute.ConstructorArguments.FirstOrDefault().Value);
        }

        /// <summary>
        /// Gets all attributes from a command and the groups it belongs to.
        /// </summary>
        /// <param name="command">The command to get the attributes from.</param>
        /// <returns>A collection of attributes.</returns>
        private static IEnumerable<CustomAttributeData> GetCommandAttributeTree(Command command)
        {
            return (command.Parent is null)
                ? command.Module.ModuleType.CustomAttributes
                : command.Parent.Module.ModuleType.CustomAttributes.Concat(GetCommandAttributeTree(command.Parent));
        }
    }
}