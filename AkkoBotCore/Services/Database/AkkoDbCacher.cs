using AkkoBot.Command.Abstractions;
using AkkoBot.Services.Database.Entities;
using System.Collections.Generic;
using System.Linq;

namespace AkkoBot.Services.Database
{
    public class AkkoDbCacher : ICommandService
    {
        public HashSet<ulong> BlackList { get; init; }
        public string DefaultPrefix { get; set; }
        public Dictionary<ulong, GuildConfigEntity> Guilds { get; init; }
        public List<PlayingStatusEntity> PlayingStatuses { get; init; }

        public AkkoDbCacher(AkkoDbContext dbContext)
        {
            BlackList = dbContext.Blacklist.Select(x => x.TypeId).ToHashSet() ?? new();
            DefaultPrefix = dbContext.BotConfig.Select(x => x.DefaultPrefix).FirstOrDefault() ?? "!";
            Guilds = dbContext.GuildConfigs.ToDictionary(x => x.GuildId) ?? new();
            PlayingStatuses = dbContext.PlayingStatuses.ToList() ?? new();
        }
    }
}
