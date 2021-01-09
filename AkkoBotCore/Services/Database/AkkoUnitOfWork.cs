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

        public int SaveChanges()
            => _db.SaveChanges();

        public Task<int> SaveChangesAsync()
            => _db.SaveChangesAsync();
    }
}
