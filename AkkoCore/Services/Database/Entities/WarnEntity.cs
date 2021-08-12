using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Services.Database.Abstractions;
using AkkoCore.Services.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AkkoCore.Services.Database.Entities
{
    /// <summary>
    /// Stores warnings issued to users on Discord guilds.
    /// </summary>
    [Comment("Stores warnings issued to users on servers.")]
    public class WarnEntity : DbEntity
    {
        private readonly string _note;

        /// <summary>
        /// The Discord user associated with this infraction.
        /// </summary>
        public DiscordUserEntity UserRel { get; init; }

        /// <summary>
        /// The settings of the Discord guild this infraction is associated with.
        /// </summary>
        public GuildConfigEntity GuildConfigRel { get; init; }

        /// <summary>
        /// The timer associated with this infraction.
        /// </summary>
        public TimerEntity TimerRel { get; init; }

        /// <summary>
        /// The database ID of the timer associated with this infraction.
        /// </summary>
        public int? TimerIdFK { get; set; }

        /// <summary>
        /// The ID of the Discord guild this infraction is associated with.
        /// </summary>
        public ulong GuildIdFK { get; init; }

        //public int? DbUserIdFK { get; init; }

        /// <summary>
        /// The ID of the Discord user that committed the infraction.
        /// </summary>
        public ulong UserIdFK { get; init; }

        /// <summary>
        /// The ID of the Discord user that issued the infraction.
        /// </summary>
        public ulong AuthorId { get; init; }

        /// <summary>
        /// The type of this infraction.
        /// </summary>
        public WarnType Type { get; init; }

        /// <summary>
        /// The content of the infraction.
        /// </summary>
        [MaxLength(AkkoConstants.MaxMessageLength)]
        public string WarningText
        {
            get => _note;
            init => _note = value?.MaxLength(AkkoConstants.MaxMessageLength) ?? "-";
        }
    }
}