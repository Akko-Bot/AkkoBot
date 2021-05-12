﻿using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
            ["user.voicechat"] = (context) => context.Member?.VoiceState?.Channel.Name,

            /* Miscelaneous */
            ["rng"] = (context) => context.Services.GetService<Random>().Next()
        };

        private readonly IReadOnlyDictionary<string, Func<CommandContext, object, object>> _parameterizedActions = new Dictionary<string, Func<CommandContext, object, object>>()
        {
            /* Miscelaneous */

            ["remaining.text"] = (context, endMatchIndex) => context.RawArgumentString[(int)endMatchIndex..].Trim(),

            ["rng"] = (context, parameter) =>
            {
                // If array is the wrong length or contains invalid arguments, quit
                if (parameter is not string[] arguments || arguments.Length is < 1 or > 2
                    | (!arguments.TryGetValue(0, out var first) & !arguments.TryGetValue(1, out var second))
                    | (!int.TryParse(first, out var x) & !int.TryParse(second, out var y)))
                    return null;

                if (x > y)
                    (x, y) = (y, x);

                return context.Services.GetService<Random>().Next(x, y);
            },

            ["choose"] = (context, parameter) =>
            {
                return (parameter is not string[] arguments || arguments.Length == 0)
                    ? null
                    : arguments[context.Services.GetService<Random>().Next(0, arguments.Length)].Trim();
            }
        };

        public bool TryParse(CommandContext context, Match match, out object result)
        {
            var groups = match.Groups.Values
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))    // This is needed because for some reason groups can contain empty values.
                .ToArray();                                         // Contains capture group + matches.

            if (groups.Length <= 2 && _placeholderActions.TryGetValue(groups[1].Value, out var action))
            {
                result = action(context);
                return true;
            }
            else if (_parameterizedActions.TryGetValue(groups[1].Value, out var pAction))
            {
                object parameter = (groups.Length <= 2)
                    ? match.Index + match.Length
                    : groups[2].Value.Split(',');

                result = pAction(context, parameter);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
    }
}