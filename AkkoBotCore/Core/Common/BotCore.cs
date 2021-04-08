using AkkoBot.Extensions;
using AkkoBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AkkoBot.Core.Common
{
    /// <summary>
    /// Class that contains the Discord client and the command handlers.
    /// </summary>
    public class BotCore
    {
        public DiscordShardedClient BotShardedClient { get; }
        public IReadOnlyDictionary<int, CommandsNextExtension> CommandExt { get; }

        internal BotCore(DiscordShardedClient client, IReadOnlyDictionary<int, CommandsNextExtension> cmdHandler)
        {
            BotShardedClient = client;
            CommandExt = cmdHandler;

            // Register command modules
            RegisterCommandModules();
        }

        /// <summary>
        /// Returns the service container for the specified bot shard.
        /// </summary>
        /// <param name="shard">The bot shard to get the IoC container from.</param>
        /// <returns>The IoC container servicing this bot shard or <see langword="null"/> if there is none.</returns>
        /// <exception cref="IndexOutOfRangeException"/>
        public IServiceProvider GetServices(int shard)
            => CommandExt[shard].Services;

        /// <summary>
        /// Registers all commands in the project into the command handler.
        /// </summary>
        private void RegisterCommandModules()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var converters = GeneralService.GetImplementables(typeof(IArgumentConverter));
            var cogs = GeneralService.GetCogs();

            // Loop through the list of selected assemblies and register
            // each one of them to the command handler of each shard.
            foreach (var cmdHandler in CommandExt.Values)
            {
                cmdHandler.RegisterCommands(assembly);

                foreach (var converter in converters)
                    cmdHandler.RegisterConverter(converter);

                foreach (var cog in cogs)
                    cmdHandler.RegisterCommands(cog);
            }
        }
    }
}