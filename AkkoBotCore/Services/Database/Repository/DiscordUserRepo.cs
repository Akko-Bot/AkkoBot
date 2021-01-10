using AkkoBot.Services.Database.Entities;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Repository
{
    public class DiscordUserRepo : DbRepository<DiscordUserEntity>
    {
        private readonly AkkoDbContext _db;

        public DiscordUserRepo(AkkoDbContext db) : base(db)
            => _db = db;

        public async Task CreateOrUpdate(DiscordUser user)
        {
            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO discord_users(user_id, username, discriminator) " +
                $"VALUES({user.Id}, '{user.Username}', '{user.Discriminator}') " +
                @"ON CONFLICT (user_id) " +
                @"DO " +
                $"UPDATE SET username = '{user.Username}', discriminator = '{user.Discriminator}';"
            );
        }
    }
}
