using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;

namespace AkkoBot.Services.Database.Queries
{
    public static class WarnPunishQueryExt
    {
        /// <summary>
        /// Adds or updates a <see cref="WarnPunishEntity"/> to the database.
        /// </summary>
        /// <param name="dbContext">This database context.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbContext dbContext, WarnPunishEntity newEntry)
            => Upsert(dbContext.Set<WarnPunishEntity>(), newEntry);

        /// <summary>
        /// Adds or updates a <see cref="WarnPunishEntity"/> to the database.
        /// </summary>
        /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbSet<WarnPunishEntity> table, WarnPunishEntity newEntry)
        {
            var oldEntry = table.Local.FirstOrDefault(x => x.GuildIdFK == newEntry.GuildIdFK && x.WarnAmount == newEntry.WarnAmount)
                ?? table.AsNoTracking().FirstOrDefault(x => x.GuildIdFK == newEntry.GuildIdFK && x.WarnAmount == newEntry.WarnAmount);

            if (oldEntry is null)
                return table.Add(newEntry);
            else if (oldEntry == newEntry)
                return table.Attach(oldEntry);
            else
            {
                oldEntry.Interval = newEntry.Interval;
                oldEntry.PunishRoleId = newEntry.PunishRoleId;
                oldEntry.Type = newEntry.Type;

                return table.Update(oldEntry);
            }
        }
    }
}