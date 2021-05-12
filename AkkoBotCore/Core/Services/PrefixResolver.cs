using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Core.Services
{
    /// <summary>
    /// Class that encapsulates the prefix resolver.
    /// </summary>
    internal class PrefixResolver
    {
        private readonly IDbCache _dbCache;

        internal PrefixResolver(IDbCache dbCache)
            => _dbCache = dbCache;

        /// <summary>
        /// Decides whether a Discord message starts with a command prefix.
        /// </summary>
        /// <param name="msg">Message to be processed.</param>
        /// <returns>Positive integer if the prefix is present, -1 otherwise.</returns>
        internal async Task<int> ResolvePrefixAsync(DiscordMessage msg)
        {
            // Server prefix needs to be changed
            return (msg.Channel.IsPrivate)
                ? msg.GetStringPrefixLength(_dbCache.BotConfig.BotPrefix)
                : msg.GetStringPrefixLength((await _dbCache.GetDbGuildAsync(msg.Channel.Guild.Id)).Prefix);
        }
    }
}