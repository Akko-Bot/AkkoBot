using AkkoCore.Services.Database.Abstractions;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoCore.Services.Database.Entities;

/// <summary>
/// Stores data related to individual Discord users.
/// </summary>
[Comment("Stores data related to individual Discord users.")]
public class DiscordUserEntity : DbEntity
{
    private string _username = "Unknown";
    private string _discriminator = "0";

    /// <summary>
    /// The infractions associated with this user.
    /// </summary>
    public List<WarnEntity>? WarnRel { get; init; }

    /// <summary>
    /// The timers this associated with this user.
    /// </summary>
    public List<TimerEntity>? TimerRel { get; init; }

    /// <summary>
    /// The ID of the Discord user.
    /// </summary>
    public ulong UserId { get; init; }

    /// <summary>
    /// The username of the Discord user.
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string Username
    {
        get => _username;
        set => _username = value ?? "Unknown";
    }

    /// <summary>
    /// The discriminator of the Discord user.
    /// </summary>
    [Required]
    [StringLength(4)]
    [Column(TypeName = "varchar(4)")]
    public string Discriminator
    {
        get => _discriminator;
        set => _discriminator = value ?? "0";
    }

    /// <summary>
    /// The username and discriminator of the Discord user.
    /// </summary>
    /// <remarks>This property is not mapped.</remarks>
    [NotMapped]
    public string FullName
        => $"{Username}#{Discriminator}";

    public DiscordUserEntity()
    {
    }

    public DiscordUserEntity(DiscordUser user)
    {
        UserId = user.Id;
        Username = user.Username;
        Discriminator = user.Discriminator;
    }

    /* Overrides */

    public override string ToString()
        => Username;

    public static bool operator ==(DiscordUserEntity x, DiscordUserEntity y)
        => x.UserId == y.UserId && x.Username == y.Username;

    public static bool operator !=(DiscordUserEntity x, DiscordUserEntity y)
        => !(x == y);

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || (obj is not null && obj is DiscordUserEntity dbUser && this == dbUser);

    public override int GetHashCode()
        => base.GetHashCode();
}