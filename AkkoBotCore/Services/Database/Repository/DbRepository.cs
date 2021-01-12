using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Repository
{
    /// <summary>
    /// Small wrapper class for performing operations on a database table.
    /// </summary>
    public class DbRepository<T> : IRepository<T> where T : DbEntity
    {
        private readonly DbSet<T> _table;

        public DbRepository(AkkoDbContext db)
            => _table = db.Set<T>();

        /// <summary>
        /// Returns a database entry that contains the specified parameter as a primary key.
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="TValue"></typeparam>
        /// <remarks>This method does not use the database cache. It throws a <see cref="NullReferenceException"/> if the table doesn't contain a primary key of the same type as <typeparamref name="TValue"/>.</remarks>
        /// <returns>A database entry.</returns>
        public virtual async Task<T> GetAsync<TValue>(TValue value)
            => await _table.FindAsync(value);

        /// <summary>
        /// Returns multiple database entries which primary keys meet the conditions specified in the <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">Expression tree to filter the result.</param>
        /// <remarks>This method does not use the database cache.</remarks>
        /// <returns>A collection of database entries.</returns>
        public virtual async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> expression)
            => await _table.Where(expression).ToListAsync();

        /// <summary>
        /// Returns all database entries in this table.
        /// </summary>
        /// <remarks>This method does not use the database cache.</remarks>
        /// <returns>A collection of database entries.</returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
            => await _table.ToListAsync();

        /// <summary>
        /// Adds the entry specified in <paramref name="newEntity"/> to the database.
        /// </summary>
        /// <param name="newEntity">Entry to be added to the database.</param>
        public virtual async Task CreateAsync(T newEntity)
            => await _table.AddAsync(newEntity);

        /// <summary>
        /// Adds the entries specified in the <paramref name="newEntities"/> in bulk to the database.
        /// </summary>
        /// <param name="newEntities">A collection of database entries.</param>
        public virtual async Task CreateRangeAsync(params T[] newEntities)
            => await _table.AddRangeAsync(newEntities);

        /// <summary>
        /// Updates a database entry with the entity specified in <paramref name="newEntity"/>.
        /// </summary>
        /// <param name="newEntity">A database entry.</param>
        public virtual void Update(T newEntity)
            => _table.Update(newEntity);

        /// <summary>
        /// Updates multiple database entries in bulk with the entities specified in <paramref name="newEntities"/>.
        /// </summary>
        /// <param name="newEntities">A collection of database entries.</param>
        public virtual void UpdateRange(params T[] newEntities)
            => _table.UpdateRange(newEntities);

        /// <summary>
        /// Removes the entry specified in <paramref name="oldEntity"/> from the database.
        /// </summary>
        /// <param name="newEntities">A database entry.</param>
        public virtual void Delete(T oldEntity)
            => _table.Remove(oldEntity);

        /// <summary>
        /// Removes multiple entries specified in <paramref name="oldEntity"/> from the database.
        /// </summary>
        /// <param name="newEntities">A collection of database entries.</param>
        public virtual void DeleteRange(params T[] oldEntities)
            => _table.RemoveRange(oldEntities);
    }
}
