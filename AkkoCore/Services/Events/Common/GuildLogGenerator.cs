using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Config.Abstractions;
using AkkoCore.Config.Models;
using AkkoCore.Enums;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Events.Abstractions;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AkkoCore.Services.Events.Common;

/// <summary>
/// Generates guild log messages.
/// </summary>
[CommandService<IGuildLogGenerator>(ServiceLifetime.Singleton)]
internal sealed class GuildLogGenerator : IGuildLogGenerator
{
    private readonly ILocalizer _localizer;
    private readonly IDbCache _dbCache;
    private readonly GuildLogService _logService;
    private readonly BotConfig _botconfig;

    public GuildLogGenerator(ILocalizer localizer, IDbCache dbCache, GuildLogService logService, BotConfig botconfig)
    {
        _localizer = localizer;
        _dbCache = dbCache;
        _logService = logService;
        _botconfig = botconfig;
    }

    public DiscordWebhookBuilder GetMessageDeleteLog(DiscordMessage message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message), "Deleted message cannot be null.");

        var settings = GetMessageSettings(message.Channel.GuildId);

        var webhookMessage = new SerializableDiscordEmbed()
            .WithColor((message.Author as DiscordMember)?.Color.ToString())
            .WithAuthor("log_message_deleted_title", message.JumpLink.AbsoluteUri)
            .WithTitle(DiscordUserExt.GetFullname(message.Author))
            .WithDescription($"{_localizer.GetResponseString(settings.Locale, "channel")}: {message.Channel.Mention} | {message.Channel.Name}\n\n{message.Content}")
            .AddField("author_mention", message.Author.Mention, true)
            .AddField("deleted_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
            .WithFooter($"{_localizer.GetResponseString(settings.Locale, "id")}: {message.Id}")
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(webhookMessage, settings);
    }

    public DiscordWebhookBuilder GetMessageUpdateLog(MessageUpdateEventArgs eventArgs)
    {
        if (eventArgs.MessageBefore is null)
            throw new ArgumentNullException(nameof(eventArgs), "Previous state of an edited message cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var message = new SerializableDiscordEmbed()
            .WithColor((eventArgs.Message.Author as DiscordMember)?.Color.ToString())
            .WithAuthor("log_message_edited_title", eventArgs.Message.JumpLink.AbsoluteUri)
            .WithTitle(eventArgs.Message.Author.GetFullname())
            .WithDescription(
                $"{_localizer.GetResponseString(settings.Locale, "channel")}: {eventArgs.Message.Channel.Mention} | {eventArgs.Message.Channel.Name}\n\n" +
                $"{_localizer.GetResponseString(settings.Locale, "before")}:\n{eventArgs.MessageBefore.Content}\n\n" +
                $"{_localizer.GetResponseString(settings.Locale, "after")}:\n{eventArgs.Message.Content}"
            )
            .AddField("author_mention", eventArgs.Message.Author.Mention, true)
            .AddField("edited_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
            .WithFooter($"{_localizer.GetResponseString(settings.Locale, "id")}: {eventArgs.Message.Id}")
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetMessageBulkDeleteLog(IEnumerable<DiscordMessage> messages, Stream stream, MessageBulkDeleteEventArgs eventArgs)
    {
        if (messages is null || !messages.Any())
            throw new ArgumentException("Message collection cannot be null or empty.", nameof(messages));
        else if (!stream.CanWrite)
            throw new ArgumentException("Stream must allow writing.", nameof(stream));
        else if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event arguments cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var extraInfo = $"==> {_localizer.GetResponseString(settings.Locale, "log_messages_deleted")}: {eventArgs.Messages.Count}" + Environment.NewLine;
        var fileContent = _logService.GenerateMessageLog(messages, eventArgs.Channel, settings.Locale, extraInfo);

        // The async version is only worth it if you intend to cancel the write
        stream.Write(Encoding.UTF8.GetBytes(fileContent!));
        stream.Position = 0;

        return new DiscordWebhookBuilder()
            .AddFile($"Logs_BulkDelete_{eventArgs.Channel.Name}_{DateTimeOffset.Now}.txt", stream);
    }

    public DiscordWebhookBuilder GetEmojiUpdateLog(DiscordGuild server, DiscordEmoji emoji, int action, string? oldEmojiName = default)
    {
        if (server is null || emoji is null)
            throw new ArgumentNullException((server is null) ? nameof(server) : nameof(emoji), (server is null) ? "Discord guild cannot be null" : "Emoji cannot be null.");

        var settings = GetMessageSettings(server.Id);

        var description = (action is 0 && emoji.Name.Equals(oldEmojiName, StringComparison.Ordinal))
            ? _localizer.GetResponseString(settings.Locale, "log_emoji_edited_simple")
            : action switch
            {
                0 => _localizer.GetResponseString(settings.Locale, "log_emoji_edited"),
                < 0 => _localizer.GetResponseString(settings.Locale, "log_emoji_added"),
                > 0 => _localizer.GetResponseString(settings.Locale, "log_emoji_deleted")
            };

        var message = new SerializableDiscordEmbed()
            .WithColor((action <= 0) ? settings.OkColor : settings.ErrorColor)
            .WithTitle("log_emoji_title")
            .WithThumbnail(emoji.Url)
            .WithDescription(string.Format(description, Formatter.InlineCode(oldEmojiName ?? emoji.Name), Formatter.InlineCode(emoji.Name)))
            .AddField(AkkoConstants.ValidWhitespace, DateTimeOffset.Now.ToDiscordTimestamp())
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetCreatedInviteLog(InviteCreateEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var message = new SerializableDiscordEmbed()
            .WithColor(settings.OkColor)
            .WithTitle("log_invite_created_title")
            .WithDescription(eventArgs.Invite.GetInviteLink())
            .AddField("author", eventArgs.Invite.Inviter.GetFullname(), true)
            .AddField("code", eventArgs.Invite.Code, true)
            .AddField("created_on", eventArgs.Invite.CreatedAt.ToDiscordTimestamp(), true)
            .AddField("channel", (settings.UseEmbed) ? eventArgs.Channel.Mention : $"#{eventArgs.Channel.Name}", true)
            .AddField("invite_temporary", (eventArgs.Invite.IsTemporary) ? AkkoStatics.SuccessEmoji.Name : AkkoStatics.FailureEmoji.Name, true)
            .AddField("expires_on", (eventArgs.Invite.MaxAge is 0) ? "-" : eventArgs.Invite.CreatedAt.AddSeconds(eventArgs.Invite.MaxAge).ToDiscordTimestamp(), true)
            .WithFooter($"{_localizer.FormatLocalized(settings.Locale, "uses_left")}: {((eventArgs.Invite.MaxUses is 0) ? "-" : (eventArgs.Invite.MaxUses - eventArgs.Invite.Uses))}")
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetDeletedInviteLog(InviteDeleteEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var message = new SerializableDiscordEmbed()
            .WithColor(settings.ErrorColor)
            .WithTitle("log_invite_deleted_title")
            .WithDescription(eventArgs.Invite.GetInviteLink())
            .AddField("code", eventArgs.Invite.Code, true)
            .AddField("channel", (settings.UseEmbed) ? eventArgs.Channel.Mention : $"#{eventArgs.Channel.Name}", true)
            .AddField("deleted_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetBannedUserLog(DiscordAuditLogBanEntry auditLog, GuildBanAddEventArgs eventArgs)
    {
        if (auditLog is null)
            throw new ArgumentNullException(nameof(auditLog), "Audit log cannot be null.");
        else if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var message = new SerializableDiscordEmbed()
            .WithColor(settings.ErrorColor)
            .WithTitle("ban_title")
            .WithDescription($"{eventArgs.Member.Mention} | {eventArgs.Member.GetFullname()}")
            .AddField("moderator", auditLog.UserResponsible.GetFullname(), true)
            .AddField("reason", auditLog.Reason, true)
            .AddField("banned_on", DateTimeOffset.Now.ToDiscordTimestamp())
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetUnbannedUserLog(DiscordAuditLogBanEntry auditLog, GuildBanRemoveEventArgs eventArgs)
    {
        if (auditLog is null)
            throw new ArgumentNullException(nameof(auditLog), "Audit log cannot be null.");
        else if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var message = new SerializableDiscordEmbed()
            .WithColor(settings.OkColor)
            .WithTitle("log_unban_title")
            .WithDescription($"{eventArgs.Member.Mention} | {eventArgs.Member.GetFullname()}")
            .AddField("moderator", auditLog.UserResponsible.GetFullname(), true)
            .AddField("reason", auditLog.Reason, true)
            .AddField("unbanned_on", DateTimeOffset.Now.ToDiscordTimestamp())
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetCreatedRoleLog(GuildRoleCreateEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var message = new SerializableDiscordEmbed()
            .WithColor(settings.OkColor)
            .WithTitle("log_rolecreated_title")
            .AddField("name", eventArgs.Role.Name, true)
            .AddField("id", eventArgs.Role.Id.ToString(), true)
            .AddField("created_on", DateTimeOffset.Now.ToDiscordTimestamp())
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetDeletedRoleLog(GuildRoleDeleteEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var message = new SerializableDiscordEmbed()
            .WithColor(settings.ErrorColor)
            .WithTitle("log_roledeleted_title")
            .AddField("name", eventArgs.Role.Name, true)
            .AddField("id", eventArgs.Role.Id.ToString(), true)
            .AddField("deleted_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
            .AddField("permissions", string.Join(", ", eventArgs.Role.Permissions.ToLocalizedStrings(_localizer, settings.Locale)))
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetEditedRoleLog(GuildRoleUpdateEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var message = new SerializableDiscordEmbed()
            .WithColor(settings.OkColor)
            .WithTitle("log_roleedited_title");

        if (eventArgs.RoleBefore.Name.Equals(eventArgs.RoleAfter.Name, StringComparison.Ordinal))
            message.AddField("name", eventArgs.RoleAfter.Name, true);
        else
        {
            message.AddField("old_name", eventArgs.RoleBefore.Name, true)
                .AddField("new_name", eventArgs.RoleAfter.Name, true);
        }

        message.AddField("id", eventArgs.RoleAfter.Id.ToString(), true)
            .AddField("edited_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
            .AddField("permissions", string.Join(", ", eventArgs.RoleAfter.Permissions.ToLocalizedStrings(_localizer, settings.Locale)))
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetCreatedChannelLog(ChannelCreateEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var message = new SerializableDiscordEmbed()
            .WithColor(settings.OkColor)
            .WithAuthor("log_channelcreated_title")
            .WithTitle("#" + eventArgs.Channel.Name)
            .AddField("type", eventArgs.Channel.Type.ToString(), true)
            .AddField("id", eventArgs.Channel.Id.ToString(), true)
            .AddField("created_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetDeletedChannelLog(ChannelDeleteEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var message = new SerializableDiscordEmbed()
            .WithColor(settings.OkColor)
            .WithAuthor("log_channeldeleted_title")
            .WithTitle("#" + eventArgs.Channel.Name)
            .WithDescription(eventArgs.Channel.Topic)
            .AddField("type", eventArgs.Channel.Type.ToString(), true)
            .AddField("id", eventArgs.Channel.Id.ToString(), true)
            .AddField("deleted_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetEditedChannelLog(ChannelUpdateEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var message = new SerializableDiscordEmbed()
            .WithColor(settings.OkColor)
            .WithAuthor("log_channeledited_title")
            .WithTitle("#" + eventArgs.ChannelAfter.Name);

        if (eventArgs.ChannelBefore.Topic.Equals(eventArgs.ChannelAfter.Topic, StringComparison.Ordinal))
            message.WithDescription(eventArgs.ChannelAfter.Topic);

        if (eventArgs.ChannelBefore.Name.Equals(eventArgs.ChannelAfter.Name, StringComparison.Ordinal))
        {
            message.AddField("old_name", eventArgs.ChannelBefore.Name, true)
            .AddField("new_name", eventArgs.ChannelAfter.Name, true);
        }

        message.AddField("id", eventArgs.ChannelAfter.Id.ToString(), true)
            .AddField("type", eventArgs.ChannelAfter.Type.ToString(), true)
            .AddField("edited_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetVoiceStateLog(VoiceStateUpdateEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild?.Id);

        var voiceState = eventArgs.GetVoiceState();
        var description = (voiceState) switch
        {
            UserVoiceState.Connected => _localizer.FormatLocalized(settings.Locale, "log_voicestate_connected", eventArgs.User.Mention, Formatter.Bold(eventArgs.After.Channel.Name)),
            UserVoiceState.Disconnected => _localizer.FormatLocalized(settings.Locale, "log_voicestate_disconnected", eventArgs.User.Mention, Formatter.Bold(eventArgs.Before.Channel.Name)),
            UserVoiceState.Moved => _localizer.FormatLocalized(settings.Locale, "log_voicestate_moved", eventArgs.User.Mention, Formatter.Bold(eventArgs.Before.Channel.Name), Formatter.Bold(eventArgs.After.Channel.Name)),
            _ => throw new ArgumentException($"Voice state of value \"{voiceState}\" is invalid.", nameof(eventArgs))
        };

        var message = new SerializableDiscordEmbed()
            .WithColor((voiceState is UserVoiceState.Disconnected) ? settings.ErrorColor : settings.OkColor)
            .WithAuthor(eventArgs.User.GetFullname(), imageUrl: eventArgs.User.AvatarUrl ?? eventArgs.User.DefaultAvatarUrl)
            .WithDescription(description)
            .AddField(AkkoConstants.ValidWhitespace, DateTimeOffset.Now.ToDiscordTimestamp());

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetJoiningMemberLog(GuildMemberAddEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild.Id);
        var message = GetBaseMemberActivityLog(settings, eventArgs.Member)
            .AddField("joined_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
            .WithTitle("log_joiningmember_title")
            .WithFooter($"{_localizer.FormatLocalized(settings.Locale, "id")}: {eventArgs.Member.Id}")
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetJoiningAltLog(GuildMemberAddEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild.Id);
        var timeDifference = DateTimeOffset.Now.Subtract(eventArgs.Member.CreationTimestamp);
        var message = GetBaseMemberActivityLog(settings, eventArgs.Member)
            .AddField("joined_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
            .WithTitle("log_joiningalt_title")
            .WithFooter(
                $"{_localizer.FormatLocalized(settings.Locale, "time_difference")}: {GetSmallestTimeString(timeDifference, settings.Locale)} | " +
                $"{_localizer.FormatLocalized(settings.Locale, "id")}: {eventArgs.Member.Id}"
            )
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetLeavingMemberLog(GuildMemberRemoveEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild.Id);
        var message = GetBaseMemberActivityLog(settings, eventArgs.Member)
            .WithTitle("log_leavingmember_title")
            .AddField("left_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
            .WithFooter($"{_localizer.FormatLocalized(settings.Locale, "id")}: {eventArgs.Member.Id}")
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetLeavingAltLog(GuildMemberRemoveEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

        var settings = GetMessageSettings(eventArgs.Guild.Id);

        var timeDifference = DateTimeOffset.Now.Subtract(eventArgs.Member.JoinedAt);

        var message = GetBaseMemberActivityLog(settings, eventArgs.Member)
            .WithTitle("log_leavingalt_title")
            .AddField("left_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
            .WithFooter(
                $"{_localizer.FormatLocalized(settings.Locale, "stayed_for")}: {GetSmallestTimeString(timeDifference, settings.Locale)} | " +
                $"{_localizer.FormatLocalized(settings.Locale, "id")}: {eventArgs.Member.Id}"
            )
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    public DiscordWebhookBuilder GetRoleChangeLog(GuildMemberUpdateEventArgs eventArgs)
    {
        if (eventArgs is null)
            throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");
        else if (eventArgs.RolesBefore.Count == eventArgs.RolesAfter.Count)
            throw new ArgumentException("Cannot log role changes if no role was added or removed.", nameof(eventArgs));

        var settings = GetMessageSettings(eventArgs.Guild.Id);
        var wasAdded = eventArgs.RolesAfter.Count > eventArgs.RolesBefore.Count;
        var target = (wasAdded)
            ? eventArgs.RolesBefore
            : eventArgs.RolesAfter;

        var role = eventArgs.RolesAfter
            .Concat(eventArgs.RolesBefore)
            .Except(target)
            .First();

        var message = new SerializableDiscordEmbed()
            .WithColor(settings.OkColor)
            .WithThumbnail(eventArgs.Member.AvatarUrl ?? eventArgs.Member.DefaultAvatarUrl)
            .WithAuthor((wasAdded) ? "add_role" : "remove_role")
            .WithTitle(eventArgs.Member.GetFullname())
            .AddField("role", role.Mention, true)
            .AddField("name", role.Name, true)
            .AddField("id", role.Id.ToString(), true)
            .WithFooter($"{_localizer.FormatLocalized(settings.Locale, "id")}: {eventArgs.Member.Id}")
            .WithLocalization(_localizer, settings.Locale);

        return GetStandardMessage(message, settings);
    }

    /// <summary>
    /// Returns the appropriate webhook message for the guild's embed setting.
    /// </summary>
    /// <param name="message">The webhook message to send.</param>
    /// <param name="settings">The message settings.</param>
    /// <returns>The webhook message.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DiscordWebhookBuilder GetStandardMessage(SerializableDiscordEmbed message, IMessageSettings settings)
        => (settings.UseEmbed) ? message.BuildWebhookMessage() : new DiscordWebhookBuilder() { Content = message.Decompose() };

    /// <summary>
    /// Gets the message settings for the specified Discord guild ID.
    /// </summary>
    /// <param name="sid">The Discord guild ID or <see langword="null"/> if there isn't one.</param>
    /// <returns>The appropriate message settings.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IMessageSettings GetMessageSettings(ulong? sid)
        => (_dbCache.Guilds.TryGetValue(sid ?? default, out var dbGuild)) ? dbGuild : _botconfig;

    /// <summary>
    /// Returns the smallest time string for the specified time span.
    /// </summary>
    /// <param name="time">The time span.</param>
    /// <param name="locale">Locale to translate the time to.</param>
    /// <returns>The time string.</returns>
    private string GetSmallestTimeString(TimeSpan time, string locale)
    {
        return (time.TotalDays >= 1.0)
            ? $"{time.TotalDays:0.00} {_localizer.FormatLocalized(locale, "days")}"
            : (time.TotalHours >= 1.0)
                ? $"{time.TotalHours:0.00} {_localizer.FormatLocalized(locale, "hours")}"
                : (time.TotalMinutes >= 1.0)
                    ? $"{time.TotalMinutes:0.00} {_localizer.FormatLocalized(locale, "minutes")}"
                    : $"{time.TotalSeconds:0.00} {_localizer.FormatLocalized(locale, "seconds")}";
    }

    /// <summary>
    /// Gets the base log for when a user joins or leaves the Discord guild.
    /// </summary>
    /// <param name="settings">The guild message settings.</param>
    /// <param name="user">The Discord user.</param>
    /// <returns>The base log.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SerializableDiscordEmbed GetBaseMemberActivityLog(IMessageSettings settings, DiscordMember user)
    {
        return new SerializableDiscordEmbed()
            .WithColor(settings.OkColor)
            .WithThumbnail(user.AvatarUrl ?? user.DefaultAvatarUrl)
            .WithDescription($"{user.Mention} | {user.GetFullname()}")
            .AddField("created_on", user.CreationTimestamp.ToDiscordTimestamp(), true);
    }
}