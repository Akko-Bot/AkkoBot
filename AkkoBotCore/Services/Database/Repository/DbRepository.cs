using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Repository
{
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
        /// <remarks>It will throw a <see cref="NullReferenceException"/> if the table doesn't contain a property of the same type as <typeparamref name="TValue"/>.</remarks>
        /// <returns>A database entry.</returns>
        public virtual async Task<T> GetAsync<TValue>(TValue value)
            => await _table.FindAsync(value);

        /// <summary>
        /// Returns a database entry that meets the conditions specified in the <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">Expression tree to filter the result.</param>
        /// <returns>A collection of database entries.</returns>
        public virtual async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> expression)
            => await _table.Where(expression).ToListAsync();

        public virtual async Task<IEnumerable<T>> GetAllAsync()
            => await _table.ToListAsync();

        public virtual async Task CreateAsync(T newEntity)
            => await _table.AddAsync(newEntity);

        public virtual async Task CreateRangeAsync(params T[] newEntities)
            => await _table.AddRangeAsync(newEntities);

        public virtual void Update(T newEntity)
            => _table.Update(newEntity);

        public virtual void UpdateRange(params T[] newEntity)
            => _table.UpdateRange(newEntity);

        public virtual void Delete(T oldEntity)
            => _table.Remove(oldEntity);

        public virtual void DeleteRange(params T[] oldEntities)
            => _table.RemoveRange(oldEntities);
    }
}
