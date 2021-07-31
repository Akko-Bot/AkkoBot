﻿using AkkoBot.Extensions;
using AkkoBot.Models.Serializable;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Events.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.Entities;
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

        public DiscordWebhookBuilder GetDeleteLog(DiscordMessage message)
        {
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
    }
}
