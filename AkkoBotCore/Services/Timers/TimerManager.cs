using System.Collections.Generic;
using System;
using AkkoBot.Services.Database;
using System.Collections.Concurrent;
using System.Linq;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using AkkoBot.Services.Timers.Abstractions;

namespace AkkoBot.Services.Timers
{
    /// <summary>
    /// This class initializes timers from the database and manages their execution and lifetime.
    /// </summary>
    public class TimerManager : ITimerManager
    {
        private const int _timerDayAge = 2;
        private readonly ConcurrentDictionary<int, IAkkoTimer> _timers = new();
        private readonly TimerActions _action;

        public TimerManager(DiscordShardedClient clients, AkkoDbContext dbContext, TimerActions action)
        {
            _action = action;

            // Initialize the cache
            var timerEntities = dbContext.Timers.ToList();

            foreach (var client in clients.ShardClients.Values)
                GenerateGuildTimers(timerEntities, client);

            // TODO: Make another method for timers that don't rely on guild ids
        }

        /// <summary>
        /// Attempts to get the <see cref="IAkkoTimer"/> associated with a certain ID.
        /// </summary>
        /// <param name="id">The ID of the <see cref="IAkkoTimer"/>.</param>
        /// <param name="timer">An <see cref="IAkkoTimer"/> if the timer is found, <see langword="null"/> otherwise.</param>
        /// <returns><see langword="true"/> if the timer is found, <see langword="false"/> otherwise.</returns>
        public bool TryGetValue(int id, out IAkkoTimer timer)
            => _timers.TryGetValue(id, out timer);

        /// <summary>
        /// Attempts to remove the specified <see cref="IAkkoTimer"/> from the cache.
        /// </summary>
        /// <param name="timer">The timer to be removed.</param>
        /// <returns><see langword="true"/> if it is successfully removed, <see langword="false"/> otherwise.</returns>
        public bool TryRemove(IAkkoTimer timer)
            => TryRemove(timer.Id);

        /// <summary>
        /// Attempts to add the specified <see cref="IAkkoTimer"/> to the cache.
        /// </summary>
        /// <param name="timer">The timer to be added.</param>
        /// <returns><see langword="true"/> if it is successfully added, <see langword="false"/> otherwise.</returns>
        public bool TryAdd(IAkkoTimer timer)
        {
            if (_timers.TryAdd(timer.Id, timer))
            {
                timer.OnDispose += AutoRemoval;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to remove an <see cref="IAkkoTimer"/> with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the timer to be removed.</param>
        /// <returns><see langword="true"/> if it gets successfully removed, <see langword="false"/> otherwise.</returns>
        public bool TryRemove(int id)
        {
            if (_timers.TryRemove(id, out var oldTimer))
            {
                oldTimer.OnDispose -= AutoRemoval;
                oldTimer.Dispose();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to update an <see cref="IAkkoTimer"/> object stored in the cache.
        /// </summary>
        /// <param name="timer">The timer to replace the instance present in the cache.</param>
        /// <returns><see langword="true"/> if it gets successfully updated, <see langword="false"/> otherwise.</returns>
        public bool TryUpdate(IAkkoTimer timer)
        {
            if (!_timers.TryGetValue(timer.Id, out var oldTimer))
                return false;

            if (_timers.TryUpdate(timer.Id, timer, oldTimer))
            {
                timer.OnDispose += AutoRemoval;
                oldTimer.OnDispose -= AutoRemoval;
                oldTimer.Dispose();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Generates an <see cref="IAkkoTimer"/> based on a database entry and adds it to the cache.
        /// </summary>
        /// <param name="client">The Discord client that fetched the database entry.</param>
        /// <param name="entity">The database entry.</param>
        /// <remarks>
        /// This method ensures that only timers triggering within the next 
        /// few days get initialized and added to the cache.
        /// </remarks>
        /// <returns><see langword="true"/> if the timer was generated and added, <see langword="false"/> otherwise.</returns>
        public bool AddOrUpdateByEntity(DiscordClient client, TimerEntity entity)
        {
            if (entity.ElapseAt.Subtract(DateTimeOffset.Now) > TimeSpan.FromDays(_timerDayAge))
                return false;

            var timer = GenerateTimer(client, entity);

            return (_timers.ContainsKey(entity.Id))
                ? TryUpdate(timer)
                : TryAdd(timer);
        }

        /// <summary>
        /// Generates and initializes timers from a collection of database entries.
        /// </summary>
        /// <param name="entities">The collection of database entries.</param>
        /// <param name="client">A Discord client to get its visible guilds from.</param>
        private void GenerateGuildTimers(IEnumerable<TimerEntity> entities, DiscordClient client)
        {
            var filteredEntities = entities.Where(entity =>
                entity.GuildId.HasValue
                && client.Guilds.ContainsKey(entity.GuildId.Value)
                && entity.ElapseAt.Subtract(DateTimeOffset.Now) <= TimeSpan.FromDays(_timerDayAge)
            );

            foreach (var entity in filteredEntities)
                _timers.TryAdd(entity.Id, GenerateTimer(client, entity));
        }

        /// <summary>
        /// Gets and initializes a timer based on a dabatase entry.
        /// </summary>
        /// <param name="client">The Discord client that fetched the database entry.</param>
        /// <param name="entity">The database entry.</param>
        /// <returns>An active <see cref="AkkoTimer"/>.</returns>
        private IAkkoTimer GenerateTimer(DiscordClient client, TimerEntity entity)
        {
            var timer = entity.Type switch
            {
                TimerType.TimedBan => new AkkoTimer(entity, async () => await _action.Unban(entity.Id, client.Guilds[entity.GuildId.Value], entity.UserId.Value)),
                TimerType.TimedMute => throw new NotImplementedException(),
                TimerType.TimedWarn => throw new NotImplementedException(),
                TimerType.Reminder => throw new NotImplementedException(),
                TimerType.Repeater => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };

            timer.OnDispose += AutoRemoval;
            return timer;
        }

        /// <summary>
        /// Removes an element from the internal cache.
        /// </summary>
        /// <remarks>
        /// This method is used by <see cref="IAkkoTimer"/> objects to notify
        /// the <see cref="TimerManager"/> that they need to be removed from the cache.
        /// </remarks>
        /// <param name="obj">An object representing an <see cref="IAkkoTimer"/>.</param>
        /// <param name="args">Event arguments.</param>
        private void AutoRemoval(object obj, EventArgs args)
        {
            var timer = obj as IAkkoTimer;
            _timers.TryRemove(timer.Id, out _);
        }

        /// <summary>
        /// Releases all resources used by this <see cref="TimerManager"/>.
        /// </summary>
        public void Dispose()
        {
            foreach (var timer in _timers)
            {
                timer.Value.OnDispose -= AutoRemoval;
                timer.Value.Dispose();
            }

            _timers.Clear();

            GC.SuppressFinalize(this);
        }
    }
}