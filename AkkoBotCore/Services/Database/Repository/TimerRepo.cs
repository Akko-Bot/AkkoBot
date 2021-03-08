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
        /// Gets a database entry from the referenced entity object.
        /// </summary>
        /// <param name="referenceEntity">The model to look for in the database.</param>
        /// <remarks>The entry is filtered by TimerType, GuildId, UserId and ChannelId.</remarks>
        /// <returns>The tracked database entry, <see langword="null"/> if the entry doesn't exist.</returns>
        public TimerEntity GetTimerEntity(TimerEntity referenceEntity)
        {
            return base.Table.FirstOrDefault(x =>
                x.Type == referenceEntity.Type
                && x.GuildId == referenceEntity.GuildId
                && x.UserId == referenceEntity.UserId
                && x.ChannelId == referenceEntity.ChannelId
                && x.RoleId == referenceEntity.RoleId
            );
        }

        /// <summary>
        /// Upserts the specified <paramref name="newEntry"/> to the database.
        /// </summary>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <param name="dbEntry">The tracked resulting entity to be upserted.</param>
        /// <returns><see langword="true"/> if <paramref name="dbEntry"/> is being tracked for creation, <see langword="false"/> if for updating.</returns>
        public bool AddOrUpdate(TimerEntity newEntry, out TimerEntity dbEntry)
        {
            dbEntry = GetTimerEntity(newEntry);

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