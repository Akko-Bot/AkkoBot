using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Entities
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
        [MaxLength(AkkoConstants.MessageMaxLength)]
        public string NotificationMessage
        {
            get => _notificationMessage;
            set => _notificationMessage = value?.MaxLength(AkkoConstants.MessageMaxLength);
        }

        /// <summary>
        /// Defines whether this filter is active or not.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Determines whether stickers should be filtered.
        /// </summary>
        public bool FilterStickers { get; set; }

        /// <summary>
        /// Determines whether server invites should be filtered.
        /// </summary>
        public bool FilterInvites { get; set; }

        /// <summary>
        /// Determines whether the user should be notified if they said a filtered word.
        /// </summary>
        public bool NotifyOnDelete { get; set; }

        /// <summary>
        /// Determines whether the user should be automatically warned for saying a filtered word.
        /// </summary>
        public bool WarnOnDelete { get; set; }
    }
}