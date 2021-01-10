using System;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Services.Database;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace AkkoBot.Core.Common
{
    public class PrefixResolver : ICommandService
    {
        private readonly AkkoDbCacher _dbCache;

        public PrefixResolver(AkkoDbCacher dbCache)
            => _dbCache = dbCache;

        public Task<int> ResolvePrefix(DiscordMessage msg)
        {
            return (msg.Channel.IsPrivate)
                ? Task.FromResult(msg.GetStringPrefixLength(_dbCache.DefaultPrefix, StringComparison.OrdinalIgnoreCase))
                : Task.FromResult(msg.GetStringPrefixLength(_dbCache.Guilds[msg.Channel.GuildId].Prefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}