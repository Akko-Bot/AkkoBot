using AkkoBot.Services.Database.Entities;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Repository
{
    public class DiscordUserRepo : DbRepository<DiscordUserEntity>
    {
        public DiscordUserRepo(AkkoDbContext db) : base(db) { }

        /// <summary>
        /// Tracks a user to be upserted to the database.
        /// </summary>
        /// <param name="user">User to be added or updated.</param>
        /// <returns><see langword="true"/> if the entry is being tracked for creation or updating, <see langword="false"/> if no action was performed.</returns>
        public async Task<bool> CreateOrUpdateAsync(DiscordUser user)
        {
            var dbEntry = await base.Table.FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (dbEntry is null)
                base.Create(new DiscordUserEntity(user));
            else
            {
                if (dbEntry.UserId == user.Id && dbEntry.Username.Equals(user.Username) && dbEntry.Discriminator.Equals(user.Discriminator))
                    return false;

                dbEntry.Username = user.Username;
                dbEntry.Discriminator = user.Discriminator;
                base.Update(dbEntry);
            }

            return true;
        }
    }
}