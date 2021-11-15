using AkkoCore.Services.Database.Abstractions;
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AkkoCore.Services.Database.Queries;

public static class DbContextQueryExt
{
    /// <summary>
    /// Deletes the specified entry from the table.
    /// </summary>
    /// <typeparam name="T">The type of the database entity.</typeparam>
    /// <param name="table">This database table.</param>
    /// <param name="dbEntity">The entry to be removed.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <remarks>The entry is removed based on the value of its primary key.</remarks>
    /// <returns>The amount of deleted entries.</returns>
    public static Task<int> DeleteAsync<T>(this DbSet<T> table, T dbEntity, CancellationToken token = default) where T : DbEntity
        => table.DeleteAsync(x => x.Id == dbEntity.Id, token);

    /// <summary>
    /// Deletes the specified entries from the table.
    /// </summary>
    /// <typeparam name="T">The type of the database entity.</typeparam>
    /// <param name="table">This database table.</param>
    /// <param name="dbEntities">A collection of entries to be removed.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <remarks>The entries are removed based on the value of their primary key.</remarks>
    /// <returns>The amount of deleted entries.</returns>
    public static Task<int> DeleteAsync<T>(this DbSet<T> table, IEnumerable<T> dbEntities, CancellationToken token = default) where T : DbEntity
        => table.DeleteAsync(x => dbEntities.Select(x => x.Id).Contains(x.Id), token);

    /// <summary>
    /// Marks the <paramref name="dbEntity"/> to be upserted to the database.
    /// </summary>
    /// <typeparam name="T">A database entity.</typeparam>
    /// <param name="dbContext">The database context.</param>
    /// <param name="dbEntity">The database entity.</param>
    /// <remarks>
    /// Entities with primary keys set to <see langword="default"/> are marked for addition, otherwise they are marked for update.
    /// Make sure to call this method BEFORE modifying the entity, then call <see cref="DbContext.SaveChanges"/> to apply the changes.
    /// </remarks>
    /// <returns>The tracking state of the entity.</returns>
    public static EntityEntry Upsert<T>(this DbContext dbContext, T dbEntity) where T : DbEntity
        => Upsert(dbContext.Set<T>(), dbEntity);

    /// <summary>
    /// Marks the <paramref name="dbEntity"/> to be upserted to the database.
    /// </summary>
    /// <typeparam name="T">A database entity.</typeparam>
    /// <param name="table">The database table.</param>
    /// <param name="dbEntity">The database entity.</param>
    /// <remarks>
    /// Entities with primary keys set to <see langword="default"/> are marked for addition, otherwise they are marked for update.
    /// Make sure to call this method BEFORE modifying the entity, then call <see cref="DbContext.SaveChanges"/> to apply the changes.
    /// </remarks>
    /// <returns>The tracking state of the entity.</returns>
    public static EntityEntry Upsert<T>(this DbSet<T> table, T dbEntity) where T : DbEntity
    {
        return (dbEntity.Id == default)
            ? table.Add(dbEntity)
            : table.Attach(dbEntity);
    }
}