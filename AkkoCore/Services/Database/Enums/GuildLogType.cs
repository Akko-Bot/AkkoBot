using System;

namespace AkkoCore.Services.Database.Enums;

/// <summary>
/// Represents the type of event that should be logged.
/// </summary>
[Flags]
public enum GuildLogType : long
{
    /// <summary>
    /// No log.
    /// </summary>
    None = 0,

    /// <summary>
    /// Any event not recognized by the bot.
    /// </summary>
    Unknown = 1L << 0,

    /* Messages */ 

    /// <summary>
    /// When a Discord message is created.
    /// </summary>
    MessageCreated = 1L << 1,

    /// <summary>
    /// When a Discord message is edited.
    /// </summary>
    MessageUpdated = 1L << 2,

    /// <summary>
    /// When a Discord message is deleted.
    /// </summary>
    MessageDeleted = 1L << 3,

    /// <summary>
    /// When multiple Discord messages are deleted.
    /// </summary>
    MessageBulkDeleted = 1L << 4,

    /// <summary>
    /// When a Discord message gets pinned.
    /// </summary>
    MessagePinned = 1L << 5, // There is no reliable way of getting pin updates, since the timestamp doesn't match the message timestamp.

    /* Emojis */

    /// <summary>
    /// When a guild emoji is created.
    /// </summary>
    EmojiCreated = 1L << 6,

    /// <summary>
    /// When a guild emoji is edited.
    /// </summary>
    EmojiUpdated = 1L << 7,

    /// <summary>
    /// When a guild emoji is deleted.
    /// </summary>
    EmojiDeleted = 1L << 8,

    /* Invites */

    /// <summary>
    /// When a guild invite is created.
    /// </summary>
    InviteCreated = 1L << 9,

    /// <summary>
    /// When a guild invite is deleted.
    /// </summary>
    InviteDeleted = 1L << 10,

    /* Punishments */

    /// <summary>
    /// When a Discord user is banned from a guild.
    /// </summary>
    UserBanned = 1L << 11,

    /// <summary>
    /// When a Discord user is unbanned from a guild.
    /// </summary>
    UserUnbanned = 1L << 12,

    /* Roles */

    /// <summary>
    /// When a Discord role is created.
    /// </summary>
    RoleCreated = 1L << 13,

    /// <summary>
    /// When a Discord role is edited.
    /// </summary>
    RoleUpdated = 1L << 14,

    /// <summary>
    /// When a Discord role is deleted.
    /// </summary>
    RoleDeleted = 1L << 15,

    /// <summary>
    /// When a Discord role is assigned to a user.
    /// </summary>
    RoleAssigned = 1L << 16,

    /// <summary>
    /// When a Discord role is revoked from a user.
    /// </summary>
    RoleRevoked = 1L << 17,

    /* Channels */

    /// <summary>
    /// When a Discord channel is created.
    /// </summary>
    ChannelCreated = 1L << 18,

    /// <summary>
    /// When a Discord channel is edited.
    /// </summary>
    ChannelUpdated = 1L << 19,

    /// <summary>
    /// When a Discord channel is deleted.
    /// </summary>
    ChannelDeleted = 1L << 20,

    /* Voice State */

    /// <summary>
    /// When a Discord user connects to a voice channel.
    /// </summary>
    VoiceConnected = 1L << 21,

    /// <summary>
    /// When a Discord user moves from a voice channel.
    /// </summary>
    VoiceMoved = 1L << 22,

    /// <summary>
    /// When a Discord user disconnects from a voice channel.
    /// </summary>
    VoiceDisconnected = 1L << 23,

    /* Users */

    /// <summary>
    /// When a Discord user joins the guild.
    /// </summary>
    UserJoined = 1L << 24,

    /// <summary>
    /// When a Discord user leaves the guild.
    /// </summary>
    UserLeft = 1L << 25,

    /// <summary>
    /// When a Discord user changes their nickname.
    /// </summary>
    NicknameChanged = 1L << 26,

    /* Alts */

    /// <summary>
    /// When a possible alt joins the guild.
    /// </summary>
    AltJoined = 1L << 27,

    /// <summary>
    /// When a possible alt leaves the guild.
    /// </summary>
    AltLeft = 1L << 28,

    /* Presence */

    /// <summary>
    /// When a Discord user starts an activity.
    /// </summary>
    UserActivityCreated = 1L << 29,

    /// <summary>
    /// When a Discord user changes their activity.
    /// </summary>
    UserActivityUpdated = 1L << 30,

    /// <summary>
    /// When a Discord user stops an activity.
    /// </summary>
    UserActivityRemoved = 1L << 31,

    /// <summary>
    /// When a Discord user changes their connection status (Online, Idle, Busy, Streaming, Offline).
    /// </summary>
    UserStatusUpdated = 1L << 32,

    /// <summary>
    /// When a Discord user changes their avatar.
    /// </summary>
    UserAvatarUpdated = 1L << 33,

    /// <summary>
    /// When a Discord user changes their username.
    /// </summary>
    UserNameUpdated = 1L << 34,

    /* Groups */

    /// <summary>
    /// Channel create, delete, and update.
    /// </summary>
    ChannelEvents = ChannelCreated | ChannelUpdated | ChannelDeleted,

    /// <summary>
    /// Ban and unban.
    /// </summary>
    PunishmentEvents = UserBanned | UserUnbanned,

    /// <summary>
    /// Member join and leave.
    /// </summary>
    MemberEvents = UserJoined | UserLeft | NicknameChanged,

    /// <summary>
    /// Message edit, delete, and bulk delete.
    /// </summary>
    MessageEvents = MessageCreated | MessagePinned | MessageUpdated | MessageDeleted | MessageBulkDeleted,

    /// <summary>
    /// Voice connect, move, and disconnect.
    /// </summary>
    VoiceEvents = VoiceConnected | VoiceMoved | VoiceDisconnected,

    /// <summary>
    /// Role create, delete, and update.
    /// </summary>
    RoleEvents = RoleCreated | RoleUpdated | RoleDeleted | RoleAssigned | RoleRevoked,

    /// <summary>
    /// Invite create and delete.
    /// </summary>
    InviteEvents = InviteCreated | InviteDeleted,

    /// <summary>
    /// Emoji updated.
    /// </summary>
    EmojiEvents = EmojiCreated | EmojiUpdated | EmojiDeleted,

    /// <summary>
    /// Join and leave events for possible alt accounts.
    /// </summary>
    AltEvents = AltJoined | AltLeft,

    /// <summary>
    /// User activity, avatar and username changes.
    /// </summary>
    PresenceEvents = UserActivityCreated | UserActivityUpdated | UserActivityRemoved | UserStatusUpdated  | UserAvatarUpdated | UserNameUpdated,

    /// <summary>
    /// All events.
    /// </summary>
    All = Unknown | ChannelEvents | PunishmentEvents | MemberEvents | MessageEvents
        | VoiceEvents | RoleEvents | InviteEvents | EmojiEvents | PresenceEvents | AltEvents
}