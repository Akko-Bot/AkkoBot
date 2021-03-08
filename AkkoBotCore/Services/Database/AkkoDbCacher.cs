using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Timers;
using AkkoBot.Services.Timers.Abstractions;
using ConcurrentCollections;
using DSharpPlus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AkkoBot.Services.Database
{
    /// <summary>
    /// This class acts as a singleton cache for UoW objects.
    /// </summary>
    public class AkkoDbCacher : IDbCacher
    {
        private bool _isDisposed = false;

        public ConcurrentHashSet<ulong> Blacklist { get; private set; }
        public BotConfigEntity BotConfig { get; set; }
        public LogConfigEntity LogConfig { get; set; }
        public ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; private set; }
        public List<PlayingStatusEntity> PlayingStatuses { get; private set; }

        // Lazily instantiated
        public ITimerManager Timers { get; set; }

        public AkkoDbCacher(AkkoDbContext dbContext)
        {
            Blacklist = dbContext.Blacklist.Select(x => x.ContextId).ToConcurrentHashSet();
            BotConfig = dbContext.BotConfig.FirstOrDefault();
            LogConfig = dbContext.LogConfig.FirstOrDefault();
            Guilds = new(); // Guild configs will be loaded into the cache as needed.
            PlayingStatuses = dbContext.PlayingStatuses.Where(x => x.RotationTime != TimeSpan.Zero).ToList();
        }

        /// <summary>
        /// Reinitializes the database cache.
        /// </summary>
        /// <param name="botId">Discord ID of the bot.</param>
        public void Reset(ulong botId)
        {
            Blacklist.Clear();
            BotConfig = new();
            LogConfig = new();
            Guilds.Clear();
            PlayingStatuses.Clear();
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
                    Blacklist.Clear();
                    Guilds.Clear();
                    Timers.Dispose();
                    PlayingStatuses.Clear();
                    PlayingStatuses.TrimExcess();
                }

                Blacklist = null;
                BotConfig = null;
                LogConfig = null;
                Guilds = null;
                Timers = null;
                PlayingStatuses = null;

                _isDisposed = true;
            }
        }
    }
}
