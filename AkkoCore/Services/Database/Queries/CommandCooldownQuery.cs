using AkkoCore.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;

namespace AkkoCore.Services.Database.Queries;

public static class CommandCooldownQuery
{
    /// <summary>
    /// Adds or updates a <see cref="CommandCooldownEntity"/> to the database.
    /// </summary>
    /// <param name="dbContext">This database context.</param>
    /// <param name="newEntry">The entry to be added or updated.</param>
    /// <returns>The tracked entity.</returns>
    public static EntityEntry Upsert(this DbContext dbContext, CommandCooldownEntity newEntry)
        => Upsert(dbContext.Set<CommandCooldownEntity>(), newEntry);

    /// <summary>
    /// Adds or updates a <see cref="CommandCooldownEntity"/> to the database.
    /// </summary>
    /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
    /// <param name="newEntry">The entry to be added or updated.</param>
    /// <returns>The tracked entity.</returns>
    public static EntityEntry Upsert(this DbSet<CommandCooldownEntity> table, CommandCooldownEntity newEntry)
    {
        var oldEntry = table.Local.FirstOrDefault(x => x.GuildIdFK == newEntry.GuildIdFK && x.Command == newEntry.Command)
            ?? table.AsNoTracking().FirstOrDefault(x => x.GuildIdFK == newEntry.GuildIdFK && x.Command == newEntry.Command);

        if (oldEntry is null)
            return table.Add(newEntry);
        else if (oldEntry == newEntry)
            return table.Attach(oldEntry);
        else
        {
            oldEntry.Cooldown = newEntry.Cooldown;

            return table.Update(oldEntry);
        }
    }
}