using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Services;
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

        // Change this to pull from the database
        public static Task<int> ResolvePrefix(DiscordMessage msg)
            => Task.FromResult(msg.GetStringPrefixLength(">>>", StringComparison.OrdinalIgnoreCase));
    }
}
