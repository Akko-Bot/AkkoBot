using AkkoCore.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoCore.Services.Database.Entities;

/// <summary>
/// Stores data about users that got muted in a specific Discord guild..
/// </summary>
[Comment("Stores data about users that got muted in a specific server.")]
public class MutedUserEntity : DbEntity
{
    /// <summary>
    /// The settings of the Discord guild this muted user is associated with.
    /// </summary>
    public GuildConfigEntity? GuildConfigRel { get; init; }

    /// <summary>
    /// The ID of the Discord guild this muted user is associated with.
    /// </summary>
    public ulong GuildIdFK { get; init; }

    /// <summary>
    /// The ID of the Discord user that got muted.
    /// </summary>
    public ulong UserId { get; init; }
}