using AkkoCore.Commands.Attributes;
using AkkoCore.Common;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Timers.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;

namespace AkkoCore.Services.Caching;

/// <summary>
/// Defines an object that caches Discord-related elements.
/// </summary>
[CommandService<IAkkoCache>(ServiceLifetime.Singleton)]
public sealed class AkkoCache : IAkkoCache
{
    public ConcurrentDictionary<ulong, DynamicRingBuffer<DiscordMessage>> GuildMessageCache { get; private set; } = new();
    public ConcurrentDictionary<string, Command> DisabledCommandCache { get; internal set; } = null!;
    public ITimerManager Timers { get; private set; }

    public AkkoCache(ITimerManager timerManager)
        => Timers = timerManager;

    public void Dispose()
    {
        foreach (var messageCache in GuildMessageCache.Values)
            messageCache.Clear();

        GuildMessageCache?.Clear();
        DisabledCommandCache?.Clear();
        Timers?.Dispose();

        GuildMessageCache = null!;
        DisabledCommandCache = null!;
        Timers = null!;

        GC.SuppressFinalize(this);
    }
}