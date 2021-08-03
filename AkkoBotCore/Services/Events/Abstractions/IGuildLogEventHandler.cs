﻿using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events.Abstractions
{
    /// <summary>
    /// Represents an object that handles guild log events.
    /// </summary>
    public interface IGuildLogEventHandler
    {
        /// <summary>
        /// Caches the created message if the guild is logging message deletes.
        /// </summary>
        Task CacheMessageOnCreationAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

        /// <summary>
        /// Logs a deleted message.
        /// </summary>
        Task LogDeletedMessageAsync(DiscordClient client, MessageDeleteEventArgs eventArgs);

        /// <summary>
        /// Logs an edited message.
        /// </summary>
        Task LogUpdatedMessageAsync(DiscordClient client, MessageUpdateEventArgs eventArgs);

        /// <summary>
        /// Logs messages deleted in bulk.
        /// </summary>
        Task LogBulkDeletedMessagesAsync(DiscordClient client, MessageBulkDeleteEventArgs eventArgs);

        /// <summary>
        /// Logs addition, edit or removal of a guild's emoji.
        /// </summary>
        Task LogEmojiUpdateAsync(DiscordClient client, GuildEmojisUpdateEventArgs eventArgs);

        /// <summary>
        /// Logs invite creation.
        /// </summary>
        Task LogCreatedInviteAsync(DiscordClient client, InviteCreateEventArgs eventArgs);

        /// <summary>
        /// Logs invite deletion.
        /// </summary>
        Task LogDeletedInviteAsync(DiscordClient client, InviteDeleteEventArgs eventArgs);

        /// <summary>
        /// Logs user ban.
        /// </summary>
        Task LogBannedUserAsync(DiscordClient client, GuildBanAddEventArgs eventArgs);

        /// <summary>
        /// Logs user unban
        /// </summary>
        Task LogUnbannedUserAsync(DiscordClient client, GuildBanRemoveEventArgs eventArgs);
    }
}