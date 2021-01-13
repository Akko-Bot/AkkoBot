using System.Collections.Concurrent;
using System.Collections.Generic;
using AkkoBot.Services.Database.Entities;

namespace AkkoBot.Services.Database.Abstractions
{
    public interface IDbCacher
    {
        HashSet<ulong> Blacklist { get; }
        BotConfigEntity BotConfig { get; }
        ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; }
        List<PlayingStatusEntity> PlayingStatuses { get; }

        void Clear();
    }
}