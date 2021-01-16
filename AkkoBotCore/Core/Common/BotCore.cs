using System;
using System.Collections.Generic;
using System.Linq;
using AkkoBot.Command.Abstractions;
using AkkoBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Core.Common
{
    /// <summary>
    /// Class that contains the Discord client and the command handlers.
    /// </summary>
    public class BotCore
    {
        public DiscordShardedClient BotClient { get; }
        public IReadOnlyDictionary<int, CommandsNextExtension> CommandExt { get; }

        public BotCore(DiscordShardedClient client, IReadOnlyDictionary<int, CommandsNextExtension> cmdHandler)
        {
            BotClient = client;
            CommandExt = cmdHandler;

            // Register command modules
            RegisterCommandModules();

            // Override the default format for help commands
            //CommandExt.SetHelpFormatter<HelpFormatter>();
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
            var modules = GeneralService.GetImplementables(typeof(AkkoCommandModule)).ToArray();

            // Loop through the list of selected assemblies and register
            // each one of them to the command handler of each shard.
            foreach (var cmdHandler in CommandExt.Values)
            {
                foreach (var cmdModule in modules)
                    cmdHandler.RegisterCommands(cmdModule);
            }

            BotClient.Logger.LogInformation(
                new EventId(LoggerEvents.Startup.Id, "Startup"),
                $"{modules.Length} command modules were successfully loaded."
            );
        }
    }
}
