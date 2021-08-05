﻿using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;

namespace AkkoBot.Services.Events.Abstractions
{
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
        /// <param name="server">The server the event took place.</param>
        /// <param name="emoji">The emoji that got added/edited/deleted.</param>
        /// <param name="action">
        /// Amount of emojis before minus amount of emojis after. Less than 0 means the emoji was added, more than 0 means the emoji was deleted and 0 means the emoji was modified.
        /// </param>
        /// <param name="oldEmojiName">The previous name of the emoji - only relevant if the emoji was modified.</param>
        /// <returns></returns>
        /// <returns>The guild log message.</returns>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="server"/> or <paramref name="emoji"/> are <see langword="null"/>.
        /// </exception>
        DiscordWebhookBuilder GetEmojiUpdateLog(DiscordGuild server, DiscordEmoji emoji, int action, string oldEmojiName = null);

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
        /// Generates a log message for a <see cref="VoiceStateUpdateEventArgs"/> event.
        /// </summary>
        /// <param name="eventArgs">The event argument.</param>
        /// <returns>The guild log message.</returns>
        /// <exception cref="ArgumentException">Occurs when the voice state is not valid for logging.</exception>
        /// <exception cref="ArgumentNullException">Occurs when <paramref name="eventArgs"/> is <see langword="null"/>.</exception>
        DiscordWebhookBuilder GetVoiceStateLog(VoiceStateUpdateEventArgs eventArgs);
    }
}