using AkkoBot.Commands.Abstractions;
using AkkoBot.Config;
using AkkoBot.Extensions;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="GuildLogEntity"/> objects.
    /// </summary>
    public class GuildLogService : ICommandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;
        private readonly BotConfig _botConfig;
        private readonly DiscordWebhookClient _webhookClient;

        public GuildLogService(IServiceScopeFactory scopeFactory, IDbCache dbCache, BotConfig botConfig, DiscordWebhookClient webhookClient)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCache;
            _botConfig = botConfig;
            _webhookClient = webhookClient;
        }



        /// <summary>
        /// Starts logging of a guild event.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="channel">The channel where </param>
        /// <param name="logType">The type of guild event to generate logs for.</param>
        /// <param name="name">The name of the webhook.</param>
        /// <param name="avatar">The image stream of the webhook's avatar.</param>
        /// <returns><see langword="true"/> if the guild log was created, <see langword="false"/> is it was updated.</returns>
        public async Task<bool> AddLogAsync(CommandContext context, DiscordChannel channel, GuildLog logType, string name = null, Stream avatar = null)
        {
            if (channel.Type is not ChannelType.Text and not ChannelType.News and not ChannelType.Store)
                throw new ArgumentException("Logs can only be output to text channels.", nameof(channel));

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            if (!_dbCache.GuildLogs.TryGetValue(context.Guild.Id, out var guildLogs))
                guildLogs ??= new();

            var anyChannelLog = guildLogs.FirstOrDefault(x => x.ChannelId == channel.Id);
            
            var webhook = (anyChannelLog is null)
                ? await channel.CreateWebhookAsync(name ?? _botConfig.WebhookLogName)
                : _webhookClient.GetRegisteredWebhook(anyChannelLog.WebhookId)
                    ?? await context.Client.GetWebhookAsync(anyChannelLog.WebhookId)
                    ?? await channel.CreateWebhookAsync(name ?? _botConfig.WebhookLogName);

            var guildLog = guildLogs.FirstOrDefault(x => x.Type == logType)
                ?? new()
                {
                    GuildIdFK = context.Guild.Id,
                    ChannelId = channel.Id,
                    WebhookId = webhook.Id,
                    IsActive = true,
                    Type = logType
                };

            // If webhook is not being used by any other log, modify it
            if (guildLog.ChannelId != channel.Id && guildLogs.Count(x => x.ChannelId == channel.Id) <= 1)
            {
                _webhookClient.TryRemove(guildLog.WebhookId);
                webhook = await webhook.ModifyAsync(name ?? webhook.Name, avatar, channel.Id);
            }

            // Update entry
            guildLog.ChannelId = channel.Id;
            guildLog.WebhookId = webhook.Id;
            guildLog.IsActive = true;
            guildLog.Type = logType;

            db.Update(guildLog);
            await db.SaveChangesAsync();

            // Update cache
            var result = guildLogs.Add(guildLog);
            _dbCache.GuildLogs.AddOrUpdate(context.Guild.Id, guildLogs, (_, _) => guildLogs);

            // Update webhook cache
            _webhookClient.TryAdd(webhook);

            return result;
        }
    }
}
