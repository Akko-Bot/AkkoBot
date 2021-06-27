using AkkoBot.Commands.Abstractions;
using AkkoBot.Services.Timers.Abstractions;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Concurrent;

namespace AkkoBot.Services.Caching.Abstractions
{
    /// <summary>
    /// Represents an object responsible for caching whose instantiation depends partially on external data.
    /// </summary>
    public interface IAkkoCache : IDisposable
    {
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
        /// Manages command cooldown.
        /// </summary>
        ICommandCooldown CooldownCommands { get; }
    }
}