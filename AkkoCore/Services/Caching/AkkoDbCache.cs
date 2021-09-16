using AkkoCore.Commands.Abstractions;
using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Queries;
using ConcurrentCollections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Services.Database
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
        public List<PlayingStatusEntity> PlayingStatuses { get; private set; }
        public ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<AliasEntity>> Aliases { get; private set; }
        public ConcurrentDictionary<ulong, FilteredWordsEntity> FilteredWords { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<FilteredContentEntity>> FilteredContent { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<PollEntity>> Polls { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<RepeaterEntity>> Repeaters { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<VoiceRoleEntity>> VoiceRoles { get; private set; }
        public ConcurrentDictionary<ulong, GatekeepEntity> Gatekeeping { get; private set; }
        public ConcurrentDictionary<ulong, AutoSlowmodeEntity> AutoSlowmode { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<GuildLogEntity>> GuildLogs { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<TagEntity>> Tags { get; private set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<PermissionOverrideEntity>> PermissionOverrides { get; private set; }
        public ICommandCooldown CommandCooldown { get; private set; }

        public AkkoDbCache(IServiceScopeFactory scopeFactory, ICommandCooldown commandCooldown)
        {
            _scopeFactory = scopeFactory;
            CommandCooldown = commandCooldown;

            using var scope = scopeFactory.GetScopedService<AkkoDbContext>(out var dbContext);

            // The properties below are global
            Users = dbContext.DiscordUsers.ToConcurrentDictionary(x => x.UserId);
            Blacklist = dbContext.Blacklist.Select(x => x.ContextId).ToConcurrentHashSet();
            PlayingStatuses = dbContext.PlayingStatuses.Where(x => x.RotationTime != TimeSpan.Zero).ToList();

            // The properties below can either be global or specific to a guild
            Aliases = new();
            Aliases.TryAdd( // Load global aliases
                default,
                dbContext.Aliases
                    .Where(x => !x.GuildIdFK.HasValue)
                    .ToConcurrentHashSet()
            );

            Tags = new();
            Tags.TryAdd(    // Load global tags
                default,
                dbContext.Tags
                    .Where(x => !x.GuildIdFK.HasValue)
                    .ToConcurrentHashSet()
            );

            PermissionOverrides = new();
            PermissionOverrides.TryAdd(    // Load global command permission overrides
                default,
                dbContext.PermissionOverride
                    .Where(x => !x.GuildIdFK.HasValue)
                    .ToConcurrentHashSet()
            );

            //The properties below are specific to a guild and are cached on demand
            Guilds = new();
            Repeaters = new();
            VoiceRoles = new();
            FilteredWords = new();
            FilteredContent = new();
            Gatekeeping = new();
            Polls = new();
            AutoSlowmode = new();
            GuildLogs = new();
        }

        public async ValueTask<GuildConfigEntity> GetDbGuildAsync(ulong sid, BotConfig botConfig = default)
        {
            if (Guilds.TryGetValue(sid, out var dbGuild))
                return dbGuild;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            dbGuild = await db.GuildConfig
                .IncludeCacheable()
                .FirstOrDefaultAsync(x => x.GuildId == sid);

            if (dbGuild is null)
            {
                dbGuild = new GuildConfigEntity(botConfig) { GuildId = sid };
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

            if (dbGuild.AutoSlowmodeRel is not null)
                AutoSlowmode.TryAdd(dbGuild.GuildId, dbGuild.AutoSlowmodeRel);

            if (dbGuild.AliasRel.Count is not 0)
                Aliases.TryAdd(dbGuild.GuildId, dbGuild.AliasRel.ToConcurrentHashSet());

            if (dbGuild.FilteredContentRel.Count is not 0)
                FilteredContent.TryAdd(dbGuild.GuildId, dbGuild.FilteredContentRel.ToConcurrentHashSet());

            if (dbGuild.VoiceRolesRel.Count is not 0)
                VoiceRoles.TryAdd(dbGuild.GuildId, dbGuild.VoiceRolesRel.ToConcurrentHashSet());

            if (dbGuild.RepeaterRel.Count is not 0)
                Repeaters.TryAdd(dbGuild.GuildId, dbGuild.RepeaterRel.ToConcurrentHashSet());

            if (dbGuild.PollRel.Count is not 0)
                Polls.TryAdd(dbGuild.GuildId, dbGuild.PollRel.ToConcurrentHashSet());

            if (dbGuild.GuildLogsRel.Count is not 0)
                GuildLogs.TryAdd(dbGuild.GuildId, dbGuild.GuildLogsRel.ToConcurrentHashSet());

            if (dbGuild.TagsRel.Count is not 0)
                Tags.TryAdd(dbGuild.GuildId, dbGuild.TagsRel.ToConcurrentHashSet());

            if (dbGuild.PermissionOverrideRel.Count is not 0)
                PermissionOverrides.TryAdd(dbGuild.GuildId, dbGuild.PermissionOverrideRel.ToConcurrentHashSet());

            if (dbGuild.CommandCooldownRel.Count is not 0)
                CommandCooldown.LoadFromEntities(dbGuild.CommandCooldownRel);

            return true;
        }

        public bool TryRemoveDbGuild(ulong sid)
        {
            Guilds.TryRemove(sid, out var dbGuild);
            Gatekeeping.TryRemove(sid, out _);

            FilteredWords.TryRemove(sid, out _);

            FilteredContent.TryRemove(sid, out _);
            AutoSlowmode.TryRemove(sid, out _);
            Aliases.TryRemove(sid, out var aliases);
            VoiceRoles.TryRemove(sid, out var filters);
            Repeaters.TryRemove(sid, out var voiceRoles);
            Polls.TryRemove(sid, out var polls);
            GuildLogs.TryRemove(sid, out var guildLogs);
            Tags.TryRemove(sid, out var guildTags);
            PermissionOverrides.TryRemove(sid, out var permOverrides);

            if (dbGuild.CommandCooldownRel.Count is not 0)
                CommandCooldown.UnloadFromEntities(dbGuild.CommandCooldownRel);

            aliases?.Clear();
            filters?.Clear();
            voiceRoles?.Clear();
            polls?.Clear();
            guildLogs?.Clear();
            guildTags?.Clear();
            permOverrides?.Clear();

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
                    PlayingStatuses?.Clear();
                    PlayingStatuses?.TrimExcess();
                    FilteredWords?.Clear();
                    FilteredContent?.Clear();
                    Gatekeeping?.Clear();
                    AutoSlowmode?.Clear();
                    CommandCooldown?.Dispose();

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

                    if (GuildLogs is not null)
                    {
                        foreach (var group in GuildLogs.Values)
                            group.Clear();

                        GuildLogs.Clear();
                    }

                    if (Tags is not null)
                    {
                        foreach (var group in Tags.Values)
                            group.Clear();

                        Tags.Clear();
                    }

                    if (PermissionOverrides is not null)
                    {
                        foreach (var group in PermissionOverrides.Values)
                            group.Clear();

                        PermissionOverrides.Clear();
                    }
                }

                Blacklist = null;
                Guilds = null;
                PlayingStatuses = null;
                Aliases = null;
                FilteredWords = null;
                FilteredContent = null;
                Polls = null;
                Repeaters = null;
                VoiceRoles = null;
                Gatekeeping = null;
                AutoSlowmode = null;
                GuildLogs = null;
                Tags = null;
                CommandCooldown = null;
                PermissionOverrides = null;

                _isDisposed = true;
            }
        }
    }
}