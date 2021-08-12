using System;

namespace AkkoCore.Services.Timers.Abstractions
{
    /// <summary>
    /// Represents an object that performs an action when a certain point in time is reached.
    /// </summary>
    public interface IAkkoTimer : IDisposable
    {
        /// <summary>
        /// Gets the database ID of this timer.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the interval for how frequently this timer should trigger.
        /// </summary>
        TimeSpan Interval { get; }

        /// <summary>
        /// Gets the date time when this timer is going to trigger.
        /// </summary>
        DateTimeOffset ElapseAt { get; }

        /// <summary>
        /// Gets the time when this timer is going to trigger.
        /// </summary>
        TimeSpan ElapseIn { get; }

        /// <summary>
        /// Gets whether this timer is active or not.
        /// </summary>
        /// <value><see langword="true"/> if it is active, <see langword="false"/> otherwise.</value>
        bool Enabled { get; }

        /// <summary>
        /// Gets whether this timer disables itself after triggering once.
        /// </summary>
        /// <value><see langword="true"/> if it doesn't disable, <see langword="false"/> otherwise.</value>
        bool AutoReset { get; }

        /// <summary>
        /// Triggers when this timer gets disposed.
        /// </summary>
        /// <remarks>It triggers only once.</remarks>
        event EventHandler OnDispose;
    }
}