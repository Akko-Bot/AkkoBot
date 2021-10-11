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
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public IReadOnlyDictionary<int, SlashCommandsExtension> SlashExt { get; }

        internal BotCore(
            DiscordShardedClient client,
            IReadOnlyDictionary<int, CommandsNextExtension> cmdHandlers,
            IReadOnlyDictionary<int, SlashCommandsExtension> slashHandlers)
        {
            BotShardedClient = client;
            CommandExt = cmdHandlers;
            SlashExt = slashHandlers;

            // Register command modules
            RegisterCommandModules();
        }

        /// <summary>
        /// Registers all commands in the project into the command handler.
        /// </summary>
        private void RegisterCommandModules()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var converters = AkkoUtilities.GetConcreteTypesOf<IArgumentConverter>(assembly);
            var cogs = AkkoUtilities.GetCogAssemblies().ToArray();
            var cogSetups = AkkoUtilities.GetCogSetups().ToArray();

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

                // Register cog argument converters
                foreach (var cogSetup in cogSetups)
                    cogSetup.RegisterArgumentConverters(cmdHandler);

                // Register cog commands
                foreach (var cog in cogs)
                    cmdHandler.RegisterCommands(cog);
            }

            foreach (var slashHandler in SlashExt.Values)
            {
                // Register all default slash commands, globally
                slashHandler.RegisterCommands(assembly);

                // Register all slash commands from cogs
                foreach (var cog in cogs)
                    slashHandler.RegisterCogCommands(cog);
            }
        }

        public void Dispose()
        {
            // Dispose singletons
            var discordEvents = CommandExt[0].Services.GetService<IDiscordEventManager>();
            discordEvents?.UnregisterStartupEvents();
            discordEvents?.UnregisterDefaultEvents();

            CommandExt[0].Services.GetService<ILocalizer>()?.Dispose();
            CommandExt[0].Services.GetService<ILoggerFactory>()?.Dispose();
            CommandExt[0].Services.GetService<IAkkoLoggerProvider>()?.Dispose();
            CommandExt[0].Services.GetService<IAkkoCache>()?.Dispose();
            CommandExt[0].Services.GetService<IDbCache>()?.Dispose();
            CommandExt[0].Services.GetService<ITimerManager>()?.Dispose();
            CommandExt[0].Services.GetService<ICommandCooldown>()?.Dispose();
            CommandExt[0].Services.GetService<IGatekeepEventHandler>()?.Dispose();
            CommandExt[0].Services.GetService<IInteractionEventHandler>()?.Dispose();

            // Dispose scoped
            foreach (var cmdHandler in CommandExt.Values)
                cmdHandler.Services.GetService<AkkoDbContext>()?.Dispose();

            // Dispose clients - this also disposes the extensions
            foreach (var client in BotShardedClient.ShardClients.Values)
                client.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}