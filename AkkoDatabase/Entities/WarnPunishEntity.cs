using AkkoDatabase.Abstractions;
using AkkoDatabase.Enums;
using Microsoft.EntityFrameworkCore;
using System;

namespace AkkoDatabase.Entities
{
    /// <summary>
    /// Stores punishments to be automatically applied once a user reaches a certain amount of warnings.
    /// </summary>
    [Comment("Stores punishments to be automatically applied once a user reaches a certain amount of warnings.")]
    public class WarnPunishEntity : DbEntity
    {
        /// <summary>
        /// The settings of the Discord guild this punishment is associated with.
        /// </summary>
        public GuildConfigEntity GuildConfigRel { get; init; }

        /// <summary>
        /// The ID of the Discord guild this punishment is associated with.
        /// </summary>
        public ulong GuildIdFK { get; init; }

        /// <summary>
        /// The amount of warnings required to trigger this punishment.
        /// </summary>
        public int WarnAmount { get; init; }

        /// <summary>
        /// The type of the punishment.
        /// </summary>
        public PunishmentType Type { get; set; }

        /// <summary>
        /// The time interval this punishment should last.
        /// </summary>
        /// <remarks>This property is <see langword="null"/> for permanent punishments.</remarks>
        public TimeSpan? Interval { get; set; }

        /// <summary>
        /// The ID of the punishment role.
        /// </summary>
        /// <remarks>This property is <see langword="null"/> for punishments that don't involve a role.</remarks>
        public ulong? PunishRoleId { get; set; }
    }
}