using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Services.Database.Abstractions;
using AkkoCore.Services.Database.Enums;
using Kotz.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoCore.Services.Database.Entities;

/// <summary>
/// Stores settings and data related to gatekeeping.
/// </summary>
[Comment("Stores settings and data related to gatekeeping.")]
public class GatekeepEntity : DbEntity
{
    private string? _customSanitizedName;
    private string? _greetMessage;
    private string? _farewellMessage;

    /// <summary>
    /// The settings of the Discord guild these gatekeeping settings are associated with.
    /// </summary>
    public GuildConfigEntity? GuildConfigRel { get; init; }

    /// <summary>
    /// The ID of the Discord guild these settings are associated with.
    /// </summary>
    public ulong GuildIdFK { get; init; }

    /// <summary>
    /// Determines whether names starting with symbols should be sanitized.
    /// </summary>
    public bool SanitizeNames { get; set; }

    /// <summary>
    /// Defines the replacement name to be assigned to users whose names should be sanitized.
    /// </summary>
    [MaxLength(AkkoConstants.MaxUsernameLength)]
    public string? CustomSanitizedName
    {
        get => _customSanitizedName;
        set => _customSanitizedName = value?.MaxLength(AkkoConstants.MaxUsernameLength);
    }

    /// <summary>
    /// Defines the message to be sent when a user joins the guild.
    /// </summary>
    [MaxLength(AkkoConstants.MaxMessageLength)]
    public string? GreetMessage
    {
        get => _greetMessage;
        set => _greetMessage = value?.MaxLength(AkkoConstants.MaxMessageLength);
    }

    /// <summary>
    /// Defines the message to be sent when a user leaves the guild.
    /// </summary>
    [MaxLength(AkkoConstants.MaxMessageLength)]
    public string? FarewellMessage
    {
        get => _farewellMessage;
        set => _farewellMessage = value?.MaxLength(AkkoConstants.MaxMessageLength);
    }

    /// <summary>
    /// Defines the Discord channel the greet message should be sent to.
    /// </summary>
    public ulong? GreetChannelId { get; set; }

    /// <summary>
    /// Defines the Discord channel the farewell message should be sent to.
    /// </summary>
    public ulong? FarewellChannelId { get; set; }

    /// <summary>
    /// Defines the punishment role to be applied to alt users.
    /// </summary>
    public ulong? AntiAltRoleId { get; set; }

    /// <summary>
    /// Defines whether greet messages should be sent in direct message.
    /// </summary>
    public bool GreetDm { get; set; }

    /// <summary>
    /// Defines whether alt users should be automatically punished on join.
    /// </summary>
    public bool AntiAlt { get; set; }

    /// <summary>
    /// Defines the type of punishment to apply on alt users.
    /// </summary>
    /// <remarks>Currently only supports Mute, Kick, Ban and AddRole.</remarks>
    public PunishmentType AntiAltPunishType { get; set; }

    /// <summary>
    /// Defines how long the greet message should last before it gets deleted.
    /// </summary>
    public TimeSpan GreetDeleteTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Defines how long the farewell message should last before it gets deleted.
    /// </summary>
    public TimeSpan FarewellDeleteTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Defines the time difference between joining date and creation date for a user to be considered an alt.
    /// </summary>
    public TimeSpan AntiAltTime { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Defines whether gatekeeping is active for this guild.
    /// </summary>
    /// <remarks>This property is not mapped.</remarks>
    [NotMapped]
    public bool IsActive
        => SanitizeNames || AntiAlt
        || ((GreetChannelId is not null || GreetDm) && !string.IsNullOrWhiteSpace(GreetMessage))
        || (FarewellChannelId is not null && !string.IsNullOrWhiteSpace(FarewellMessage));
}