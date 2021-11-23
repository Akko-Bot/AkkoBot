using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Extensions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AkkoCore.Commands.Common;

/// <summary>
/// Keeps track of commands and users on a global and/or guild cooldown.
/// </summary>
[CommandService<ICommandCooldown>(ServiceLifetime.Singleton)]
internal sealed class AkkoCooldown : ICommandCooldown
{
    private ConcurrentDictionary<string, TimeSpan> _globalCooldown = new();
    private ConcurrentDictionary<(string, ulong), TimeSpan> _serverCooldown = new();
    private ConcurrentDictionary<(string, ulong), DateTimeOffset> _recentUsers = new();

    public IReadOnlyDictionary<string, TimeSpan> GlobalCommands => _globalCooldown;
    public IReadOnlyDictionary<(string, ulong), TimeSpan> GuildCommands => _serverCooldown;

    public AkkoCooldown(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);
        LoadFromEntities(db.CommandCooldown.Where(x => !x.GuildIdFK.HasValue));
    }

    public bool ContainsCommand(Command cmd, DiscordGuild? server = default)
    {
        return (server is null)
            ? _globalCooldown.ContainsKey(cmd.QualifiedName)
            : _globalCooldown.ContainsKey(cmd.QualifiedName) || _serverCooldown.ContainsKey((cmd.QualifiedName, server.Id));
    }

    public bool AddCommand(Command cmd, TimeSpan duration, DiscordGuild? server = default)
    {
        return (server is null)
            ? _globalCooldown.TryAdd(cmd.QualifiedName, duration)
            : _serverCooldown.TryAdd((cmd.QualifiedName, server.Id), duration);
    }

    public bool RemoveCommand(string qualifiedCommand, ulong? sid)
    {
        return (!sid.HasValue)
            ? _globalCooldown.TryRemove(qualifiedCommand, out _)
            : _serverCooldown.TryRemove((qualifiedCommand, sid.Value), out _);
    }

    public bool AddUser(Command cmd, DiscordUser user)
        => _recentUsers.TryAdd((cmd.QualifiedName, user.Id), DateTimeOffset.Now);

    public bool IsOnCooldown(Command cmd, DiscordUser user, DiscordGuild? server = default)
    {
        // Check cooldown
        if (IsOnGlobalCooldown(cmd, user) || IsOnServerCooldown(cmd, user, server))
            return true;

        _recentUsers.TryRemove((cmd.QualifiedName, user.Id), out _);
        return false;
    }

    public ICommandCooldown LoadFromEntities(IEnumerable<CommandCooldownEntity> dbCommands)
    {
        foreach (var dbCmd in dbCommands)
        {
            if (!dbCmd.GuildIdFK.HasValue)
                _globalCooldown.AddOrUpdate(dbCmd.Command, dbCmd.Cooldown, (_, _) => dbCmd.Cooldown);
            else
                _serverCooldown.AddOrUpdate((dbCmd.Command, dbCmd.GuildIdFK.Value), dbCmd.Cooldown, (_, _) => dbCmd.Cooldown);
        }

        return this;
    }

    public ICommandCooldown UnloadFromEntities(IEnumerable<CommandCooldownEntity> dbCommands)
    {
        foreach (var dbCmd in dbCommands)
        {
            if (!dbCmd.GuildIdFK.HasValue)
                _globalCooldown.TryRemove(dbCmd.Command, out _);
            else
                _serverCooldown.TryRemove((dbCmd.Command, dbCmd.GuildIdFK.Value), out _);
        }

        return this;
    }

    public void Dispose()
    {
        _globalCooldown?.Clear();
        _serverCooldown?.Clear();
        _recentUsers?.Clear();

        _globalCooldown = default!;
        _serverCooldown = default!;
        _recentUsers = default!;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Checks if the specified command and user are on a global cooldown.
    /// </summary>
    /// <param name="cmd">The command on cooldown.</param>
    /// <param name="user">The user to be checked.</param>
    /// <returns><see langword="true"/> if the user has an active global cooldown for <paramref name="cmd"/>, <see langword="false"/> otherwise.</returns>
    private bool IsOnGlobalCooldown(Command cmd, DiscordUser user)
    {
        return _globalCooldown.TryGetValue(cmd.QualifiedName, out var cooldown)
            && _recentUsers.TryGetValue((cmd.QualifiedName, user.Id), out var time)
            && DateTimeOffset.Now.Subtract(time) <= cooldown;
    }

    /// <summary>
    /// Checks if the specified command and user are on a server cooldown.
    /// </summary>
    /// <param name="cmd">The command on cooldown.</param>
    /// <param name="user">The user to be checked.</param>
    /// <param name="server">The Discord guild specific to the cooldown.</param>
    /// <returns><see langword="true"/> if the user has an active server cooldown for <paramref name="cmd"/>, <see langword="false"/> otherwise.</returns>
    private bool IsOnServerCooldown(Command cmd, DiscordUser user, DiscordGuild? server)
    {
        return _serverCooldown.TryGetValue((cmd.QualifiedName, server?.Id ?? default), out var cooldown)
            && _recentUsers.TryGetValue((cmd.QualifiedName, user.Id), out var time)
            && DateTimeOffset.Now.Subtract(time) <= cooldown;
    }
}