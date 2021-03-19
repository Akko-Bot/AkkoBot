using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using ConcurrentCollections;
using System.Collections.Concurrent;
using System.Linq;

namespace AkkoBot.Services.Database.Repository
{
    public class AliasRepo : DbRepository<AliasEntity>
    {
        public ConcurrentDictionary<ulong, ConcurrentHashSet<AliasEntity>> Cache { get; }

        public AliasRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
            => Cache = dbCacher.Aliases;

        public bool AddOrUpdate(AliasEntity newEntry, ulong? sid, out AliasEntity dbEntry)
        {
            dbEntry = base.Table.FirstOrDefault(x => x.GuildId == sid && x.Alias == newEntry.Alias);

            if (dbEntry is null)
            {
                base.Create(newEntry);
                dbEntry = newEntry;

                return true;
            }
            else
            {
                dbEntry.IsDynamic = newEntry.IsDynamic;
                dbEntry.Arguments = newEntry.Arguments;

                return false;
            }
        }
    }
}