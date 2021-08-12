using AkkoCore.Commands.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Events.Abstractions;
using AkkoCore.Services.Localization.Abstractions;
using AkkoCore.Services.Logging.Abstractions;
using AkkoCore.Services.Timers.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;

namespace AkkoCore.Core.Common
{
    /// <summary>
    /// Class that contains the Discord client and the command handlers.
    /// </summary>
    public class BotCore : IDisposable
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
        /// Registers all commands in the project into the command handler.
        /// </summary>
        private void RegisterCommandModules()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var converters = GeneralService.GetConcreteTypesOf(typeof(IArgumentConverter));
            var cogs = GeneralService.GetCogs();

            // Loop through the list of selected assemblies and register
            // each one of them to the command handler of each shard.
            foreach (var cmdHandler in CommandExt.Values)
            {
                // Remove the default TimeSpan converter, as Akko has her own converter
                cmdHandler.UnregisterConverter<TimeSpan>();

                // Register all core commands
                cmdHandler.RegisterCommands(assembly);

                // Register all argument converters
                foreach (var converter in converters)
                    cmdHandler.RegisterConverter(converter);

                // Register cog commands
                foreach (var cog in cogs)
                    cmdHandler.RegisterCommands(cog);
            }
        }

        public void Dispose()
        {
            // Dispose singletons
            CommandExt[0].Services.GetService<ILocalizer>()?.Dispose();
            CommandExt[0].Services.GetService<ILoggerFactory>()?.Dispose();
            CommandExt[0].Services.GetService<IAkkoLoggerProvider>()?.Dispose();
            CommandExt[0].Services.GetService<IAkkoCache>()?.Dispose();
            CommandExt[0].Services.GetService<IDbCache>()?.Dispose();
            CommandExt[0].Services.GetService<ITimerManager>()?.Dispose();
            CommandExt[0].Services.GetService<ICommandCooldown>()?.Dispose();
            CommandExt[0].Services.GetService<HttpClient>()?.Dispose();
            CommandExt[0].Services.GetService<IGatekeepEventHandler>()?.Dispose();

            // Dispose scoped
            foreach (var cmdHandler in CommandExt.Values)
                cmdHandler.Services.GetService<AkkoDbContext>()?.Dispose();

            // Dispose clients
            foreach (var client in BotShardedClient.ShardClients.Values)
                client.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}