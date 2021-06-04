using AkkoBot.Config;
using AkkoBot.Core.Services.Abstractions;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Core.Services
{
    /// <summary>
    /// Processes Discord messages for the presence of a command prefix.
    /// </summary>
    internal class PrefixResolver : IPrefixResolver
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
                ? msg.GetStringPrefixLength(_botConfig.BotPrefix)
                : msg.GetStringPrefixLength((await _dbCache.GetDbGuildAsync(msg.Channel.Guild.Id)).Prefix);
        }
    }
}