using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.CommandsNext;
using System.Collections.Concurrent;
using System.Linq;

namespace AkkoBot.Services.Database.Repository
{
    public class BotConfigRepo : DbRepository<BotConfigEntity>
    {
        private readonly IDbCacher _dbCacher;
        public BotConfigEntity Cache { get; private set; }
        public ConcurrentDictionary<string, Command> DisabledCommandCache { get; }

        public BotConfigRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
        {
            _dbCacher = dbCacher;
            Cache = dbCacher.BotConfig;
            DisabledCommandCache = dbCacher.DisabledCommandCache;
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

        /// <summary>
        /// Adds a disabled command to the database.
        /// </summary>
        /// <param name="cmd">The command to be disabled.</param>
        /// <returns><see langword="true"/> if the command is being tracked for insertion, <see langword="false"/> otherwise.</returns>
        public bool AddDisabledCommand(Command cmd)
        {
            if (!DisabledCommandCache.TryAdd(cmd.QualifiedName, cmd))
                return false;

            var dbEntry = base.Table.FirstOrDefault();
            dbEntry.DisabledCommands.Add(cmd.QualifiedName);

            return true;
        }

        /// <summary>
        /// Removes a disabled command from the database.
        /// </summary>
        /// <param name="qualifiedName">The qualified name of the command.</param>
        /// <param name="command">The command to be enabled.</param>
        /// <returns><see langword="true"/> if the command is being tracked for removal, <see langword="false"/> otherwise.</returns>
        public bool RemoveDisabledCommand(string qualifiedName, out Command command)
        {
            if (!DisabledCommandCache.TryRemove(qualifiedName, out command))
                return false;

            var dbEntry = base.Table.FirstOrDefault();
            dbEntry.DisabledCommands.Remove(command.QualifiedName);

            return true;
        }
    }
}