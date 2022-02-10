using AkkoCog.AntiPhishing.AntiPhishing.Models;
using AkkoCore.Commands.Attributes;
using AkkoCore.Config.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services.Database.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AkkoCog.AntiPhishing.AntiPhishing.Services;

/// <summary>
/// Manages activation and deactivation of the anti-phishing filter
/// for Discord guilds.
/// </summary>
[CommandService(ServiceLifetime.Singleton)]
public sealed class AntiPhishingService
{
    private readonly ConcurrentDictionary<ulong, AntiPhishingGuildConfig> _cache;
    private readonly string _configPath;

    private readonly IConfigLoader _configLoader;

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
    /// Gets the anti-phishing config for the guild of the specified ID.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <param name="guildConfig">The anti-phishing guild config or <see langword="null"/> if it's not found.</param>
    /// <returns><see langword="true"/> if the guild config is found, <see langword="false"/> otherwise.</returns>
    public bool TryGetAntiPhishingConfig(ulong sid, [MaybeNullWhen(false)] out AntiPhishingGuildConfig guildConfig)
        => _cache.TryGetValue(sid, out guildConfig);

    /// <summary>
    /// Toggles the anti-phishing filter for the guild of the specified ID.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <returns><see langword="true"/> if the filter was enabled, <see langword="false"/> otherwise.</returns>
    public bool ToggleAntiPhishing(ulong sid)
    {
        if (!_cache.TryGetValue(sid, out var config))
            config = new() { GuildId = sid };

        config.IsActive = !config.IsActive;
        _cache[sid] = config;

        _configLoader.SaveConfig(_cache.Values, _configPath);

        return config.IsActive;
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

        if (!_cache.TryGetValue(sid, out var config))
            config = new() { GuildId = sid, IsActive = false };

        config.PunishmentType = punishmentType;
        _cache[sid] = config;

        _configLoader.SaveConfig(_cache.Values, _configPath);

        return true;
    }

    /// <summary>
    /// Adds or removes ignored IDs for the guild of the specified ID.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <param name="ids">The IDs to be added or removed.</param>
    /// <returns><see langword="true"/> if the ignored list was changed, <see langword="false"/> otherwise.</returns>
    public bool ToggleIgnoredIds(ulong sid, IEnumerable<ulong> ids)
    {
        if (ids is null || !ids.Any())
            return false;

        if (!_cache.TryGetValue(sid, out var config))
            config = new() { GuildId = sid, IsActive = false };

        foreach (var id in ids)
        {
            if (config.IgnoredIds.Contains(id))
                config.IgnoredIds.Remove(id);
            else
                config.IgnoredIds.Add(id);
        }

        _cache[sid] = config;
        _configLoader.SaveConfig(_cache.Values, _configPath);

        return true;
    }
}