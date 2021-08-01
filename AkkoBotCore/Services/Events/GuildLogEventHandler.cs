using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Config;
using AkkoBot.Extensions;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events
{
    /// <summary>
    /// Handles guild log events.
    /// </summary>
    public class GuildLogEventHandler : IGuildLogEventHandler
    {
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
            if (eventArgs.Guild is null || eventArgs.Message.Author.IsBot
                || !TryGetGuildLog(eventArgs.Guild.Id, GuildLog.MessageEvents, out var guildLog) || !guildLog.IsActive)
                return Task.CompletedTask;

            if (!_akkoCache.GuildMessageCache.TryGetValue(eventArgs.Guild.Id, out var messageCache))
            {
                messageCache = new(_botConfig.MessageSizeCache);    // TODO: change this size
                _akkoCache.GuildMessageCache.TryAdd(eventArgs.Guild.Id, messageCache);
            }

            messageCache.Add(eventArgs.Message);

            return Task.CompletedTask;
        }

        public async Task LogUpdatedMessageAsync(DiscordClient client, MessageUpdateEventArgs eventArgs)
        {
            if (eventArgs.Guild is null  || eventArgs.Message.Author.IsBot
                || !TryGetGuildLog(eventArgs.Guild.Id, GuildLog.MessageEvents, out var guildLog)
                || !guildLog.IsActive)
                return;

            // Cache uncached edited messages, but don't log them.
            if (eventArgs.MessageBefore is null)
            {
                if (!_akkoCache.GuildMessageCache.TryGetValue(eventArgs.Guild.Id, out var messageCache))
                {
                    messageCache = new(_botConfig.MessageSizeCache) { eventArgs.Message }; // TODO: change this size
                    _akkoCache.GuildMessageCache.TryAdd(eventArgs.Guild.Id, messageCache);
                }

                messageCache.Add(eventArgs.Message);
                return;
            }


            var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog);

            if (webhook is not null)
                await webhook.ExecuteAsync(_logGenerator.GetMessageUpdateLog(eventArgs));
        }

        public async Task LogDeletedMessageAsync(DiscordClient client, MessageDeleteEventArgs eventArgs)
        {
            if (eventArgs.Guild is null || eventArgs.Message.Author?.IsBot is not false
                || !TryGetGuildLog(eventArgs.Guild.Id, GuildLog.MessageEvents, out var guildLog)
                || !guildLog.IsActive
                || !_akkoCache.GuildMessageCache.TryGetValue(eventArgs.Guild.Id, out var messageCache)
                || !messageCache.TryGet(x => x.Id == eventArgs.Message.Id, out var message))
                return;

            var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog);

            // Remove from the cache
            messageCache.Remove(x => x.Id == eventArgs.Message.Id);

            if (webhook is not null)
                await webhook.ExecuteAsync(_logGenerator.GetMessageDeleteLog(message));
        }

        public async Task LogBulkDeletedMessagesAsync(DiscordClient client, MessageBulkDeleteEventArgs eventArgs)
        {
            if (eventArgs.Guild is null || eventArgs.Messages.All(x => x.Author?.IsBot is not false)
                || !TryGetGuildLog(eventArgs.Guild.Id, GuildLog.MessageEvents, out var guildLog)
                || !guildLog.IsActive
                || !_akkoCache.GuildMessageCache.TryGetValue(eventArgs.Guild.Id, out var messageCache))
                return;

            using var stream = new MemoryStream();
            var firstDeletedTime = eventArgs.Messages.Min(x => x.CreationTimestamp);

            var messages = messageCache
                .Where(x => x?.ChannelId == eventArgs.Channel.Id && x.CreationTimestamp >= firstDeletedTime)
                .OrderBy(x => x.CreationTimestamp)
                .ToArray();

            var webhook = await GetWebhookAsync(client, eventArgs.Guild, guildLog);

            // Remove from the cache
            // The loop is needed because Remove() only removes one item from the ring buffer :facepalm:
            foreach (var message in messages)
                messageCache.Remove(x => x.Id == message.Id);

            if (webhook is not null)
                await webhook.ExecuteAsync(_logGenerator.GetMessageBulkDeleteLog(messages, stream, eventArgs));
        }

        /// <summary>
        /// Gets the guildlog for the specified Discord guild.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <param name="logType">The type of guild log to get.</param>
        /// <param name="guildLog">The resulting guild log.</param>
        /// <returns><see langword="true"/> if the guild log was found, <see langword="false"/> otherwise.</returns>
        private bool TryGetGuildLog(ulong sid, GuildLog logType, out GuildLogEntity guildLog)
        {
            _dbCache.GuildLogs.TryGetValue(sid, out var guildLogs);
            guildLog = guildLogs?.FirstOrDefault(x => logType.HasFlag(x.Type));

            return guildLog is not null;
        }

        /// <summary>
        /// Gets the webhook for the specified guild log.
        /// </summary>
        /// <param name="client">The Discord client with access to the webhook.</param>
        /// <param name="server">The Discord guild where the webhook is.</param>
        /// <param name="guildLog">The guild log to be processed.</param>
        /// <returns>The log's <see cref="DiscordWebhook"/> or <see langword="null"/> if the channel associated with the log got deleted.</returns>
        /// <exception cref="ArgumentException">Occurs when the IDs of the Discord guild and the guild log don't match.</exception>
        private async ValueTask<DiscordWebhook> GetWebhookAsync(DiscordClient client, DiscordGuild server, GuildLogEntity guildLog)
        {
            if (server.Id != guildLog.GuildIdFK)
                throw new ArgumentException("Guild ID and guild log ID cannot differ.");

            // If channel was deleted, remove the guild log
            if (!server.Channels.TryGetValue(guildLog.ChannelId, out var channel))
            {
                client.Logger.LogWarning(_guildLogEvent, $"The channel for a \"{guildLog.Type}\" guild log was deleted. Removing the guild log from the database.");

                _dbCache.GuildLogs.TryGetValue(server.Id, out var guildLogs);
                using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

                // Remove guild log from the database
                await db.GuildLogs.DeleteAsync(x => x.GuildIdFK == server.Id && x.ChannelId == guildLog.ChannelId).ConfigureAwait(false); ;

                // Remove guild log from the cache
                if (guildLogs.TryRemove(guildLog) && guildLogs.Count is 0)
                    _dbCache.GuildLogs.TryRemove(server.Id, out _);

                // Remove logged messages
                if (guildLog.Type is GuildLog.MessageEvents && _akkoCache.GuildMessageCache.TryRemove(server.Id, out var messageCache))
                    messageCache.Clear();

                return null;
            }

            try
            {
                return _webhookClient.GetRegisteredWebhook(guildLog.WebhookId)
                    ?? await _webhookClient.AddWebhookAsync(guildLog.WebhookId, client).ConfigureAwait(false); ;
            }
            catch
            {
                client.Logger.LogWarning(_guildLogEvent, $"The webhook for a \"{guildLog.Type}\" guild log was deleted and is being recreated.");

                // Create a webhook and cache it
                using var avatar = await _utilities.GetOnlineStreamAsync(client.CurrentUser.AvatarUrl ?? client.CurrentUser.DefaultAvatarUrl).ConfigureAwait(false);
                var webhook = await channel.CreateWebhookAsync(_botConfig.WebhookLogName, avatar).ConfigureAwait(false);
                _webhookClient.AddWebhook(webhook);

                // Update the guild log
                using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

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
}
