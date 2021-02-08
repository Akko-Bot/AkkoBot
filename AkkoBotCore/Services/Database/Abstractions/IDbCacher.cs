using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Timers;
using ConcurrentCollections;

namespace AkkoBot.Services.Database.Abstractions
{
    /// <summary>
    /// Represents a default database cacher for an <see cref="IUnitOfWork"/>.
    /// </summary>
    public interface IDbCacher : IDisposable
    {
        ConcurrentHashSet<ulong> Blacklist { get; }
        BotConfigEntity BotConfig { get; set; }
        LogConfigEntity LogConfig { get; set; }
        ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; }
        TimerManager Timers { get; set; }
        List<PlayingStatusEntity> PlayingStatuses { get; }

        /// <summary>
        /// Reinitializes the database cache.
        /// </summary>
        /// <param name="botId">Discord ID of the bot.</param>
        void Reset(ulong botId);
    }
}