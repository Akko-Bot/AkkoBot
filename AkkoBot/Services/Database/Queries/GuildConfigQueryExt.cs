using AkkoDatabase.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AkkoBot.Services.Database.Queries
{
    public static class GuildConfigQueryExt
    {
        /// <summary>
        /// Includes all navigation properties relevant for caching of a <see cref="GuildConfigEntity"/>.
        /// </summary>
        /// <param name="query">This query.</param>
        /// <remarks>Included properties: GatekeepRel, FilteredWordsRel, FilteredContentRel, VoiceRolesRel, RepeaterRel, PollRel, AutoSlowmodeRel</remarks>
        /// <returns>A query that includes the aforementioned navigation properties.</returns>
        public static IQueryable<GuildConfigEntity> IncludeCacheable(this IQueryable<GuildConfigEntity> query)
        {
            return query
                .Include(x => x.GatekeepRel)
                .Include(x => x.FilteredWordsRel)
                .Include(x => x.FilteredContentRel)
                .Include(x => x.VoiceRolesRel)
                .Include(x => x.RepeaterRel)
                .Include(x => x.PollRel)
                .Include(x => x.AutoSlowmodeRel)
                .Include(x => x.GuildLogsRel);
        }
    }
}