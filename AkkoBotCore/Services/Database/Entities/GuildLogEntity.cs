using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Represents the type of event that should be logged.
    /// </summary>
    [Flags]
    public enum GuildLog
    {
        /// <summary>
        /// No log.
        /// </summary>
        None = 0,

        /// <summary>
        /// Any event not recognized by the bot.
        /// </summary>
        Unknown = 1 << 0,

        /// <summary>
        /// Channel create, delete, update and pin update.
        /// </summary>
        ChannelEvents = 1 << 1,

        /// <summary>
        /// Ban and unban.
        /// </summary>
        BanEvents = 1 << 2,

        /// <summary>
        /// Member join, leave and update.
        /// </summary>
        MemberEvents = 1 << 3,

        /// <summary>
        /// Message edit, delete and bulk delete.
        /// </summary>
        MessageEvents = 1 << 4,

        /// <summary>
        /// Voice connect, move and disconnect.
        /// </summary>
        VoiceEvents = 1 << 5,

        /// <summary>
        /// Role create, delete and update.
        /// </summary>
        RoleEvents = 1 << 6,

        /// <summary>
        /// Invite create and delete.
        /// </summary>
        InviteEvents = 1 << 7,

        /// <summary>
        /// Integration updated.
        /// </summary>
        Integration = 1 << 8,

        /// <summary>
        /// Emoji updated.
        /// </summary>
        Emojis = 1 << 9,

        /// <summary>
        /// Guild update.
        /// </summary>
        GuildUpdate = 1 << 10,

        /// <summary>
        /// User presence update.
        /// </summary>
        UserPresence = 1 << 11,

        /// <summary>
        /// All events.
        /// </summary>
        All = Unknown | ChannelEvents | BanEvents | MemberEvents | MessageEvents| VoiceEvents
            | RoleEvents | InviteEvents | Integration | Emojis | GuildUpdate | UserPresence
    }

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
        public GuildLog Type { get; set; }
    }
}
