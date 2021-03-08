using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using System.Linq;

namespace AkkoBot.Services.Database.Repository
{
    public class BotConfigRepo : DbRepository<BotConfigEntity>
    {
        private readonly IDbCacher _dbCacher;
        public BotConfigEntity Cache { get; private set; }

        public BotConfigRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
        {
            _dbCacher = dbCacher;
            Cache = dbCacher.BotConfig;
        }

        /// <summary>
        /// Adds an entry for the bot's settings into the database.
        /// </summary>
        /// <param name="uid">ID of the bot.</param>
        /// <remarks>If an entry already exists for a given ID, it does nothing.</remarks>
        /// <returns><see langword="true"/> if the entry got added to EF Core's tracker, <see langword="false"/> otherwise.</returns>
        public bool TryCreate()
        {
            Cache = base.GetAllSync().FirstOrDefault();

            if (Cache is null)
            {
                Cache = _dbCacher.BotConfig = new BotConfigEntity();
                base.Create(Cache);

                return true;
            }

            _dbCacher.BotConfig = Cache;

            return false;
        }
    }
}