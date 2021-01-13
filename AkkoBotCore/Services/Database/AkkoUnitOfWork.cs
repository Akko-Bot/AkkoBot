using AkkoBot.Command.Abstractions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Repository;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database
{
    public class AkkoUnitOfWork : IUnitOfWork
    {
        private readonly AkkoDbContext _db;

        public DiscordUserRepo DiscordUsers { get; }
        public BlacklistRepo Blacklist { get; }
        public BotConfigRepo BotConfig { get; }
        public GuildConfigRepo GuildConfigs { get; }
        public PlayingStatusRepo PlayingStatuses { get; }

        public AkkoUnitOfWork(AkkoDbContext db, IDbCacher dbCache)
        {
            _db = db;

            DiscordUsers = new(db);
            Blacklist = new(db, dbCache);
            BotConfig = new(db, dbCache);
            GuildConfigs = new(db, dbCache);
            PlayingStatuses = new(db, dbCache);
        }

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        public int SaveChanges()
            => _db.SaveChanges();

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous save operation.
        /// The task result contains the number of state entries written to the database.
        /// </returns>
        public Task<int> SaveChangesAsync()
            => _db.SaveChangesAsync();

        /// <summary>
        /// Releases the allocated resources for this unit of work.
        /// </summary>
        public void Dispose()
        {
            _db.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
