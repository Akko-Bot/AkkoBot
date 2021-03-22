using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores how many times a user got punished in a server.")]
    public class OccurrenceEntity : DbEntity
    {
        public GuildConfigEntity GuildConfigRel { get; init; }

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
            return new OccurrenceEntity()
            {
                Notices = x.Notices + y.Notices,
                Warnings = x.Warnings + y.Warnings,
                Mutes = x.Mutes + y.Mutes,
                Kicks = x.Kicks + y.Kicks,
                Softbans = x.Softbans + y.Softbans,
                Bans = x.Bans + y.Bans
            };
        }

        public static OccurrenceEntity operator -(OccurrenceEntity x, OccurrenceEntity y)
        {
            return new OccurrenceEntity()
            {
                Notices = x.Notices - y.Notices,
                Warnings = x.Warnings - y.Warnings,
                Mutes = x.Mutes - y.Mutes,
                Kicks = x.Kicks - y.Kicks,
                Softbans = x.Softbans - y.Softbans,
                Bans = x.Bans - y.Bans
            };
        }
    }
}