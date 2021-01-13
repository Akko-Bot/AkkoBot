using System;
using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Core.Common
{
    public class Startup
    {
        private readonly IUnitOfWork _db;

        public Startup(IUnitOfWork db)
            => _db = db;

        public void RegisterEvents(BotCore botCore)
        {
            if (_db is null)
                throw new NullReferenceException("No database Unit of Work was found.");

            // Create bot configs on ready, if there isn't one already
            botCore.BotClient.Ready += LoadBotConfig;

            // Save visible guilds on ready
            botCore.BotClient.GuildDownloadCompleted += SaveNewGuildsAsync;

            // Save guild on join
            botCore.BotClient.GuildCreated += SaveGuildOnJoin;

            // Decache guild on leave
            botCore.BotClient.GuildDeleted += DecacheGuildOnLeave;

            // Command error logging
            foreach (var handler in botCore.CommandExt.Values)
                handler.CommandErrored += LogCmdErrors;
        }


        /* Event Methods */

        // Creates bot settings on startup, if there isn't one already
        private async Task LoadBotConfig(DiscordClient client, ReadyEventArgs eventArgs)
        {
            // If there is no BotConfig entry in the database, create one.
            await _db.BotConfig.TryCreateAsync(new BotConfigEntity());
        }

        // Saves guilds to the db on startup
        private async Task SaveNewGuildsAsync(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            // Filter out the guilds that are already in the database
            var newGuilds = client.Guilds.Keys
                .Except((await _db.GuildConfigs.GetAllAsync())
                    .Select(dbGuild => dbGuild.GuildId))
                .Select(key => new GuildConfigEntity() { GuildId = key })
                .ToArray();

            // Save the new guilds to the database
            await _db.GuildConfigs.CreateRangeAsync(newGuilds);
            await _db.SaveChangesAsync();

            // Cache the new guilds
            foreach (var guild in newGuilds)
                _db.GuildConfigs.Cache.TryAdd(guild.GuildId, guild);
        }

        // Saves default guild settings to the db and caches it
        private async Task SaveGuildOnJoin(DiscordClient client, GuildCreateEventArgs eventArgs)
        {
            await _db.GuildConfigs.TryCreateAsync(eventArgs.Guild);
        }

        private Task DecacheGuildOnLeave(DiscordClient client, GuildDeleteEventArgs eventArgs)
        {
            _db.GuildConfigs.Cache.TryRemove(eventArgs.Guild.Id, out _);
            return Task.CompletedTask;
        }

        private Task LogCmdErrors(CommandsNextExtension cmdHandler, CommandErrorEventArgs eventArgs)
        {
            if (eventArgs.Exception is not ChecksFailedException and not CommandNotFoundException)
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