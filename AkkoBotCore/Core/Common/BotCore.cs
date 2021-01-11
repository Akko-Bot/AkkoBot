using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Services;
using AkkoBot.Services.Database;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Core.Common
{
    public class BotCore
    {
        //private readonly Credentials _creds; // TODO: get prefix from database
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
        /// Returns the service container for this bot instance.
        /// </summary>
        /// <returns>The IoC container servicing this bot or <see langword="null"/> if there is none.</returns>
        public IServiceProvider GetServices()
            => CommandExt.Values.FirstOrDefault()?.Services;

        /// <summary>
        /// Returns a specific service registered in this bot instance.
        /// </summary>
        /// <typeparam name="T">Class of the registered service.</typeparam>
        /// <returns>The registered service or <see langword="null"/> if it hasn't been registered.</returns>
        public T GetService<T>()
            => (T)CommandExt.Values.FirstOrDefault()?.Services.GetService(typeof(T));

        /// <summary>
        /// Registers all commands in the project into the command handler.
        /// </summary>
        private void RegisterCommandModules()
        {
            var modules = GeneralService.GetImplementables(typeof(AkkoCommandModule)).ToArray();

            // Loop through the list of selected assemblies and register
            // each one of them on the command handler of each shard.
            foreach (var cmdHandler in CommandExt.Values)
            {
                foreach (var cmdModule in modules)
                    cmdHandler.RegisterCommands(cmdModule);
            }

            BotClient.Logger.LogInformation(
                new EventId(LoggerEvents.Startup.Id, "AkkoBot"),
                $"{modules.Length} command modules were successfully loaded on {CommandExt.Count} shards"
            );
        }
    }
}
