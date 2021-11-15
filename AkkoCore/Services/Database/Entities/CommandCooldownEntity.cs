using AkkoCore.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace AkkoCore.Services.Database.Entities;

/// <summary>
/// Stores a command whose execution is restricted by a cooldown.
/// </summary>
[Comment("Stores commands whose execution is restricted by a cooldown.")]
public class CommandCooldownEntity : DbEntity
{
    /// <summary>
    /// The settings of the Discord guild this command cooldown is associated with.
    /// </summary>
    public GuildConfigEntity? GuildConfigRel { get; init; }

    /// <summary>
    /// The ID of the Discord guild associated with this cooldown.
    /// </summary>
    /// <remarks>This property is <see langword="null"/> for global cooldowns.</remarks>
    public ulong? GuildIdFK { get; init; }

    /// <summary>
    /// The qualified name of the command.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Command { get; init; } = null!;

    /// <summary>
    /// Determines the time for the cooldown.
    /// </summary>
    public TimeSpan Cooldown { get; set; }
}