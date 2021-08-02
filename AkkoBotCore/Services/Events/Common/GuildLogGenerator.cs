using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models.Serializable;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Events.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public GuildLogGenerator(ILocalizer localizer, IDbCache dbCache, GuildLogService logService)
        {
            _localizer = localizer;
            _dbCache = dbCache;
            _logService = logService;
        }

        public DiscordWebhookBuilder GetMessageDeleteLog(DiscordMessage message)
        {
            if (message is null)
                throw new ArgumentException("Deleted message cannot be null.", nameof(message));

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

            return (dbGuild.UseEmbed)
                ? webhookMessage.BuildWebhookMessage()
                : new DiscordWebhookBuilder() { Content = webhookMessage.Deconstruct() };
        }

        public DiscordWebhookBuilder GetMessageUpdateLog(MessageUpdateEventArgs eventArgs)
        {
            if (eventArgs.MessageBefore is null)
                throw new ArgumentException("Previous state of an edited message cannot be null.", nameof(eventArgs));

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

            return (dbGuild.UseEmbed)
                ? message.BuildWebhookMessage()
                : new DiscordWebhookBuilder() { Content = message.Deconstruct() };
        }

        public DiscordWebhookBuilder GetMessageBulkDeleteLog(IEnumerable<DiscordMessage> messages, Stream stream, MessageBulkDeleteEventArgs eventArgs)
        {
            if (messages is null || !messages.Any())
                throw new ArgumentException("Message collection cannot be null or empty.", nameof(messages));
            else if (!stream.CanWrite)
                throw new ArgumentException("Stream must allow writing.", nameof(stream));
            else if (eventArgs is null)
                throw new ArgumentException("Event arguments cannot be null.", nameof(eventArgs));

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
                throw new ArgumentException((server is null) ? "Discord guild cannot be null" : "Emoji cannot be null.", (server is null) ? nameof(server) : nameof(emoji));

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
                .WithColor(dbGuild.OkColor)
                .WithTitle("log_emoji_title")
                .WithThumbnail(emoji.Url)
                .WithDescription(string.Format(description, Formatter.InlineCode(oldEmojiName ?? emoji.Name), Formatter.InlineCode(emoji.Name)))
                .AddField(AkkoConstants.ValidWhitespace, DateTimeOffset.Now.ToDiscordTimestamp())
                .WithLocalization(_localizer, dbGuild.Locale);

            return (dbGuild.UseEmbed)
               ? message.BuildWebhookMessage()
               : new DiscordWebhookBuilder() { Content = message.Deconstruct() };
        }
    }
}
