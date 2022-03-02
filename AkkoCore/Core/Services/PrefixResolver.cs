using AkkoCore.Commands.Attributes;
using AkkoCore.Common;
using AkkoCore.Config.Models;
using AkkoCore.Core.Abstractions;
using AkkoCore.Services.Caching.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Core.Services;

/// <summary>
/// Processes Discord messages for the presence of a command prefix.
/// </summary>
[CommandService<IPrefixResolver>(ServiceLifetime.Singleton)]
internal sealed class PrefixResolver : IPrefixResolver
{
    private readonly IDbCache _dbCache;
    private readonly BotConfig _botConfig;
    private readonly DiscordShardedClient _shardedClient;

    public PrefixResolver(IDbCache dbCache, BotConfig botConfig, DiscordShardedClient shardedClient)
    {
        _dbCache = dbCache;
        _botConfig = botConfig;
        _shardedClient = shardedClient;
    }

    public async Task<int> ResolvePrefixAsync(DiscordMessage msg)
    {
        if (!_botConfig.MentionPrefix)
            return await ResolveNonMentionPrefixAsync(msg);

        var match = AkkoRegexes.DiscordUser.Match(msg.Content);
        var isMentionPrefix = match.Success && ulong.TryParse(match.Groups[1].ToString(), out var userId)
            && userId == _shardedClient.CurrentUser.Id && msg.Content.StartsWith(match.Value, StringComparison.Ordinal);

        return (isMentionPrefix)
            ? match.Groups[0].Length
            : await ResolveNonMentionPrefixAsync(msg);
    }

    /// <summary>
    /// Resolves prefixes that are not mentions to the bot.
    /// </summary>
    /// <param name="message">The user's message.</param>
    /// <returns>Length of the prefix, if present. -1 otherwise.</returns>
    private async Task<int> ResolveNonMentionPrefixAsync(DiscordMessage message)
    {
        return (message.Channel.IsPrivate) // Server prefix needs to be changed
            ? message.GetStringPrefixLength(_botConfig.Prefix)
            : message.GetStringPrefixLength((await _dbCache.GetDbGuildAsync(message.Channel.Guild.Id, _botConfig)).Prefix);
    }
}