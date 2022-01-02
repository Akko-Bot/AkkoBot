using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Utilities.Services;
using AkkoCore.Config.Models;
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
    private readonly BotConfig _botConfig;
    private readonly DiscordWebhookClient _webhookClient;
    private readonly UtilitiesService _utilities;

    public GuildLogEventHandler(IServiceScopeFactory scopeFactory, IGuildLogGenerator logGenerator, IAkkoCache akkoCache, IDbCache dbCache,
        BotConfig botConfig, DiscordWebhookClient webhookClient, UtilitiesService utilities)
    {
        _scopeFactory = scopeFactory;
        _logGenerator = logGenerator;
        _akkoCache = akkoCache;
        _dbCache = dbCache;
        _botConfig = botConfig;
        _webhookClient = webhookClient;
        _utilities = utilities;
    }

    public Task CacheMessageOnCreationAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Message.Author?.IsBot is not false
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.MessageEvents, out var guildLog) || !guildLog!.IsActive)
            return Task.CompletedTask;

        if (!_akkoCache.GuildMessageCache.TryGetValue(eventArgs.Guild.Id, out var messageCache))
        {
            messageCache = new(_botConfig.MessageSizeCache);
            _akkoCache.GuildMessageCache.TryAdd(eventArgs.Guild.Id, messageCache);
        }

        messageCache.Add(eventArgs.Message);

        return Task.CompletedTask;
    }

    public async Task LogUpdatedMessageAsync(DiscordClient client, MessageUpdateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Message.Author?.IsBot is not false
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.MessageEvents, out var guildLog)
            || !guildLog!.IsActive
            || eventArgs.MessageBefore?.Content.Equals(eventArgs.Message.Content, StringComparison.Ordinal) is true  // This check is needed because pins trigger this event

            || (eventArgs.Message.Author is DiscordMember member && IsIgnoredContext(eventArgs.Guild.Id, member.Roles.Select(x => x.Id).Append(eventArgs.Author.Id).Append(eventArgs.Channel.Id))))
            return;

        // Cache uncached edited messages, but don't log them.
        if (eventArgs.MessageBefore is null)
        {
            if (!_akkoCache.GuildMessageCache.TryGetValue(eventArgs.Guild.Id, out var messageCache))
            {
                messageCache = new(_botConfig.MessageSizeCache) { eventArgs.Message };
                _akkoCache.GuildMessageCache.TryAdd(eventArgs.Guild.Id, messageCache);
            }

            messageCache.Add(eventArgs.Message);
            return;
        }

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetMessageUpdateLog(eventArgs)).ConfigureAwait(false);
    }

    public async Task LogDeletedMessageAsync(DiscordClient client, MessageDeleteEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Message.Author?.IsBot is not false
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.MessageEvents, out var guildLog)
            || !guildLog!.IsActive
            || !_akkoCache.GuildMessageCache.TryGetValue(eventArgs.Guild.Id, out var messageCache)
            || !messageCache.TryGetValue(x => x.Id == eventArgs.Message.Id, out var message)
            || (eventArgs.Message.Author is DiscordMember member && IsIgnoredContext(eventArgs.Guild.Id, member.Roles.Select(x => x.Id).Append(eventArgs.Message.Author.Id).Append(eventArgs.Channel.Id))))
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetMessageDeleteLog(message)).ConfigureAwait(false);

        // Remove from the cache
        messageCache.Remove(x => x?.Id == eventArgs.Message.Id);
    }

    public async Task LogBulkDeletedMessagesAsync(DiscordClient client, MessageBulkDeleteEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Messages.All(x => x.Author?.IsBot is not false)
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.MessageEvents, out var guildLog)
            || !guildLog!.IsActive
            || !_akkoCache.GuildMessageCache.TryGetValue(eventArgs.Guild.Id, out var messageCache)
            || IsIgnoredContext(eventArgs.Guild.Id, eventArgs.Channel.Id))
            return;

        using var stream = new MemoryStream();
        var firstDeletedTime = eventArgs.Messages.Min(x => x.CreationTimestamp);

        var messages = messageCache
            .Where(x => x?.ChannelId == eventArgs.Channel.Id && x.CreationTimestamp >= firstDeletedTime)
            .OrderBy(x => x.CreationTimestamp);

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetMessageBulkDeleteLog(messages, stream, eventArgs)).ConfigureAwait(false);

        // Remove from the cache
        messageCache.Remove(x => x?.Id == eventArgs.Channel.Id && x.CreationTimestamp >= firstDeletedTime);
    }

    public async Task LogEmojiUpdateAsync(DiscordClient client, GuildEmojisUpdateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.EmojiEvents, out var guildLog)
            || !guildLog!.IsActive)
            return;

        var target = (eventArgs.EmojisBefore.Count > eventArgs.EmojisAfter.Count)
            ? eventArgs.EmojisAfter.Values
            : eventArgs.EmojisBefore.Values;

        var emoji = eventArgs.EmojisAfter.Values
            .Concat(eventArgs.EmojisBefore.Values)
            .Except(target)
            .First();

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
        {
            await webhook.ExecuteAsync(
                _logGenerator.GetEmojiUpdateLog(
                    eventArgs.Guild,
                    emoji,
                    eventArgs.EmojisBefore.Count - eventArgs.EmojisAfter.Count,
                    eventArgs.EmojisBefore.Values.FirstOrDefault(x => x.Id == emoji.Id)?.Name
                )
            ).ConfigureAwait(false);
        }
    }

    public async Task LogCreatedInviteAsync(DiscordClient client, InviteCreateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.InviteEvents, out var guildLog)
            || !guildLog!.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetCreatedInviteLog(eventArgs)).ConfigureAwait(false);
    }

    public async Task LogDeletedInviteAsync(DiscordClient client, InviteDeleteEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.InviteEvents, out var guildLog)
            || !guildLog!.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetDeletedInviteLog(eventArgs)).ConfigureAwait(false);
    }

    public async Task LogBannedUserAsync(DiscordClient client, GuildBanAddEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ViewAuditLog))
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.BanEvents, out var guildLog) || !guildLog!.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);
        var auditLog = (await eventArgs.Guild.GetAuditLogsAsync(1, null, AuditLogActionType.Ban).ConfigureAwait(false))[0];

        if (auditLog is DiscordAuditLogBanEntry banLog && webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetBannedUserLog(banLog, eventArgs)).ConfigureAwait(false);
    }

    public async Task LogUnbannedUserAsync(DiscordClient client, GuildBanRemoveEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ViewAuditLog))
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.BanEvents, out var guildLog) || !guildLog!.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);
        var auditLog = (await eventArgs.Guild.GetAuditLogsAsync(1, null, AuditLogActionType.Unban).ConfigureAwait(false))[0];

        if (auditLog is DiscordAuditLogBanEntry unbanLog && webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetUnbannedUserLog(unbanLog, eventArgs)).ConfigureAwait(false);
    }

    public async Task LogCreatedRoleAsync(DiscordClient client, GuildRoleCreateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.RoleEvents, out var guildLog)
            || !guildLog!.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetCreatedRoleLog(eventArgs)).ConfigureAwait(false);
    }

    public async Task LogDeletedRoleAsync(DiscordClient client, GuildRoleDeleteEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.RoleEvents, out var guildLog)
            || !guildLog!.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetDeletedRoleLog(eventArgs)).ConfigureAwait(false);
    }

    public async Task LogEditedRoleAsync(DiscordClient client, GuildRoleUpdateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.RoleEvents, out var guildLog)
            || !guildLog!.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetEditedRoleLog(eventArgs)).ConfigureAwait(false);
    }

    public async Task LogCreatedChannelAsync(DiscordClient client, ChannelCreateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.ChannelEvents, out var guildLog)
            || !guildLog!.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetCreatedChannelLog(eventArgs)).ConfigureAwait(false);
    }

    public async Task LogDeletedChannelAsync(DiscordClient client, ChannelDeleteEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.ChannelEvents, out var guildLog)
            || !guildLog!.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetDeletedChannelLog(eventArgs)).ConfigureAwait(false);
    }

    public async Task LogEditedChannelAsync(DiscordClient client, ChannelUpdateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.ChannelEvents, out var guildLog)
            || !guildLog!.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetEditedChannelLog(eventArgs)).ConfigureAwait(false);
    }

    public async Task LogVoiceStateAsync(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
    {
        if (eventArgs.Before == eventArgs.After || eventArgs.Guild is null
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.VoiceEvents, out var guildLog) || !guildLog!.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            await webhook.ExecuteAsync(_logGenerator.GetVoiceStateLog(eventArgs)).ConfigureAwait(false);
    }

    public async Task LogJoiningMemberAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.MemberEvents, out var guildLog) || !guildLog.IsActive
            || IsAltEventsEnabled(eventArgs.Guild.Id, eventArgs.Member))
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            _ = webhook.ExecuteAsync(_logGenerator.GetJoiningMemberLog(eventArgs));
    }

    public async Task LogLeavingMemberAsync(DiscordClient client, GuildMemberRemoveEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.MemberEvents, out var guildLog) || !guildLog.IsActive
            || IsAltEventsEnabled(eventArgs.Guild.Id, eventArgs.Member))
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            _ = webhook.ExecuteAsync(_logGenerator.GetLeavingMemberLog(eventArgs));
    }

    public async Task LogJoiningAltAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.AltEvents, out var guildLog) || !guildLog.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            _ = webhook.ExecuteAsync(_logGenerator.GetJoiningAltLog(eventArgs));
    }

    public async Task LogLeavingAltAsync(DiscordClient client, GuildMemberRemoveEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.AltEvents, out var guildLog) || !guildLog.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            _ = webhook.ExecuteAsync(_logGenerator.GetLeavingAltLog(eventArgs));
    }

    public async Task LogMemberRoleChangeAsync(DiscordClient client, GuildMemberUpdateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.RolesBefore.Count == eventArgs.RolesAfter.Count
            || !TryGetGuildLog(eventArgs.Guild.Id, GuildLogType.RoleEvents, out var guildLog) || !guildLog.IsActive)
            return;

        var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog).ConfigureAwait(false);

        if (webhook is not null)
            _ = webhook.ExecuteAsync(_logGenerator.GetRoleChangeLog(eventArgs));
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
    private bool TryGetGuildLogs(ulong sid, GuildLogType logType, out IReadOnlyList<GuildLogEntity> guildLogs)
    {
        _dbCache.GuildLogs.TryGetValue(sid, out var dbGuildLogs);
        guildLogs = dbGuildLogs?.Where(x => logType.HasFlag(x.Type)).ToArray()
            ?? Array.Empty<GuildLogEntity>();

        return guildLogs.Count is not 0;
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