using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Queries;
using AkkoBot.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events
{
    /// <summary>
    /// Handles caching of guild settings.
    /// </summary>
    internal class GuildLoadHandler : IGuildLoadHandler
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;

        public GuildLoadHandler(IServiceScopeFactory scopeFactory, IDbCache dbCache)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCache;
        }

        public Task AddGuildOnJoinAsync(DiscordClient client, GuildCreateEventArgs eventArgs)
        {
            if (_dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild))
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

                dbGuild = await _dbCache.GetDbGuildAsync(eventArgs.Guild.Id);

                var gatekeep = await db.Gatekeeping.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.GuildIdFK == eventArgs.Guild.Id);

                var filteredWords = await db.FilteredWords.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.GuildIdFK == eventArgs.Guild.Id);

                var filteredContent = await db.FilteredContent
                    .Fetch(x => x.GuildIdFK == eventArgs.Guild.Id)
                    .ToArrayAsync();

                _dbCache.Guilds.TryAdd(dbGuild.GuildId, dbGuild);
                _dbCache.Gatekeeping.TryAdd(dbGuild.GuildId, gatekeep);

                if (filteredWords is not null)
                    _dbCache.FilteredWords.TryAdd(filteredWords.GuildIdFK, filteredWords);

                if (filteredContent.Length is not 0)
                    _dbCache.FilteredContent.TryAdd(dbGuild.GuildId, new(filteredContent));
            });
        }

        public Task RemoveGuildOnLeaveAsync(DiscordClient client, GuildDeleteEventArgs eventArgs)
        {
            _dbCache.Guilds.TryRemove(eventArgs.Guild.Id, out _);
            _dbCache.Gatekeeping.TryRemove(eventArgs.Guild.Id, out _);
            _dbCache.FilteredWords.TryRemove(eventArgs.Guild.Id, out _);
            _dbCache.FilteredContent.TryRemove(eventArgs.Guild.Id, out var filters);

            filters.Clear();

            return Task.CompletedTask;
        }
    }
}