using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Represents the type of a warning.
    /// </summary>
    public enum WarnType
    {
        /// <summary>
        /// Represents a note about a user or incident.
        /// </summary>
        Notice,

        /// <summary>
        /// Represents a warning.
        /// </summary>
        Warning
    }

    /// <summary>
    /// Stores warnings issued to users on Discord guilds.
    /// </summary>
    [Comment("Stores warnings issued to users on servers.")]
    public class WarnEntity : DbEntity
    {
        private readonly string _note;

        /// <summary>
        /// The settings of the Discord guild this warning is associated with.
        /// </summary>
        public GuildConfigEntity GuildConfigRel { get; init; }

        /// <summary>
        /// The ID of the Discord guild this warning is associated with.
        /// </summary>
        public ulong GuildIdFK { get; init; }

        /// <summary>
        /// The ID of the Discord user that received the warning.
        /// </summary>
        public ulong UserId { get; init; }

        /// <summary>
        /// The ID of the Discord user that issued the warning.
        /// </summary>
        public ulong AuthorId { get; init; }

        /// <summary>
        /// The type of this warning.
        /// </summary>
        public WarnType Type { get; init; }

        /// <summary>
        /// The content of the warning.
        /// </summary>
        [MaxLength(AkkoConstants.MessageMaxLength)]
        public string WarningText
        {
            get => _note;
            init => _note = value?.MaxLength(AkkoConstants.MessageMaxLength) ?? "-";
        }
    }
}