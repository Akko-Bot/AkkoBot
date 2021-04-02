using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Repository
{
    public class ReminderRepo : DbRepository<ReminderEntity>
    {
        public ReminderRepo(AkkoDbContext db) : base(db) { }

        /// <summary>
        /// Gets the amount of reminders under the specified Discord user ID.
        /// </summary>
        /// <param name="uid">The ID of the Discord user.</param>
        /// <returns>The amount of reminders.</returns>
        public async Task<int> UserReminderCountAsync(ulong uid)
            => await base.Table.CountAsync(x => x.AuthorId == uid);
    }
}
