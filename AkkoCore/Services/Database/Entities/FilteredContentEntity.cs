using AkkoCore.Services.Database.Abstractions;
using AkkoCore.Services.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoCore.Services.Database.Entities;

/// <summary>
/// Stores the content filters to be applied to a Discord channel.
/// </summary>
[Comment("Stores the content filters to be applied to a Discord channel.")]
public class FilteredContentEntity : DbEntity
{
    /// <summary>
    /// The settings of the Discord guild this filter is associated with.
    /// </summary>
    public GuildConfigEntity? GuildConfigRel { get; init; }

    /// <summary>
    /// The ID of the Discord guild this filter is associated with.
    /// </summary>
    public ulong GuildIdFK { get; init; }

    /// <summary>
    /// The ID of the Discord channel this filter has been applied to.
    /// </summary>
    public ulong ChannelId { get; init; }

    /// <summary>
    /// Contains the type of content for which this filter is active.
    /// </summary>
    public ContentFilter ContentType { get; set; }

    /// <summary>
    /// Checks whether this filter is active.
    /// </summary>
    /// <remarks> This property is not mapped.</remarks>
    /// <value><see langword="true"/> if active, <see langword="false"/> otherwise.</value>
    [NotMapped]
    public bool IsActive
        => ContentType is not ContentFilter.None;
}