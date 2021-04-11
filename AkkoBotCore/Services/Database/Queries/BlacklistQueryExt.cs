using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;

namespace AkkoBot.Services.Database.Queries
{
    public static class BlacklistQueryExt
    {
        /// <summary>
        /// Adds or updates a <see cref="BlacklistEntity"/> to the database.
        /// </summary>
        /// <param name="dbContext">This database context.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbContext dbContext, BlacklistEntity newEntry)
            => Upsert(dbContext.Set<BlacklistEntity>(), newEntry);

        /// <summary>
        /// Adds or updates a <see cref="BlacklistEntity"/> to the database.
        /// </summary>
        /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbSet<BlacklistEntity> table, BlacklistEntity newEntry)
        {
            var oldEntry = table.Local.FirstOrDefault(x => x.ContextId == newEntry.ContextId)
                ?? table.AsNoTracking().FirstOrDefault(x => x.ContextId == newEntry.ContextId);

            if (oldEntry is null)
                return table.Add(newEntry);
            else if (oldEntry == newEntry)
                return table.Attach(oldEntry);
            else
            {
                oldEntry.Name = newEntry.Name;
                oldEntry.Reason = newEntry.Reason;
                oldEntry.Type = newEntry.Type;

                return table.Update(oldEntry);
            }
        }
    }
}