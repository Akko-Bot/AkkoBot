using System;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace AkkoBot.Core.Common
{
    /// <summary>
    /// Class that encapsulates the prefix resolver.
    /// </summary>
    public class PrefixResolver
    {
        private readonly IUnitOfWork _db;

        public PrefixResolver(IUnitOfWork db)
            => _db = db;

        /// <summary>
        /// Decides whether a Discord message starts with a command prefix.
        /// </summary>
        /// <param name="msg">Message to be processed.</param>
        /// <returns>Positive integer if the prefix is present, -1 otherwise.</returns>
        public async Task<int> ResolvePrefix(DiscordMessage msg)
        {
            // Server prefix needs to be changed
            return (msg.Channel.IsPrivate)
                ? msg.GetStringPrefixLength(_db.BotConfig.Cache.BotPrefix, StringComparison.OrdinalIgnoreCase)
                : msg.GetStringPrefixLength(await _db.GuildConfigs.GetPrefixAsync(msg.Channel.GuildId), StringComparison.OrdinalIgnoreCase);
        }
    }
}