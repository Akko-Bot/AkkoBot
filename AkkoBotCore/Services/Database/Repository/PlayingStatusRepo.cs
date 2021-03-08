using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;

namespace AkkoBot.Services.Database.Repository
{
    public class PlayingStatusRepo : DbRepository<PlayingStatusEntity>
    {
        /// <summary>
        /// Caches all rotating statuses. Static statuses are excluded.
        /// </summary>
        public List<PlayingStatusEntity> Cache { get; }

        public PlayingStatusRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
            => Cache = dbCacher.PlayingStatuses;

        /// <summary>
        /// Creates a tracking entry for adding or updating a static playing status to the database.
        /// </summary>
        /// <param name="newEntry">Playing status to be added.</param>
        /// <param name="dbEntry">The tracked entity.</param>
        /// <returns><see langword="true"/> if the entry is going to be added to the database, <see langword="false"/> if it is going to be updated.</returns>
        public bool AddOrUpdateStatic(PlayingStatusEntity newEntry, out PlayingStatusEntity dbEntry)
        {
            dbEntry = Table.FirstOrDefault(x => x.RotationTime == TimeSpan.Zero);
            var success = dbEntry is not null;

            if (success)
            {
                dbEntry.Message = newEntry.Message;
                dbEntry.Type = newEntry.Type;
            }
            else
            {
                base.Create(newEntry);
                dbEntry = newEntry;
            }

            return success;
        }

        /// <summary>
        /// Tracks all playing statuses stored in the database for removal and clears the cache.
        /// </summary>
        /// <returns>The amount of entries tracked for removal.</returns>
        public async Task<int> ClearAsync()
        {
            var entries = (await base.GetAllAsync()).ToArray();
            base.DeleteRange(entries);
            Cache.Clear();

            return entries.Length;
        }
    }
}