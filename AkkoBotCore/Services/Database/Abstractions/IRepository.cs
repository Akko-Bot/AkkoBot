using System.Collections.Generic;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Abstractions
{
    interface IRepository<T>
    {
        //Task<T> GetAsync(ulong id);
        Task<IEnumerable<T>> GetAllAsync();
        Task CreateAsync(T newEntity);
        Task CreateRangeAsync(params T[] newEntities);
        void Update(T newEntity);
        void UpdateRange(params T[] newEntity);
        void Delete(T oldEntity);
        void DeleteRange(params T[] oldEntities);
    }
}
