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
        protected DbSet<T> Table { get; }

        public DbRepository(AkkoDbContext db)
            => Table = db.Set<T>();

        /// <summary>
        /// Returns a database entry that contains the specified parameter as a primary key.
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="TValue"></typeparam>
        /// <remarks>This method does not use the database cache. It throws an exception if the table doesn't contain a primary key of the same type as <typeparamref name="TValue"/>.</remarks>
        /// <returns>A database entry or <see langword="null"/> if not found.</returns>
        /// <exception cref="NullReferenceException"/>
        public virtual T GetSync<TValue>(TValue value)
            => Table.Find(value);

        /// <summary>
        /// Returns multiple database entries whose primary keys meet the criteria specified in the <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">Expression tree to filter the result.</param>
        /// <remarks>This method does not use the database cache.</remarks>
        /// <returns>A collection of database entries. The collection will be empty if no entity matches the criterias from <paramref name="expression"/>.</returns>
        public virtual IEnumerable<T> GetSync(Expression<Func<T, bool>> expression)
            => Table.Where(expression).ToList();

        /// <summary>
        /// Returns all database entries in this table.
        /// </summary>
        /// <remarks>This method does not use the database cache.</remarks>
        /// <returns>A collection of database entries. The collection will be empty if no entries are present.</returns>
        public virtual IEnumerable<T> GetAllSync()
            => Table.ToList();

        /// <summary>
        /// Returns a database entry that contains the specified parameter as a primary key.
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="TValue"></typeparam>
        /// <remarks>This method does not use the database cache. It throws an exception if the table doesn't contain a primary key of the same type as <typeparamref name="TValue"/>.</remarks>
        /// <returns>A database entry or <see langword="null"/> if not found.</returns>
        /// <exception cref="NullReferenceException"/>
        public virtual async Task<T> GetAsync<TValue>(TValue value)
            => await Table.FindAsync(value);

        /// <summary>
        /// Returns multiple database entries whose primary keys meet the criteria specified in the <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">Expression tree to filter the result.</param>
        /// <remarks>This method does not use the database cache.</remarks>
        /// <returns>A collection of database entries. The collection will be empty if no entity matches the criterias from <paramref name="expression"/>.</returns>
        public virtual async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> expression)
            => await Table.Where(expression).ToListAsync();

        /// <summary>
        /// Returns all database entries in this table.
        /// </summary>
        /// <remarks>This method does not use the database cache.</remarks>
        /// <returns>A collection of database entries. The collection will be empty if no entries are present.</returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
            => await Table.ToListAsync();

        /// <summary>
        /// Adds the entry specified in <paramref name="newEntity"/> to the database.
        /// </summary>
        /// <param name="newEntity">Entry to be added to the database.</param>
        public virtual void Create(T newEntity)
            => Table.Add(newEntity);

        /// <summary>
        /// Adds the entries specified in the <paramref name="newEntities"/> in bulk to the database.
        /// </summary>
        /// <param name="newEntities">A collection of database entries.</param>
        public virtual void CreateRange(params T[] newEntities)
            => Table.AddRange(newEntities);

        /// <summary>
        /// Adds the entry specified in <paramref name="newEntity"/> to the database.
        /// </summary>
        /// <param name="newEntity">Entry to be added to the database.</param>
        /// <remarks>
        /// This should only be used for value generators.
        /// Use the synchronous version otherwise.
        /// </remarks>
        public virtual async Task CreateAsync(T newEntity)
            => await Table.AddAsync(newEntity);

        /// <summary>
        /// Adds the entries specified in the <paramref name="newEntities"/> in bulk to the database.
        /// </summary>
        /// <param name="newEntities">A collection of database entries.</param>
        /// <remarks>
        /// This should only be used for value generators.
        /// Use the synchronous version otherwise.
        /// </remarks>
        public virtual async Task CreateRangeAsync(params T[] newEntities)
            => await Table.AddRangeAsync(newEntities);

        /// <summary>
        /// Updates a database entry with the entity specified in <paramref name="newEntity"/>.
        /// </summary>
        /// <param name="newEntity">A database entry.</param>
        public virtual void Update(T newEntity)
            => Table.Update(newEntity);

        /// <summary>
        /// Updates multiple database entries in bulk with the entities specified in <paramref name="newEntities"/>.
        /// </summary>
        /// <param name="newEntities">A collection of database entries.</param>
        public virtual void UpdateRange(params T[] newEntities)
            => Table.UpdateRange(newEntities);

        /// <summary>
        /// Removes the entry specified in <paramref name="oldEntity"/> from the database.
        /// </summary>
        /// <param name="newEntities">A database entry.</param>
        public virtual void Delete(T oldEntity)
            => Table.Remove(oldEntity);

        /// <summary>
        /// Removes multiple entries specified in <paramref name="oldEntity"/> from the database.
        /// </summary>
        /// <param name="newEntities">A collection of database entries.</param>
        public virtual void DeleteRange(params T[] oldEntities)
            => Table.RemoveRange(oldEntities);
    }
}
