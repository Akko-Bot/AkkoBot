using AkkoBot.Commands.Modules.Self.Services;
using AkkoBot.Core.Common;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Queries;
using AkkoBot.Services.Timers.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Core.Services
{
    /// <summary>
    /// Manages the behavior the bot should have for specific Discord events on startup.
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
        /// Registers events the bot should listen to once it connects to Discord.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        internal void RegisterEvents()
        {
            if (_botCore is null)
                throw new InvalidOperationException("Bot core cannot be null.");

            // Create bot configs on ready, if there isn't one already
            _botCore.BotShardedClient.Ready += LoadInitialStateAsync;

            // Save visible guilds on ready
            _botCore.BotShardedClient.GuildDownloadCompleted += SaveNewGuilds;

            // Initialize the timers stored in the database
            _botCore.BotShardedClient.GuildDownloadCompleted += InitializeTimers;

            // Caches guild filtered words
            _botCore.BotShardedClient.GuildDownloadCompleted += CacheFilteredElements;

            // Executes startup commands
            _botCore.BotShardedClient.GuildDownloadCompleted += ExecuteStartupCommands;
        }

        /* Event Methods */

        /// <summary>
        /// Creates bot settings on startup, if there isn't one already.
        /// </summary>
        private async Task LoadInitialStateAsync(DiscordClient client, ReadyEventArgs eventArgs)
        {
            await InitializePlayingStatuses(client).ConfigureAwait(false);
            await UnregisterCommands(client).ConfigureAwait(false);
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

                if (newGuilds.Length is not 0)
                {
                    // Save the new guilds to the database
                    await db.BulkCopyAsync(100, newGuilds);

                    // Cache the new guilds
                    foreach (var guild in newGuilds)
                        _dbCache.Guilds.TryAdd(guild.GuildId, guild);
                }
            });
        }

        /// <summary>
        /// Initializes the timers.
        /// </summary>
        private Task<bool> InitializeTimers(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            var cmdHandler = client.GetCommandsNext();
            _dbCache.Timers ??= cmdHandler.Services.GetService<ITimerManager>();

            return _dbCache.Timers.CreateClientTimersAsync(client).AsTask();
        }

        /// <summary>
        /// Caches the filtered words of all guilds available to this client.
        /// </summary>
        private Task CacheFilteredElements(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            return Task.Run(async () =>
            {
                var db = _scope.ServiceProvider.GetService<AkkoDbContext>();

                var filteredWords = await db.FilteredWords.Fetch(x => x.Words.Count != 0)
                    .Where(x => client.Guilds.Keys.Contains(x.GuildIdFK))
                    .ToArrayAsyncEF();

                var filteredContent = await db.FilteredContent.Fetch(x => x.IsAttachmentOnly || x.IsCommandOnly || x.IsImageOnly || x.IsInviteOnly || x.IsUrlOnly)
                    .Where(x => client.Guilds.Keys.Contains(x.GuildIdFK))
                    .ToArrayAsyncEF();

                foreach (var entry in filteredWords)
                    _dbCache.FilteredWords.TryAdd(entry.GuildIdFK, entry);

                foreach (var entry in filteredContent)
                    _dbCache.FilteredContent.TryAdd(entry.GuildIdFK, new(filteredContent.Where(x => x.GuildIdFK == entry.GuildIdFK)));
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

                var cmdHandler = client.GetCommandsNext();
                var startupCmds = await db.AutoCommands.Fetch(x => x.Type == AutoCommandType.Startup)
                    .Where(x => eventArgs.Guilds.Keys.Contains(x.GuildId))
                    .Select(x => new AutoCommandEntity() { AuthorId = x.AuthorId, GuildId = x.GuildId, ChannelId = x.ChannelId, CommandString = x.CommandString })
                    .ToArrayAsyncEF();

                foreach (var dbCmd in startupCmds)
                {
                    var cmd = cmdHandler.FindCommand(dbCmd.CommandString, out var args);

                    if (cmd is null || !eventArgs.Guilds.TryGetValue(dbCmd.GuildId, out var server) || !server.Channels.TryGetValue(dbCmd.ChannelId, out var channel))
                        continue;

                    var prefix = (await _dbCache.GetDbGuildAsync(server.Id)).Prefix;

                    var fakeContext = cmdHandler.CreateFakeContext(await client.GetUserAsync(dbCmd.AuthorId), channel, cmd.QualifiedName + " " + args, prefix, cmd, args);

                    if (!(await cmd.RunChecksAsync(fakeContext, false)).Any())
                        _ = cmd.ExecuteAsync(fakeContext);  // Can't await because it takes too long to run
                }
            });
        }

        /* Utility Methods */

        /// <summary>
        /// Initializes the bot's playing statuses.
        /// </summary>
        /// <param name="client">The Discord client.</param>
        private async Task InitializePlayingStatuses(DiscordClient client)
        {
            var db = _scope.ServiceProvider.GetService<AkkoDbContext>();
            var pStatus = await db.PlayingStatuses
                .Fetch(x => x.RotationTime == TimeSpan.Zero)
                .Select(x => new PlayingStatusEntity() { Message = x.Message, Type = x.Type, StreamUrl = x.StreamUrl })
                .FirstOrDefaultAsyncEF();

            if (pStatus is not null)
                await client.UpdateStatusAsync(pStatus.Activity);
            else if (_dbCache.BotConfig.RotateStatus && _dbCache.PlayingStatuses.Count != 0)
            {
                _dbCache.BotConfig.RotateStatus = !_dbCache.BotConfig.RotateStatus;
                await _services.GetService<StatusService>().RotateStatusesAsync();
            }
        }

        /// <summary>
        /// Unregisters disabled commands from the command handler of the current client,
        /// then sets up the cache for disabled commands if it hasn't been already.
        /// </summary>
        /// <param name="client">The current Discord client.</param>
        private Task UnregisterCommands(DiscordClient client)
        {
            var disabledCommands = new ConcurrentDictionary<string, Command>(); // Initialize the cache
            var cmdHandler = client.GetCommandsNext();                          // Initialize the command handler

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