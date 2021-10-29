using AkkoCore.Models.EventArgs;
using Emzi0767.Utilities;
using System;

namespace AkkoCore.Core.Abstractions
{
    /// <summary>
    /// Represents an object that shuts down a <see cref="Bot"/>.
    /// </summary>
    public interface IBotLifetime : IDisposable
    {
        /// <summary>
        /// Fires when the bot shuts down or restarts.
        /// </summary>
        event AsyncEventHandler<IBotLifetime, ShutdownEventArgs> OnShutdown;

        /// <summary>
        /// Defines whether the bot should launch after stopping.
        /// </summary>
        bool RestartBot { get; }

        /// <summary>
        /// Restarts the bot.
        /// </summary>
        void Restart();

        /// <summary>
        /// Restarts the bot after the specified time.
        /// </summary>
        /// <param name="time">Time to wait before restarting.</param>
        void Restart(TimeSpan time);

        /// <summary>
        /// Shuts the bot down.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Shuts the bot down after the specified time.
        /// </summary>
        /// <param name="time">Time to wait before shutting down.</param>
        void Shutdown(TimeSpan time);
    }
}