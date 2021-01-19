using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AkkoBot.Services.Database
{
    public class AkkoDbCacher : IDbCacher
    {
        private bool _isDisposed = false;

        public HashSet<ulong> Blacklist { get; private set; }
        public BotConfigEntity BotConfig { get; private set; }
        public ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; private set; }
        public List<PlayingStatusEntity> PlayingStatuses { get; private set; }

        public AkkoDbCacher(AkkoDbContext dbContext)
        {
            Blacklist = dbContext.Blacklist.Select(x => x.ContextId).ToHashSet();
            BotConfig = dbContext.BotConfig.FirstOrDefault();
            Guilds = dbContext.GuildConfigs.ToConcurrentDictionary(x => x.GuildId);
            PlayingStatuses = dbContext.PlayingStatuses.ToList();
        }

        /// <summary>
        /// Resets the database cache.
        /// </summary>
        public void Reset(ulong botId)
        {
            Blacklist.Clear();
            BotConfig = new(botId);
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
                    Blacklist.TrimExcess();
                    Guilds.Clear();
                    PlayingStatuses.Clear();
                    PlayingStatuses.TrimExcess();
                }

                Blacklist = null;
                BotConfig = null;
                Guilds = null;
                PlayingStatuses = null;

                _isDisposed = true;
            }
        }
    }
}
