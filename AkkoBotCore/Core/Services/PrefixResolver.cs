using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Core.Services
{
    /// <summary>
    /// Class that encapsulates the prefix resolver.
    /// </summary>
    internal class PrefixResolver
    {
        private readonly IUnitOfWork _db;

        internal PrefixResolver(IUnitOfWork db)
            => _db = db;

        /// <summary>
        /// Decides whether a Discord message starts with a command prefix.
        /// </summary>
        /// <param name="msg">Message to be processed.</param>
        /// <returns>Positive integer if the prefix is present, -1 otherwise.</returns>
        internal Task<int> ResolvePrefix(DiscordMessage msg)
        {
            // Server prefix needs to be changed
            return (msg.Channel.IsPrivate)
                ? Task.FromResult(msg.GetStringPrefixLength(_db.BotConfig.Cache.BotPrefix, StringComparison.OrdinalIgnoreCase))
                : Task.FromResult(msg.GetStringPrefixLength(_db.GuildConfig.GetGuild(msg.Channel.GuildId).Prefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}