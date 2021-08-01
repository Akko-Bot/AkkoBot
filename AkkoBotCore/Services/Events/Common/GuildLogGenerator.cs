using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Extensions;
using AkkoBot.Models.Serializable;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Events.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
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

            _dbCache.Guilds.TryGetValue(message.Channel.Guild.Id, out var dbGuild);

            var result = new SerializableDiscordMessage()
                .WithColor((message.Author as DiscordMember)?.Color.ToString())
                .WithAuthor("message_deleted", message.JumpLink.AbsoluteUri)
                .WithTitle(message.Author.GetFullname())
                .WithDescription($"{_localizer.GetResponseString(dbGuild.Locale, "channel")}: {message.Channel.Mention} | {message.Channel.Name}\n\n{message.Content}")
                .AddField("author_mention", message.Author.Mention, true)
                .AddField("deleted_at", DateTimeOffset.Now.ToDiscordTimestamp(), true)
                .WithFooter($"{_localizer.GetResponseString(dbGuild.Locale, "id")}: {message.Id}")
                .WithLocalization(_localizer, dbGuild.Locale);

            return (dbGuild.UseEmbed)
                ? result.BuildWebhookMessage()
                : new DiscordWebhookBuilder() { Content = result.Deconstruct() };
        }

        public DiscordWebhookBuilder GetMessageUpdateLog(MessageUpdateEventArgs eventArgs)
        {
            if (eventArgs.MessageBefore is null)
                throw new ArgumentException("Previous state of an edited message cannot be null.", nameof(eventArgs));

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var result = new SerializableDiscordMessage()
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
                ? result.BuildWebhookMessage()
                : new DiscordWebhookBuilder() { Content = result.Deconstruct() };
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
    }
}
