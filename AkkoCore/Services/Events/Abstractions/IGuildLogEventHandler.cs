using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Abstractions;

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

    /// <summary>
    /// Logs role creation.
    /// </summary>
    Task LogCreatedRoleAsync(DiscordClient client, GuildRoleCreateEventArgs eventArgs);

    /// <summary>
    /// Logs role deletion.
    /// </summary>
    Task LogDeletedRoleAsync(DiscordClient client, GuildRoleDeleteEventArgs eventArgs);

    /// <summary>
    /// Logs role edition.
    /// </summary>
    Task LogEditedRoleAsync(DiscordClient client, GuildRoleUpdateEventArgs eventArgs);

    /// <summary>
    /// Logs channel creation.
    /// </summary>
    Task LogCreatedChannelAsync(DiscordClient client, ChannelCreateEventArgs eventArgs);

    /// <summary>
    /// Logs channel deletion.
    /// </summary>
    Task LogDeletedChannelAsync(DiscordClient client, ChannelDeleteEventArgs eventArgs);

    /// <summary>
    /// Logs channel edition.
    /// </summary>
    Task LogEditedChannelAsync(DiscordClient client, ChannelUpdateEventArgs eventArgs);

    /// <summary>
    /// Logs voice state updates.
    /// </summary>
    Task LogVoiceStateAsync(DiscordClient client, VoiceStateUpdateEventArgs eventArgs);

    /// <summary>
    /// Logs user join.
    /// </summary>
    Task LogJoiningMemberAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs);

    /// <summary>
    /// Logs user leave.
    /// </summary>
    Task LogLeavingMemberAsync(DiscordClient client, GuildMemberRemoveEventArgs eventArgs);

    /// <summary>
    /// Logs alt joins.
    /// </summary>
    Task LogJoiningAltAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs);

    /// <summary>
    /// Logs alt leaves.
    /// </summary>
    Task LogLeavingAltAsync(DiscordClient client, GuildMemberRemoveEventArgs eventArgs);

    /// <summary>
    /// Logs role assignments and removals.
    /// </summary>
    Task LogMemberRoleChangeAsync(DiscordClient client, GuildMemberUpdateEventArgs eventArgs);
}