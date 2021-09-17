using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Services.Database.Abstractions;
using AkkoCore.Services.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AkkoCore.Services.Database.Entities
{
    /// <summary>
    /// Stores filtered words of a Discord server.
    /// </summary>
    [Comment("Stores filtered words of a Discord server.")]
    public class FilteredWordsEntity : DbEntity
    {
        private string _notificationMessage;

        /// <summary>
        /// The settings of the Discord guild this filter is associated with.
        /// </summary>
        public GuildConfigEntity GuildConfigRel { get; init; }

        /// <summary>
        /// List of filtered words.
        /// </summary>
        public List<string> Words { get; init; } = new();

        /// <summary>
        /// List of user, role, and channel IDs allowed to bypass the filter.
        /// </summary>
        public List<long> IgnoredIds { get; init; } = new(); // Postgres does not support unsigned types for collections :(

        /// <summary>
        /// The ID of the Discord guild this filter is associated with.
        /// </summary>
        public ulong GuildIdFK { get; init; }

        /// <summary>
        /// The notification to be sent when a user sends a message with a filtered word.
        /// </summary>
        [MaxLength(AkkoConstants.MaxMessageLength)]
        public string NotificationMessage
        {
            get => _notificationMessage;
            set => _notificationMessage = value?.MaxLength(AkkoConstants.MaxMessageLength);
        }

        /// <summary>
        /// Defines whether this filter is active or not.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Defines the additional behaviors of this filter.
        /// </summary>
        public WordFilterBehavior Behavior { get; set; } = WordFilterBehavior.None;
    }
}