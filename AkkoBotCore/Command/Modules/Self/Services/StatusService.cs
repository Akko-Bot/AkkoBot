using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.Entities;

namespace AkkoBot.Command.Modules.Self.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="PlayingStatusEntity"/> objects.
    /// </summary>
    public class StatusService : ICommandService
    {
        private readonly IServiceProvider _services;

        public StatusService(IServiceProvider services)
            => _services = services;

        /// <summary>
        /// Saves a playing status to the database.
        /// </summary>
        /// <param name="activity">The Discord status.</param>
        /// <param name="time">How long should the status last before it's replaced by another one.</param>
        /// <param name="streamUrl">Stream URL, if applicable.</param>
        /// <remarks><paramref name="time"/> should be <see cref="TimeSpan.Zero"/> for static statuses.</remarks>
        public async Task CreateStatusAsync(DiscordActivity activity, TimeSpan time, string streamUrl = null)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var newEntry = new PlayingStatusEntity()
            {
                Message = activity.Name,
                Type = activity.ActivityType,
                RotationTime = time,
                StreamUrl = (activity.ActivityType == ActivityType.Streaming) ? streamUrl : null
            };

            if (time == TimeSpan.Zero)
            {
                db.PlayingStatuses.AddOrUpdateStatic(newEntry, out var dbEntry);
                await db.SaveChangesAsync();

                // Update the cache accordingly
                var oldEntry = db.PlayingStatuses.Cache.FirstOrDefault(x => x.Id == dbEntry.Id);
                if (oldEntry is not null)
                    db.PlayingStatuses.Cache.Remove(oldEntry);

                db.PlayingStatuses.Cache.Add(dbEntry);
            }
            else
            {
                db.PlayingStatuses.Create(newEntry);
                await db.SaveChangesAsync();

                db.PlayingStatuses.Cache.Add(newEntry);
            }
        }

        /// <summary>
        /// Removes all statuses from the database that meet the criteria of <paramref name="selector"/>.
        /// </summary>
        /// <param name="selector">Method for selecting which statuses should be removed.</param>
        /// <returns><see langword="true"/> if at least one status has been removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveStatusesAsync(Expression<Func<PlayingStatusEntity, bool>> selector)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            var entries = await db.PlayingStatuses.GetAsync(selector);
            
            foreach (var entry in entries)
                db.PlayingStatuses.Remove(entry);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Gets all cached statuses.
        /// </summary>
        /// <returns>A collection of statuses.</returns>
        public List<PlayingStatusEntity> GetStatuses()
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            return db.PlayingStatuses.Cache;
        }

        /// <summary>
        /// Removes all playing statuses from the database.
        /// </summary>
        /// <returns>The amount of removed entries.</returns>
        public async Task<int> ClearStatusesAsync()
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            
            var amount = await db.PlayingStatuses.ClearAsync();
            await db.SaveChangesAsync();

            return amount;
        }
    }
}