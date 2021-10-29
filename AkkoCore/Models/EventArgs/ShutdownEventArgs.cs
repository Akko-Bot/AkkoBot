using Emzi0767.Utilities;
using System;

namespace AkkoCore.Models.EventArgs
{
    /// <summary>
    /// Defines event arguments for when the bot is shutting down or restarting.
    /// </summary>
    public class ShutdownEventArgs : AsyncEventArgs
    {
        /// <summary>
        /// Defines whether the bot is scheduled for a restart.
        /// </summary>
        public bool IsRestarting { get; init; }

        /// <summary>
        /// Defines when the shutdown request was issued.
        /// </summary>
        public DateTimeOffset RequestedAt { get; init; }

        /// <summary>
        /// Defines how long it took for the request to be processed.
        /// </summary>
        /// <remarks>This equals <see cref="TimeSpan.Zero"/> when there was no delay.</remarks>
        public TimeSpan DelayTime { get; init; }

        /// <summary>
        /// Defines the time the request was processed.
        /// </summary>
        public DateTimeOffset ExecutedAt { get; }

        /// <summary>
        /// Initializes a <see cref="ShutdownEventArgs"/>.
        /// </summary>
        public ShutdownEventArgs()
        {
            RequestedAt = DateTimeOffset.Now;
            DelayTime = TimeSpan.Zero;
            ExecutedAt = RequestedAt;
        }

        /// <summary>
        /// Initializes a <see cref="ShutdownEventArgs"/>.
        /// </summary>
        /// <param name="requestedAt">The time the shutdown was requested.</param>
        /// <param name="delayTime">The time to wait before the request is processed.</param>
        /// <param name="isRestarting">Defines whether the bot is restarting after shutdown or not.</param>
        public ShutdownEventArgs(DateTimeOffset requestedAt, TimeSpan delayTime, bool isRestarting)
        {
            IsRestarting = isRestarting;
            RequestedAt = requestedAt;
            DelayTime = delayTime;
            ExecutedAt = RequestedAt.Add(DelayTime);
        }
    }
}
