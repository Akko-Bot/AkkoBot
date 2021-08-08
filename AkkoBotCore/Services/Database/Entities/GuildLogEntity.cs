using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Enums;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Represents a guild log.
    /// </summary>
    [Comment("Stores information about a guild log.")]
    public class GuildLogEntity : DbEntity
    {
        /// <summary>
        /// The settings of the Discord guild this poll is associated with.
        /// </summary>
        public GuildConfigEntity GuildConfigRel { get; init; }

        /// <summary>
        /// The ID of the Discord guild this poll is associated with.
        /// </summary>
        public ulong GuildIdFK { get; init; }

        /// <summary>
        /// The ID of the Discord channel the logs should be sent to.
        /// </summary>
        public ulong ChannelId { get; set; }

        /// <summary>
        /// The ID of the webhook responsible for sending the log message.
        /// </summary>
        public ulong WebhookId { get; set; }

        /// <summary>
        /// Defines whether this log is active or not.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The type of log to be sent.
        /// </summary>
        public GuildLogType Type { get; set; }
    }
}
