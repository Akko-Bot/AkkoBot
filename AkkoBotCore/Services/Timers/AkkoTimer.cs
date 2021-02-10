using AkkoBot.Extensions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Timers.Abstractions;
using System;
using System.Timers;

namespace AkkoBot.Services.Timers
{
    /// <summary>
    /// Encapsulates a timed action created by a Discord user.
    /// </summary>
    public sealed class AkkoTimer : IAkkoTimer
    {
        private readonly Timer _internalTimer;
        private readonly DateTimeOffset _startedAt = DateTimeOffset.Now;
        private bool _isDisposed;
        private event ElapsedEventHandler ActionHandler;

        /// <summary>
        /// Fires when this timer gets disposed.
        /// </summary>
        /// <remarks>It fires only once.</remarks>
        public event EventHandler OnDispose;

        /// <summary>
        /// Gets the database ID of this timer.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the interval for how frequently this timer should trigger.
        /// </summary>
        public TimeSpan Interval { get; }

        /// <summary>
        /// Gets whether this timer is active or not.
        /// </summary>
        /// <value><see langword="true"/> if it is active, <see langword="false"/> otherwise.</value>
        public bool Enabled
            => _internalTimer.Enabled;

        /// <summary>
        /// Gets whether this timer disables itself after triggering once.
        /// </summary>
        /// <value><see langword="true"/> if it doesn't disable, <see langword="false"/> otherwise.</value>
        public bool AutoReset
            => _internalTimer.AutoReset;

        /// <summary>
        /// Gets the time when this timer is going to trigger.
        /// </summary>
        public DateTimeOffset ElapsesAt
            => _startedAt.AddMilliseconds(_internalTimer.Interval);

        /// <summary>
        /// Initializes and starts a timer.
        /// </summary>
        /// <param name="entity">Database entry to use as reference to build this timer.</param>
        /// <param name="action">Operation to be performed when this timer triggers.</param>
        public AkkoTimer(TimerEntity entity, Action action)
        {
            Id = entity.Id;
            Interval = entity.Interval;

            // Initialize the timer
            _internalTimer = GetTimer(entity);
            _internalTimer.Elapsed += TriggerAction;

            // Initialize the event handler
            ActionHandler += (x, y) => action();

            // Start the timer
            _internalTimer.Start();
        }

        /// <summary>
        /// Builds this timer based on a database entry.
        /// </summary>
        /// <param name="entity">A database entry.</param>
        /// <returns>A <see cref="Timer"/>> object.</returns>
        private Timer GetTimer(TimerEntity entity)
        {
            var timeDifference = entity.ElapseAt.Subtract(DateTimeOffset.Now);

            timeDifference = (timeDifference <= TimeSpan.Zero)
                ? TimeFromExpiredEntity(entity)
                : TimeFromValidEntry(timeDifference, entity);

            return new Timer(timeDifference.TotalMilliseconds) { AutoReset = entity.IsRepeatable };
        }

        /// <summary>
        /// Builds a timer from a database entry that hasn't expired yet.
        /// </summary>
        private TimeSpan TimeFromValidEntry(TimeSpan timeDifference, TimerEntity entity)
        {
            if (!entity.IsAbsolute)
            {
                //Repeater, Reminder
                timeDifference = entity.Interval;
            }
            else if (entity.IsAbsolute && entity.IsRepeatable)
            {
                // Daily Repeater
                // DateTimeOffset.Add() requires a TimeSpan with whole minutes
                timeDifference = TimeSpan.FromMinutes(Math.Round(Interval.TotalMinutes));
                _startedAt.StartOfDay().Add(timeDifference);
            }

            return timeDifference;
        }

        /// <summary>
        /// Builds a timer from a database entry that has expired.
        /// </summary>
        private TimeSpan TimeFromExpiredEntity(TimerEntity entity)
        {
            TimeSpan result;

            if (entity.IsAbsolute && !entity.IsRepeatable)
            {
                // TimedBan, TimedMute, TimedWarn
                // Trigger 10 seconds after the bot connects
                result = TimeSpan.FromSeconds(10);
            }
            else if (!entity.IsAbsolute)
            {
                // Repeater, Reminder
                result = Interval;
            }
            else
            {
                // Daily Repeater
                // DateTimeOffset.Add() requires a TimeSpan with whole minutes
                result = TimeSpan.FromMinutes(Math.Round(Interval.TotalMinutes));
                _startedAt.StartOfDay().Add(result);
            }

            return result;
        }

        /// <summary>
        /// Runs the operation that needs to be performed every time the timer triggers.
        /// </summary>
        private void TriggerAction(object obj, ElapsedEventArgs args)
        {
            if (_isDisposed) // Stop the callback from doing work if
                return;      // it got triggered before disposal

            // Update the internal clock
            _internalTimer.Interval = Interval.TotalMilliseconds;
            _startedAt.AddMilliseconds(Interval.TotalMilliseconds);

            // Execute the operation
            ActionHandler.Invoke(obj, args);

            // If this timer is not repeatable, get rid of it
            if (!_internalTimer.AutoReset)
                Dispose();
        }

        /// <summary>
        /// Releases all resources used by this <see cref="AkkoTimer"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose the timer
                    _internalTimer.Elapsed -= TriggerAction;
                    _internalTimer.Stop();
                    _internalTimer.Dispose();

                    // Fire the cleanup event
                    OnDispose?.Invoke(this, new EventArgs());
                }

                ActionHandler = null;
                OnDispose = null;

                _isDisposed = true;
            }
        }
    }
}