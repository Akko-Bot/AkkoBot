using System.Collections.Generic;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Repository
{
    public class PlayingStatusRepo : DbRepository<PlayingStatusEntity>
    {
        private readonly AkkoDbContext _db;
        public List<PlayingStatusEntity> Cache { get; }

        public PlayingStatusRepo(AkkoDbContext db, AkkoDbCacher dbCacher) : base(db)
        {
            _db = db;
            Cache = dbCacher.PlayingStatuses;
        }

        /// <summary>
        /// Adds a playing status to the database and cache.
        /// </summary>
        /// <param name="pStatus">Playing status to be added.</param>
        /// <returns></returns>
        public async Task AddAsync(PlayingStatusEntity pStatus)
        {
            await base.CreateAsync(pStatus);
            Cache.Add(pStatus);
        }

        /// <summary>
        /// Removes a playing status from the database and the cache.
        /// </summary>
        /// <param name="pStatus"></param>
        /// <returns><see langword="true"/> if the entry got removed from the database, <see langword="false"/> otherwise.</returns>
        public bool Remove(PlayingStatusEntity pStatus)
        {
            base.Delete(pStatus);
            return Cache.Remove(pStatus);
        }

        /// <summary>
        /// Removes all playing statuses stored in the database and the cache.
        /// </summary>
        /// <returns>The amount of entries removed.</returns>
        public async Task<int> ClearAsync()
        {
            int rows = await _db.Database.ExecuteSqlRawAsync("DELETE FROM playing_statuses;");
            Cache.Clear();

            return rows;
        }
    }
}