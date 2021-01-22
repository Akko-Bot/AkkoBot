using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Repository
{
    public class BotConfigRepo : DbRepository<BotConfigEntity>
    {
        private readonly AkkoDbContext _db;
        private readonly IDbCacher _dbCacher;
        public BotConfigEntity Cache { get; private set; }

        public BotConfigRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
        {
            _db = db;
            _dbCacher = dbCacher;
            Cache = dbCacher.BotConfig;
        }

        /// <summary>
        /// Adds an entry for the bot's settings into the database.
        /// </summary>
        /// <param name="uid">ID of the bot.</param>
        /// <remarks>If an entry already exists for a given ID, it does nothing.</remarks>
        /// <returns><see langword="true"/> if the entry got added to the database, <see langword="false"/> otherwise.</returns>
        public async Task<bool> TryCreateAsync()
        {
            Cache = (await base.GetAllAsync()).FirstOrDefault();

            if (Cache is null) // If it is empty
            {
                Cache = _dbCacher.BotConfig = new BotConfigEntity();
                base.Create(Cache);
                await _db.SaveChangesAsync();

                return true;
            }

            _dbCacher.BotConfig = Cache;

            return false;
        }
    }
}