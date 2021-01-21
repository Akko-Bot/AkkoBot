using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AkkoBot.Services.Database.Entities;

namespace AkkoBot.Services.Database.Abstractions
{
    /// <summary>
    /// Represents a default database cacher for an <see cref="IUnitOfWork"/>.
    /// </summary>
    public interface IDbCacher : IDisposable
    {
        HashSet<ulong> Blacklist { get; }
        BotConfigEntity BotConfig { get; set; }
        LogConfigEntity LogConfig { get; set; }
        ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; }
        List<PlayingStatusEntity> PlayingStatuses { get; }

        /// <summary>
        /// Reinitializes the database cache.
        /// </summary>
        /// <param name="botId">Discord ID of the bot.</param>
        void Reset(ulong botId);
    }
}