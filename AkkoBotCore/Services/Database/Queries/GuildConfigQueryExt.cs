using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Queries
{
    public static class GuildConfigQueryExt
    {
        /// <summary>
        /// Gets the settings of the specified Discord guild.
        /// </summary>
        /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <returns> The guild settings, <see langword="null"/> if the guild is not present in the database. </returns>
        public static async Task<GuildConfigEntity> GetGuildWithVoiceRolesAsync(this DbSet<GuildConfigEntity> table, ulong sid)
        {
            return await table.AsNoTracking()
                .Include(x => x.VoiceRolesRel)
                .FirstOrDefaultAsync(x => x.GuildId == sid);
        }

        /// <summary>
        /// Gets the settings of the specified Discord guild with all users that are muted on the server.
        /// </summary>
        /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <returns>The guild settings, <see langword="null"/> if the guild is not present in the database.</returns>
        public static async Task<GuildConfigEntity> GetGuildWithMutesAsync(this DbSet<GuildConfigEntity> table, ulong sid)
        {
            return await table.AsNoTracking()
                .Include(x => x.MutedUserRel)
                .FirstOrDefaultAsync(x => x.GuildId == sid);
        }

        /// <summary>
        /// Gets the settings of the specified Discord guild with the warnings of a specific user.
        /// </summary>
        /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <param name="uid">The ID of the Discord user.</param>
        /// <param name="type">The type of entry to be fetched.</param>
        /// <remarks>The warnings will be empty if the user has none.</remarks>
        /// <returns>The guild settings with warnings, warn punishments and occurrences, <see langword="null"/> if the guild is not present in the database.</returns>
        public static async Task<GuildConfigEntity> GetGuildWithWarningsAsync(this DbSet<GuildConfigEntity> table, ulong sid, ulong uid, WarnType type)
        {
            return await table.AsNoTracking()
                .Include(x => x.OccurrenceRel.Where(x => x.UserId == uid))
                .Include(x => x.WarnRel.Where(x => x.UserId == uid && x.Type == type))
                .Include(x => x.WarnPunishRel)
                .FirstOrDefaultAsync(x => x.GuildId == sid);
        }

        /// <summary>
        /// Gets the settings of the specified Discord guild with the warnings of a specific user.
        /// </summary>
        /// <param name="table">This <see cref="DbContext.Set{TEntity}"/>.</param>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <param name="uid">The ID of the Discord user.</param>
        /// <remarks>The warnings will be empty if the user has none.</remarks>
        /// <returns>The guild settings with warnings, warn punishments and occurrences, <see langword="null"/> if the guild is not present in the database.</returns>
        public static async Task<GuildConfigEntity> GetGuildWithWarningsAsync(this DbSet<GuildConfigEntity> table, ulong sid, ulong uid)
        {
            return await table.AsNoTracking()
                .Include(x => x.OccurrenceRel.Where(x => x.UserId == uid))
                .Include(x => x.WarnRel.Where(x => x.UserId == uid))
                .Include(x => x.WarnPunishRel)
                .FirstOrDefaultAsync(x => x.GuildId == sid);
        }
    }
}