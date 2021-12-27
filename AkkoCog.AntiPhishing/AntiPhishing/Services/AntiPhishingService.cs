using AkkoCog.AntiPhishing.AntiPhishing.Models;
using AkkoCore.Commands.Attributes;
using AkkoCore.Config.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services.Database.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AkkoCog.AntiPhishing.AntiPhishing.Services;

/// <summary>
/// Manages activation and deactivation of the anti-phishing filter
/// for Discord guilds.
/// </summary>
[CommandService(ServiceLifetime.Singleton)]
public sealed class AntiPhishingService
{
    private readonly IConfigLoader _configLoader;
    private readonly ConcurrentDictionary<ulong, AntiPhishingGuildConfig> _cache;
    private readonly string _configPath;

    public AntiPhishingService(IConfigLoader configLoader)
    {
        _configLoader = configLoader;

        var configFolder = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)?.FullName ?? string.Empty, "Config");

        if (!Directory.Exists(configFolder))
            Directory.CreateDirectory(configFolder);

        _configPath = Path.Combine(configFolder, "config.yaml");
        _cache = _configLoader.LoadConfig<List<AntiPhishingGuildConfig>>(_configPath)
            .ToConcurrentDictionary(x => x.GuildId);
    }

    /// <summary>
    /// Checks if anti-phishing is active for the guild of the specified ID.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <returns><see langword="true"/> if the filter is active, <see langword="false"/> otherwise.</returns>
    public bool IsAntiPhishingActive(ulong sid)
        => _cache.TryGetValue(sid, out var guildConfig) && guildConfig.IsActive;

    /// <summary>
    /// Gets the punishment for the guild of the specified ID.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <returns>The active punishment or <see langword="null"/> if there is no punishment.</returns>
    public PunishmentType? GetPunishment(ulong sid)
        => (_cache.TryGetValue(sid, out var guildConfig)) ? guildConfig.PunishmentType : default;

    /// <summary>
    /// Toggles the anti-phishing filter for the guild of the specified ID.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <returns><see langword="true"/> if the filter was enabled, <see langword="false"/> otherwise.</returns>
    public bool ToggleAntiPhishing(ulong sid)
    {
        if (!_cache.TryGetValue(sid, out var guildConfig))
            guildConfig = new() { GuildId = sid };

        guildConfig.IsActive = !guildConfig.IsActive;
        _cache[sid] = guildConfig;

        _configLoader.SaveConfig(_cache.Values, _configPath);

        return guildConfig.IsActive;
    }

    /// <summary>
    /// Sets the punishment to be used for the guild of the specified ID.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <param name="punishmentType">
    /// The type of punishment to be applied or <see langword="null"/> if no punishment should be applied.
    /// </param>
    /// <returns><see langword="true"/> if the punishment was successfully set, <see langword="false"/> otherwise.</returns>
    public bool SetPunishment(ulong sid, PunishmentType? punishmentType)
    {
        if (punishmentType is PunishmentType.AddRole or PunishmentType.RemoveRole)
            return false;

        if (!_cache.TryGetValue(sid, out var guildConfig))
            guildConfig = new() { GuildId = sid, IsActive = false };

        guildConfig.PunishmentType = punishmentType;
        _cache[sid] = guildConfig;

        _configLoader.SaveConfig(_cache.Values, _configPath);

        return true;
    }
}