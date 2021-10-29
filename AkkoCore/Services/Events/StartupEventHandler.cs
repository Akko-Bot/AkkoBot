using AkkoCore.Commands.Modules.Self.Services;
using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using AkkoCore.Services.Database.Queries;
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

namespace AkkoCore.Services.Events
{
    /// <summary>
    /// Handles the behavior the bot should have for specific Discord events on startup.
    /// </summary>
    internal sealed class StartupEventHandler : IStartupEventHandler
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAkkoCache _akkoCache;
        private readonly IDbCache _dbCache;
        private readonly BotConfig _botConfig;
        private readonly StatusService _statusService;
        
        public StartupEventHandler(IServiceScopeFactory scopeFactory, IAkkoCache akkoCache, IDbCache dbCache, BotConfig botConfig, StatusService statusService)
        {
            _scopeFactory = scopeFactory;
            _akkoCache = akkoCache;
            _dbCache = dbCache;
            _botConfig = botConfig;
            _statusService = statusService;
        }

        // This method exists so to make it easier for modders to apply changes to the bot
        // that require custom state to be loaded on startup
        public Task LoadInitialStateAsync(DiscordClient client, ReadyEventArgs eventArgs)
            => Task.CompletedTask;

        public Task InitializeTimersAsync(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            // This method can be too slow on low-end CPUs
            Task.Run(async () => await _akkoCache.Timers.CreateClientTimersAsync(client));
            return Task.CompletedTask;
        }

        public Task SaveNewGuildsAsync(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            Task.Run(async () =>
            {
                using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

                // Filter out the guilds that are already in the database
                var newGuilds = client.Guilds.Keys
                    .Except(db.GuildConfig.Select(dbGuild => dbGuild.GuildId))
                    .Select(key => new GuildConfigEntity(_botConfig) { GuildId = key })
                    .ToArray();

                if (newGuilds.Length is not 0)
                {
                    // Save the new guilds to the database
                    await db.BulkCopyAsync(100, newGuilds);
                }
            });

            return Task.CompletedTask;
        }

        // Filters need to be cached even if the server has no activity, because they could be disabled but
        // have custom rules that users may want to read
        public Task CacheActiveGuildsAsync(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            Task.Run(async () =>
            {
                using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

                var dbGuilds = await db.GuildConfig
                    .IncludeCacheable()
                    .Where(x => (int)(x.GuildId / (ulong)Math.Pow(2, 22) % (ulong)client.ShardCount) == client.ShardId) // Replacement for "(sid >> 22) % max", since bitwise operators do not support ulongs
                    .ToArrayAsyncEF();

                foreach (var dbGuild in dbGuilds)
                {
                    // Do not cache guilds that have no passive activity
                    if (dbGuild.HasPassiveActivity && client.Guilds.ContainsKey(dbGuild.GuildId))
                        _dbCache.TryAddDbGuild(dbGuild);
                }
            });

            return Task.CompletedTask;
        }

        public Task ExecuteStartupCommandsAsync(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            Task.Run(async () =>
            {
                using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

                var cmdHandler = client.GetCommandsNext();
                var startupCmds = await db.AutoCommands
                    .Where(x => x.Type == AutoCommandType.Startup && (int)(x.GuildId / (ulong)Math.Pow(2, 22) % (ulong)client.ShardCount) == client.ShardId)
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

            return Task.CompletedTask;
        }

        public Task InitializePlayingStatuses(DiscordClient client, ReadyEventArgs eventArgs)
        {
            Task.Run(async () =>
            {
                if (_statusService.StaticStatus is not null)
                    await client.UpdateStatusAsync(_statusService.StaticStatus.Activity);
                else if (_botConfig.RotateStatus && _dbCache.PlayingStatuses.Count is not 0)
                {
                    _botConfig.RotateStatus = !_botConfig.RotateStatus;
                    await _statusService.RotateStatusesAsync();
                }
            });

            return Task.CompletedTask;
        }

        public Task UnregisterCommandsAsync(DiscordClient client, ReadyEventArgs eventArgs)
        {
            var disabledCommands = new ConcurrentDictionary<string, Command>(); // Initialize the cache
            var cmdHandler = client.GetCommandsNext();                          // Initialize the command handler

            // Unregister the disabled commands from the command handlers
            foreach (var dbCmd in _botConfig.DisabledCommands)
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
            if (_akkoCache is AkkoCache cache)
                cache.DisabledCommandCache ??= disabledCommands;

            return Task.CompletedTask;
        }
    }
}