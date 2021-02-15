using AkkoBot.Services.Database.Abstractions;

namespace AkkoBot.Services.Database.Entities
{
    public enum WarnPunishType { Mute, Kick, Softban, Ban }

    public class WarnPunishEntity : DbEntity
    {
        public GuildConfigEntity GuildConfigRel { get; set; }

        public ulong GuildIdFK { get; init; }
        public int WarnAmount { get; init; }
        public WarnPunishType Type { get; init; }
    }
}