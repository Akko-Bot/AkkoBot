using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Represents the type of punishment to be applied to a user.
    /// </summary>
    public enum PunishmentType
    {
        /// <summary>
        /// Mutes the user.
        /// </summary>
        Mute,

        /// <summary>
        /// Kicks the user.
        /// </summary>
        Kick,

        /// <summary>
        /// Soft-bans the user.
        /// </summary>
        Softban,

        /// <summary>
        /// Bans the user.
        /// </summary>
        Ban,

        /// <summary>
        /// Adds a role to the user.
        /// </summary>
        AddRole,

        /// <summary>
        /// Removes a role from the user.
        /// </summary>
        RemoveRole
    }

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