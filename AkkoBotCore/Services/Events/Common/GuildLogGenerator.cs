using AkkoBot.Extensions;
using AkkoBot.Models.Serializable;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Events.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;

namespace AkkoBot.Services.Events.Common
{
    /// <summary>
    /// Generates guild log messages.
    /// </summary>
    public class GuildLogGenerator : IGuildLogGenerator
    {
        private readonly ILocalizer _localizer;
        private readonly IDbCache _dbCache;

        public GuildLogGenerator(ILocalizer localizer, IDbCache dbCache)
        {
            _localizer = localizer;
            _dbCache = dbCache;
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
    }
}
