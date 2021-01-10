using AkkoBot.Command.Abstractions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Repository;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database
{
    public class AkkoUnitOfWork : IUnitOfWork, ICommandService
    {
        private readonly AkkoDbContext _db;

        public DiscordUserRepo DiscordUsers { get; }
        public DbRepository<BlacklistEntity> Blacklist { get; }
        public DbRepository<BotConfigEntity> BotConfig { get; }
        public GuildConfigRepo GuildConfigs { get; }
        public DbRepository<PlayingStatusEntity> PlayingStatuses { get; }

        public AkkoUnitOfWork(AkkoDbContext db, AkkoDbCacher dbCache)
        {
            _db = db;

            DiscordUsers = new(db);
            Blacklist = new(db);
            BotConfig = new(db);
            GuildConfigs = new(db, dbCache);
            PlayingStatuses = new(db);
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
    }
}
