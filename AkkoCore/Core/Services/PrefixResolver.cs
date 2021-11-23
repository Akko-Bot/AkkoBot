using AkkoCore.Commands.Attributes;
using AkkoCore.Config.Models;
using AkkoCore.Core.Abstractions;
using AkkoCore.Services.Caching.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
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

    public PrefixResolver(IDbCache dbCache, BotConfig botConfig)
    {
        _dbCache = dbCache;
        _botConfig = botConfig;
    }

    public async Task<int> ResolvePrefixAsync(DiscordMessage msg)
    {
        // Server prefix needs to be changed
        return (msg.Channel.IsPrivate)
            ? msg.GetStringPrefixLength(_botConfig.Prefix)
            : msg.GetStringPrefixLength((await _dbCache.GetDbGuildAsync(msg.Channel.Guild.Id, _botConfig)).Prefix);
    }
}