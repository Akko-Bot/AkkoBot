using AkkoCore.Commands.Abstractions;
using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AkkoCore.Commands.Formatters
{
    /// <summary>
    /// Defines the base formatter for command placeholders.
    /// </summary>
    public class CommandPlaceholders : IPlaceholderFormatter
    {
        /// <summary>
        /// Stores actions for placeholders with no parameters.
        /// </summary>
        protected readonly Dictionary<string, Func<CommandContext, object>> placeholderActions = new()
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
            ["bot.prefix"] = (context) => context.Services.GetRequiredService<BotConfig>().Prefix,
            ["bot.latency"] = (context) => context.Client.Ping,
            ["bot.shard"] = (context) => context.Client.ShardId,
            ["bot.shardcount"] = (context) => context.Client.ShardCount,
            ["bot.servercount"] = (context) => context.Client.Guilds.Count,
            ["bot.servertotal"] = (context) => context.CommandsNext.Services.GetRequiredService<DiscordShardedClient>().ShardClients.Values.Sum(client => client.Guilds.Count),
            ["bot.usercount"] = (context) => context.CommandsNext.Services.GetRequiredService<DiscordShardedClient>().ShardClients.Values.Sum(client => client.Guilds.Values.Sum(server => server.MemberCount)),

            /* Server Placeholders */

            ["server.id"] = (context) => context.Guild?.Id,
            ["server.name"] = (context) => context.Guild?.Name,
            ["server.members"] = (context) => context.Guild?.MemberCount,
            ["server.boosters"] = (context) => context.Guild.PremiumSubscriptionCount ?? 0,
            ["server.boostlevel"] = (context) => (int)context.Guild.PremiumTier,
            ["server.defaultchannel"] = (context) => context.Guild?.GetDefaultChannel().Mention,
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
            ["channel.visibleto"] = (context) => context.Channel.Users.Count,

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
            ["user.roles"] = (context) => string.Join(", ", context.Member?.Roles.Select(x => x.Name)),
            ["user.voicechat"] = (context) => context.Member?.VoiceState?.Channel.Name,

            /* Discord Timestamps */
            ["datenow.shortdateandtime"] = (context) => DateTimeOffset.Now.ToDiscordTimestamp(TimestampFormat.ShortDateTime),
            ["datenow.shortdate"] = (context) => DateTimeOffset.Now.ToDiscordTimestamp(TimestampFormat.ShortDate),
            ["datenow.shorttime"] = (context) => DateTimeOffset.Now.ToDiscordTimestamp(TimestampFormat.ShortTime),
            ["datenow.longdateandtime"] = (context) => DateTimeOffset.Now.ToDiscordTimestamp(TimestampFormat.LongDateTime),
            ["datenow.longdate"] = (context) => DateTimeOffset.Now.ToDiscordTimestamp(TimestampFormat.LongDate),
            ["datenow.longtime"] = (context) => DateTimeOffset.Now.ToDiscordTimestamp(TimestampFormat.LongTime),
            ["datenow.relativetime"] = (context) => DateTimeOffset.Now.ToDiscordTimestamp(TimestampFormat.RelativeTime),

            /* Miscelaneous */

            ["rng"] = (context) => context.Services.GetRequiredService<Random>().Next(),
            ["cmd.argument"] = (context) => context.RawArgumentString,

            /* Ban Template Placeholders - Only works on ban templates */

            ["ban.mod"] = (context) => !context.Command.Name.Equals("ban", StringComparison.InvariantCultureIgnoreCase) ? null : context.User.GetFullname(),

            ["ban.user"] = (context) =>
            {
                if (!context.Command.Name.Equals("ban", StringComparison.InvariantCultureIgnoreCase))
                    return null;

                // This will break if DiscordMember is not the first argument
                if (!ulong.TryParse(context.RawArguments[0], out var userId))
                    ulong.TryParse(context.RawArguments[0].GetDigits(), out userId);

                context.Guild.Members.TryGetValue(userId, out var user);

                return user?.GetFullname();
            },

            ["ban.reason"] = (context) =>
            {
                if (!context.Command.Name.Equals("ban", StringComparison.InvariantCultureIgnoreCase))
                    return null;

                var cmdArguments = new List<CommandArgument>(context.Overload.Arguments);
                var index = cmdArguments.FindIndex(x => x.Name.Equals("reason", StringComparison.Ordinal));

                return context.RawArguments[index];
            }
        };

        /// <summary>
        /// Stores actions for placeholders with parameters.
        /// </summary>
        protected readonly Dictionary<string, Func<CommandContext, object, object>> parameterizedActions = new()
        {
            #region Discord Timestamps

            ["date.shortdateandtime"] = (context, parameter) =>
            {
                return parameter is not string[] arguments || arguments.Length != 1
                    || !long.TryParse(arguments[0], out var unixSeconds)
                    ? null
                    : DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToDiscordTimestamp(TimestampFormat.ShortDateTime);
            },

            ["date.shortdate"] = (context, parameter) =>
            {
                return parameter is not string[] arguments || arguments.Length != 1
                    || !long.TryParse(arguments[0], out var unixSeconds)
                    ? null
                    : DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToDiscordTimestamp(TimestampFormat.ShortDate);
            },

            ["date.shorttime"] = (context, parameter) =>
            {
                return parameter is not string[] arguments || arguments.Length != 1
                    || !long.TryParse(arguments[0], out var unixSeconds)
                    ? null
                    : DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToDiscordTimestamp(TimestampFormat.ShortTime);
            },

            ["date.longdateandtime"] = (context, parameter) =>
            {
                return parameter is not string[] arguments || arguments.Length != 1
                    || !long.TryParse(arguments[0], out var unixSeconds)
                    ? null
                    : DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToDiscordTimestamp(TimestampFormat.LongDateTime);
            },

            ["date.longdate"] = (context, parameter) =>
            {
                return parameter is not string[] arguments || arguments.Length != 1
                    || !long.TryParse(arguments[0], out var unixSeconds)
                    ? null
                    : DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToDiscordTimestamp(TimestampFormat.LongDate);
            },

            ["date.longtime"] = (context, parameter) =>
            {
                return parameter is not string[] arguments || arguments.Length != 1
                    || !long.TryParse(arguments[0], out var unixSeconds)
                    ? null
                    : DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToDiscordTimestamp(TimestampFormat.LongTime);
            },

            ["date.relativetime"] = (context, parameter) =>
            {
                return parameter is not string[] arguments || arguments.Length != 1
                    || !long.TryParse(arguments[0], out var unixSeconds)
                    ? null
                    : DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToDiscordTimestamp(TimestampFormat.RelativeTime);
            },

            #endregion Discord Timestamps

            #region Miscelaneous

            ["remaining.text"] = (context, endMatchIndex) => context.RawArgumentString[(int)endMatchIndex..].Trim(),

            ["rng"] = (context, parameter) =>
            {
                // If array is the wrong length or contains invalid arguments, quit
                if (parameter is not string[] arguments || arguments.Length is not 1 and not 2
                    | (!arguments.TryGetValue(0, out var first) & !arguments.TryGetValue(1, out var second))
                    | (!int.TryParse(first, out var x) & !int.TryParse(second, out var y)))
                    return null;

                if (x > y)
                    (x, y) = (y, x);

                return context.Services.GetRequiredService<Random>().Next(x, y);
            },

            ["choose"] = (context, parameter) =>
            {
                return (parameter is not string[] arguments || arguments.Length == 0)
                    ? null
                    : arguments[context.Services.GetRequiredService<Random>().Next(0, arguments.Length)].Trim();
            },

            ["cmd.argument"] = (context, parameter) =>
            {
                return (parameter is not string[] arguments || arguments.Length != 1
                || !int.TryParse(arguments[0], out var index)
                || context.RawArguments.Count <= index)
                    ? null
                    : context.RawArguments[index];
            }

            #endregion Miscelaneous
        };

        public virtual bool TryParse(CommandContext context, Match match, out object result)
        {
            var groups = match.Groups.Values
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))    // This is needed because for some reason groups can contain empty values.
                .ToArray();                                         // Contains capture group + matches.

            if (groups.Length <= 2 && placeholderActions.TryGetValue(groups[1].Value, out var action))
            {
                result = action(context);
                return true;
            }
            else if (parameterizedActions.TryGetValue(groups[1].Value, out var pAction))
            {
                object parameter = (groups.Length <= 2) // If there is no user provided parameter
                    ? match.Index + match.Length    // int
                    : groups[2].Value.Split(',');   // string[]

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