using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Timers.Abstractions;
using ConcurrentCollections;
using DSharpPlus.CommandsNext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AkkoBot.Commands.Abstractions;

namespace AkkoBot.Services.Database
{
    /// <summary>
    /// This class acts as a singleton cache for UoW objects.
    /// </summary>
    public class AkkoDbCache : IDbCache
    {
        private readonly IServiceProvider _services;
        private bool _isDisposed = false;

        public ConcurrentHashSet<ulong> Blacklist { get; private set; }
        public BotConfigEntity BotConfig { get; private set; }
        public LogConfigEntity LogConfig { get; private set; }
        public List<PlayingStatusEntity> PlayingStatuses { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<AliasEntity>> Aliases { get; private set; }
        public ConcurrentDictionary<ulong, FilteredWordsEntity> FilteredWords { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<FilteredContentEntity>> FilteredContent { get; private set; }
        public ICommandCooldown CooldownCommands { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<PollEntity>> Polls { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<RepeaterEntity>> Repeaters { get; private set; }

        // Lazily instantiated
        public ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; private set; }

        public ITimerManager Timers { get; set; }
        public ConcurrentDictionary<string, Command> DisabledCommandCache { get; set; }

        public AkkoDbCache(IServiceProvider services)
        {
            using var scope = services.GetScopedService<AkkoDbContext>(out var dbContext);

            _services = services;
            BotConfig = dbContext.BotConfig.FirstOrDefault();
            LogConfig = dbContext.LogConfig.FirstOrDefault();
            Blacklist = dbContext.Blacklist.Select(x => x.ContextId).ToConcurrentHashSet();
            PlayingStatuses = dbContext.PlayingStatuses.Where(x => x.RotationTime != TimeSpan.Zero).ToList();
            CooldownCommands = services.GetService<ICommandCooldown>().LoadFromEntities(dbContext.CommandCooldown.AsEnumerable());

            Aliases = dbContext.Aliases
                .SplitBy(x => x.GuildId ?? default)
                .Select(x => x.ToConcurrentHashSet())
                .ToConcurrentDictionary(x => x.FirstOrDefault().GuildId ?? default);

            Polls = dbContext.Polls
                .SplitBy(x => x.GuildIdFK)
                .Select(x => x.ToConcurrentHashSet())
                .ToConcurrentDictionary(x => x.FirstOrDefault().GuildIdFK);

            Repeaters = dbContext.Repeaters
                .Where(x => x.Interval <= TimeSpan.FromDays(1))
                .SplitBy(x => x.GuildIdFK)
                .Select(x => x.ToConcurrentHashSet())
                .ToConcurrentDictionary(x => x.FirstOrDefault().GuildIdFK);

            Guilds = new(); // Guild configs are loaded into the cache as needed.
            FilteredWords = new(); // Filtered words are loaded into the cache as needed
            FilteredContent = new(); // Special filters are loaded into the cache as needed
        }

        /// <summary>
        /// Safely gets a database guild.
        /// </summary>
        /// <param name="sid">The GuildId of the database entry.</param>
        /// <remarks>If the entry doesn't exist, it creates one.</remarks>
        /// <returns>The specified <see cref="GuildConfigEntity"/>.</returns>
        public async ValueTask<GuildConfigEntity> GetGuildAsync(ulong sid)
        {
            if (Guilds.TryGetValue(sid, out var dbGuild))
                return dbGuild;

            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);
            dbGuild = await db.GuildConfig.AsNoTracking()
                .FirstOrDefaultAsync(x => x.GuildId == sid);

            if (dbGuild is null)
            {
                dbGuild = new GuildConfigEntity(BotConfig) { GuildId = sid };
                db.Add(dbGuild);
                await db.SaveChangesAsync();
            }

            Guilds.TryAdd(dbGuild.GuildId, dbGuild);
            return dbGuild;
        }

        /// <summary>
        /// Releases the allocated resources for this database cacher.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!_isDisposed)
            {
                if (isDisposing)
                {
                    Blacklist?.Clear();
                    Guilds?.Clear();
                    Timers?.Dispose();
                    PlayingStatuses?.Clear();
                    PlayingStatuses?.TrimExcess();
                    FilteredWords?.Clear();
                    FilteredContent?.Clear();
                    DisabledCommandCache?.Clear();

                    if (Aliases is not null)
                    {
                        foreach (var group in Aliases.Values)
                            group.Clear();

                        Aliases.Clear();
                    }

                    if (Polls is not null)
                    {
                        foreach (var group in Polls.Values)
                            group.Clear();

                        Polls.Clear();
                    }

                    if (Repeaters is not null)
                    {
                        foreach (var group in Repeaters.Values)
                            group.Clear();

                        Repeaters.Clear();
                    }
                }

                Blacklist = null;
                BotConfig = null;
                LogConfig = null;
                Guilds = null;
                Timers = null;
                PlayingStatuses = null;
                Aliases = null;
                FilteredWords = null;
                FilteredContent = null;
                DisabledCommandCache = null;
                CooldownCommands = null;
                Polls = null;
                Repeaters = null;

                _isDisposed = true;
            }
        }
    }
}