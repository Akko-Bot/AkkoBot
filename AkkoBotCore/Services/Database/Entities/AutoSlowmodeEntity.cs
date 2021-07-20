using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Stores the settings for the automatic slow mode of a Discord server.
    /// </summary>
    [Comment("Stores the settings for the automatic slow mode of a Discord server.")]
    public class AutoSlowmodeEntity : DbEntity
    {
        private TimeSpan _slowmodeDuration;

        /// <summary>
        /// The settings of the Discord guild this filter is associated with.
        /// </summary>
        public GuildConfigEntity GuildConfigRel { get; init; }

        /// <summary>
        /// List of IDs that won't trigger a slow mode.
        /// </summary>
        public List<long> IgnoredIds { get; init; } = new();

        /// <summary>
        /// The ID of the Discord guild this auto slow mode is associated with.
        /// </summary>
        public ulong GuildIdFK { get; init; }

        /// <summary>
        /// Defines whether this auto slow mode is active or not.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Defines the amount of successive messages necessary to trigger the slow mode.
        /// </summary>
        public int MessageAmount { get; set; }

        /// <summary>
        /// Defines the time interval the successive messages need to be sent in order to activate the slow mode.
        /// </summary>
        public TimeSpan SlowmodeTriggerTime { get; set; }

        /// <summary>
        /// Defines for how long the slow mode should last.
        /// </summary>
        public TimeSpan SlowmodeDuration { get; set; }

        /// <summary>
        /// Defines the time interval users are allowed to send messages.
        /// </summary>
        public TimeSpan SlowmodeInterval
        {
            get => _slowmodeDuration;
            set => _slowmodeDuration = (value <= TimeSpan.FromHours(6)) ? value : TimeSpan.FromHours(6);
        }
    }
}
