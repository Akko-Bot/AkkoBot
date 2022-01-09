using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Utilities.Services;
using AkkoCore.Config.Models;
using AkkoCore.Enums;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events;

/// <summary>
/// Handles guild log events.
/// </summary>
[CommandService<IGuildLogEventHandler>(ServiceLifetime.Singleton)]
internal sealed class GuildLogEventHandler : IGuildLogEventHandler
{
    private readonly TimeSpan _24hours = TimeSpan.FromDays(1);
    private readonly EventId _guildLogEvent = new(98, nameof(GuildLogEventHandler));

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGuildLogGenerator _logGenerator;
    private readonly IAkkoCache _akkoCache;
    private readonly IDbCache _dbCache;
    private readonly IEqualityComparer<DiscordEmbed> _embedComparer;
    private readonly BotConfig _botConfig;
    private readonly DiscordWebhookClient _webhookClient;
    private readonly UtilitiesService _utilities;

    public GuildLogEventHandler(IServiceScopeFactory scopeFactory, IGuildLogGenerator logGenerator, IAkkoCache akkoCache, IDbCache dbCache,
        IEqualityComparer<DiscordEmbed> embedComparer, BotConfig botConfig, DiscordWebhookClient webhookClient, UtilitiesService utilities)
    {
        _scopeFactory = scopeFactory;
        _logGenerator = logGenerator;
        _akkoCache = akkoCache;
        _dbCache = dbCache;
        _embedComparer = embedComparer;
        _botConfig = botConfig;
        _webhookClient = webhookClient;
        _utilities = utilities;
    }

    public Task CacheMessageOnCreationAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Message.Author?.IsBot is not false
            || !TryGetGuildLogs(eventArgs.Guild.Id, GuildLogType.MessageEvents, out var guildLogs) || !guildLogs.Any(x => x.IsActive))
            return Task.CompletedTask;        

        if (!_akkoCache.GuildMessageCache.TryGetValue(eventArgs.Guild.Id, out var messageCache))
        {
            messageCache = new(_botConfig.MessageSizeCache);
            _akkoCache.GuildMessageCache.TryAdd(eventArgs.Guild.Id, messageCache);
        }

        messageCache.Add(eventArgs.Message);

        return Task.CompletedTask;
    }

    public Task LogPinnedMessageAsync(DiscordClient client, MessageUpdateEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.MessagePinned, out var guildLog) || !guildLog.IsActive
            || eventArgs.MessageBefore?.Pinned == eventArgs.Message.Pinned
            || eventArgs.MessageBefore?.Content.Equals(eventArgs.Message.Content, StringComparison.Ordinal) is not true
            || eventArgs.MessageBefore?.Embeds.Any(x => !eventArgs.Message.Embeds.Contains(x, _embedComparer)) is true)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetMessagePinLog(eventArgs));
    }

    public Task LogUpdatedMessageAsync(DiscordClient client, MessageUpdateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Message.Author?.IsBot is not false
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.MessageUpdated, out var guildLog)
            || !guildLog.IsActive
            || eventArgs.MessageBefore?.Content.Equals(eventArgs.Message.Content, StringComparison.Ordinal) is true  // This check is needed because pins trigger this event

            || (eventArgs.Message.Author is DiscordMember member && IsIgnoredContext(eventArgs.Guild.Id, member.Roles.Select(x => x.Id).Append(eventArgs.Author.Id).Append(eventArgs.Channel.Id))))
            return Task.CompletedTask;

        // Cache uncached edited messages, but don't log them.
        if (eventArgs.MessageBefore is null)
        {
            CacheNewMessage(eventArgs.Guild.Id, eventArgs.Message);
            return Task.CompletedTask;
        }

        return DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetMessageUpdateLog(eventArgs));
    }

    

    public async Task LogDeletedMessageAsync(DiscordClient client, MessageDeleteEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Message.Author?.IsBot is not false
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.MessageDeleted, out var guildLog)
            || !guildLog.IsActive
            || !_akkoCache.GuildMessageCache.TryGetValue(eventArgs.Guild.Id, out var messageCache)
            || !messageCache.TryGetValue(x => x.Id == eventArgs.Message.Id, out var message)
            || (eventArgs.Message.Author is DiscordMember member && IsIgnoredContext(eventArgs.Guild.Id, member.Roles.Select(x => x.Id).Append(eventArgs.Message.Author.Id).Append(eventArgs.Channel.Id))))
            return;

        await DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetMessageDeleteLog(message)).ConfigureAwait(false);

        // Remove from the cache
        messageCache.Remove(x => x?.Id == eventArgs.Message.Id);
    }

    public async Task LogBulkDeletedMessagesAsync(DiscordClient client, MessageBulkDeleteEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Messages.All(x => x.Author?.IsBot is not false)
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.MessageBulkDeleted, out var guildLog)
            || !guildLog.IsActive
            || !_akkoCache.GuildMessageCache.TryGetValue(eventArgs.Guild.Id, out var messageCache)
            || IsIgnoredContext(eventArgs.Guild.Id, eventArgs.Channel.Id))
            return;

        using var stream = new MemoryStream();
        var firstDeletedTime = eventArgs.Messages.Min(x => x.CreationTimestamp);

        var messages = messageCache
            .Where(x => x?.ChannelId == eventArgs.Channel.Id && x.CreationTimestamp >= firstDeletedTime)
            .OrderBy(x => x.CreationTimestamp);

        await DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetMessageBulkDeleteLog(messages, stream, eventArgs)).ConfigureAwait(false);

        // Remove from the cache
        messageCache.Remove(x => x?.Id == eventArgs.Channel.Id && x.CreationTimestamp >= firstDeletedTime);
    }

    public Task LogEmojiCreateAsync(DiscordClient client, GuildEmojisUpdateEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || eventArgs.GetStatus() is not EmojiActivity.Created
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.EmojiCreated, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetEmojiLog(eventArgs));
    }

    public Task LogEmojiUpdateAsync(DiscordClient client, GuildEmojisUpdateEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || eventArgs.GetStatus() is not EmojiActivity.Updated
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.EmojiUpdated, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetEmojiLog(eventArgs));
    }

    public Task LogEmojiDeleteAsync(DiscordClient client, GuildEmojisUpdateEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || eventArgs.GetStatus() is not EmojiActivity.Deleted
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.EmojiDeleted, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetEmojiLog(eventArgs));
    }

    public Task LogCreatedInviteAsync(DiscordClient client, InviteCreateEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.InviteCreated, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetCreatedInviteLog(eventArgs));
    }

    public Task LogDeletedInviteAsync(DiscordClient client, InviteDeleteEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.InviteDeleted, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetDeletedInviteLog(eventArgs));
    }

    public async Task LogBannedUserAsync(DiscordClient client, GuildBanAddEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ViewAuditLog))
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.UserBanned, out var guildLog) || !guildLog.IsActive)
            return;

        var auditLog = (await eventArgs.Guild.GetAuditLogsAsync(1, null, AuditLogActionType.Ban).ConfigureAwait(false))[0];

        if (auditLog is DiscordAuditLogBanEntry banLog && banLog.Target.Id == eventArgs.Member.Id)
            await DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetBannedUserLog(banLog, eventArgs)).ConfigureAwait(false);
    }

    public async Task LogUnbannedUserAsync(DiscordClient client, GuildBanRemoveEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ViewAuditLog))
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.UserUnbanned, out var guildLog) || !guildLog.IsActive)
            return;

        var auditLog = (await eventArgs.Guild.GetAuditLogsAsync(1, null, AuditLogActionType.Unban).ConfigureAwait(false))[0];

        if (auditLog is DiscordAuditLogBanEntry unbanLog && unbanLog.Target.Id == eventArgs.Member.Id)
            await DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetUnbannedUserLog(unbanLog, eventArgs)).ConfigureAwait(false);
    }

    public Task LogCreatedRoleAsync(DiscordClient client, GuildRoleCreateEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.RoleCreated, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetCreatedRoleLog(eventArgs));
    }

    public Task LogDeletedRoleAsync(DiscordClient client, GuildRoleDeleteEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.RoleDeleted, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetDeletedRoleLog(eventArgs));
    }

    public Task LogEditedRoleAsync(DiscordClient client, GuildRoleUpdateEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.RoleUpdated, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetEditedRoleLog(eventArgs));
    }

    public Task LogMemberRoleAssignmentAsync(DiscordClient client, GuildMemberUpdateEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || eventArgs.RolesBefore.Count == eventArgs.RolesAfter.Count
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.RoleAssigned, out var guildLog) || !guildLog.IsActive
            || eventArgs.RolesAfter.Count <= eventArgs.RolesBefore.Count)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetRoleChangeLog(eventArgs));
    }

    public Task LogMemberRoleRevokeAsync(DiscordClient client, GuildMemberUpdateEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || eventArgs.RolesBefore.Count == eventArgs.RolesAfter.Count
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.RoleRevoked, out var guildLog) || !guildLog.IsActive
            || eventArgs.RolesBefore.Count <= eventArgs.RolesAfter.Count)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetRoleChangeLog(eventArgs));
    }

    public Task LogCreatedChannelAsync(DiscordClient client, ChannelCreateEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.ChannelCreated, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetCreatedChannelLog(eventArgs));
    }

    public Task LogDeletedChannelAsync(DiscordClient client, ChannelDeleteEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.ChannelDeleted, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetDeletedChannelLog(eventArgs));
    }

    public Task LogEditedChannelAsync(DiscordClient client, ChannelUpdateEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.ChannelUpdated, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetEditedChannelLog(eventArgs));
    }

    public Task LogVoiceStateConnectionAsync(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        return (eventArgs.Before == eventArgs.After || eventArgs.Guild is null || eventArgs.GetVoiceState() is not UserVoiceState.Connected
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.VoiceEvents, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetVoiceStateLog(eventArgs));
    }

    public Task LogVoiceStateMoveAsync(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        return (eventArgs.Before == eventArgs.After || eventArgs.Guild is null || eventArgs.GetVoiceState() is not UserVoiceState.Moved
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.VoiceEvents, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetVoiceStateLog(eventArgs));
    }

    public Task LogVoiceStateDisconnectionAsync(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        return (eventArgs.Before == eventArgs.After || eventArgs.Guild is null || eventArgs.GetVoiceState() is not UserVoiceState.Disconnected
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.VoiceEvents, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetVoiceStateLog(eventArgs));
    }

    public Task LogJoiningMemberAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.UserJoined, out var guildLog) || !guildLog.IsActive
            || IsAltEventsEnabled(eventArgs.Guild.Id, eventArgs.Member))
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetJoiningMemberLog(eventArgs));
    }

    public Task LogLeavingMemberAsync(DiscordClient client, GuildMemberRemoveEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.UserLeft, out var guildLog) || !guildLog.IsActive
            || IsAltEventsEnabled(eventArgs.Guild.Id, eventArgs.Member))
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetLeavingMemberLog(eventArgs));
    }

    public Task LogJoiningAltAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.AltJoined, out var guildLog) || !guildLog.IsActive
            || !IsAltEventsEnabled(eventArgs.Guild.Id, eventArgs.Member))
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetJoiningAltLog(eventArgs));
    }

    public Task LogLeavingAltAsync(DiscordClient client, GuildMemberRemoveEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.AltLeft, out var guildLog) || !guildLog.IsActive
            || !IsAltEventsEnabled(eventArgs.Guild.Id, eventArgs.Member))
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () => _logGenerator.GetLeavingAltLog(eventArgs));
    }

    public Task LogMemberNicknameChangeAsync(DiscordClient client, GuildMemberUpdateEventArgs eventArgs)
    {
        return (eventArgs.Guild is null || eventArgs.NicknameBefore == eventArgs.NicknameAfter
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.NicknameChanged, out var guildLog) || !guildLog.IsActive)
            ? Task.CompletedTask
            : DispatchLogAsync(client, eventArgs.Guild, guildLog, () =>
                _logGenerator.GetNameChangeLog(
                    eventArgs.Member,
                    eventArgs.Guild.Id,
                    eventArgs.NicknameBefore,
                    eventArgs.NicknameAfter,
                    "nickname_changed"
                )
            );
    }

    /// <summary>
    /// Determines if the specified user is an alt.
    /// </summary>
    /// <param name="user">The user to be analysed.</param>
    /// <param name="gatekeep">The gatekeep settings.</param>
    /// <returns><see langword="true"/> is the user is an alt, <see langword="false"/> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsAlt(DiscordUser user, GatekeepEntity? gatekeep)
        => DateTimeOffset.Now.Subtract(user.CreationTimestamp) < (gatekeep?.AntiAltTime ?? _24hours);

    /// <summary>
    /// Checks if the provided ids are from an ignored context.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <param name="ids">The IDs to be checked.</param>
    /// <returns><see langword="true"/> if at least one of the IDs is ignored or if the guild is not cached, <see langword="false"/> otherwise.</returns>
    private bool IsIgnoredContext(ulong sid, params ulong[] ids)
        => IsIgnoredContext(sid, ids.AsEnumerable());

    /// <summary>
    /// Checks if the provided ids are from an ignored context.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <param name="ids">The IDs to be checked.</param>
    /// <returns><see langword="true"/> if at least one of the IDs is ignored or if the guild is not cached, <see langword="false"/> otherwise.</returns>
    private bool IsIgnoredContext(ulong sid, IEnumerable<ulong> ids)
    {
        if (!_dbCache.Guilds.TryGetValue(sid, out var dbGuild))
            return true;

        foreach (long id in ids)
        {
            if (dbGuild.GuildLogBlacklist.Contains(id))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if <paramref name="user"/> is an alt and if AltEvents are enabled for the guild of the specified ID.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <param name="user">The Discord user.</param>
    /// <returns><see langword="true"/> if the user is an alt and AltEvents are enabled, <see langword="false"/> otherwise.</returns>
    private bool IsAltEventsEnabled(ulong sid, DiscordMember user)
    {
        _dbCache.Gatekeeping.TryGetValue(sid, out var gatekeep);
        return TryGetGuildLog(sid, GuildLogType.AltEvents, out var altLog) && altLog.IsActive && IsAlt(user, gatekeep);
    }

    /// <summary>
    /// Gets the guildlog for the specified Discord guild.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <param name="logType">The type of guild log to get.</param>
    /// <param name="guildLog">The resulting guild log.</param>
    /// <returns><see langword="true"/> if the guild log was found, <see langword="false"/> otherwise.</returns>
    private bool TryGetGuildLog(ulong sid, GuildLogType logType, [MaybeNullWhen(false)] out GuildLogEntity guildLog)
    {
        _dbCache.GuildLogs.TryGetValue(sid, out var guildLogs);
        guildLog = guildLogs?.FirstOrDefault(x => logType.HasFlag(x.Type));

        return guildLog is not null;
    }

    /// <summary>
    /// Gets the guildlogs for the specified Discord guild.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <param name="logType">The type of guild log to get.</param>
    /// <param name="guildLogs">The resulting guild logs.</param>
    /// <returns><see langword="true"/> if at least one guild log was found, <see langword="false"/> otherwise.</returns>
    private bool TryGetGuildLogs(ulong sid, GuildLogType logType, out IEnumerable<GuildLogEntity> guildLogs)
    {
        _dbCache.GuildLogs.TryGetValue(sid, out var dbGuildLogs);
        guildLogs = dbGuildLogs?.Where(x => logType.HasFlag(x.Type))
            ?? Enumerable.Empty<GuildLogEntity>();

        return guildLogs.Any();
    }

    /// <summary>
    /// Sends a guild log to its appropriate webhook.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="server">The Discord guild the webhook is from.</param>
    /// <param name="guildLog">The guild log settings.</param>
    /// <param name="responseFactory">The method responsible for generating the log message.</param>
    /// <returns><see langword="true"/> if the log was sent, <see langword="false"/> otherwise.</returns>
    private async Task<bool> DispatchLogAsync(DiscordClient client, DiscordGuild server, GuildLogEntity guildLog, Func<DiscordWebhookBuilder> responseFactory)
    {
        var webhook = await GetWebhookAsync(client, server, guildLog).ConfigureAwait(false);

        if (webhook is null)
            return false;

        await webhook.ExecuteAsync(responseFactory()).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Caches a new message. If the message cache doesn't exist for the provided
    /// <paramref name="sid"/>, it creates one.
    /// </summary>
    /// <param name="sid">The Id of the Discord guild.</param>
    /// <param name="message">The message to be cached.</param>
    private void CacheNewMessage(ulong sid, DiscordMessage message)
    {
        if (!_akkoCache.GuildMessageCache.TryGetValue(sid, out var messageCache))
        {
            messageCache = new(_botConfig.MessageSizeCache);
            _akkoCache.GuildMessageCache.TryAdd(sid, messageCache);
        }

        messageCache.Add(message);
    }

    /// <summary>
    /// Gets the webhook for the specified guild log.
    /// </summary>
    /// <param name="client">The Discord client with access to the webhook.</param>
    /// <param name="server">The Discord guild where the webhook is.</param>
    /// <param name="guildLog">The guild log to be processed.</param>
    /// <returns>The log's <see cref="DiscordWebhook"/> or <see langword="null"/> if the channel associated with the log got deleted.</returns>
    /// <exception cref="ArgumentException">Occurs when the IDs of the Discord guild and the guild log don't match.</exception>
    private async ValueTask<DiscordWebhook?> GetWebhookAsync(DiscordClient client, DiscordGuild server, GuildLogEntity guildLog)
    {
        if (server.Id != guildLog.GuildIdFK)
            throw new ArgumentException("Guild ID and guild log ID cannot differ.");

        // If channel was deleted, remove the guild log
        if (!server.Channels.TryGetValue(guildLog.ChannelId, out var channel))
        {
            client.Logger.LogWarning(_guildLogEvent, $"The channel for a \"{guildLog.Type}\" guild log was deleted. Removing the guild log from the database.");

            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            // Remove guild log from the database
            await db.GuildLogs.DeleteAsync(x => x.GuildIdFK == server.Id && x.ChannelId == guildLog.ChannelId).ConfigureAwait(false);

            // Remove guild log from the cache
            if (_dbCache.GuildLogs.TryGetValue(server.Id, out var guildLogs) && guildLogs.TryRemove(guildLog) && guildLogs.Count is 0)
                _dbCache.GuildLogs.TryRemove(server.Id, out _);

            // Remove logged messages
            if (guildLog.Type is GuildLogType.MessageEvents && _akkoCache.GuildMessageCache.TryRemove(server.Id, out var messageCache))
                messageCache.Clear();

            return default;
        }

        try
        {
            return _webhookClient.GetRegisteredWebhook(guildLog.WebhookId)
                ?? await _webhookClient.AddWebhookAsync(guildLog.WebhookId, client).ConfigureAwait(false);
        }
        catch
        {
            client.Logger.LogWarning(_guildLogEvent, $"The webhook for a \"{guildLog.Type}\" guild log was deleted and is being recreated.");

            // Create a webhook and cache it
            using var avatar = await _utilities.GetOnlineStreamAsync(client.CurrentUser.AvatarUrl ?? client.CurrentUser.DefaultAvatarUrl).ConfigureAwait(false);
            var webhook = await channel.CreateWebhookAsync(_botConfig.WebhookLogName, avatar).ConfigureAwait(false);
            _webhookClient.AddWebhook(webhook);

            // Update the guild log
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            // Update the database entry
            await db.GuildLogs.UpdateAsync(
                x => x.GuildIdFK == server.Id,
                _ => new() { WebhookId = webhook.Id }
            ).ConfigureAwait(false);

            // Update the cached guild log
            guildLog.WebhookId = webhook.Id;

            return webhook;
        }
    }
}