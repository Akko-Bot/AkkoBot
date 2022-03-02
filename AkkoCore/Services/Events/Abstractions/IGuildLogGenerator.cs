using AkkoCore.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;

namespace AkkoCore.Services.Events.Abstractions;

/// <summary>
/// Represents an object that generates guild log messages.
/// </summary>
public interface IGuildLogGenerator
{
    /// <summary>
    /// Generates a log message for a <see cref="MessageDeleteEventArgs"/> event.
    /// </summary>
    /// <param name="message">The message that got deleted.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="message"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetMessageDeleteLog(DiscordMessage message);

    /// <summary>
    /// Generates a log message for a <see cref="MessageUpdateEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <see cref="MessageUpdateEventArgs.MessageBefore"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetMessageUpdateLog(MessageUpdateEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="MessageBulkDeleteEventArgs"/> event.
    /// </summary>
    /// <param name="messages">The messages to be logged.</param>
    /// <param name="stream">The stream for the file the messages are being saved to.</param>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentException">
    /// Occurs when <paramref name="messages"/> or <paramref name="stream"/> are <see langword="null"/> or the message collection is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetMessageBulkDeleteLog(IEnumerable<DiscordMessage> messages, Stream stream, MessageBulkDeleteEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="GuildEmojisUpdateEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event arguments.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="server"/> or <paramref name="emoji"/> are <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Occurs when the emoji is not found. This would indicate an issue with D#+</exception>
    /// <exception cref="NotSupportedException">Occurs when <see cref="GuildEmojisUpdateEventArgsExt.GetStatus(GuildEmojisUpdateEventArgs)"/> returns an unknown activity type.</exception>
    DiscordWebhookBuilder GetEmojiLog(GuildEmojisUpdateEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="InviteCreateEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetCreatedInviteLog(InviteCreateEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="InviteDeleteEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetDeletedInviteLog(InviteDeleteEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="GuildBanAddEventArgs"/> event.
    /// </summary>
    /// <param name="auditLog">The ban audit log.</param>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="auditLog"/> or <paramref name="eventArgs"/> are <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetBannedUserLog(DiscordAuditLogBanEntry auditLog, GuildBanAddEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="GuildBanRemoveEventArgs"/> event.
    /// </summary>
    /// <param name="auditLog">The unban audit log.</param>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="auditLog"/> or <paramref name="eventArgs"/> are <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetUnbannedUserLog(DiscordAuditLogBanEntry auditLog, GuildBanRemoveEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="GuildRoleCreateEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetCreatedRoleLog(GuildRoleCreateEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="GuildRoleDeleteEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetDeletedRoleLog(GuildRoleDeleteEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="GuildRoleUpdateEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetEditedRoleLog(GuildRoleUpdateEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="ChannelCreateEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetCreatedChannelLog(ChannelCreateEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="ChannelDeleteEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetDeletedChannelLog(ChannelDeleteEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="ChannelUpdateEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetEditedChannelLog(ChannelUpdateEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="ThreadCreateEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetCreatedThreadLog(ThreadCreateEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="ThreadDeleteEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetDeletedThreadLog(ThreadDeleteEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="ThreadUpdateEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetEditedThreadLog(ThreadUpdateEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="VoiceStateUpdateEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentException">Occurs when the voice state is not valid for logging.</exception>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetVoiceStateLog(VoiceStateUpdateEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="GuildMemberAddEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetJoiningMemberLog(GuildMemberAddEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="GuildMemberRemoveEventArgs"/> event.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetLeavingMemberLog(GuildMemberRemoveEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="GuildMemberAddEventArgs"/> event when the user is an alt.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetJoiningAltLog(GuildMemberAddEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="GuildMemberRemoveEventArgs"/> event when the user is an alt.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetLeavingAltLog(GuildMemberRemoveEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="GuildMemberUpdateEventArgs"/> event when a role is assigned or revoked from a user.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentException">Occurs when there was no change in roles.</exception>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetRoleChangeLog(GuildMemberUpdateEventArgs eventArgs);

    /// <summary>
    /// Generates a log message for a <see cref="GuildMemberUpdateEventArgs"/> event when a user changes their nickname.
    /// </summary>
    /// <param name="user">The user who got updated.</param>
    /// <param name="serverId">The ID of the Discord guild.</param>
    /// <param name="oldName">The user's previous name.</param>
    /// <param name="newName">The user's current name.</param>
    /// <param name="logTitle">The title of the log. Localizable.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentException">Occurs when there was no change in nickname.</exception>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetNameChangeLog(DiscordUser user, ulong serverId, string? oldName, string? newName, string logTitle);

    /// <summary>
    /// Generates a log message for a <see cref="MessageUpdateEventArgs"/> event when a message gets pinned or unpinned.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The guild log message.</returns>
    /// <exception cref="ArgumentException">Occurs when the Guild in <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
    DiscordWebhookBuilder GetMessagePinLog(MessageUpdateEventArgs eventArgs);
}