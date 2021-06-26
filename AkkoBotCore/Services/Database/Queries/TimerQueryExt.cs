using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;

namespace AkkoBot.Services.Database.Queries
{
    public static class TimerQueryExt
    {
        /// <summary>
        /// Adds or updates an <see cref="TimerEntity"/> to the database.
        /// </summary>
        /// <param name="dbContext">This database context.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbContext dbContext, TimerEntity newEntry)
            => Upsert(dbContext.Set<TimerEntity>(), newEntry);

        /// <summary>
        /// Adds or updates an <see cref="TimerEntity"/> to the database.
        /// </summary>
        /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbSet<TimerEntity> table, TimerEntity newEntry)
        {
            var oldEntry = GetTimerEntity(table, newEntry);

            if (oldEntry is null)
                return table.Add(newEntry);
            else if (oldEntry == newEntry)
                return table.Attach(oldEntry);
            else
            {
                newEntry.Id = oldEntry.Id;
                oldEntry = newEntry;

                return table.Update(oldEntry);
            }
        }

        /// <summary>
        /// Gets a database entry from the referenced entity object.
        /// </summary>
        /// <param name="referenceEntity">The model to look for in the database.</param>
        /// <remarks>The entry is filtered by TimerType, GuildId, UserId and ChannelId.</remarks>
        /// <returns>The tracked database entry, <see langword="null"/> if the entry doesn't exist.</returns>
        private static TimerEntity GetTimerEntity(DbSet<TimerEntity> table, TimerEntity referenceEntity)
        {
            return table.FirstOrDefault(x =>
                    x.Type == referenceEntity.Type
                    && x.GuildIdFK == referenceEntity.GuildIdFK
                    && x.UserIdFK == referenceEntity.UserIdFK
                    && x.ChannelId == referenceEntity.ChannelId
                    && x.RoleId == referenceEntity.RoleId
                );
        }
    }
}