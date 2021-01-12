using System.Threading.Tasks;
using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Repository
{
    public class BotConfigRepo : DbRepository<BotConfigEntity>
    {
        private readonly AkkoDbContext _db;
        public BotConfigEntity Cache { get; private set; }

        public BotConfigRepo(AkkoDbContext db, AkkoDbCacher dbCacher) : base(db)
        {
            _db = db;
            Cache = dbCacher.BotConfig;
        }

        /// <summary>
        /// Adds an entry for the bot's settings into the database.
        /// </summary>
        /// <param name="botConfig">The new bot settings.</param>
        /// <remarks>If an entry already exists, it does nothing.</remarks>
        /// <returns></returns>
        public async Task TryCreateAsync(BotConfigEntity botConfig)
        {
            // Add to the database
            var result = await _db.Database.ExecuteSqlRawAsync(
                @"DO $$ BEGIN " +
                @"   IF (SELECT COUNT(*) FROM bot_config) = 0 THEN " +
                @"       INSERT INTO bot_config(default_prefix, log_format, log_time_format, respond_to_dms, case_sensitive_commands, message_size_cache) " +
                $"       VALUES('{botConfig.DefaultPrefix}', '{botConfig.LogFormat}', '{botConfig.LogTimeFormat}', {botConfig.RespondToDms}, {botConfig.CaseSensitiveCommands}, {botConfig.MessageSizeCache}); " +
                @"   END IF; " +
                @"END $$;"
            );

            // Change the cache if the query inserted a row
            if (result > 0)
                Cache = botConfig;
        }
    }
}