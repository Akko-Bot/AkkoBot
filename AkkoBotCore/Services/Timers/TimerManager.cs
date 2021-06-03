using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Timers.Abstractions;
using DSharpPlus;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace AkkoBot.Services.Timers
{
    /// <summary>
    /// This class initializes timers from the database and manages their execution and lifetime.
    /// </summary>
    public class TimerManager : ITimerManager
    {
        private const int _updateTimerDayAge = 1;
        private readonly IServiceProvider _services;
        private readonly ITimerActions _action;
        private readonly Timer _updateTimer = new(TimeSpan.FromDays(_updateTimerDayAge).TotalMilliseconds);
        private readonly ConcurrentDictionary<int, IAkkoTimer> _timers = new();
        private ImmutableArray<TimerEntity> _timerEntries = ImmutableArray.Create<TimerEntity>();
        private bool _isFlushed = true;

        public TimerManager(IServiceProvider services, ITimerActions action)
        {
            _services = services;
            _action = action;

            // Initialize the internal update timer
            _updateTimer.Elapsed += UpdateFromDb;
            _updateTimer.Elapsed += RemoveLongRunningTimers;
            _updateTimer.Start();
        }

        public bool TryGetValue(int id, out IAkkoTimer timer)
            => _timers.TryGetValue(id, out timer);

        public bool TryRemove(IAkkoTimer timer)
            => TryRemove(timer.Id);

        public bool TryAdd(IAkkoTimer timer)
        {
            if (_timers.TryAdd(timer.Id, timer))
            {
                timer.OnDispose += TimerAutoRemoval;
                return true;
            }

            return false;
        }

        public bool TryRemove(int id)
        {
            if (_timers.TryRemove(id, out var oldTimer))
            {
                oldTimer.OnDispose -= TimerAutoRemoval;
                oldTimer.Dispose();
                return true;
            }

            return false;
        }

        public bool TryUpdate(IAkkoTimer timer)
        {
            if (!_timers.TryGetValue(timer.Id, out var oldTimer) && _timers.TryUpdate(timer.Id, timer, oldTimer))
            {
                timer.OnDispose += TimerAutoRemoval;
                oldTimer.OnDispose -= TimerAutoRemoval;
                oldTimer.Dispose();
                return true;
            }

            return false;
        }

        public bool AddOrUpdateByEntity(DiscordClient client, TimerEntity entity)
        {
            if (!entity.IsActive || entity.ElapseAt.Subtract(DateTimeOffset.Now) > TimeSpan.FromDays(_updateTimerDayAge))
                return false;

            var timer = GetTimer(client, entity);

            return (_timers.ContainsKey(entity.Id))
                ? TryUpdate(timer)
                : TryAdd(timer);
        }

        public async ValueTask<bool> CreateClientTimersAsync(DiscordClient client)
        {
            var cachedTimersAmount = _timers.Count;

            if (_timerEntries.Length is 0 && _isFlushed)
            {
                _isFlushed = false;
                await LoadDbEntriesCacheAsync();
                _updateTimer.Elapsed += DeleteDbEntriesCache;
            }

            var timerEntries = _timerEntries.Where(x => x.ElapseAt.Subtract(DateTimeOffset.Now) <= TimeSpan.FromDays(_updateTimerDayAge));
            GenerateTimers(timerEntries, client);

            return _timers.Count > cachedTimersAmount;
        }

        /// <summary>
        /// Generates and initializes timers from a collection of database entries.
        /// </summary>
        /// <param name="entities">The collection of database entries.</param>
        /// <param name="client">A Discord client to get its visible guilds from.</param>
        private void GenerateTimers(IEnumerable<TimerEntity> entities, DiscordClient client)
        {
            var guildEntities = entities.Where(entity =>
                !_timers.ContainsKey(entity.Id)
                && entity.GuildIdFK.HasValue
                && client.Guilds.ContainsKey(entity.GuildIdFK.Value)
            );

            var nonGuildEntities = entities.Where(entity =>
                !_timers.ContainsKey(entity.Id)
                && !entity.GuildIdFK.HasValue
            );

            foreach (var entity in guildEntities)
                _timers.TryAdd(entity.Id, GetTimer(client, entity));

            foreach (var entity in nonGuildEntities)
                _timers.TryAdd(entity.Id, GetTimer(client, entity));
        }

        /// <summary>
        /// Gets and initializes a timer based on a dabatase entry.
        /// </summary>
        /// <param name="client">The Discord client that fetched the database entry.</param>
        /// <param name="entity">The database entry.</param>
        /// <returns>An active <see cref="AkkoTimer"/>.</returns>
        private IAkkoTimer GetTimer(DiscordClient client, TimerEntity entity)
        {
            client.Guilds.TryGetValue(entity.GuildIdFK ?? default, out var server);

            var timer = entity.Type switch
            {
                TimerType.TimedBan => new AkkoTimer(entity, () => _action.UnbanAsync(entity.Id, server, entity.UserIdFK)),
                TimerType.TimedMute => new AkkoTimer(entity, () => _action.UnmuteAsync(entity.Id, server, entity.UserIdFK)),
                TimerType.TimedWarn => new AkkoTimer(entity, () => _action.RemoveOldWarningAsync(entity.Id, server, entity.UserIdFK)),
                TimerType.TimedRole => new AkkoTimer(entity, () => _action.AddPunishRoleAsync(entity.Id, server, entity.UserIdFK)),
                TimerType.TimedUnrole => new AkkoTimer(entity, () => _action.RemovePunishRoleAsync(entity.Id, server, entity.UserIdFK)),
                TimerType.Reminder => new AkkoTimer(entity, () => _action.SendReminderAsync(entity.Id, client, server)),
                TimerType.Repeater => new AkkoTimer(entity, () => _action.SendRepeaterAsync(entity.Id, client, server)),
                TimerType.Command => new AkkoTimer(entity, () => _action.ExecuteCommandAsync(entity.Id, client, server)),
                _ => throw new NotImplementedException($"Timer of type {entity.Type} has no action implemented."),
            };

            timer.OnDispose += TimerAutoRemoval;

            // If it's a daily timer, set it to automatically create the permanent version of itself
            if (entity.TimeOfDay.HasValue && entity.Interval != TimeSpan.FromDays(1))
                timer.OnDispose += async (timer, _) => await UpdateDailyTimer(timer as IAkkoTimer, client);

            return timer;
        }

        /// <summary>
        /// Updates a daily timer with a definitive version of itself.
        /// </summary>
        /// <param name="oldTimer">The timer that's currently being disposed.</param>
        /// <param name="client">The Discord client that fetched the database entry</param>
        private async Task UpdateDailyTimer(IAkkoTimer oldTimer, DiscordClient client)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);
            var dbTimer = await db.Timers.FindAsync(oldTimer.Id);

            // Update database
            if (dbTimer is not null)
            {
                dbTimer.Interval = TimeSpan.FromDays(1);
                dbTimer.ElapseAt = dbTimer.ElapseAt.Add(dbTimer.Interval);
                db.Update(dbTimer);

                await db.SaveChangesAsync();

                AddOrUpdateByEntity(client, dbTimer);
            }
        }

        /// <summary>
        /// Loads all elegible database timers into memory.
        /// </summary>
        private async Task LoadDbEntriesCacheAsync()
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            _timerEntries = _timerEntries.AddRange(
                await db.Timers
                    .Where(x => x.IsActive && x.ElapseAt - DateTimeOffset.Now <= TimeSpan.FromDays(_updateTimerDayAge))
                    .ToArrayAsyncLinqToDB()
            );
        }

        /// <summary>
        /// Reads the next timers from the database to be elapsed, initializes and caches them.
        /// </summary>
        private void UpdateFromDb(object obj, ElapsedEventArgs args)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);
            var clients = _services.GetService<DiscordShardedClient>();

            var nextEntries = db.Timers
                .ToLinqToDB()
                .Where(x => x.IsActive && x.ElapseAt - DateTimeOffset.Now <= TimeSpan.FromDays(_updateTimerDayAge))
                .ToArray();

            foreach (var client in clients.ShardClients.Values)
                GenerateTimers(nextEntries, client);
        }

        /// <summary>
        /// Ensures that no long-running timer remains in the cache.
        /// </summary>
        private void RemoveLongRunningTimers(object obj, ElapsedEventArgs args)
        {
            foreach (var timer in _timers.Values.Where(x => x.ElapseIn > TimeSpan.FromDays(_updateTimerDayAge)))
                this.TryRemove(timer);
        }

        /// <summary>
        /// Clears the timer entries cache, as it is only needed on startup.
        /// </summary>
        private void DeleteDbEntriesCache(object obj, ElapsedEventArgs args)
        {
            _isFlushed = true;
            _timerEntries = _timerEntries.Clear();
            _updateTimer.Elapsed -= DeleteDbEntriesCache;
        }

        /// <summary>
        /// Removes a timer from the internal cache.
        /// </summary>
        /// <remarks>
        /// This method is used by <see cref="IAkkoTimer"/> objects to notify
        /// the <see cref="TimerManager"/> that they need to be removed from the cache.
        /// </remarks>
        /// <param name="obj">An object representing an <see cref="IAkkoTimer"/>.</param>
        /// <param name="args">Event arguments.</param>
        private void TimerAutoRemoval(object obj, EventArgs args)
        {
            var timer = obj as IAkkoTimer;
            _timers.TryRemove(timer.Id, out _);
        }

        public void Dispose()
        {
            // Clear the timer cache
            foreach (var timer in _timers.Values)
            {
                timer.OnDispose -= TimerAutoRemoval;
                timer.Dispose();
            }

            _timers.Clear();
            _timerEntries = _timerEntries.Clear();

            // Dispose the internal timer
            _updateTimer.Elapsed -= UpdateFromDb;
            _updateTimer.Elapsed -= RemoveLongRunningTimers;
            _updateTimer.Stop();
            _updateTimer.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}