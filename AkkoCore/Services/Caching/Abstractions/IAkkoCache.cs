using AkkoCore.Services.Timers.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kotz.Collections;
using System;
using System.Collections.Concurrent;

namespace AkkoCore.Services.Caching.Abstractions;

/// <summary>
/// Represents an object responsible for caching external data or objects that need to be lazily instantiated.
/// </summary>
public interface IAkkoCache : IDisposable
{
    /// <summary>
    /// Contains messages cached from a Discord guild.
    /// </summary>
    /// <remarks>The <see langword="ulong"/> is the ID of the guild. Messages are only cached if the guild is logging messages.</remarks>
    ConcurrentDictionary<ulong, RingBuffer<DiscordMessage>> GuildMessageCache { get; }

    /// <summary>
    /// Contains commands that have been globally disabled.
    /// </summary>
    /// <remarks>
    /// The <see langword="string"/> is the command's qualified name.
    /// This property is lazily initialized on startup.
    /// </remarks>
    ConcurrentDictionary<string, Command> DisabledCommandCache { get; }

    /// <summary>
    /// Manages creation, execution and removal of <see cref="IAkkoTimer"/>s.
    /// </summary>
    /// <remarks>This property is lazily initialized on startup.</remarks>
    ITimerManager Timers { get; }

    /// <summary>
    /// Clears the message cache for the Discord guild of the specified ID.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <returns><see langword="true"/> if the cache was successfully removed, <see langword="false"/> if there was no cache.</returns>
    bool UnloadMessageCache(ulong sid);
}