using AkkoCore.Services.Database.Abstractions;
using AkkoCore.Services.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AkkoCore.Services.Database.Entities;

/// <summary>
/// Stores modrole data.
/// </summary>
[Comment("Stores modrole data.")]
public sealed class ModroleEntity : DbEntity
{
    /// <summary>
    /// The settings of the Discord guild this modrole is associated with.
    /// </summary>
    public GuildConfigEntity GuildConfigRel { get; init; } = null!;

    /// <summary>
    /// The ID of the Discord guild associated with this modrole.
    /// </summary>
    public ulong GuildIdFK { get; init; }

    /// <summary>
    /// The role ID of this modrole.
    /// </summary>
    public ulong ModroleId { get; init; }

    /// <summary>
    /// Determines the behavior of this modrole.
    /// </summary>
    public ModroleBehavior Behavior { get; set; }

    /// <summary>
    /// The ID of roles this modrole is allowed to interact with.
    /// </summary>
    public List<long> TargetRoleIds { get; init; } = new();
}