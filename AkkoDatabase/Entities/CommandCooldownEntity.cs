using AkkoDatabase.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace AkkoDatabase.Entities
{
    /// <summary>
    /// Stores a command whose execution is restricted by a cooldown.
    /// </summary>
    [Comment("Stores commands whose execution is restricted by a cooldown.")]
    public class CommandCooldownEntity : DbEntity
    {
        /// <summary>
        /// The qualified name of the command.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Command { get; init; }

        /// <summary>
        /// The ID of the Discord guild associated with this cooldown.
        /// </summary>
        /// <remarks>This property is <see langword="null"/> for commands on a global cooldown.</remarks>
        public ulong? GuildId { get; init; }

        /// <summary>
        /// Determines the time for the cooldown.
        /// </summary>
        public TimeSpan Cooldown { get; set; }
    }
}