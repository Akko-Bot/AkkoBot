using AkkoCore.Common;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Timers.Abstractions;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace AkkoCore.Services.Timers
{
    /// <summary>
    /// Encapsulates a timed action created by a Discord user.
    /// </summary>
    public sealed class AkkoTimer : IAkkoTimer
    {
        private readonly Timer _internalTimer;
        private bool _isDisposed;

        private event ElapsedEventHandler ActionHandler;

        public event EventHandler OnDispose = null!;

        public int Id { get; }
        public TimeSpan Interval { get; }
        public DateTimeOffset ElapseAt { get; private set; }

        public TimeSpan ElapseIn
            => ElapseAt.Subtract(DateTimeOffset.Now);

        public bool Enabled
            => _internalTimer.Enabled;

        public bool AutoReset
            => _internalTimer.AutoReset;

        /// <summary>
        /// Initializes and starts a timer.
        /// </summary>
        /// <param name="entity">Database entry to use as reference to build this timer.</param>
        /// <param name="action">Operation to be performed when this timer triggers.</param>
        public AkkoTimer(TimerEntity entity, Func<Task> action)
        {
            var isProvisory = entity.TimeOfDay.HasValue && entity.Interval != TimeSpan.FromDays(1);

            Id = entity.Id;
            Interval = (isProvisory) ? TimeSpan.FromDays(1) : entity.Interval;
            ElapseAt = entity.ElapseAt;

            // Initialize the timer
            _internalTimer = GetTimer(entity, isProvisory);
            _internalTimer.Elapsed += TriggerAction;

            // Initialize the event handler
            ActionHandler += async (_, _) => await action();

            // Start the timer
            _internalTimer.Start();
        }

        /// <summary>
        /// Builds this timer based on a database entry.
        /// </summary>
        /// <param name="entity">A database entry.</param>
        /// <param name="isProvisory">Determines whether this timer is going to be replaced after it triggers.</param>
        /// <returns>A <see cref="Timer"/> object.</returns>
        private Timer GetTimer(TimerEntity entity, bool isProvisory)
        {
            var timeLeft = entity.ElapseAt.Subtract(DateTimeOffset.Now);

            timeLeft = (timeLeft <= TimeSpan.Zero)
                ? TimeFromExpiredEntity(entity)
                : TimeFromValidEntry(timeLeft, entity);

            return new Timer(timeLeft.TotalMilliseconds) { AutoReset = entity.IsRepeatable && !isProvisory };
        }

        /// <summary>
        /// Gets the time interval from a database entry that hasn't expired yet.
        /// </summary>
        private TimeSpan TimeFromValidEntry(TimeSpan timeDifference, TimerEntity entity)
        {
            return (entity.IsRepeatable && entity.TimeOfDay.HasValue)
                ? TimeOfDay.GetInterval(entity.ElapseAt)
                : (entity.IsRepeatable) ? TimeSpan.FromMinutes(entity.Interval.TotalMinutes) : timeDifference;
        }

        /// <summary>
        /// Gets the time interval from a database entry that has expired.
        /// </summary>
        private TimeSpan TimeFromExpiredEntity(TimerEntity entity)
        {
            var result = (entity.IsRepeatable && entity.TimeOfDay.HasValue)
                ? TimeOfDay.GetInterval(entity.ElapseAt)
                : (entity.IsRepeatable) ? TimeSpan.FromMinutes(entity.Interval.TotalMinutes) : TimeSpan.FromSeconds(10);

            ElapseAt = DateTimeOffset.Now.Add(result);
            return result;
        }

        /// <summary>
        /// Runs the operation that needs to be performed every time the timer triggers.
        /// </summary>
        private void TriggerAction(object obj, ElapsedEventArgs args)
        {
            if (_isDisposed) // Prevent the callback from doing work
                return;      // if it got triggered before disposal

            // Update the internal clock
            _internalTimer.Interval = Interval.TotalMilliseconds;
            ElapseAt = ElapseAt.Add(Interval);

            // Execute the operation
            ActionHandler.Invoke(obj, args);

            // If this timer is not repeatable, get rid of it
            if (!_internalTimer.AutoReset)
                Dispose();
        }

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
                    _internalTimer.AutoReset = false;
                    _internalTimer.Elapsed -= TriggerAction;
                    _internalTimer.Stop();
                    _internalTimer.Dispose();

                    // Fire the cleanup event
                    OnDispose?.Invoke(this, EventArgs.Empty);
                }

                ActionHandler = null!;
                OnDispose = null!;

                _isDisposed = true;
            }
        }
    }
}