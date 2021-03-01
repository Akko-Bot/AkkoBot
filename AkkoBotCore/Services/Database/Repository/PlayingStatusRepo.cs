using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Repository
{
    public class PlayingStatusRepo : DbRepository<PlayingStatusEntity>
    {
        private readonly AkkoDbContext _db;
        public List<PlayingStatusEntity> Cache { get; }

        public PlayingStatusRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
        {
            _db = db;
            Cache = dbCacher.PlayingStatuses;
        }

        /// <summary>
        /// Gets a playing status that meets the <paramref name="comparer"/> criteria.
        /// </summary>
        /// <param name="comparer">A method that returns <see langword="true"/> when the criteria is met.</param>
        /// <returns>The first playing status that meets the criteria, <see langword="null"/> if none is found.</returns>
        public async Task<PlayingStatusEntity> GetStatusAsync(Func<PlayingStatusEntity, bool> comparer)
        {
            var result = Cache.FirstOrDefault(s => comparer(s));

            if (result is null)
            {
                result = await base.GetAsync(comparer);
                Cache.Add(result);
            }

            return result;
        }

        /// <summary>
        /// Gets playing statuses that meet the <paramref name="comparer"/> criteria.
        /// </summary>
        /// <param name="comparer">A method that returns <see langword="true"/> when the criteria is met.</param>
        /// <returns>A collection of playing statuses. It will be empty if none is found.</returns>
        public async Task<IEnumerable<PlayingStatusEntity>> GetStatusesAsync(Func<PlayingStatusEntity, bool> comparer)
        {
            var result = Cache.Where(s => comparer(s));

            if (result is null || !result.Any())
            {
                result = (await base.GetAllAsync()).Where(s => comparer(s));

                foreach (var status in result)
                    Cache.Add(status);
            }

            return result;
        }

        /// <summary>
        /// Creates a tracking entry for adding a playing status to the database.
        /// </summary>
        /// <param name="newEntry">Playing status to be added.</param>
        /// <returns></returns>
        public bool Add(PlayingStatusEntity newEntry)
        {
            var entry = Table.FirstOrDefault(x => x.RotationTime == TimeSpan.Zero);

            if (entry is null)
            {
                base.Create(newEntry);
                Cache.Add(newEntry);
            }
            else
            {
                entry.Message = newEntry.Message;
                entry.Type = newEntry.Type;
                
                var oldEntry = Cache.FirstOrDefault(x => x.RotationTime == TimeSpan.Zero);
                Cache.Remove(oldEntry);
                Cache.Add(entry);
            }

            return entry is null;
        }

        /// <summary>
        /// Creates a tracking entry for removing a playing status from the database
        /// </summary>
        /// <param name="newEntry"></param>
        /// <returns><see langword="true"/> if the entry got removed from the database, <see langword="false"/> otherwise.</returns>
        public bool Remove(PlayingStatusEntity newEntry)
        {
            base.Delete(newEntry);
            return Cache.Remove(newEntry);
        }

        /// <summary>
        /// Removes all playing statuses stored in the database and the cache.
        /// </summary>
        /// <returns>The amount of entries removed.</returns>
        public async Task<int> ClearAsync()
        {
            var rows = await _db.Database.ExecuteSqlRawAsync("DELETE FROM playing_statuses;");
            Cache.Clear();

            return rows;
        }
    }
}