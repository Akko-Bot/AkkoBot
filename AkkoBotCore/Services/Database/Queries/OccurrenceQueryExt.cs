using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;

namespace AkkoBot.Services.Database.Queries
{
    public static class OccurrenceQueryExt
    {
        /// <summary>
        /// Adds or updates an <see cref="OccurrenceEntity"/> to the database.
        /// </summary>
        /// <param name="dbContext">This database context.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbContext dbContext, OccurrenceEntity newEntry)
            => Upsert(dbContext.Set<OccurrenceEntity>(), newEntry);

        /// <summary>
        /// Adds or updates an <see cref="OccurrenceEntity"/> to the database.
        /// </summary>
        /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbSet<OccurrenceEntity> table, OccurrenceEntity newEntry)
        {
            var oldEntry = table.Local.FirstOrDefault(x => x.GuildIdFK == newEntry.GuildIdFK && x.UserId == newEntry.UserId)
                ?? table.FirstOrDefault(x => x.GuildIdFK == newEntry.GuildIdFK && x.UserId == newEntry.UserId);

            if (oldEntry is null)
                return table.Add(newEntry);
            else
            {
                oldEntry += newEntry;
                return table.Update(oldEntry);
            }
        }
    }
}