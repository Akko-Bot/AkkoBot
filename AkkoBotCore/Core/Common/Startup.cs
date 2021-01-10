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

            // Add visible guilds on ready
            botCore.BotClient.Ready += CacheAvailableGuilds;

            // Command error logging
            foreach (var handler in botCore.CommandExt.Values)
                handler.CommandErrored += LogCmdErrors;
        }


        /* Event Methods */

        private async Task CacheAvailableGuilds(DiscordClient client, ReadyEventArgs eventArgs)
        {
            // Filter out the guilds that are already in the database
            var newGuilds = client.Guilds.Keys
                .Except((await _db.GuildConfigs.GetAllAsync())
                    .Select(dbGuild => dbGuild.GuildId))
                .Select(key => new GuildConfigEntity() { GuildId = key })
                .ToArray();

            await _db.GuildConfigs.CreateRangeAsync(newGuilds);
            await _db.SaveChangesAsync();
        }

        // I may move this elsewhere
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