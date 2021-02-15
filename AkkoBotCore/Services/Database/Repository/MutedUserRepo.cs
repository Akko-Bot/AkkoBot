using AkkoBot.Services.Database.Entities;
using DSharpPlus.Entities;
using System.Linq;

namespace AkkoBot.Services.Database.Repository
{
    public class MutedUserRepo : DbRepository<MutedUserEntity>
    {
        public MutedUserRepo(AkkoDbContext db) : base(db) { }

        /// <summary>
        /// Gets the database entry of a muted user.
        /// </summary>
        /// <param name="server">Discord server.</param>
        /// <param name="user">Discord user.</param>
        /// <returns>The database of the muted user, <see langword="null"/> if it doesn't exist.</returns>
        public MutedUserEntity GetMutedUser(DiscordGuild server, DiscordUser user)
            => GetMutedUser(server.Id, user.Id);

        /// <summary>
        /// Gets the database entry of a muted user.
        /// </summary>
        /// <param name="sid">ID of the Discord guild.</param>
        /// <param name="uid">ID of the Discord user.</param>
        /// <returns>The database of the muted user, <see langword="null"/> if it doesn't exist.</returns>
        public MutedUserEntity GetMutedUser(ulong sid, ulong uid)
            => base.Table.FirstOrDefault(x => x.GuildIdFK == sid && x.UserId == uid);
    }
}