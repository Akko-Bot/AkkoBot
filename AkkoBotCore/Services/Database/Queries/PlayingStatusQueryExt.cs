using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Linq;

namespace AkkoBot.Services.Database.Queries
{
    public static class PlayingStatusQueryExt
    {
        /// <summary>
        /// Adds or updates a <see cref="PlayingStatusEntity"/> to the database.
        /// </summary>
        /// <param name="dbContext">This database context.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbContext dbContext, PlayingStatusEntity newEntry)
            => Upsert(dbContext.Set<PlayingStatusEntity>(), newEntry);

        /// <summary>
        /// Adds or updates a <see cref="PlayingStatusEntity"/> to the database.
        /// </summary>
        /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbSet<PlayingStatusEntity> table, PlayingStatusEntity newEntry)
        {
            var oldEntry = table.Local.FirstOrDefault(x => x.RotationTime == TimeSpan.Zero)
                ?? table.AsNoTracking().FirstOrDefault(x => x.RotationTime == TimeSpan.Zero);

            if (oldEntry is null)
                return table.Add(newEntry);
            else if (oldEntry == newEntry)
                return table.Attach(oldEntry);
            else
            {
                oldEntry.Message = newEntry.Message;
                oldEntry.Type = newEntry.Type;

                return table.Update(oldEntry);
            }
        }
    }
}