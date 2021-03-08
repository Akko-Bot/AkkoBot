using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using System.Linq;

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
        /// Creates a tracking entry for log settings if there isn't one already, and caches it.
        /// </summary>
        /// <returns><see langword="true"/> if the entry got added to EF Core's tracker, <see langword="false"/> otherwise.</returns>
        public bool TryCreate()
        {
            Cache = base.GetAllSync().FirstOrDefault();

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