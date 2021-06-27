using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Timers.Abstractions;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace AkkoBot.Services.Caching
{
    /// <summary>
    /// Defines an object that caches Discord-related elements.
    /// </summary>
    public class AkkoCache : IAkkoCache
    {
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
            DisabledCommandCache?.Clear();
            Timers?.Dispose();
            CooldownCommands?.Dispose();

            DisabledCommandCache = null;
            Timers = null;
            CooldownCommands = null;

            GC.SuppressFinalize(this);
        }
    }
}