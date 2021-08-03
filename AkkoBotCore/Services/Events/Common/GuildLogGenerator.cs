using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Common;
using AkkoBot.Config;
using AkkoBot.Extensions;
using AkkoBot.Models.Serializable;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Events.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AkkoBot.Services.Events.Common
{
    /// <summary>
    /// Generates guild log messages.
    /// </summary>
    public class GuildLogGenerator : IGuildLogGenerator
    {
        private readonly ILocalizer _localizer;
        private readonly IDbCache _dbCache;
        private readonly GuildLogService _logService;
        private readonly BotConfig _botConfig;

        public GuildLogGenerator(ILocalizer localizer, IDbCache dbCache, GuildLogService logService, BotConfig botConfig)
        {
            _localizer = localizer;
            _dbCache = dbCache;
            _logService = logService;
            _botConfig = botConfig;
        }

        public DiscordWebhookBuilder GetMessageDeleteLog(DiscordMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message), "Deleted message cannot be null.");

            _dbCache.Guilds.TryGetValue((ulong)message.Channel.Guild.Id, out var dbGuild);

            var webhookMessage = new SerializableDiscordMessage()
                .WithColor((string)((message.Author as DiscordMember)?.Color.ToString()))
                .WithAuthor("message_deleted", (string)message.JumpLink.AbsoluteUri)
                .WithTitle(DiscordUserExt.GetFullname(message.Author))
                .WithDescription($"{_localizer.GetResponseString(dbGuild.Locale, "channel")}: {message.Channel.Mention} | {message.Channel.Name}\n\n{message.Content}")
                .AddField("author_mention", (string)message.Author.Mention, true)
                .AddField("deleted_at", DateTimeOffset.Now.ToDiscordTimestamp(), true)
                .WithFooter($"{_localizer.GetResponseString(dbGuild.Locale, "id")}: {message.Id}")
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(webhookMessage, dbGuild);
        }

        public DiscordWebhookBuilder GetMessageUpdateLog(MessageUpdateEventArgs eventArgs)
        {
            if (eventArgs.MessageBefore is null)
                throw new ArgumentNullException(nameof(eventArgs), "Previous state of an edited message cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordMessage()
                .WithColor((eventArgs.Message.Author as DiscordMember)?.Color.ToString())
                .WithAuthor("message_edited", eventArgs.Message.JumpLink.AbsoluteUri)
                .WithTitle(eventArgs.Message.Author.GetFullname())
                .WithDescription(
                    $"{_localizer.GetResponseString(dbGuild.Locale, "channel")}: {eventArgs.Message.Channel.Mention} | {eventArgs.Message.Channel.Name}\n\n" +
                    $"{_localizer.GetResponseString(dbGuild.Locale, "before")}:\n{eventArgs.MessageBefore.Content}\n\n" +
                    $"{_localizer.GetResponseString(dbGuild.Locale, "after")}:\n{eventArgs.Message.Content}"
                )
                .AddField("author_mention", eventArgs.Message.Author.Mention, true)
                .AddField("edited_at", DateTimeOffset.Now.ToDiscordTimestamp(), true)
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

            var message = new SerializableDiscordMessage()
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

            var message = new SerializableDiscordMessage()
                .WithColor(dbGuild.OkColor)
                .WithTitle("log_invite_created_title")
                .WithDescription(eventArgs.Invite.GetInviteLink())
                .AddField("author", eventArgs.Invite.Inviter.GetFullname(), true)
                .AddField("code", eventArgs.Invite.Code, true)
                .AddField("created_at", eventArgs.Invite.CreatedAt.ToDiscordTimestamp(), true)
                .AddField("channel", (dbGuild.UseEmbed) ? eventArgs.Channel.Mention : $"#{eventArgs.Channel.Name}", true)
                .AddField("invite_temporary", (eventArgs.Invite.IsTemporary) ? AkkoEntities.SuccessEmoji.Name : AkkoEntities.FailureEmoji.Name, true)
                .AddField("expires_at", (eventArgs.Invite.MaxAge is 0) ? "-" : eventArgs.Invite.CreatedAt.AddSeconds(eventArgs.Invite.MaxAge).ToDiscordTimestamp(), true)
                .WithFooter($"{_localizer.FormatLocalized(dbGuild.Locale, "uses_left")}: {((eventArgs.Invite.MaxUses is 0) ? "-" : (eventArgs.Invite.MaxUses - eventArgs.Invite.Uses))}")
                .WithLocalization(_localizer, dbGuild.Locale);

            return GetStandardMessage(message, dbGuild);
        }

        public DiscordWebhookBuilder GetDeletedInviteLog(InviteDeleteEventArgs eventArgs)
        {
            if (eventArgs is null)
                throw new ArgumentNullException(nameof(eventArgs), "Event argument cannot be null.");

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var message = new SerializableDiscordMessage()
                .WithColor(dbGuild.ErrorColor)
                .WithTitle("log_invite_deleted_title")
                .WithDescription(eventArgs.Invite.GetInviteLink())
                .AddField("code", eventArgs.Invite.Code, true)
                .AddField("channel", (dbGuild.UseEmbed) ? eventArgs.Channel.Mention : $"#{eventArgs.Channel.Name}", true)
                .AddField("deleted_at", DateTimeOffset.Now.ToDiscordTimestamp(), true)
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
        private DiscordWebhookBuilder GetStandardMessage(SerializableDiscordMessage message, GuildConfigEntity dbGuild)
            => (dbGuild.UseEmbed) ? message.BuildWebhookMessage() : new DiscordWebhookBuilder() { Content = message.Deconstruct() };
    }
}
