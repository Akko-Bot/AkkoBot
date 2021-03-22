using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Timers.Abstractions;
using ConcurrentCollections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AkkoBot.Services.Database.Abstractions
{
    /// <summary>
    /// Represents a default database cache for an <see cref="IUnitOfWork"/>.
    /// </summary>
    public interface IDbCacher : IDisposable
    {
        ConcurrentHashSet<ulong> Blacklist { get; }
        BotConfigEntity BotConfig { get; set; }
        LogConfigEntity LogConfig { get; set; }
        ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; }
        ITimerManager Timers { get; set; }
        List<PlayingStatusEntity> PlayingStatuses { get; }
        ConcurrentDictionary<ulong, ConcurrentHashSet<AliasEntity>> Aliases { get; }
        ConcurrentDictionary<ulong, FilteredWordsEntity> FilteredWords { get; }
    }
}