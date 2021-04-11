using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Queries
{
    public static class DbContextQueryExt
    {
        /// <summary>
        /// Gets all database entries that match the criteria of the <paramref name="predicate"/> without tracking.
        /// </summary>
        /// <typeparam name="T">The type of the database entity.</typeparam>
        /// <param name="predicate">Predicate to select the entries that should be included in the resulting query.</param>
        /// <remarks>If <paramref name="predicate"/> is <see langword="null"/>, it gets all <typeparamref name="T"/> entries.</remarks>
        /// <returns>A database query.</returns>
        public static IQueryable<T> Fetch<T>(this DbContext dbContext, Expression<Func<T, bool>> predicate = null) where T : DbEntity
            => Fetch(dbContext.Set<T>(), predicate);

        /// <summary>
        /// Gets all database entries that match the criteria of the <paramref name="predicate"/> without tracking.
        /// </summary>
        /// <typeparam name="T">The type of the database entity.</typeparam>
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
        /// Gets the amount of database entries that match the criteria of the <paramref name="predicate"/> without tracking.
        /// </summary>
        /// <typeparam name="T">The type of the database entity.</typeparam>
        /// <param name="predicate">Predicate to select the entries to be counted in the query.</param>
        /// <remarks>If <paramref name="predicate"/> is <see langword="null"/>, it gets the total count of <typeparamref name="T"/> entries.</remarks>
        /// <returns>The amount of matching entries.</returns>
        public static Task<int> CountAsync<T>(this DbContext dbContext, Expression<Func<T, bool>> predicate = null) where T : DbEntity
        {
            return (predicate is null)
                ? dbContext.Set<T>().AsNoTracking().CountAsync()
                : dbContext.Set<T>().AsNoTracking().CountAsync(predicate);
        }
    }
}