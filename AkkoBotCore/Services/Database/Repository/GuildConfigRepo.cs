using AkkoBot.Services.Database.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Repository
{
    public class GuildConfigRepo : DbRepository<GuildConfigEntity>
    {
        private readonly AkkoDbCacher _dbCacher;

        public GuildConfigRepo(AkkoDbContext db, AkkoDbCacher dbCacher) : base(db)
        {
            _dbCacher = dbCacher;
        }

        public async Task<GuildConfigEntity> GetGuild(ulong sid)
        {
            return (_dbCacher.Guilds.ContainsKey(sid))
                ? _dbCacher.Guilds[sid]
                : await base.GetAsync(sid);
        }
    }
}
