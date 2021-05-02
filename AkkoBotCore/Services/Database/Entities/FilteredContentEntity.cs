using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores the content filters to be applied to a Discord channel.")]
    public class FilteredContentEntity : DbEntity
    {
        public GuildConfigEntity GuildConfigRel { get; set; }

        public ulong GuildIdFK { get; init; }
        public ulong ChannelId { get; init; }
        public bool IsAttachmentOnly { get; set; }
        public bool IsImageOnly { get; set; }
        public bool IsUrlOnly { get; set; }
        public bool IsInviteOnly { get; set; }
        public bool IsCommandOnly { get; set; }

        /// <summary>
        /// Gets the name of all active filters.
        /// </summary>
        /// <returns>A collection of names of all active filters.</returns>
        public IEnumerable<string> GetActiveFilters()
            => GetFilters().Where(x => x.Value).Select(x => x.Key);

        /// <summary>
        /// Checks whether this filter is active.
        /// </summary>
        /// <returns><see langword="true"/> if at least one filter is active, <see langword="false"/> otherwise.</returns>
        public bool IsActive()
            => IsAttachmentOnly || IsImageOnly || IsUrlOnly || IsInviteOnly || IsCommandOnly;

        /// <summary>
        /// Gets the filters and the value they are currently set to.
        /// </summary>
        /// <returns>The filters and its value.</returns>
        public IReadOnlyDictionary<string, bool> GetFilters()
        {
            return new Dictionary<string, bool>()
            {
                [nameof(IsAttachmentOnly).ToSnakeCase()] = IsAttachmentOnly,
                [nameof(IsImageOnly).ToSnakeCase()] = IsImageOnly,
                [nameof(IsUrlOnly).ToSnakeCase()] = IsUrlOnly,
                [nameof(IsInviteOnly).ToSnakeCase()] = IsInviteOnly,
                [nameof(IsCommandOnly).ToSnakeCase()] = IsCommandOnly
            };
        }
    }
}
