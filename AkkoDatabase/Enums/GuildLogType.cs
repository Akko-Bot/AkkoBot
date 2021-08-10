using System;

namespace AkkoDatabase.Enums
{
    /// <summary>
    /// Represents the type of event that should be logged.
    /// </summary>
    [Flags]
    public enum GuildLogType
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
        /// Channel create, delete, and update.
        /// </summary>
        ChannelEvents = 1 << 1, // There is no reliable way of getting pin updates, since the timestamp doesn't match the message timestamp.

        /// <summary>
        /// Ban and unban.
        /// </summary>
        BanEvents = 1 << 2,

        /// <summary>
        /// Member join and leave.
        /// </summary>
        MemberEvents = 1 << 3,  // There is no value to logging updates.

        /// <summary>
        /// Message edit, delete, and bulk delete.
        /// </summary>
        MessageEvents = 1 << 4,

        /// <summary>
        /// Voice connect, move, and disconnect.
        /// </summary>
        VoiceEvents = 1 << 5,

        /// <summary>
        /// Role create, delete, and update.
        /// </summary>
        RoleEvents = 1 << 6,

        /// <summary>
        /// Invite create and delete.
        /// </summary>
        InviteEvents = 1 << 7,

        /// <summary>
        /// Emoji updated.
        /// </summary>
        EmojiEvents = 1 << 8,

        /// <summary>
        /// User presence update.
        /// </summary>
        UserPresence = 1 << 9,

        /// <summary>
        /// All events.
        /// </summary>
        All = Unknown | ChannelEvents | BanEvents | MemberEvents | MessageEvents
            | VoiceEvents | RoleEvents | InviteEvents | EmojiEvents | UserPresence
    }
}