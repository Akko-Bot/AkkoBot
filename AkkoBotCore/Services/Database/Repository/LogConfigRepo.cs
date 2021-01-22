using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;

namespace AkkoBot.Services.Database.Repository
{
    public class LogConfigRepo : DbRepository<LogConfigEntity>
    {
        //private readonly AkkoDbContext _db;
        private readonly IDbCacher _dbCacher;
        public LogConfigEntity Cache { get; private set; }

        public LogConfigRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
        {
            _dbCacher = dbCacher;
            Cache = _dbCacher.LogConfig;
        }

        public async Task<bool> TryCreateAsync()
        {
            Cache = (await base.GetAllAsync()).FirstOrDefault();

            if (Cache is null)
            {
                Cache = _dbCacher.LogConfig = new LogConfigEntity();
                base.Create(Cache);
                return false;
            }

            _dbCacher.LogConfig = Cache;

            return true;
        }
    }
}