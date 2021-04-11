using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AkkoBot.Commands.Formatters
{
    public class AkkoPlaceholders : IPlaceholderFormatter, ICommandService
    {
        private readonly IReadOnlyDictionary<string, Func<CommandContext, object>> _placeholderActions = new Dictionary<string, Func<CommandContext, object>>()
        {
            /* Bot Placeholders */

            ["bot.id"] = (context) => context.Client.CurrentUser.Id,
            ["bot.name"] = (context) => context.Client.CurrentUser.Username,
            ["bot.discrim"] = (context) => context.Client.CurrentUser.Discriminator,
            ["bot.fullname"] = (context) => context.Client.CurrentUser.GetFullname(),
            ["bot.mention"] = (context) => context.Client.CurrentUser.Mention,
            ["bot.creationdate"] = (context) => context.Client.CurrentUser.CreationTimestamp,
            ["bot.avatar"] = (context) => context.Client.CurrentUser.AvatarUrl,
            ["bot.status"] = (context) => context.Client.CurrentUser.Presence.Status,
            ["bot.prefix"] = (context) => context.Services.GetService<IDbCache>().BotConfig.BotPrefix,
            ["bot.latency"] = (context) => context.Client.Ping,
            ["bot.shard"] = (context) => context.Client.ShardId,
            ["bot.shardcount"] = (context) => context.Client.ShardCount,
            ["bot.servercount"] = (context) => context.Client.Guilds.Count,
            ["bot.servertotal"] = (context) => context.CommandsNext.Services.GetService<DiscordShardedClient>().ShardClients.Values.Sum(client => client.Guilds.Count),
            ["bot.usercount"] = (context) => context.CommandsNext.Services.GetService<DiscordShardedClient>().ShardClients.Values.Sum(client => client.Guilds.Values.Sum(server => server.MemberCount)),

            /* Server Placeholders */

            ["server.id"] = (context) => context.Guild?.Id,
            ["server.name"] = (context) => context.Guild?.Name,
            ["server.members"] = (context) => context.Guild?.MemberCount,
            ["server.prefix"] = (context) => context.Prefix,
            ["p"] = (context) => context.Prefix,

            /* Channel Placeholders */

            ["channel.id"] = (context) => context.Channel.Id,
            ["channel.name"] = (context) => context.Channel.Name,
            ["channel.mention"] = (context) => context.Channel.Mention,
            ["channel.topic"] = (context) => context.Channel.Topic,
            ["channel.creationdate"] = (context) => context.Channel.CreationTimestamp,
            ["channel.nsfw"] = (context) => context.Channel.IsNSFW,
            ["channel.category"] = (context) => context.Channel.Parent?.Name,
            ["channel.position"] = (context) => context.Channel.Position,
            ["channel.slowmode"] = (context) => context.Channel.PerUserRateLimit ?? default,
            ["channel.visibleto"] = (context) => context.Channel.Users.Count(),

            /* User Placeholders */

            ["user.id"] = (context) => context.User.Id,
            ["user.name"] = (context) => context.User.Username,
            ["user.discrim"] = (context) => context.User.Discriminator,
            ["user.fullname"] = (context) => context.User.GetFullname(),
            ["user.nickname"] = (context) => context.Member?.DisplayName,
            ["user.mention"] = (context) => context.User.Mention,
            ["user.avatar"] = (context) => context.User.AvatarUrl,
            ["user.creationdate"] = (context) => context.User.CreationTimestamp,
            ["user.joindate"] = (context) => context.Member?.JoinedAt,
            ["user.joindifference"] = (context) => context.Member.JoinedAt.Subtract(context.User.CreationTimestamp),
            ["user.flags"] = (context) => context.User.Flags,
            ["user.locale"] = (context) => context.User.Locale,
            ["user.2fa"] = (context) => context.User.MfaEnabled ?? false,
            ["user.hierarchy"] = (context) => context.Member?.Hierarchy,
            ["user.color"] = (context) => context.Member?.Color,
            ["user.nitrodate"] = (context) => context.Member?.PremiumSince,
            ["user.nitrotype"] = (context) => context.Member?.PremiumType,
            ["user.roles"] = (context) => string.Join(", ", context.Member?.Roles.Select(x => x.Name).ToArray() ?? Array.Empty<string>()),
            ["user.voicechat"] = (context) => context.Member?.VoiceState?.Channel.Name
        };

        /// <summary>
        /// Parses a string placeholder to the value it represents.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="placeholder">The placeholder.</param>
        /// <param name="result">The string representation of the parsed placeholder, <see langword="null"/> if parsing fails.</param>
        /// <returns>The value of the boxed object the placeholder represents, <see langword="null"/> if parsing fails.</returns>
        public object Parse(CommandContext context, string placeholder, out string result)
        {
            object obj = null;
            result = null;

            if (_placeholderActions.TryGetValue(placeholder, out var action))
            {
                obj = action(context);
                result = obj?.ToString();
            }

            return obj;
        }
    }
}