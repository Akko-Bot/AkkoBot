using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Abstractions
{
    public interface IRepository<T>
    {
        Task<T> GetAsync<TValue>(TValue value);
        Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> expression);
        Task<IEnumerable<T>> GetAllAsync();
        Task CreateAsync(T newEntity);
        Task CreateRangeAsync(params T[] newEntities);
        void Update(T newEntity);
        void UpdateRange(params T[] newEntity);
        void Delete(T oldEntity);
        void DeleteRange(params T[] oldEntities);
    }
}
