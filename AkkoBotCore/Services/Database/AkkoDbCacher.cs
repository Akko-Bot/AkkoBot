using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Entities;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AkkoBot.Services.Database
{
    public class AkkoDbCacher : ICommandService
    {
        public HashSet<ulong> Blacklist { get; }
        public BotConfigEntity BotConfig { get; private set; }
        public ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; }
        public List<PlayingStatusEntity> PlayingStatuses { get; }

        public AkkoDbCacher(AkkoDbContext dbContext)
        {
            Blacklist = dbContext.Blacklist.Select(x => x.TypeId).ToHashSet();
            BotConfig = dbContext.BotConfig.FirstOrDefault() ?? new();
            Guilds = dbContext.GuildConfigs.ToConcurrentDictionary(x => x.GuildId);
            PlayingStatuses = dbContext.PlayingStatuses.ToList();
        }

        /// <summary>
        /// Completely resets the database cache.
        /// </summary>
        public void Clear()
        {
            Blacklist.Clear();
            BotConfig = new();
            Guilds.Clear();
            PlayingStatuses.Clear();
        }
    }
}
