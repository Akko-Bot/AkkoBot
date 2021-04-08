using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Timers;

namespace AkkoBot.Commands.Modules.Self.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="PlayingStatusEntity"/> objects.
    /// </summary>
    public class StatusService : AkkoCommandService
    {
        private readonly Timer _rotationTimer = new();
        private int _currentStatusIndex = 0;

        private readonly IServiceProvider _services;
        private readonly IDbCacher _dbCache;
        private readonly DiscordShardedClient _clients;

        public StatusService(IServiceProvider services, IDbCacher dbCache, DiscordShardedClient clients) : base(services)
        {
            _services = services;
            _dbCache = dbCache;
            _clients = clients;
        }

        /// <summary>
        /// Saves a playing status to the database.
        /// </summary>
        /// <param name="activity">The Discord status.</param>
        /// <param name="time">How long should the status last before it's replaced by another one.</param>
        /// <remarks><paramref name="time"/> should be <see cref="TimeSpan.Zero"/> for static statuses.</remarks>
        /// <returns><see langword="true"/> if the status has been successfuly saved to the database, <see langword="false"/> otherwise.</returns>
        public async Task<bool> CreateStatusAsync(DiscordActivity activity, TimeSpan time)
        {
            if (string.IsNullOrWhiteSpace(activity.Name))
                return false;

            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var newEntry = new PlayingStatusEntity()
            {
                Message = activity.Name,
                Type = activity.ActivityType,
                RotationTime = time,
                StreamUrl = activity.StreamUrl
            };

            if (time == TimeSpan.Zero)
                db.PlayingStatuses.AddOrUpdateStatic(newEntry, out _);
            else
            {
                db.PlayingStatuses.Create(newEntry);
                db.PlayingStatuses.Cache.Add(newEntry);
            }

            await db.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Removes all statuses from the database that meet the criteria of <paramref name="selector"/>.
        /// </summary>
        /// <param name="selector">Method for selecting which statuses should be removed.</param>
        /// <returns><see langword="true"/> if at least one status has been removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveStatusesAsync(Expression<Func<PlayingStatusEntity, bool>> selector)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();
            var entries = (await db.PlayingStatuses.GetAsync(selector)).ToArray();

            db.PlayingStatuses.DeleteRange(entries);

            foreach (var entry in entries)
                db.PlayingStatuses.Cache.Remove(entry);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Gets all cached rotating statuses.
        /// </summary>
        /// <returns>A collection of statuses.</returns>
        public List<PlayingStatusEntity> GetStatuses()
            => base.Scope.ServiceProvider.GetService<IDbCacher>().PlayingStatuses;

        /// <summary>
        /// Removes all playing statuses from the database.
        /// </summary>
        /// <returns>The amount of removed entries.</returns>
        public async Task<int> ClearStatusesAsync()
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            var amount = await db.PlayingStatuses.ClearAsync();
            await db.SaveChangesAsync();

            return amount;
        }

        /// <summary>
        /// Toggles rotation of the statuses currently saved in the database.
        /// </summary>
        /// <returns><see langword="true"/> if rotation has been toggled, <see langword="false"/> if there was no status to rotate.</returns>
        public async Task<bool> RotateStatusesAsync()
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            // Update the database entry
            db.BotConfig.Cache.RotateStatus = !db.BotConfig.Cache.RotateStatus;
            db.BotConfig.Update(db.BotConfig.Cache);

            if (db.BotConfig.Cache.RotateStatus)
            {
                // Start rotation
                var firstStatus = db.PlayingStatuses.Cache.FirstOrDefault();

                if (firstStatus is null)
                    return false;

                foreach (var client in _clients.ShardClients.Values)
                    await client.UpdateStatusAsync(firstStatus.GetActivity());

                _rotationTimer.Interval = firstStatus.RotationTime.TotalMilliseconds;
                _rotationTimer.Elapsed += async (x, y) => await SetNextStatusAsync();
                _rotationTimer.Start();
            }
            else
            {
                // Stop rotation
                _currentStatusIndex = 0;

                _rotationTimer.Elapsed -= async (x, y) => await SetNextStatusAsync();
                _rotationTimer.Stop();

                var staticStatus = db.PlayingStatuses.Table.FirstOrDefault(x => x.RotationTime == TimeSpan.Zero);

                if (staticStatus is not null)
                {
                    foreach (var client in _clients.ShardClients.Values)
                        await client.UpdateStatusAsync(staticStatus.GetActivity());
                }
            }

            await db.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Advances the bot's status to the next in the rotation list.
        /// </summary>
        private async Task SetNextStatusAsync()
        {
            if (++_currentStatusIndex >= _dbCache.PlayingStatuses.Count)
                _currentStatusIndex = 0;

            var nextStatus = _dbCache.PlayingStatuses[_currentStatusIndex];
            _rotationTimer.Interval = nextStatus.RotationTime.TotalMilliseconds;

            foreach (var client in _clients.ShardClients.Values)
                await client.UpdateStatusAsync(nextStatus.GetActivity());
        }
    }
}