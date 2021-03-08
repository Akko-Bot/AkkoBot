using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Repository;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database
{
    public class AkkoUnitOfWork : IUnitOfWork
    {
        private bool _isDisposed = false;
        private readonly AkkoDbContext _db;

        public DiscordUserRepo DiscordUsers { get; private set; }
        public BlacklistRepo Blacklist { get; private set; }
        public BotConfigRepo BotConfig { get; private set; }
        public LogConfigRepo LogConfig { get; private set; }
        public GuildConfigRepo GuildConfig { get; private set; }
        public TimerRepo Timers { get; private set; }
        public PlayingStatusRepo PlayingStatuses { get; private set; }

        public AkkoUnitOfWork(AkkoDbContext db, IDbCacher dbCacher)
        {
            _db = db;

            DiscordUsers = new(db);
            Blacklist = new(db, dbCacher);
            BotConfig = new(db, dbCacher);
            LogConfig = new(db, dbCacher);
            GuildConfig = new(db, dbCacher);
            Timers = new(db, dbCacher);
            PlayingStatuses = new(db, dbCacher);
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!_isDisposed)
            {
                if (isDisposing)
                {
                    _db.Dispose();
                }

                DiscordUsers = null;
                Blacklist = null;
                BotConfig = null;
                GuildConfig = null;
                PlayingStatuses = null;

                _isDisposed = true;
            }
        }
    }
}