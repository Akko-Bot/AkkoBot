using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Enums;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Events.Abstractions;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AkkoCore.Services.Events.Common
{
    /// <summary>
    /// Generates guild log messages.
    /// </summary>
    public class GuildLogGenerator : IGuildLogGenerator
    {
        private readonly TimeSpan _24hours = TimeSpan.FromDays(1);

        private readonly ILocalizer _localizer;
        private readonly IDbCache _dbCache;
        private readonly GuildLogService _logService;

        public GuildLogGenerator(ILocalizer localizer, IDbCache dbCache, GuildLogService logService)
        {
            _localizer = localizer;
            _dbCache = dbCache;
            _logService = logService;
        }

        public DiscordWebhookBuilder GetMessageDeleteLog(DiscordMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message), "Deleted message cannot be null.");

            _dbCache.Guilds.TryGetValue(message.Channel.Guild.Id, out var dbGuild);

            var webhookMessage = new SerializableDiscordEmbed()
                .WithColor((message.Author as DiscordMember)?.Color.ToString())
                .WithAuthor("log_message_deleted_title", message.JumpLink.AbsoluteUri)
                .WithTitle(DiscordUserExt.GetFullname(message.Author))
                .WithDescription($"{_localizer.GetResponseString(dbGuild.Locale, "channel")}: {message.Channel.Mention} | {message.Channel.Name}\n\n{message.Content}")
                .AddField("author_mention", message.Author.Mention, true)
                .AddField("deleted_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
                .WithFooter($"{_localizer.GetResponseString(dbGuild.Locale, "id")}: {message.Id}")
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(webhookMessage, dbGuild);
        }

        public DiscordWebhookBuilder GetMessageUpdateLog(MessageUpdateEventArgs eventArgs)
        {
            if (eventArgs.MessageBefore is null)
                throw new ArgumentNullException(nameof(eventArgs), "Previous state of an edited message cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordEmbed()
                .WithColor((eventArgs.Message.Author as DiscordMember)?.Color.ToString())
                .WithAuthor("log_message_edited_title", eventArgs.Message.JumpLink.AbsoluteUri)
                .WithTitle(eventArgs.Message.Author.GetFullname())
                .WithDescription(
                    $"{_localizer.GetResponseString(dbGuild.Locale, "channel")}: {eventArgs.Message.Channel.Mention} | {eventArgs.Message.Channel.Name}\n\n" +
                    $"{_localizer.GetResponseString(dbGuild.Locale, "before")}:\n{eventArgs.MessageBefore.Content}\n\n" +
                    $"{_localizer.GetResponseString(dbGuild.Locale, "after")}:\n{eventArgs.Message.Content}"
                )
                .AddField("author_mention", eventArgs.Message.Author.Mention, true)
                .AddField("edited_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
                .WithFooter($"{_localizer.GetResponseString(dbGuild.Locale, "id")}: {eventArgs.Message.Id}")
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetMessageBulkDeleteLog(IEnumerable<DiscordMessage> messages, Stream stream, MessageBulkDeleteEventArgs eventArgs)
        {
            if (messages is null || !messages.Any())
                throw new ArgumentException("Message collection cannot be null or empty.", nameof(messages));
            else if (!stream.CanWrite)
                throw new ArgumentException("Stream must allow writing.", nameof(stream));
            else if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event arguments cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var extraInfo = $"==> {_localizer.GetResponseString(dbGuild.Locale, "log_messages_deleted")}: {eventArgs.Messages.Count}" + Environment.NewLine;
            var fileContent = _logService.GenerateMessageLog(messages, eventArgs.Channel, dbGuild.Locale, extraInfo);

            // The async version is only worth it if you intend to cancel the write
            stream.Write(Encoding.UTF8.GetBytes(fileContent));
            stream.Position = 0;

            return new DiscordWebhookBuilder()
                .AddFile($"Logs_BulkDelete_{eventArgs.Channel.Name}_{DateTimeOffset.Now}.txt", stream);
        }

        public DiscordWebhookBuilder GetEmojiUpdateLog(DiscordGuild server, DiscordEmoji emoji, int action, string oldEmojiName = null)
        {
            if (server is null || emoji is null)
                throw new ArgumentNullException((server is null) ? nameof(server) : nameof(emoji), (server is null) ? "Discord guild cannot be null" : "Emoji cannot be null.");

            _dbCache.Guilds.TryGetValue(server.Id, out var dbGuild);

            var description = (action is 0 && emoji.Name.Equals(oldEmojiName, StringComparison.Ordinal))
                ? _localizer.GetResponseString(dbGuild.Locale, "log_emoji_edited_simple")
                : action switch
                {
                    0 => _localizer.GetResponseString(dbGuild.Locale, "log_emoji_edited"),
                    < 0 => _localizer.GetResponseString(dbGuild.Locale, "log_emoji_added"),
                    > 0 => _localizer.GetResponseString(dbGuild.Locale, "log_emoji_deleted")
                };

            var message = new SerializableDiscordEmbed()
                .WithColor((action <= 0) ? dbGuild.OkColor : dbGuild.ErrorColor)
                .WithTitle("log_emoji_title")
                .WithThumbnail(emoji.Url)
                .WithDescription(string.Format(description, Formatter.InlineCode(oldEmojiName ?? emoji.Name), Formatter.InlineCode(emoji.Name)))
                .AddField(AkkoConstants.ValidWhitespace, DateTimeOffset.Now.ToDiscordTimestamp())
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetCreatedInviteLog(InviteCreateEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordEmbed()
                .WithColor(dbGuild.OkColor)
                .WithTitle("log_invite_created_title")
                .WithDescription(eventArgs.Invite.GetInviteLink())
                .AddField("author", eventArgs.Invite.Inviter.GetFullname(), true)
                .AddField("code", eventArgs.Invite.Code, true)
                .AddField("created_on", eventArgs.Invite.CreatedAt.ToDiscordTimestamp(), true)
                .AddField("channel", (dbGuild.UseEmbed) ? eventArgs.Channel.Mention : $"#{eventArgs.Channel.Name}", true)
                .AddField("invite_temporary", (eventArgs.Invite.IsTemporary) ? AkkoStatics.SuccessEmoji.Name : AkkoStatics.FailureEmoji.Name, true)
                .AddField("expires_on", (eventArgs.Invite.MaxAge is 0) ? "-" : eventArgs.Invite.CreatedAt.AddSeconds(eventArgs.Invite.MaxAge).ToDiscordTimestamp(), true)
                .WithFooter($"{_localizer.FormatLocalized(dbGuild.Locale, "uses_left")}: {((eventArgs.Invite.MaxUses is 0) ? "-" : (eventArgs.Invite.MaxUses - eventArgs.Invite.Uses))}")
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetDeletedInviteLog(InviteDeleteEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordEmbed()
                .WithColor(dbGuild.ErrorColor)
                .WithTitle("log_invite_deleted_title")
                .WithDescription(eventArgs.Invite.GetInviteLink())
                .AddField("code", eventArgs.Invite.Code, true)
                .AddField("channel", (dbGuild.UseEmbed) ? eventArgs.Channel.Mention : $"#{eventArgs.Channel.Name}", true)
                .AddField("deleted_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetBannedUserLog(DiscordAuditLogBanEntry auditLog, GuildBanAddEventArgs eventArgs)
        {
            if (auditLog is null)
                throw new ArgumentNullException(nameof(auditLog), "Audit log cannot be null.");
            else if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordEmbed()
                .WithColor(dbGuild.ErrorColor)
                .WithTitle("ban_title")
                .WithDescription($"{eventArgs.Member.Mention} | {eventArgs.Member.GetFullname()}")
                .AddField("moderator", auditLog.UserResponsible.GetFullname(), true)
                .AddField("reason", auditLog.Reason, true)
                .AddField("banned_on", DateTimeOffset.Now.ToDiscordTimestamp())
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetUnbannedUserLog(DiscordAuditLogBanEntry auditLog, GuildBanRemoveEventArgs eventArgs)
        {
            if (auditLog is null)
                throw new ArgumentNullException(nameof(auditLog), "Audit log cannot be null.");
            else if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordEmbed()
                .WithColor(dbGuild.OkColor)
                .WithTitle("log_unban_title")
                .WithDescription($"{eventArgs.Member.Mention} | {eventArgs.Member.GetFullname()}")
                .AddField("moderator", auditLog.UserResponsible.GetFullname(), true)
                .AddField("reason", auditLog.Reason, true)
                .AddField("unbanned_on", DateTimeOffset.Now.ToDiscordTimestamp())
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetCreatedRoleLog(GuildRoleCreateEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordEmbed()
                .WithColor(dbGuild.OkColor)
                .WithTitle("log_rolecreated_title")
                .AddField("name", eventArgs.Role.Name, true)
                .AddField("id", eventArgs.Role.Id.ToString(), true)
                .AddField("created_on", DateTimeOffset.Now.ToDiscordTimestamp())
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetDeletedRoleLog(GuildRoleDeleteEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordEmbed()
                .WithColor(dbGuild.ErrorColor)
                .WithTitle("log_roledeleted_title")
                .AddField("name", eventArgs.Role.Name, true)
                .AddField("id", eventArgs.Role.Id.ToString(), true)
                .AddField("deleted_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
                .AddField("permissions", string.Join(", ", eventArgs.Role.Permissions.ToLocalizedStrings(_localizer, dbGuild.Locale)))
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetEditedRoleLog(GuildRoleUpdateEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordEmbed()
                .WithColor(dbGuild.OkColor)
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
                .AddField("permissions", string.Join(", ", eventArgs.RoleAfter.Permissions.ToLocalizedStrings(_localizer, dbGuild.Locale)))
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetCreatedChannelLog(ChannelCreateEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordEmbed()
                .WithColor(dbGuild.OkColor)
                .WithAuthor("log_channelcreated_title")
                .WithTitle("#" + eventArgs.Channel.Name)
                .AddField("type", eventArgs.Channel.Type.ToString(), true)
                .AddField("id", eventArgs.Channel.Id.ToString(), true)
                .AddField("created_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetDeletedChannelLog(ChannelDeleteEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordEmbed()
                .WithColor(dbGuild.OkColor)
                .WithAuthor("log_channeldeleted_title")
                .WithTitle("#" + eventArgs.Channel.Name)
                .WithDescription(eventArgs.Channel.Topic)
                .AddField("type", eventArgs.Channel.Type.ToString(), true)
                .AddField("id", eventArgs.Channel.Id.ToString(), true)
                .AddField("deleted_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetEditedChannelLog(ChannelUpdateEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordEmbed()
                .WithColor(dbGuild.OkColor)
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
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetVoiceStateLog(VoiceStateUpdateEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var voiceState = eventArgs.GetVoiceState();
            var description = (voiceState) switch
            {
                UserVoiceState.Connected => _localizer.FormatLocalized(dbGuild.Locale, "log_voicestate_connected", eventArgs.User.Mention, Formatter.Bold(eventArgs.After.Channel.Name)),
                UserVoiceState.Disconnected => _localizer.FormatLocalized(dbGuild.Locale, "log_voicestate_disconnected", eventArgs.User.Mention, Formatter.Bold(eventArgs.Before.Channel.Name)),
                UserVoiceState.Moved => _localizer.FormatLocalized(dbGuild.Locale, "log_voicestate_moved", eventArgs.User.Mention, Formatter.Bold(eventArgs.Before.Channel.Name), Formatter.Bold(eventArgs.After.Channel.Name)),
                _ => throw new ArgumentException($"Voice state of value \"{voiceState}\" is invalid.", nameof(eventArgs))
            };

            var message = new SerializableDiscordEmbed()
                .WithColor((voiceState is UserVoiceState.Disconnected) ? dbGuild.ErrorColor : dbGuild.OkColor)
                .WithAuthor(eventArgs.User.GetFullname(), imageUrl: eventArgs.User.AvatarUrl ?? eventArgs.User.DefaultAvatarUrl)
                .WithDescription(description)
                .AddField(AkkoConstants.ValidWhitespace, DateTimeOffset.Now.ToDiscordTimestamp());

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetJoiningMemberLog(GuildMemberAddEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);
            _dbCache.Gatekeeping.TryGetValue(eventArgs.Guild.Id, out var gatekeeper);

            var timeDifference = DateTimeOffset.Now.Subtract(eventArgs.Member.CreationTimestamp);
            var message = new SerializableDiscordEmbed()
                .WithColor(dbGuild.OkColor)
                .WithThumbnail(eventArgs.Member.AvatarUrl ?? eventArgs.Member.DefaultAvatarUrl)
                .WithTitle("log_joiningmember_title")
                .WithDescription($"{eventArgs.Member.Mention} | {eventArgs.Member.GetFullname()}")
                .AddField("created_on", eventArgs.Member.CreationTimestamp.ToDiscordTimestamp(), true)
                .AddField("joined_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
                .WithFooter(
                    ((timeDifference < (gatekeeper?.AntiAltTime ?? _24hours))
                        ? $"{_localizer.FormatLocalized(dbGuild.Locale, "time_difference")}: {GetSmallestTimeString(timeDifference, dbGuild.Locale)} | "
                        : string.Empty) +

                    $"{_localizer.FormatLocalized(dbGuild.Locale, "id")}: {eventArgs.Member.Id}"
                )
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetLeavingMemberLog(GuildMemberRemoveEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var timeDifference = DateTimeOffset.Now.Subtract(eventArgs.Member.JoinedAt);
            var message = new SerializableDiscordEmbed()
                .WithColor(dbGuild.OkColor)
                .WithThumbnail(eventArgs.Member.AvatarUrl ?? eventArgs.Member.DefaultAvatarUrl)
                .WithTitle("log_leavingmember_title")
                .WithDescription($"{eventArgs.Member.Mention} | {eventArgs.Member.GetFullname()}")
                .AddField("created_on", eventArgs.Member.CreationTimestamp.ToDiscordTimestamp(), true)
                .AddField("left_on", DateTimeOffset.Now.ToDiscordTimestamp(), true)
                .WithFooter(
                    $"{_localizer.FormatLocalized(dbGuild.Locale, "stayed_for")}: {GetSmallestTimeString(timeDifference, dbGuild.Locale)} | " +
                    $"{_localizer.FormatLocalized(dbGuild.Locale, "id")}: {eventArgs.Member.Id}"
                )
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        /// <summary>
        /// Returns the appropriate webhook message for the guild's embed setting.
        /// </summary>
        /// <param name="message">The webhook message to send.</param>
        /// <param name="dbGuild">The guild settings.</param>
        /// <returns>The webhook message.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DiscordWebhookBuilder GetStandardMessage(SerializableDiscordEmbed message, GuildConfigEntity dbGuild)
            => (dbGuild.UseEmbed) ? message.BuildWebhookMessage() : new DiscordWebhookBuilder() { Content = message.Deconstruct() };

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
    }
}