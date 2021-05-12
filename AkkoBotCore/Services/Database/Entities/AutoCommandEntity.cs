using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Represents the type of an automatic command.
    /// </summary>
    public enum AutoCommandType
    {
        /// <summary>
        /// Represents an autocommand that runs once at startup.
        /// </summary>
        Startup,

        /// <summary>
        /// Represents an autocommand that runs once at a specific time.
        /// </summary>
        Scheduled,

        /// <summary>
        /// Represents an autocommand that runs multiple times at specific intervals of time.
        /// </summary>
        Repeated
    }

    /// <summary>
    /// Stores command data and the context it should be automatically sent to.
    /// </summary>
    [Comment("Stores command data and the context it should be automatically sent to.")]
    public class AutoCommandEntity : DbEntity
    {
        private readonly string _commandString;

        /// <summary>
        /// The database ID of the timer associated with this autocommand.
        /// </summary>
        /// <remarks>This property is <see langword="null"/> if this autocommand is of type <see cref="AutoCommandType.Startup"/>.</remarks>
        public int? TimerId { get; init; }

        /// <summary>
        /// The qualified command name with its arguments.
        /// </summary>
        [Required]
        [MaxLength(AkkoConstants.MessageMaxLength)]
        public string CommandString
        {
            get => _commandString;
            init => _commandString = value?.MaxLength(AkkoConstants.MessageMaxLength);
        }

        /// <summary>
        /// The ID of the Discord guild associated with this autocommand.
        /// </summary>
        public ulong GuildId { get; init; }

        /// <summary>
        /// The ID of the Discord user who created this autocommand.
        /// </summary>
        public ulong AuthorId { get; init; }

        /// <summary>
        /// The ID of the Discord channel where the autocommand is supposed to execute.
        /// </summary>
        public ulong ChannelId { get; init; }

        /// <summary>
        /// The type of this autocommand.
        /// </summary>
        public AutoCommandType Type { get; init; }
    }
}