using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Repository
{
    public class CommandRepo : DbRepository<CommandEntity>
    {
        public CommandRepo(AkkoDbContext db) : base(db)
        {
        }

        /// <summary>
        /// Gets the amount of automatic commands under the specified Discord user ID.
        /// </summary>
        /// <param name="uid">The ID of the Discord user.</param>
        /// <returns>The amount of commands.</returns>
        public async Task<int> UserCommandCountAsync(ulong uid)
            => await base.Table.CountAsync(x => x.AuthorId == uid);
    }
}