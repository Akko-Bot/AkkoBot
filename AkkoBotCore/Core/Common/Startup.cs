using System;
using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Core.Common
{
    public class Startup
    {
        // cmd error Logging
        // database stuff - prefix, guilds, cache, etc
        private readonly AkkoUnitOfWork _db;

        public Startup(AkkoUnitOfWork db)
        {
            _db = db;
        }

        public void RegisterEvents(BotCore botCore)
        {
            if (_db is null)
                throw new NullReferenceException("No database Unit of Work was found.");

            // Load bot configs on ready
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

        private async Task LoadBotConfig(DiscordClient client, ReadyEventArgs eventArgs)
        {
            await _db.BotConfig.TryCreateAsync(new BotConfigEntity());
        }

        // Saves to the db
        private async Task SaveNewGuildsAsync(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs)
        {
            // Filter out the guilds that are already in the database
            var newGuilds = client.Guilds.Keys
                .Except((await _db.GuildConfigs.GetAllAsync())
                    .Select(dbGuild => dbGuild.GuildId))
                .Select(key => new GuildConfigEntity() { GuildId = key })
                .ToArray();

            // TODO: Should I bother adding these guilds to the db cache right away?
            // Might be bad for big bots, memory-wise

            await _db.GuildConfigs.CreateRangeAsync(newGuilds);
            await _db.SaveChangesAsync();
        }

        // Saves to the db and caches it
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
            cmdHandler.Client.Logger.LogError(
                new EventId(LoggerEvents.Misc.Id, "Command"),
                $"[Shard {cmdHandler.Client.ShardId}]\n" + eventArgs.Exception.ToString()
            );

            return Task.CompletedTask;
        }
    }
}