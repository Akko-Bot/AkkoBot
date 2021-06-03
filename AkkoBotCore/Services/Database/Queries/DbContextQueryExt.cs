using AkkoBot.Services.Database.Abstractions;
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Queries
{
    public static class DbContextQueryExt
    {
        /// <summary>
        /// Gets all database entries that match the criteria of the <paramref name="predicate"/> without tracking.
        /// </summary>
        /// <typeparam name="T">The type of the database entity.</typeparam>
        /// <param name="dbContext">This database context.</param>
        /// <param name="predicate">Predicate to select the entries that should be included in the resulting query.</param>
        /// <remarks>If <paramref name="predicate"/> is <see langword="null"/>, it gets all <typeparamref name="T"/> entries.</remarks>
        /// <returns>A database query.</returns>
        public static IQueryable<T> Fetch<T>(this DbContext dbContext, Expression<Func<T, bool>> predicate = null) where T : DbEntity
            => Fetch(dbContext.Set<T>(), predicate);

        /// <summary>
        /// Gets all database entries that match the criteria of the <paramref name="predicate"/> without tracking.
        /// </summary>
        /// <typeparam name="T">The type of the database entity.</typeparam>
        /// <param name="table">This database table.</param>
        /// <param name="predicate">Predicate to select the entries that should be included in the resulting query.</param>
        /// <remarks>If <paramref name="predicate"/> is <see langword="null"/>, it gets all <typeparamref name="T"/> entries.</remarks>
        /// <returns>A database query.</returns>
        public static IQueryable<T> Fetch<T>(this DbSet<T> table, Expression<Func<T, bool>> predicate = null) where T : DbEntity
        {
            return (predicate is null)
                ? table.AsNoTracking()
                : table.AsNoTracking().Where(predicate);
        }

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
    }
}