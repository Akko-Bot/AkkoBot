using AkkoBot.Commands.Modules.Self.Services;
using AkkoBot.Core.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Timers.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Core.Services
{
    /// <summary>
    /// Registers events the bot should listen to once it connects to Discord.
    /// </summary>
    internal class Startup
    {
        private readonly IServiceProvider _services;
        private readonly BotCore _botCore;

        internal Startup(BotCore botCore, IServiceProvider services)
        {
            _botCore = botCore;
            _services = services;
        }

        /// <summary>
        /// Defines the core behavior the bot should have for specific Discord events.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        internal void RegisterEvents()
        {
            if (_services is null)
                throw new ArgumentNullException(nameof(_services), "No IoC container was found.");
            else if (_botCore is null)
                throw new ArgumentNullException(nameof(_botCore), "Bot core cannot be null.");

            // Create bot configs on ready, if there isn't one already
            _botCore.BotShardedClient.Ready += LoadBotConfig;

            // Initialize the timers stored in the database
            _botCore.BotShardedClient.GuildDownloadCompleted += InitializeTimers;

            // Save visible guilds on ready
            _botCore.BotShardedClient.GuildDownloadCompleted += SaveNewGuilds;

            // Caches guild filtered words
            _botCore.BotShardedClient.GuildDownloadCompleted += CacheFilteredWords;

            // Save guild on join
            _botCore.BotShardedClient.GuildCreated += SaveGuildOnJoin;

            // Decache guild on leave
            _botCore.BotShardedClient.GuildDeleted += DecacheGuildOnLeave;

            // Command logging
            foreach (var cmdHandler in _botCore.CommandExt.Values)
            {
                cmdHandler.CommandExecuted += LogCmdExecution;
                cmdHandler.CommandErrored += LogCmdError;
            }
        }

        /* Event Methods */

        /// <summary>
        /// Creates bot settings on startup, if there isn't one already.
        /// </summary>
        private Task LoadBotConfig(DiscordClient client, ReadyEventArgs eventArgs)
        {
            return Task.Run(async () =>
            {
                using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

                // If there is no BotConfig entry in the database, create one.
                db.LogConfig.TryCreate();
                db.BotConfig.TryCreate();
                await db.SaveChangesAsync();

                #region Playing Status Initialization
                // Initialize the custom status, if there is one
                var pStatus = db.PlayingStatuses.Table.FirstOrDefault(x => x.RotationTime == TimeSpan.Zero);

                if (pStatus is not null)
                    await client.UpdateStatusAsync(pStatus.GetActivity());
                else if (db.BotConfig.Cache.RotateStatus && db.PlayingStatuses.Cache.Count != 0)
                {
                    db.BotConfig.Cache.RotateStatus = !db.BotConfig.Cache.RotateStatus;
                    await _services.GetService<StatusService>().RotateStatusesAsync();
                }
                #endregion

                await UnregisterCommands(client);
            });
        }

        /// <summary>
        /// Initializes the timers.
        /// </summary>
        private Task InitializeTimers(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            // May want to remove this method
            var cmdHandler = client.GetExtension<CommandsNextExtension>();
            cmdHandler.Services.GetService<IDbCacher>().Timers ??= cmdHandler.Services.GetService<ITimerManager>();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Saves new guilds to the database on startup and caches them.
        /// </summary>
        private Task SaveNewGuilds(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            return Task.Run(async () =>
            {
                using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

                var botConfig = db.BotConfig.Cache;

                // Filter out the guilds that are already in the database
                var newGuilds = client.Guilds.Keys
                    .Except((await db.GuildConfig.GetAllAsync()).Select(dbGuild => dbGuild.GuildId))
                    .Select(key => new GuildConfigEntity(botConfig) { GuildId = key })
                    .ToArray();

                // Save the new guilds to the database
                db.GuildConfig.CreateRange(newGuilds);
                await db.SaveChangesAsync();

                // Cache the new guilds
                foreach (var guild in newGuilds)
                    db.GuildConfig.Cache.TryAdd(guild.GuildId, guild);
            });
        }

        /// <summary>
        /// Caches the filtered words of all guilds available to this client.
        /// </summary>
        private Task CacheFilteredWords(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            return Task.Run(() =>
            {
                using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

                foreach (var entry in db.GuildConfig.Table.Include(x => x.FilteredWordsRel).Where(x => x.FilteredWordsRel != null && x.FilteredWordsRel.Words.Count != 0))
                    db.GuildConfig.FilteredWordsCache.TryAdd(entry.GuildId, entry.FilteredWordsRel);
            });
        }

        /// <summary>
        /// Saves default guild settings to the database and caches when the bot joins a guild.
        /// </summary>
        private async Task SaveGuildOnJoin(DiscordClient client, GuildCreateEventArgs eventArgs)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            db.GuildConfig.TryCreate(eventArgs.Guild);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Remove a guild from the cache when the bot is removed from it.
        /// </summary>
        private Task DecacheGuildOnLeave(DiscordClient client, GuildDeleteEventArgs eventArgs)
        {
            var dbCache = _services.GetService<IDbCacher>();
            dbCache.Guilds.TryRemove(eventArgs.Guild.Id, out _);
            dbCache.FilteredWords.TryRemove(eventArgs.Guild.Id, out _);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs basic information about command execution.
        /// </summary>
        private Task LogCmdExecution(CommandsNextExtension cmdHandler, CommandExecutionEventArgs eventArgs)
        {
            cmdHandler.Client.Logger.LogCommand(LogLevel.Information, eventArgs.Context);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs exceptions thrown during command execution.
        /// </summary>
        private Task LogCmdError(CommandsNextExtension cmdHandler, CommandErrorEventArgs eventArgs)
        {
            if (eventArgs.Exception
            is not ArgumentException            // Ignore commands with invalid arguments and subcommands that do not exist
            and not ChecksFailedException       // Ignore command check fails
            and not CommandNotFoundException    // Ignore commands that do not exist
            and not InvalidOperationException)  // Ignore groups that are not commands themselves
            {
                cmdHandler.Client.Logger.LogCommand(
                    LogLevel.Error,
                    eventArgs.Context,
                    eventArgs.Exception
                );
            }

            return Task.CompletedTask;
        }

        /* Utility Methods */

        /// <summary>
        /// Unregisters disabled commands from the command handler of the current client,
        /// then sets up the cache for disabled commands if it hasn't been already.
        /// </summary>
        /// <param name="client">The current Discord client.</param>
        private Task UnregisterCommands(DiscordClient client)
        {
            var dbCache = _services.GetService<IDbCacher>();

            var disabledCommands = new ConcurrentDictionary<string, Command>(); // Initialize the cache
            var cmdHandler = client.GetExtension<CommandsNextExtension>();      // Initialize the command handler

            // Unregister the disabled commands from the command handlers
            foreach (var dbCmd in dbCache.BotConfig.DisabledCommands)
            {
                var cmd = cmdHandler.FindCommand(dbCmd, out _);

                if (cmd is not null)
                {
                    // Add command to the cache of disabled commands
                    disabledCommands.TryAdd(cmd.QualifiedName, cmd);

                    // Unregister the command from the command handler
                    cmdHandler.UnregisterCommands(cmd);
                }
            }

            // Set the cache of disabled commands
            dbCache.DisabledCommandCache ??= disabledCommands;

            return Task.CompletedTask;
        }
    }
}