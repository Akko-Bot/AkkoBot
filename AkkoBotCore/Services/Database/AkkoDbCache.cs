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
        public ConcurrentDictionary<ulong, GatekeepEntity> Gatekeeping { get; private set; }

        /* Lazily instantiated */

        public ITimerManager Timers { get; set; }   // ITimerManager has TimerActions, which has IDbCache as a dependency.
        public ConcurrentDictionary<string, Command> DisabledCommandCache { get; set; }

        public AkkoDbCache(IServiceScopeFactory scopeFactory, ICommandCooldown cmdCooldown, BotConfig botConfig, LogConfig logConfig)
        {
            using var scope = scopeFactory.GetScopedService<AkkoDbContext>(out var dbContext);

            _scopeFactory = scopeFactory;

            // The properties below are global
            BotConfig = botConfig;
            LogConfig = logConfig;
            Users = dbContext.DiscordUsers.ToConcurrentDictionary(x => x.UserId);
            Blacklist = dbContext.Blacklist.AsNoTracking().Select(x => x.ContextId).ToConcurrentHashSet();
            PlayingStatuses = dbContext.PlayingStatuses.Fetch(x => x.RotationTime != TimeSpan.Zero).ToList();

            // The properties below can either be global or specific to a guild
            CooldownCommands = cmdCooldown.LoadFromEntities(dbContext.CommandCooldown.Fetch());
            Aliases = dbContext.Aliases
                .AsNoTracking()
                .SplitBy(x => x.GuildId ?? default)
                .Select(x => x.ToConcurrentHashSet())
                .ToConcurrentDictionary(x => x.FirstOrDefault().GuildId ?? default);

            //The properties below are specific to a guild and are cached on demand
            Guilds = new();
            Repeaters = new();
            VoiceRoles = new();
            FilteredWords = new();
            FilteredContent = new();
            Gatekeeping = new();
            Polls = new();
        }

        public async ValueTask<GuildConfigEntity> GetDbGuildAsync(ulong sid)
        {
            if (Guilds.TryGetValue(sid, out var dbGuild))
                return dbGuild;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);
            dbGuild = await db.GuildConfig
                .AsNoTracking()
                .IncludeCacheable()
                .FirstOrDefaultAsync(x => x.GuildId == sid);

            if (dbGuild is null)
            {
                dbGuild = new GuildConfigEntity(BotConfig) { GuildId = sid };
                db.Add(dbGuild);

                await db.SaveChangesAsync();
            }

            TryAddDbGuild(dbGuild);
            Guilds.AddOrUpdate(dbGuild.GuildId, dbGuild, (_, _) => dbGuild);

            return dbGuild;
        }

        public bool TryAddDbGuild(GuildConfigEntity dbGuild)
        {
            if (Guilds.ContainsKey(dbGuild.GuildId))
                return false;

            Guilds.TryAdd(dbGuild.GuildId, dbGuild);

            if (dbGuild.GatekeepRel is not null)
                Gatekeeping.TryAdd(dbGuild.GuildId, dbGuild.GatekeepRel);

            if (dbGuild.FilteredWordsRel is not null)
                FilteredWords.TryAdd(dbGuild.GuildId, dbGuild.FilteredWordsRel);

            if (dbGuild.FilteredContentRel.Count is not 0)
                FilteredContent.TryAdd(dbGuild.GuildId, dbGuild.FilteredContentRel.ToConcurrentHashSet());

            if (dbGuild.VoiceRolesRel.Count is not 0)
                VoiceRoles.TryAdd(dbGuild.GuildId, dbGuild.VoiceRolesRel.ToConcurrentHashSet());

            if (dbGuild.RepeaterRel.Count is not 0)
                Repeaters.TryAdd(dbGuild.GuildId, dbGuild.RepeaterRel.ToConcurrentHashSet());

            if (dbGuild.PollRel.Count is not 0)
                Polls.TryAdd(dbGuild.GuildId, dbGuild.PollRel.ToConcurrentHashSet());

            return true;
        }

        public bool TryRemoveDbGuild(ulong sid)
        {
            Guilds.TryRemove(sid, out var dbGuild);
            Gatekeeping.TryRemove(sid, out _);
           
            FilteredWords.TryRemove(sid, out _);
           
            FilteredContent.TryRemove(sid, out _);
            VoiceRoles.TryRemove(sid, out var filters);
            Repeaters.TryRemove(sid, out var voiceRoles);
            Polls.TryRemove(sid, out var polls);

            filters?.Clear();
            voiceRoles?.Clear();
            polls?.Clear();

            return dbGuild is not null;
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
                    Gatekeeping?.Clear();

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
                Gatekeeping = null;

                _isDisposed = true;
            }
        }
    }
}