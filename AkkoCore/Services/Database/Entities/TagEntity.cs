using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Services.Database.Abstractions;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using Kotz.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AkkoCore.Services.Database.Entities;

/// <summary>
/// Stores data related to a tag.
/// </summary>
[Comment("Stores data related to a tag.")]
public class TagEntity : DbEntity
{
    private DateTimeOffset _lastDayUsed = DateTimeOffset.Now.StartOfDay().ToOffset(TimeSpan.Zero);

    /// <summary>
    /// The settings of the Discord guild this tag is associated with.
    /// </summary>
    /// <remarks>This property is <see langword="null"/> for global tags.</remarks>
    public GuildConfigEntity? GuildConfigRel { get; init; }

    /// <summary>
    /// List of IDs ignored by this tag.
    /// </summary>
    public List<long> IgnoredIds { get; init; } = new();

    /// <summary>
    /// The ID of the Discord guild this tag is associated with. This is <see langword="null"/> for global tags.
    /// </summary>
    public ulong? GuildIdFK { get; init; }

    /// <summary>
    /// The ID of the Discord user that created this tag.
    /// </summary>
    public ulong AuthorId { get; init; }

    /// <summary>
    /// The content required for this tag to be triggered.
    /// </summary>
    [Required]
    [MaxLength(AkkoConstants.MaxMessageLength)]
    public string Trigger { get; set; } = null!;

    /// <summary>
    /// The content to be sent to Discord. If <see cref="IsEmoji"/> is <see langword="true"/>,
    /// this property contains emoji strings that should be used to react to the triggering message.
    /// </summary>
    [Required]
    [MaxLength(AkkoConstants.MaxMessageLength)]
    public string Response { get; set; } = null!;

    /// <summary>
    /// Determines whether this tag should be sent as emoji reactions to the triggering message.
    /// </summary>
    public bool IsEmoji { get; set; }

    /// <summary>
    /// Determines the action to be taken when this tag is triggered.
    /// </summary>
    public TagBehavior Behavior { get; set; } = TagBehavior.None;

    /// <summary>
    /// Determines the permissions required from the user for this tag to be triggered.
    /// </summary>
    public Permissions AllowedPerms { get; set; } = Permissions.None;

    /// <summary>
    /// Defines the last day this tag was used.
    /// </summary>
    public DateTimeOffset LastDayUsed
    {
        get => _lastDayUsed;
        set => _lastDayUsed = value.StartOfDay().ToOffset(TimeSpan.Zero);
    }
}