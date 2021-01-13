using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Repository
{
    public class DiscordUserRepo : DbRepository<DiscordUserEntity>
    {
        private readonly AkkoDbContext _db;

        public DiscordUserRepo(AkkoDbContext db) : base(db)
            => _db = db;

        /// <summary>
        /// Upserts a user into the database.
        /// </summary>
        /// <param name="user">User to be added or updated.</param>
        /// <returns></returns>
        public async Task CreateOrUpdateAsync(DiscordUser user)
        {
            var eUser = new DiscordUserEntity(user);

            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO discord_users(user_id, username, discriminator, date_added) " +
                $"VALUES({eUser.UserId}, '{eUser.Username}', '{eUser.Discriminator}', '{eUser.DateAdded:O}') " +
                @"ON CONFLICT (user_id) " +
                @"DO UPDATE " +
                $"SET username = '{user.Username}', discriminator = '{user.Discriminator}';"
            );
        }
    }
}
