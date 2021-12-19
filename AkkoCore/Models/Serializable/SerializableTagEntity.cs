using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using System;

namespace AkkoCore.Models.Serializable;

/// <summary>
/// A serializable version of <see cref="TagEntity"/> for importing and exporting.
/// </summary>
public sealed record SerializableTagEntity
{
    /// <summary>
    /// The ID of the Discord user that created this tag.
    /// </summary>
    public ulong AuthorId { get; init; }

    /// <summary>
    /// The content required for this tag to be triggered.
    /// </summary>
    public string Trigger { get; init; } = string.Empty;

    /// <summary>
    /// The content to be sent to Discord.
    /// </summary>
    public string Response { get; init; } = string.Empty;

    /// <summary>
    /// Determines whether this tag should be sent as emoji reactions to the triggering message.
    /// </summary>
    public bool IsEmoji { get; init; }

    /// <summary>
    /// Determines the action to be taken when this tag is triggered.
    /// </summary>
    public TagBehavior Behavior { get; init; }

    /// <summary>
    /// Determines the permissions required from the user for this tag to be triggered.
    /// </summary>
    public Permissions AllowedPerms { get; init; }

    /// <summary>
    /// Defines the last day this tag was used.
    /// </summary>
    public DateTimeOffset LastDayUsed { get; init; }

    public SerializableTagEntity()
    {
    }

    public SerializableTagEntity(TagEntity dbTag)
    {
        AuthorId = dbTag.AuthorId;
        Trigger = dbTag.Trigger;
        Response = dbTag.Response;
        IsEmoji = dbTag.IsEmoji;
        Behavior = dbTag.Behavior;
        AllowedPerms = dbTag.AllowedPerms;
        LastDayUsed = dbTag.LastDayUsed;
    }

    /// <summary>
    /// Builds the <see cref="TagEntity"/> this object represents.
    /// </summary>
    /// <param name="sid">The ID of the Discord server or <see langword="null"/> if the tag is global.</param>
    /// <returns>A <see cref="TagEntity"/>.</returns>
    public TagEntity Build(ulong? sid)
    {
        return new TagEntity()
        {
            GuildIdFK = sid,
            AuthorId = this.AuthorId,
            Trigger = this.Trigger,
            Response = this.Response,
            IsEmoji = this.IsEmoji,
            Behavior = this.Behavior,
            AllowedPerms = this.AllowedPerms,
            LastDayUsed = this.LastDayUsed
        };
    }
}