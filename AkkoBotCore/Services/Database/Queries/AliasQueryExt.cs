using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;

namespace AkkoBot.Services.Database.Queries
{
    public static class AliasQueryExt
    {
        /// <summary>
        /// Adds or updates an <see cref="AliasEntity"/> to the database.
        /// </summary>
        /// <param name="dbContext">This database context.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbContext dbContext, AliasEntity newEntry)
            => Upsert(dbContext.Set<AliasEntity>(), newEntry);

        /// <summary>
        /// Adds or updates an <see cref="AliasEntity"/> to the database.
        /// </summary>
        /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbSet<AliasEntity> table, AliasEntity newEntry)
        {
            var oldEntry = table.Local.FirstOrDefault(x => x.GuildId == newEntry.GuildId && x.Alias == newEntry.Alias)
                ?? table.FirstOrDefault(x => x.GuildId == newEntry.GuildId && x.Alias == newEntry.Alias);

            if (oldEntry is null)
                return table.Add(newEntry);
            else if (oldEntry == newEntry)
                return table.Attach(oldEntry);
            else
            {
                oldEntry.IsDynamic = newEntry.IsDynamic;
                oldEntry.Arguments = newEntry.Arguments;
                oldEntry.Command = newEntry.Command;

                return table.Update(oldEntry);
            }
        }
    }
}