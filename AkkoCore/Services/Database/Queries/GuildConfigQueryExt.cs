using AkkoCore.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AkkoCore.Services.Database.Queries
{
    public static class GuildConfigQueryExt
    {
        /// <summary>
        /// Includes all navigation properties relevant for caching of a <see cref="GuildConfigEntity"/>.
        /// </summary>
        /// <param name="query">This query.</param>
        /// <remarks>
        /// Included properties:
        /// AliasRel, CommandCooldownRel, TagsRel, GatekeepRel, FilteredWordsRel, FilteredContentRel, VoiceRolesRel,
        /// RepeaterRel, PollRel, AutoSlowmodeRel, PermissionOverrideRel
        /// </remarks>
        /// <returns>A query that includes the aforementioned navigation properties.</returns>
        public static IQueryable<GuildConfigEntity> IncludeCacheable(this IQueryable<GuildConfigEntity> query)
        {
            return query
                .Include(x => x.AliasRel)
                .Include(x => x.CommandCooldownRel)
                .Include(x => x.TagsRel)
                .Include(x => x.GatekeepRel)
                .Include(x => x.FilteredWordsRel)
                .Include(x => x.FilteredContentRel)
                .Include(x => x.VoiceRolesRel)
                .Include(x => x.RepeaterRel)
                .Include(x => x.PollRel)
                .Include(x => x.AutoSlowmodeRel)
                .Include(x => x.GuildLogsRel)
                .Include(x => x.PermissionOverrideRel);
        }
    }
}