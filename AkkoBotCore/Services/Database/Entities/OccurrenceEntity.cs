using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores how many times a user got punished in a server.")]
    public class OccurrenceEntity : DbEntity
    {
        public GuildConfigEntity GuildConfigRel { get; set; }

        public ulong GuildIdFK { get; init; }
        public ulong UserId { get; init; }
        public int Notices { get; set; }
        public int Warnings { get; set; }
        public int Mutes { get; set; }
        public int Kicks { get; set; }
        public int Softbans { get; set; }
        public int Bans { get; set; }

        public static OccurrenceEntity operator +(OccurrenceEntity x, OccurrenceEntity y)
        {
            x.Notices += y.Notices;
            x.Warnings += y.Warnings;
            x.Mutes += y.Mutes;
            x.Kicks += y.Kicks;
            x.Softbans += y.Softbans;
            x.Bans += y.Bans;

            return x;
        }

        public static OccurrenceEntity operator -(OccurrenceEntity x, OccurrenceEntity y)
        {
            x.Notices -= y.Notices;
            x.Warnings -= y.Warnings;
            x.Mutes -= y.Mutes;
            x.Kicks -= y.Kicks;
            x.Softbans -= y.Softbans;
            x.Bans -= y.Bans;

            return x;
        }
    }
}