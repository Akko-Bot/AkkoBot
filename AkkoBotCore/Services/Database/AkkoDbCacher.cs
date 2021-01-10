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
        public HashSet<ulong> BlackList { get; init; }
        public string DefaultPrefix { get; set; }
        public ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; init; }
        public List<PlayingStatusEntity> PlayingStatuses { get; init; }

        public AkkoDbCacher(AkkoDbContext dbContext)
        {
            BlackList = dbContext.Blacklist.Select(x => x.TypeId).ToHashSet() ?? new();
            DefaultPrefix = dbContext.BotConfig.Select(x => x.DefaultPrefix).FirstOrDefault() ?? "!";
            Guilds = dbContext.GuildConfigs.ToConcurrentDictionary(x => x.GuildId) ?? new();
            PlayingStatuses = dbContext.PlayingStatuses.ToList() ?? new();
        }
    }
}
