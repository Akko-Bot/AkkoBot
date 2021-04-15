using AkkoBot.Commands.Modules.Self.Services;
using AkkoBot.Core.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Queries;
using AkkoBot.Services.Timers.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
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
        private readonly IDbCache _dbCache;
        private readonly IServiceScope _scope;
        private readonly BotCore _botCore;

        internal Startup(BotCore botCore, IServiceProvider services)
        {
            _botCore = botCore;
            _services = services;
            _dbCache = services.GetService<IDbCache>();
            _scope = services.CreateScope();
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

            // Executes startup commands
            _botCore.BotShardedClient.GuildDownloadCompleted += ExecuteStartupCommands;

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
                var db = _scope.ServiceProvider.GetService<AkkoDbContext>();

                #region Playing Status Initialization

                // Initialize the custom status, if there is one
                var pStatus = await db.PlayingStatuses.Fetch(x => x.RotationTime == TimeSpan.Zero)
                    .FirstOrDefaultAsync();

                if (pStatus is not null)
                    await client.UpdateStatusAsync(pStatus.GetActivity());
                else if (_dbCache.BotConfig.RotateStatus && _dbCache.PlayingStatuses.Count != 0)
                {
                    _dbCache.BotConfig.RotateStatus = !_dbCache.BotConfig.RotateStatus;
                    await _services.GetService<StatusService>().RotateStatusesAsync();
                }

                #endregion Playing Status Initialization

                await UnregisterCommands(client);
            });
        }

        /// <summary>
        /// Initializes the timers.
        /// </summary>
        private Task InitializeTimers(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            // May want to remove this method
            _dbCache.Timers ??= _services.GetService<ITimerManager>();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Saves new guilds to the database on startup and caches them.
        /// </summary>
        private Task SaveNewGuilds(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            return Task.Run(async () =>
            {
                var db = _scope.ServiceProvider.GetService<AkkoDbContext>();

                var botConfig = _dbCache.BotConfig;

                // Filter out the guilds that are already in the database
                var newGuilds = client.Guilds.Keys
                    .Except(db.GuildConfig.AsNoTracking().Select(dbGuild => dbGuild.GuildId))
                    .Select(key => new GuildConfigEntity(botConfig) { GuildId = key })
                    .ToArray();

                // Save the new guilds to the database
                db.GuildConfig.AddRange(newGuilds);
                await db.SaveChangesAsync();

                // Cache the new guilds
                foreach (var guild in newGuilds)
                    _dbCache.Guilds.TryAdd(guild.GuildId, guild);
            });
        }

        /// <summary>
        /// Caches the filtered words of all guilds available to this client.
        /// </summary>
        private Task CacheFilteredWords(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            return Task.Run(async () =>
            {
                var db = _scope.ServiceProvider.GetService<AkkoDbContext>();

                var filteredWords = (await db.FilteredWords.AsNoTracking()
                    .Where(x => x.Words.Count != 0)
                    .ToArrayAsync())    // Query the database
                    .Where(x => client.Guilds.ContainsKey(x.GuildIdFK));

                foreach (var entry in filteredWords)
                    _dbCache.FilteredWords.TryAdd(entry.GuildIdFK, entry);
            });
        }

        /// <summary>
        /// Executes startup commands.
        /// </summary>
        private Task ExecuteStartupCommands(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            return Task.Run(async () =>
            {
                var db = _scope.ServiceProvider.GetService<AkkoDbContext>();

                var cmdHandler = client.GetExtension<CommandsNextExtension>();
                var startupCmds = await db.AutoCommands.Fetch(x => x.Type == CommandType.Startup)
                    .ToArrayAsync();

                foreach (var dbCmd in startupCmds.Where(x => eventArgs.Guilds.ContainsKey(x.GuildId)))
                {
                    var cmd = cmdHandler.FindCommand(dbCmd.CommandString, out var args);

                    if (cmd is null || !eventArgs.Guilds.TryGetValue(dbCmd.GuildId, out var server) || !server.Channels.TryGetValue(dbCmd.ChannelId, out var channel))
                        continue;

                    var prefix = _services.GetService<IDbCache>().Guilds[server.Id].Prefix;

                    var fakeContext = cmdHandler.CreateFakeContext(await client.GetUserAsync(dbCmd.AuthorId), channel, cmd.QualifiedName + " " + args, prefix, cmd, args);

                    if (!(await cmd.RunChecksAsync(fakeContext, false)).Any())
                        _ = cmd.ExecuteAsync(fakeContext);  // Can't await because it takes too long to run
                }
            });
        }

        /// <summary>
        /// Saves default guild settings to the database and caches when the bot joins a guild.
        /// </summary>
        private Task SaveGuildOnJoin(DiscordClient client, GuildCreateEventArgs eventArgs)
        {
            if (_dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild))
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                var db = _scope.ServiceProvider.GetService<AkkoDbContext>();

                dbGuild = await _dbCache.GetGuildAsync(eventArgs.Guild.Id);
                var filteredWords = await db.FilteredWords.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.GuildIdFK == eventArgs.Guild.Id);

                _dbCache.Guilds.TryAdd(dbGuild.GuildId, dbGuild);

                if (filteredWords is not null)
                    _dbCache.FilteredWords.TryAdd(filteredWords.GuildIdFK, filteredWords);
            });
        }

        /// <summary>
        /// Remove a guild from the cache when the bot is removed from it.
        /// </summary>
        private Task DecacheGuildOnLeave(DiscordClient client, GuildDeleteEventArgs eventArgs)
        {
            _dbCache.Guilds.TryRemove(eventArgs.Guild.Id, out _);
            _dbCache.FilteredWords.TryRemove(eventArgs.Guild.Id, out _);

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
            is ArgumentException            // Ignore commands with invalid arguments and subcommands that do not exist
            or ChecksFailedException        // Ignore command check fails
            or CommandNotFoundException     // Ignore commands that do not exist
            or InvalidOperationException)   // Ignore groups that are not commands themselves
            {
                return Task.CompletedTask;
            }

            cmdHandler.Client.Logger.LogCommand(
                LogLevel.Error,
                eventArgs.Context,
                eventArgs.Exception
            );

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
            var disabledCommands = new ConcurrentDictionary<string, Command>(); // Initialize the cache
            var cmdHandler = client.GetExtension<CommandsNextExtension>();      // Initialize the command handler

            // Unregister the disabled commands from the command handlers
            foreach (var dbCmd in _dbCache.BotConfig.DisabledCommands)
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
            _dbCache.DisabledCommandCache ??= disabledCommands;

            return Task.CompletedTask;
        }
    }
}