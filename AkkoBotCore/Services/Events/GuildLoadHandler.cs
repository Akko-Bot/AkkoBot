using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Events.Abstractions;
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

        public GuildLoadHandler(IDbCache dbCache)
            => _dbCache = dbCache;

        public async Task AddGuildOnJoinAsync(DiscordClient client, GuildCreateEventArgs eventArgs)
        {
            if (_dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out _))
                return;

            var dbGuild = await _dbCache.GetDbGuildAsync(eventArgs.Guild.Id);
            _dbCache.Guilds.AddOrUpdate(dbGuild.GuildId, dbGuild, (_, _) => dbGuild);
        }

        public Task RemoveGuildOnLeaveAsync(DiscordClient client, GuildDeleteEventArgs eventArgs)
        {
            _dbCache.Guilds.TryRemove(eventArgs.Guild.Id, out _);
            _dbCache.Gatekeeping.TryRemove(eventArgs.Guild.Id, out _);
            _dbCache.FilteredWords.TryRemove(eventArgs.Guild.Id, out _);
            _dbCache.FilteredContent.TryRemove(eventArgs.Guild.Id, out var filters);
            _dbCache.VoiceRoles.TryRemove(eventArgs.Guild.Id, out var voiceRoles);
            _dbCache.Polls.TryRemove(eventArgs.Guild.Id, out var polls);

            filters?.Clear();
            voiceRoles?.Clear();
            polls?.Clear();

            return Task.CompletedTask;
        }
    }
}