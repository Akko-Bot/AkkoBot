using AkkoCore.Extensions;
using AkkoCore.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AkkoCore.Services.Database.Entities
{
    /// <summary>
    /// Stores the content filters to be applied to a Discord channel.
    /// </summary>
    [Comment("Stores the content filters to be applied to a Discord channel.")]
    public class FilteredContentEntity : DbEntity
    {
        /// <summary>
        /// The settings of the Discord guild this filter is associated with.
        /// </summary>
        public GuildConfigEntity GuildConfigRel { get; init; }

        /// <summary>
        /// The ID of the Discord guild this filter is associated with.
        /// </summary>
        public ulong GuildIdFK { get; init; }

        /// <summary>
        /// The ID of the Discord channel this filter has been applied to.
        /// </summary>
        public ulong ChannelId { get; init; }

        /// <summary>
        /// Determines whether only messages with attachments are allowed.
        /// </summary>
        public bool IsAttachmentOnly { get; set; }

        /// <summary>
        /// Determines whether only messages with images are allowed.
        /// </summary>
        public bool IsImageOnly { get; set; }

        /// <summary>
        /// Determines whether only messages with URLs are allowed.
        /// </summary>
        public bool IsUrlOnly { get; set; }

        /// <summary>
        /// Determines whether only messages with server invites are allowed.
        /// </summary>
        public bool IsInviteOnly { get; set; }

        /// <summary>
        /// Determines whether only messages with valid commands are allowed.
        /// </summary>
        public bool IsCommandOnly { get; set; }

        /// <summary>
        /// Gets the name of all currently active content filters.
        /// </summary>
        /// <remarks>This property is not mapped.</remarks>
        [NotMapped]
        public IEnumerable<string> ActiveFilters
            => Filters.Where(x => x.Value).Select(x => x.Key);

        /// <summary>
        /// Checks whether this filter is active.
        /// </summary>
        /// <remarks> This property is not mapped.</remarks>
        /// <value><see langword="true"/> if active, <see langword="false"/> otherwise.</value>
        [NotMapped]
        public bool IsActive
            => IsAttachmentOnly || IsImageOnly || IsUrlOnly || IsInviteOnly || IsCommandOnly;

        /// <summary>
        /// Gets the content filters and the value they are currently set to.
        /// </summary>
        /// <remarks> This property is not mapped.</remarks>
        /// <value>The filters' name and their corresponding value.</value>
        [NotMapped]
        public IReadOnlyDictionary<string, bool> Filters => new Dictionary<string, bool>()
        {
            [nameof(IsAttachmentOnly).ToSnakeCase()] = IsAttachmentOnly,
            [nameof(IsImageOnly).ToSnakeCase()] = IsImageOnly,
            [nameof(IsUrlOnly).ToSnakeCase()] = IsUrlOnly,
            [nameof(IsInviteOnly).ToSnakeCase()] = IsInviteOnly,
            [nameof(IsCommandOnly).ToSnakeCase()] = IsCommandOnly
        };
    }
}