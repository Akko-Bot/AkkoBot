using AkkoBot.Services.Database.Entities;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Repository
{
    public class GuildConfigRepo : DbRepository<GuildConfigEntity>
    {
        private readonly AkkoDbContext _db;
        private readonly AkkoDbCacher _dbCacher;

        public GuildConfigRepo(AkkoDbContext db, AkkoDbCacher dbCacher) : base(db)
        {
            _db = db;
            _dbCacher = dbCacher;
        }

        public async Task<GuildConfigEntity> GetGuildAsync(ulong sid)
        {
            if (_dbCacher.Guilds.ContainsKey(sid))
                return _dbCacher.Guilds[sid];
            else
            {
                var guild = await base.GetAsync(sid);
                _dbCacher.Guilds.TryAdd(guild.GuildId, guild);

                return guild;
            }
        }

        public async Task TryAddAsync(DiscordGuild guild)
        {
            var dGuild = new GuildConfigEntity();

            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO discord_users(guild_id, prefix, use_embed, ok_color, error_color) " +
                $"VALUES({guild.Id}, '{_dbCacher.DefaultPrefix}', {true}, '{dGuild.OkColor}', '{dGuild.ErrorColor}') " +
                @"ON CONFLICT (guild_id) " +
                @"DO NOTHING;"
            );
        }

        public async Task CreateOrUpdateAsync(GuildConfigEntity guild)
        {
            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO discord_users(guild_id, prefix, use_embed, ok_color, error_color) " +
                $"VALUES({guild.GuildId}, '{guild.Prefix}', {guild.UseEmbed}, '{guild.OkColor}', '{guild.ErrorColor}') " +
                @"ON CONFLICT (guild_id) " +
                @"DO " +
                @"UPDATE SET " +
                $"prefix = '{guild.Prefix}', use_embed = '{guild.UseEmbed}', " +
                $"ok_color = '{guild.OkColor}', error_color = '{guild.ErrorColor}';"
            );

            // Update the cache
            if (!_dbCacher.Guilds.TryAdd(guild.GuildId, guild))
                _dbCacher.Guilds.TryUpdate(guild.GuildId, guild, _dbCacher.Guilds[guild.GuildId]);
        }
    }
}
