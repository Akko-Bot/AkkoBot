using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Stores the amount of infractions commited by a user in a Discord guild..
    /// </summary>
    [Comment("Stores the amount of infractions commited by a user in a server.")]
    public class OccurrenceEntity : DbEntity
    {
        /// <summary>
        /// The settings of the Discord guild these occurrences are associated with.
        /// </summary>
        public GuildConfigEntity GuildConfigRel { get; init; }

        /// <summary>
        /// The ID of the Discord guild this these occurrences are associated with.
        /// </summary>
        public ulong GuildIdFK { get; init; }

        /// <summary>
        /// The ID of the Discord user these occurrences are associated with.
        /// </summary>
        public ulong UserId { get; init; }

        /// <summary>
        /// The amount of notices.
        /// </summary>
        public int Notices { get; set; }

        /// <summary>
        /// The amount of warnings.
        /// </summary>
        public int Warnings { get; set; }

        /// <summary>
        /// The amount of mutes.
        /// </summary>
        public int Mutes { get; set; }

        /// <summary>
        /// The amount of kicks.
        /// </summary>
        public int Kicks { get; set; }

        /// <summary>
        /// The amount of soft-bans.
        /// </summary>
        public int Softbans { get; set; }

        /// <summary>
        /// The amount of bans.
        /// </summary>
        public int Bans { get; set; }

        public static OccurrenceEntity operator +(OccurrenceEntity x, OccurrenceEntity y)
        {
            return new OccurrenceEntity()
            {
                Id = x.Id,
                DateAdded = x.DateAdded,
                GuildIdFK = x.GuildIdFK,
                UserId = x.UserId,
                GuildConfigRel = x.GuildConfigRel,
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
                Id = x.Id,
                DateAdded = x.DateAdded,
                GuildIdFK = x.GuildIdFK,
                UserId = x.UserId,
                GuildConfigRel = x.GuildConfigRel,
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