using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Events.Abstractions;
using AkkoEntities.Config;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events
{
    /// <summary>
    /// Handles caching of guild settings.
    /// </summary>
    internal class GuildLoadHandler : IGuildLoadHandler
    {
        private readonly IDbCache _dbCache;
        private readonly BotConfig _botConfig;

        public GuildLoadHandler(IDbCache dbCache, BotConfig botConfig)
        {
            _dbCache = dbCache;
            _botConfig = botConfig;
        }

        public async Task AddGuildOnJoinAsync(DiscordClient client, GuildCreateEventArgs eventArgs)
        {
            if (_dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out _))
                return;

            var dbGuild = await _dbCache.GetDbGuildAsync(eventArgs.Guild.Id, _botConfig);
            _dbCache.Guilds.AddOrUpdate(dbGuild.GuildId, dbGuild, (_, _) => dbGuild);
        }

        public Task RemoveGuildOnLeaveAsync(DiscordClient client, GuildDeleteEventArgs eventArgs)
        {
            _dbCache.TryRemoveDbGuild(eventArgs.Guild.Id);
            return Task.CompletedTask;
        }
    }
}