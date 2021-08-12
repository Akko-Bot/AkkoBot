using AkkoCore.Services.Database.Entities;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;

namespace AkkoCore.Services.Database.Queries
{
    public static class DiscordUserQueryExt
    {
        /// <summary>
        /// Adds or updates a <see cref="DiscordUserEntity"/> to the database.
        /// </summary>
        /// <param name="dbContext">This database context.</param>
        /// <param name="user">A Discord user.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbContext dbContext, DiscordUser user)
            => Upsert(dbContext.Set<DiscordUserEntity>(), new DiscordUserEntity(user));

        /// <summary>
        /// Adds or updates a <see cref="DiscordUserEntity"/> to the database.
        /// </summary>
        /// <param name="dbContext">This database context.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbContext dbContext, DiscordUserEntity newEntry)
            => Upsert(dbContext.Set<DiscordUserEntity>(), newEntry);

        /// <summary>
        /// Adds or updates a <see cref="DiscordUserEntity"/> to the database.
        /// </summary>
        /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
        /// <param name="user">A Discord user.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbSet<DiscordUserEntity> table, DiscordUser user)
            => Upsert(table, new DiscordUserEntity(user));

        /// <summary>
        /// Adds or updates a <see cref="DiscordUserEntity"/> to the database.
        /// </summary>
        /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
        /// <param name="newEntry">The entry to be added or updated.</param>
        /// <returns>The tracked entity.</returns>
        public static EntityEntry Upsert(this DbSet<DiscordUserEntity> table, DiscordUserEntity newEntry)
        {
            var oldEntry = table.Local.FirstOrDefault(x => x.UserId == newEntry.UserId)
                ?? table.AsNoTracking().FirstOrDefault(x => x.UserId == newEntry.UserId);

            if (oldEntry is null)
                return table.Add(newEntry);
            else if (oldEntry == newEntry)
                return table.Attach(oldEntry);
            else
            {
                oldEntry.Username = newEntry.Username;
                oldEntry.Discriminator = newEntry.Discriminator;
                return table.Update(oldEntry);
            }
        }
    }
}