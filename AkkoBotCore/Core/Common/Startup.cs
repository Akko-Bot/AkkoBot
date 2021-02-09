using System;
using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using AkkoBot.Services.Timers.Abstractions;
using DSharpPlus.CommandsNext.Exceptions;
using AkkoBot.Services;

namespace AkkoBot.Core.Common
{
    /// <summary>
    /// Registers events the bot should listen to once it connects to Discord.
    /// </summary>
    public class Startup
    {
        private readonly IUnitOfWork _db;
        private BotCore _botCore;

        public Startup(IUnitOfWork db)
            => _db = db;

        /// <summary>
        /// Defines the core behavior the bot should have for specific Discord events.
        /// </summary>
        /// <param name="botCore">The bot to register the events to.</param>
        /// <exception cref="NullReferenceException"/>
        public void RegisterEvents(BotCore botCore)
        {
            if (_db is null)
                throw new NullReferenceException("No database Unit of Work was found.");

            _botCore = botCore;

            // Create bot configs on ready, if there isn't one already
            botCore.BotShardedClient.Ready += LoadBotConfig;

            // Initialize the timers stored in the database
            botCore.BotShardedClient.GuildDownloadCompleted += InitializeTimers;

            // Save visible guilds on ready
            botCore.BotShardedClient.GuildDownloadCompleted += SaveNewGuildsAsync;

            // Save guild on join
            botCore.BotShardedClient.GuildCreated += SaveGuildOnJoin;

            // Decache guild on leave
            botCore.BotShardedClient.GuildDeleted += DecacheGuildOnLeave;

            // Command logging
            foreach (var cmdHandler in botCore.CommandExt.Values)
            {
                cmdHandler.CommandExecuted += LogCmdExecution;
                cmdHandler.CommandErrored += LogCmdError;
            }
        }


        /* Event Methods */

        // Creates bot settings on startup, if there isn't one already
        private Task LoadBotConfig(DiscordClient client, ReadyEventArgs eventArgs)
        {
            // If there is no BotConfig entry in the database, create one.
            _db.LogConfig.TryCreate();
            _db.BotConfig.TryCreate();
            _db.SaveChanges();

            return Task.CompletedTask;
        }

        // Initialize the timers
        private Task InitializeTimers(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            var cmdHandler = _botCore.CommandExt[client.ShardId];
            cmdHandler.Services.GetService<IDbCacher>().Timers = cmdHandler.Services.GetService<ITimerManager>();

            return Task.CompletedTask;
        }

        // Saves guilds to the db on startup
        private async Task SaveNewGuildsAsync(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            var botConfig = _db.BotConfig.Cache;

            // Filter out the guilds that are already in the database
            var newGuilds = client.Guilds.Keys
                .Except((await _db.GuildConfig.GetAllAsync()).Select(dbGuild => dbGuild.GuildId))
                .Select(key => new GuildConfigEntity(botConfig) { GuildId = key })
                .ToArray();

            // Save the new guilds to the database
            _db.GuildConfig.CreateRange(newGuilds);
            await _db.SaveChangesAsync();

            // Cache the new guilds
            foreach (var guild in newGuilds)
                _db.GuildConfig.Cache.TryAdd(guild.GuildId, guild);
        }

        // Saves default guild settings to the db and caches it
        private Task SaveGuildOnJoin(DiscordClient client, GuildCreateEventArgs eventArgs)
        {
            _db.GuildConfig.TryCreate(eventArgs.Guild);
            _db.SaveChanges();

            return Task.CompletedTask;
        }

        // Remove a guild from the cache when the bot is removed from it.
        private Task DecacheGuildOnLeave(DiscordClient client, GuildDeleteEventArgs eventArgs)
        {
            _db.GuildConfig.Cache.TryRemove(eventArgs.Guild.Id, out _);
            return Task.CompletedTask;
        }

        // Log basic information about command execution.
        private Task LogCmdExecution(CommandsNextExtension cmdHandler, CommandExecutionEventArgs eventArgs)
        {
            cmdHandler.Client.Logger.BeginScope(eventArgs.Context);

            cmdHandler.Client.Logger.LogInformation(
                new EventId(LoggerEvents.Misc.Id, "Command"),
                eventArgs.Context.Message.Content
            );

            return Task.CompletedTask;
        }

        // Log exceptions thrown on command execution.
        private Task LogCmdError(CommandsNextExtension cmdHandler, CommandErrorEventArgs eventArgs)
        {
            if (eventArgs.Exception
            is not ArgumentException            // Ignore commands with invalid arguments and subcommands that do not exist
            and not ChecksFailedException       // Ignore command check fails
            and not CommandNotFoundException    // Ignore commands that do not exist
            and not InvalidOperationException)  // Ignore groups that are not commands themselves
            {
                cmdHandler.Client.Logger.BeginScope(eventArgs.Context);

                cmdHandler.Client.Logger.LogError(
                    new EventId(LoggerEvents.Misc.Id, "Command"),
                    eventArgs.Exception,
                    eventArgs.Context.Message.Content
                );
            }

            return Task.CompletedTask;
        }
    }
}