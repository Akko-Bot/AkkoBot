using AkkoCore.Commands.Attributes;
using AkkoCore.Common;
using AkkoCore.Config.Abstractions;
using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Queries;
using DSharpPlus;
using DSharpPlus.Entities;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Timers;

namespace AkkoCore.Commands.Modules.Self.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="PlayingStatusEntity"/> objects.
    /// </summary>
    [CommandService(ServiceLifetime.Singleton)]
    public sealed class StatusService
    {
        private readonly Timer _rotationTimer = new();
        private int _currentStatusIndex = 0;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;
        private readonly IConfigLoader _configLoader;
        private readonly BotConfig _botConfig;
        private readonly DiscordShardedClient _clients;

        public StatusService(IServiceScopeFactory scopeFactory, IDbCache dbCache, IConfigLoader configLoader, BotConfig botConfig, DiscordShardedClient clients)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCache;
            _configLoader = configLoader;
            _botConfig = botConfig;
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

            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            var newEntry = new PlayingStatusEntity()
            {
                Message = activity.Name,
                Type = activity.ActivityType,
                RotationTime = time,
                StreamUrl = activity.StreamUrl
            };

            if (time == TimeSpan.Zero)
                db.Upsert(newEntry); // Static status should not be cached
            else
            {
                db.Add(newEntry);
                _dbCache.PlayingStatuses.Add(newEntry);
            }

            await db.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Removes all statuses from the database that meet the criteria of <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">Method to select which statuses should be removed.</param>
        /// <returns><see langword="true"/> if at least one status has been removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveStatusesAsync(Expression<Func<PlayingStatusEntity, bool>> predicate)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);
            var entries = await db.PlayingStatuses
                .Where(predicate)
                .Select(x => x.Id)
                .ToArrayAsyncEF();

            _dbCache.PlayingStatuses.RemoveAll(x => entries.Contains(x.Id));

            return await db.PlayingStatuses.DeleteAsync(x => entries.Contains(x.Id)) is not 0;
        }

        /// <summary>
        /// Gets all cached rotating statuses.
        /// </summary>
        /// <returns>A collection of statuses.</returns>
        public IReadOnlyCollection<PlayingStatusEntity> GetStatuses()
            => _dbCache.PlayingStatuses;

        /// <summary>
        /// Removes all playing statuses from the database.
        /// </summary>
        /// <returns>The amount of removed entries.</returns>
        public async Task<int> ClearStatusesAsync()
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);
            _dbCache.PlayingStatuses.Clear();

            return await db.PlayingStatuses.DeleteAsync();
        }

        /// <summary>
        /// Toggles rotation of the statuses currently saved in the database.
        /// </summary>
        /// <returns><see langword="true"/> if rotation has been toggled, <see langword="false"/> if there was no status to rotate.</returns>
        public async Task<bool> RotateStatusesAsync()
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            // Update the botconfig
            _botConfig.RotateStatus = !_botConfig.RotateStatus;
            _configLoader.SaveConfig(_botConfig, AkkoEnvironment.BotConfigPath);

            if (_botConfig.RotateStatus)
            {
                // Start rotation
                var firstStatus = _dbCache.PlayingStatuses.FirstOrDefault();

                if (firstStatus is null)
                    return false;

                foreach (var client in _clients.ShardClients.Values)
                    await client.UpdateStatusAsync(firstStatus.Activity);

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

                var staticStatus = await db.PlayingStatuses
                    .FirstOrDefaultAsyncEF(x => x.RotationTime == TimeSpan.Zero);

                if (staticStatus is not null)
                {
                    foreach (var client in _clients.ShardClients.Values)
                        await client.UpdateStatusAsync(staticStatus.Activity);
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
                await client.UpdateStatusAsync(nextStatus.Activity);
        }
    }
}