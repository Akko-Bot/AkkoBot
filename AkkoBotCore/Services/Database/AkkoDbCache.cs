using AkkoBot.Commands.Abstractions;
using AkkoBot.Config;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Queries;
using AkkoBot.Services.Timers.Abstractions;
using ConcurrentCollections;
using DSharpPlus.CommandsNext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database
{
    /// <summary>
    /// This class caches entries retrieved from the database.
    /// </summary>
    public class AkkoDbCache : IDbCache
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private bool _isDisposed = false;

        public ConcurrentDictionary<ulong, DiscordUserEntity> Users { get; private set; }
        public ConcurrentHashSet<ulong> Blacklist { get; private set; }
        public BotConfig BotConfig { get; private set; }
        public LogConfig LogConfig { get; private set; }
        public List<PlayingStatusEntity> PlayingStatuses { get; private set; }
        public ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<AliasEntity>> Aliases { get; private set; }
        public ConcurrentDictionary<ulong, FilteredWordsEntity> FilteredWords { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<FilteredContentEntity>> FilteredContent { get; private set; }
        public ICommandCooldown CooldownCommands { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<PollEntity>> Polls { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<RepeaterEntity>> Repeaters { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<VoiceRoleEntity>> VoiceRoles { get; private set; }

        /* Lazily instantiated */

        public ITimerManager Timers { get; set; }   // ITimerManager has TimerActions, which has IDbCache as a dependency.
        public ConcurrentDictionary<string, Command> DisabledCommandCache { get; set; }

        public AkkoDbCache(IServiceScopeFactory scopeFactory, ICommandCooldown cmdCooldown, BotConfig botConfig, LogConfig logConfig)
        {
            using var scope = scopeFactory.GetScopedService<AkkoDbContext>(out var dbContext);

            _scopeFactory = scopeFactory;
            BotConfig = botConfig;
            LogConfig = logConfig;
            Users = dbContext.DiscordUsers.ToConcurrentDictionary(x => x.UserId);
            Blacklist = dbContext.Blacklist.AsNoTracking().Select(x => x.ContextId).ToConcurrentHashSet();
            PlayingStatuses = dbContext.PlayingStatuses.Fetch(x => x.RotationTime != TimeSpan.Zero).ToList();
            CooldownCommands = cmdCooldown.LoadFromEntities(dbContext.CommandCooldown.Fetch());

            Aliases = dbContext.Aliases
                .AsNoTracking()
                .SplitBy(x => x.GuildId ?? default)
                .Select(x => x.ToConcurrentHashSet())
                .ToConcurrentDictionary(x => x.FirstOrDefault().GuildId ?? default);

            Polls = dbContext.Polls
                .AsNoTracking()
                .SplitBy(x => x.GuildIdFK)
                .Select(x => x.ToConcurrentHashSet())
                .ToConcurrentDictionary(x => x.FirstOrDefault().GuildIdFK);

            Repeaters = dbContext.Repeaters
                .Fetch(x => x.Interval <= TimeSpan.FromDays(1))
                .SplitBy(x => x.GuildIdFK)
                .Select(x => x.ToConcurrentHashSet())
                .ToConcurrentDictionary(x => x.FirstOrDefault().GuildIdFK);

            VoiceRoles = dbContext.VoiceRoles
                .SplitBy(x => x.GuildIdFk)
                .Select(x => x.ToConcurrentHashSet())
                .ToConcurrentDictionary(x => x.FirstOrDefault().GuildIdFk);

            Guilds = new();             // Guild configs are loaded into the cache as needed.
            FilteredWords = new();      // Filtered words are loaded into the cache as needed
            FilteredContent = new();    // Special filters are loaded into the cache as needed
        }

        public async ValueTask<GuildConfigEntity> GetDbGuildAsync(ulong sid)
        {
            if (Guilds.TryGetValue(sid, out var dbGuild))
                return dbGuild;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);
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
                    Users?.Clear();
                    Blacklist?.Clear();
                    Guilds?.Clear();
                    Timers?.Dispose();
                    CooldownCommands?.Dispose();
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

                    if (VoiceRoles is not null)
                    {
                        foreach (var group in VoiceRoles.Values)
                            group.Clear();

                        VoiceRoles.Clear();
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
                VoiceRoles = null;

                _isDisposed = true;
            }
        }
    }
}