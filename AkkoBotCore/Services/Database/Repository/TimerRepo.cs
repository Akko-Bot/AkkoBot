using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Timers.Abstractions;
using System;
using System.Linq;

namespace AkkoBot.Services.Database.Repository
{
    public class TimerRepo : DbRepository<TimerEntity>
    {
        public ITimerManager Cache { get; }

        public TimerRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
            => Cache = dbCacher.Timers;

        /// <summary>
        /// Upserts an entry to the database.
        /// </summary>
        /// <param name="newEntry">The entry to be added or updated to.</param>
        /// <param name="selector">A method that defines the entry that needs to be updated, if it exists.</param>
        /// <param name="dbEntry">The tracked resulting entity to be upserted.</param>
        /// <returns><see langword="true"/> if <paramref name="dbEntry"/> is being tracked for creation, <see langword="false"/> if for updating.</returns>
        public bool AddOrUpdate(TimerEntity newEntry, Func<TimerEntity, bool> selector, out TimerEntity dbEntry)
        {
            dbEntry = base.Table.FirstOrDefault(selector);

            if (dbEntry is null)
            {
                dbEntry = newEntry;
                base.Create(newEntry);
                return true;
            }
            else
            {
                base.Delete(dbEntry);       // This is needed to change the tracking internal state. TODO: investigate further
                newEntry.Id = dbEntry.Id;
                dbEntry = newEntry;
                base.Update(dbEntry);

                return false;
            }
        }
    }
}