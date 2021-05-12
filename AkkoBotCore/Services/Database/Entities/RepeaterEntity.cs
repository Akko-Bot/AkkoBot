using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Stores repeater data and the context it should be sent to.
    /// </summary>
    [Comment("Stores repeater data and the context it should be sent to.")]
    public class RepeaterEntity : DbEntity
    {
        private string _content;

        /// <summary>
        /// The settings of the Discord guild this repeater is associated with.
        /// </summary>
        public GuildConfigEntity GuildConfigRel { get; init; }

        /// <summary>
        /// The database ID of the timer this repeater is associated with.
        /// </summary>
        public int TimerId { get; init; }

        /// <summary>
        /// The content of this repeater.
        /// </summary>
        [Required]
        [MaxLength(AkkoConstants.MessageMaxLength)]
        public string Content
        {
            get => _content;
            init => _content = value?.MaxLength(AkkoConstants.MessageMaxLength) ?? "-";
        }

        /// <summary>
        /// The ID of the Discord guild associated with this repeater.
        /// </summary>
        public ulong GuildIdFK { get; init; }

        /// <summary>
        /// The ID of the Discord user who created this repeater.
        /// </summary>
        public ulong AuthorId { get; init; }

        /// <summary>
        /// The ID of the Discord channel associated with this repeater.
        /// </summary>
        public ulong ChannelId { get; init; }

        /// <summary>
        /// The time interval this repeater should trigger.
        /// </summary>
        public TimeSpan Interval { get; init; }
    }
}