using AkkoCore.Services.Database.Enums;
using System;
using System.Collections.Generic;

namespace AkkoCog.AntiPhishing.AntiPhishing.Models;

/// <summary>
/// Defines the anti-phishing settings of a Discord guild.
/// </summary>
public sealed class AntiPhishingGuildConfig
{
    private readonly Lazy<List<ulong>> _ignoredIds = new();

    /// <summary>
    /// Defines all IDs that should be ignored by the filter.
    /// </summary>
    public List<ulong> IgnoredIds
    {
        get => _ignoredIds.Value;
        init
        {
            _ignoredIds.Value.Clear();

            foreach (var id in value)
                _ignoredIds.Value.Add(id);
        }
    }

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