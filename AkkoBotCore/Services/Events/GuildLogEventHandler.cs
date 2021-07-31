using AkkoBot.Config;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events
{
    /// <summary>
    /// Handles guild log events.
    /// </summary>
    public class GuildLogEventHandler : IGuildLogEventHandler
    {
        private readonly IGuildLogGenerator _logGenerator;
        private readonly IAkkoCache _akkoCache;
        private readonly IDbCache _dbCache;
        private readonly BotConfig _botConfig;
        private readonly DiscordWebhookClient _webhookClient;

        public GuildLogEventHandler(IGuildLogGenerator logGenerator, IAkkoCache akkoCache, IDbCache dbCache, BotConfig botConfig, DiscordWebhookClient webhookClient)
        {
            _logGenerator = logGenerator;
            _akkoCache = akkoCache;
            _dbCache = dbCache;
            _botConfig = botConfig;
            _webhookClient = webhookClient;
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

        public async Task LogDeletedMessageAsync(DiscordClient client, MessageDeleteEventArgs eventArgs)
        {
            if (eventArgs.Guild is null || eventArgs.Message.Author?.IsBot is not false
                || !TryGetGuildLog(eventArgs.Guild.Id, GuildLog.MessageEvents, out var guildLog) || !guildLog.IsActive)
                return;

            var webhook = _webhookClient.GetRegisteredWebhook(guildLog.WebhookId)
                ?? await _webhookClient.AddWebhookAsync(guildLog.WebhookId, client);

            // Remove from the cache
            if (!_akkoCache.GuildMessageCache.TryGetValue(eventArgs.Guild.Id, out var messageCache) || !messageCache.TryGet(x => x.Id == eventArgs.Message.Id, out var message))
                return;

            messageCache.Remove(x => x.Id == eventArgs.Message.Id);

            await webhook.ExecuteAsync(_logGenerator.GetDeleteLog(message));
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
    }
}
