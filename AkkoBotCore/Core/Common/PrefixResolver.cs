using System;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Services.Database;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace AkkoBot.Core.Common
{
    public class PrefixResolver
    {
        private readonly AkkoUnitOfWork _db;

        public PrefixResolver(AkkoUnitOfWork db)
            => _db = db;

        public async Task<int> ResolvePrefix(DiscordMessage msg)
        {
            // Server prefix needs to be changed
            return (msg.Channel.IsPrivate)
                ? msg.GetStringPrefixLength(_db.BotConfig.Cache.DefaultPrefix, StringComparison.OrdinalIgnoreCase)
                : msg.GetStringPrefixLength(await _db.GuildConfigs.GetPrefixAsync(msg.Channel.GuildId), StringComparison.OrdinalIgnoreCase);
        }
    }
}