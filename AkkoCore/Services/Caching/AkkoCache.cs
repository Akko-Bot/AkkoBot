using AkkoCore.Commands.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Timers.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace AkkoCore.Services.Caching
{
    /// <summary>
    /// Defines an object that caches Discord-related elements.
    /// </summary>
    public class AkkoCache : IAkkoCache
    {
        public ConcurrentDictionary<ulong, RingBuffer<DiscordMessage>> GuildMessageCache { get; private set; } = new();
        public ConcurrentDictionary<string, Command> DisabledCommandCache { get; internal set; }
        public ITimerManager Timers { get; internal set; }
        public ICommandCooldown CooldownCommands { get; private set; }

        public AkkoCache(IServiceScopeFactory scopeFactory, ICommandCooldown cmdCooldown)
        {
            using var scope = scopeFactory.GetScopedService<AkkoDbContext>(out var dbContext);
            CooldownCommands = cmdCooldown.LoadFromEntities(dbContext.CommandCooldown.ToArray());
        }

        public void Dispose()
        {
            foreach (var messageCache in GuildMessageCache.Values)
                messageCache.Clear();

            GuildMessageCache?.Clear();
            DisabledCommandCache?.Clear();
            Timers?.Dispose();
            CooldownCommands?.Dispose();

            GuildMessageCache = null;
            DisabledCommandCache = null;
            Timers = null;
            CooldownCommands = null;

            GC.SuppressFinalize(this);
        }
    }
}