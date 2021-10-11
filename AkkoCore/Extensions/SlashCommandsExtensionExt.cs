using AkkoCore.Config.Abstractions;
using AkkoCore.Services;
using DSharpPlus.SlashCommands;
using System.Linq;
using System.Reflection;

namespace AkkoCore.Extensions
{
    public static class SlashCommandsExtensionExt
    {
        /// <summary>
        /// Registers slash commands from an assembly to the command handler.
        /// </summary>
        /// <param name="slashHandler">The command handler.</param>
        /// <param name="cogAssembly">The assembly where the commands are from.</param>
        /// <param name="guildId">The ID of the guild to register the slash commands to or <see langword="null"/> if the commands should be global.</param>
        /// <remarks>If the cog has an <see cref="ICogSetup"/> defined, it will take precedence over the types present in the assembly.</remarks>
        public static void RegisterCogCommands(this SlashCommandsExtension slashHandler, Assembly cogAssembly, ulong? guildId = default)
        {
            static bool HasValidSetup(ICogSetup cogSetup)
                => cogSetup.SlashCommandsScope is not null && cogSetup.SlashCommandsScope.Count is not 0;

            var cogSetups = AkkoUtilities.GetCogSetups(cogAssembly).ToArray();

            if (!cogSetups.Any(HasValidSetup))
                slashHandler.RegisterCommands(cogAssembly, guildId);
            else
            {
                foreach (var typeContext in cogSetups.Where(HasValidSetup).SelectMany(x => x.SlashCommandsScope))
                    slashHandler.RegisterCommands(typeContext.Key, typeContext.Value);
            }
        }
    }
}