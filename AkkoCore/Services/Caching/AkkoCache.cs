﻿using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Timers.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Concurrent;

namespace AkkoCore.Services.Caching
{
    /// <summary>
    /// Defines an object that caches Discord-related elements.
    /// </summary>
    public class AkkoCache : IAkkoCache
    {
        public ConcurrentDictionary<ulong, RingBuffer<DiscordMessage>> GuildMessageCache { get; private set; } = new();
        public ConcurrentDictionary<string, Command> DisabledCommandCache { get; internal set; }
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

            GuildMessageCache = null;
            DisabledCommandCache = null;
            Timers = null;

            GC.SuppressFinalize(this);
        }
    }
}