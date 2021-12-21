using AkkoCore.Services.Database.Enums;

namespace AkkoCog.AntiPhishing.AntiPhishing.Models;

/// <summary>
/// Defines the anti-phishing settings of a Discord guild.
/// </summary>
internal sealed class AntiPhishingGuildConfig
{
    /// <summary>
    /// Defines the Id of the Discord guild.
    /// </summary>
    /// <value></value>
    public ulong GuildId { get; init; }

    /// <summary>
    /// Defines whether anti-phishing filtering are enabled for this Discord guild.
    /// </summary>
    /// <value></value>
    public bool IsActive { get; set; }

    /// <summary>
    /// Defines the punishment to be applied to the offending user.
    /// </summary>
    public PunishmentType? PunishmentType { get; set; }

    public AntiPhishingGuildConfig()
    {
    }

    public AntiPhishingGuildConfig(ulong guildId, bool isActive, PunishmentType? punishmentType)
    {
        GuildId = guildId;
        IsActive = isActive;
        PunishmentType = punishmentType;
    }
}