using AkkoCore.Commands.Attributes;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using AkkoCore.Services.Database.Queries;
using DSharpPlus.Entities;
using Kotz.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration.Services;

/// <summary>
/// Groups utility methods for manipulating <see cref="ModroleEntity"/> objects.
/// </summary>
[CommandService(ServiceLifetime.Singleton)]
public sealed class ModroleService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDbCache _dbCache;
    private readonly RoleService _roleService;

    public ModroleService(IServiceScopeFactory scopeFactory, IDbCache dbCache, RoleService roleService)
    {
        _scopeFactory = scopeFactory;
        _dbCache = dbCache;
        _roleService = roleService;
    }

    /// <summary>
    /// Gets the modroles for the specified Discord guild ID.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <returns>The modroles of the guild.</returns>
    public IReadOnlyCollection<ModroleEntity> GetModroles(ulong sid)
        => (_dbCache.Modroles.TryGetValue(sid, out var modroles)) ? modroles : new(1, 0);

    /// <summary>
    /// Gets the specified modrole from the specified guild.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <param name="modroleId">The role ID of the modrole.</param>
    /// <param name="dbModrole">The database entry of the modrole, <see langword="null"/> if it was not found.</param>
    /// <returns><see langword="true"/> if the modrole was found, <see langword="false"/> otherwise.</returns>
    public bool GetModrole(ulong sid, ulong modroleId, [MaybeNullWhen(false)] out ModroleEntity dbModrole)
    {
        _dbCache.Modroles.TryGetValue(sid, out var modroles);
        dbModrole = modroles?.FirstOrDefault(x => x.ModroleId == modroleId);

        return dbModrole is not null;
    }

    /// <summary>
    /// Updates the specified modrole settings for the specified guild.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <param name="modroleId">The role ID of the mod role.</param>
    /// <param name="setter">Method to change the settings.</param>
    /// <returns>The value returned by <paramref name="setter"/>.</returns>
    public async Task<T> SetModroleAsync<T>(ulong sid, ulong modroleId, Func<ModroleEntity, T> setter)
    {
        if (!_dbCache.Modroles.TryGetValue(sid, out var modRoles))
            modRoles = new();

        var dbModrole = modRoles.FirstOrDefault(x => x.ModroleId == modroleId)
            ?? new() { GuildIdFK = sid, ModroleId = modroleId };

        using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

        db.Modroles.Upsert(dbModrole);
        var result = setter(dbModrole);

        // Save to the database
        // Cache the entry
        if (await db.SaveChangesAsync() is not 0)
        {
            modRoles.Add(dbModrole);
            _dbCache.Modroles.TryAdd(sid, modRoles);
        }

        return result;
    }

    /// <summary>
    /// Removed the modrole from the specified Discord guild.
    /// </summary>
    /// <param name="sid">The ID of the Discord guild.</param>
    /// <param name="modroleId">The role ID of the modrole.</param>
    /// <returns><see langword="true"/> if the modrole was deleted, <see langword="false"/> otherwise.</returns>
    public async Task<bool> DeleteModroleAsync(ulong sid, ulong modroleId)
    {
        if (!GetModrole(sid, modroleId, out var dbModrole))
            return false;

        using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

        var result = await db.Modroles.DeleteAsync(dbModrole) is not 0;

        var modroles = _dbCache.Modroles[sid];
        modroles.TryRemove(dbModrole);

        if (modroles.Count is 0)
            _dbCache.Modroles.TryRemove(sid, out _);

        return result;
    }

    /// <summary>
    /// Adds the target role to the specified user.
    /// </summary>
    /// <param name="server">The Discord guild.</param>
    /// <param name="user">The Discord user who is assigning the role.</param>
    /// <param name="targetUser">The Discord user who is receiving the role.</param>
    /// <param name="targetRole">The role to be assigned.</param>
    /// <returns><see langword="true"/> if the target role was assigned, <see langword="false"/> otherwise.</returns>
    public async Task<bool> AddTargetRoleAsync(DiscordGuild server, DiscordMember user, DiscordMember targetUser, DiscordRole targetRole)
    {
        if (server.CurrentMember.Hierarchy <= targetRole.Position   // Role is higher than the bot
            || targetUser.Roles.Any(x => x.Id == targetRole.Id))    // Target user already has the target role
            return false;

        var modroles = GetModroles(server.Id)
            .Where(x => x.TargetRoleIds.Contains((long)targetRole.Id));

        foreach (var modrole in modroles)
        {
            if (await AssignTargetRoleAsync(user, targetUser, targetRole, modrole))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the target role from the specified user.
    /// </summary>
    /// <param name = "server" > The Discord guild.</param>
    /// <param name="user">The Discord user who is revoking the role.</param>
    /// <param name="targetUser">The Discord user who is losing the role.</param>
    /// <param name="targetRole">The role to be revoked.</param>
    /// <returns><see langword="true"/> if the target role was revoked, <see langword="false"/> otherwise.</returns>
    public async Task<bool> RemoveTargetRoleAsync(DiscordGuild server, DiscordMember user, DiscordMember targetUser, DiscordRole targetRole)
    {
        if (server.CurrentMember.Hierarchy <= targetRole.Position   // Role is higher than the bot
            || !targetUser.Roles.Any(x => x.Id == targetRole.Id))   // Target user doesn't have the target role
            return false;

        var modroles = GetModroles(server.Id)
            .Where(x => x.TargetRoleIds.Contains((long)targetRole.Id));

        foreach (var modrole in modroles)
        {
            if (await RevokeTargetRoleAsync(user, targetUser, targetRole, modrole))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Assigns the <paramref name="targetRole"/> to the <paramref name="targetUser"/> according to the <paramref name="modrole"/> rules.
    /// </summary>
    /// <param name="user">The Discord user who is assigning the role.</param>
    /// <param name="targetUser">The Discord user who is receiving the the role.</param>
    /// <param name="targetRole">The role to be assigned.</param>
    /// <param name="modrole">The database modrole.</param>
    /// <returns><see langword="true"/> if the target role was assigned, <see langword="false"/> otherwise.</returns>
    private async Task<bool> AssignTargetRoleAsync(DiscordMember user, DiscordMember targetUser, DiscordRole targetRole, ModroleEntity modrole)
    {
        if (!modrole.TargetRoleIds.Contains((long)targetRole.Id)
            || (modrole.Behavior.HasFlag(ModroleBehavior.EnforceHierarchy) && user.Hierarchy <= targetRole.Position))
            return false;

        if (modrole.Behavior.HasFlag(ModroleBehavior.Exclusive))
        {
            await targetUser.Roles
                .Where(x => modrole.TargetRoleIds.Contains((long)x.Id))
                .Select(x => targetUser.RevokeRoleAsync(x))
                .WhenAllAsync();
        }

        await targetUser.GrantRoleAsync(targetRole);

        return true;
    }

    /// <summary>
    /// Revokes the <paramref name="targetRole"/> to the <paramref name="targetUser"/> according to the <paramref name="modrole"/> rules.
    /// </summary>
    /// <param name="user">The Discord user who is revoking the role.</param>
    /// <param name="targetUser">The Discord user who is losing the the role.</param>
    /// <param name="targetRole">The role to be revoked.</param>
    /// <param name="modrole">The database modrole.</param>
    /// <returns><see langword="true"/> if the target role was revoked, <see langword="false"/> otherwise.</returns>
    private async Task<bool> RevokeTargetRoleAsync(DiscordMember user, DiscordMember targetUser, DiscordRole targetRole, ModroleEntity modrole)
    {
        if (!modrole.TargetRoleIds.Contains((long)targetRole.Id)
            || !targetUser.Roles.Any(x => x.Id == targetRole.Id)
            || (modrole.Behavior.HasFlag(ModroleBehavior.EnforceHierarchy) && user.Hierarchy <= targetRole.Position))
            return false;

        await targetUser.RevokeRoleAsync(targetRole);

        return true;
    }
}