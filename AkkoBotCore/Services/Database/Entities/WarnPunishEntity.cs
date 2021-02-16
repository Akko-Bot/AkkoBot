using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    public enum WarnPunishType { Mute, Kick, Softban, Ban }

    [Comment("Stores punishments to be automatically given once a user reaches a certain amount of warnings.")]
    public class WarnPunishEntity : DbEntity
    {
        public GuildConfigEntity GuildConfigRel { get; set; }

        public ulong GuildIdFK { get; init; }
        public int WarnAmount { get; init; }
        public WarnPunishType Type { get; init; }
    }
}