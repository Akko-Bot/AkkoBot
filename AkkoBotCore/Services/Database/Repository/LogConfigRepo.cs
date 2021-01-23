using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;

namespace AkkoBot.Services.Database.Repository
{
    public class LogConfigRepo : DbRepository<LogConfigEntity>
    {
        private readonly IDbCacher _dbCacher;
        public LogConfigEntity Cache { get; private set; }

        public LogConfigRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
        {
            _dbCacher = dbCacher;
            Cache = _dbCacher.LogConfig;
        }

        /// <summary>
        /// Creates an entry for log settings, if there isn't one already.
        /// </summary>
        /// <returns><see langword="true"/> if the entry got created, <see langword="false"/> otherwise.</returns>
        public async Task<bool> TryCreateAsync()
        {
            Cache = (await base.GetAllAsync()).FirstOrDefault();

            if (Cache is null)
            {
                Cache = _dbCacher.LogConfig = new LogConfigEntity();
                base.Create(Cache);
                return true;
            }

            _dbCacher.LogConfig = Cache;

            return false;
        }
    }
}