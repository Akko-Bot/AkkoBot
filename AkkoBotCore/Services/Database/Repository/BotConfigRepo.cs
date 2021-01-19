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
        public BotConfigEntity Cache { get; private set; }

        public BotConfigRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
        {
            _db = db;
            Cache = dbCacher.BotConfig;
        }

        /// <summary>
        /// Adds an entry for the bot's settings into the database.
        /// </summary>
        /// <param name="botConfig">The new bot settings.</param>
        /// <remarks>If an entry already exists, it does nothing.</remarks>
        /// <returns><see langword="true"/> if the entry got added to the database, <see langword="false"/> otherwise.</returns>
        public async Task<bool> TryCreateAsync(BotConfigEntity botConfig)
        {
            var result = await base.GetAsync(x => x.BotId == botConfig.BotId);

            if (result is null)
            {
                await base.CreateAsync(botConfig);
                await _db.SaveChangesAsync();
                Cache = botConfig;
                return true;
            }

            return false;
        }
    }
}