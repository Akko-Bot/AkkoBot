using AkkoCore.Commands.Attributes;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Queries;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="PermissionOverrideEntity"/> objects.
    /// </summary>
    [CommandService(ServiceLifetime.Singleton)]
    public sealed class PermissionOverrideService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;

        public PermissionOverrideService(IServiceScopeFactory scopeFactory, IDbCache dbCache)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Creates or updates a permission override for the current context.
        /// </summary>
        /// <typeparam name="T">The property being updated.</typeparam>
        /// <param name="sid">The Discord server ID, or <see langword="null"/> if the override should be global.</param>
        /// <param name="cmd">The command to create the override for.</param>
        /// <param name="setter">A method to update the entity.</param>
        /// <returns>The updated property.</returns>
        public async Task<T> SetPermissionOverrideAsync<T>(ulong? sid, Command cmd, Func<PermissionOverrideEntity, T> setter)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);
            var guildId = sid ?? default;

            if (!_dbCache.PermissionOverrides.TryGetValue(guildId, out var permOverrides))
                permOverrides = new();

            var permOverride = permOverrides.FirstOrDefault(x => x.GuildIdFK == sid && x.Command.Equals(cmd.QualifiedName, StringComparison.OrdinalIgnoreCase))
                ?? new() { GuildIdFK = sid, Command = cmd.QualifiedName };

            // Save to the database
            db.PermissionOverride.Upsert(permOverride);

            permOverride.IsActive = true;   // Upserted overrides should always be active, unless the setter says otherwise
            var result = setter(permOverride);

            await db.SaveChangesAsync();

            // Update the cache
            permOverrides.Add(permOverride);
            _dbCache.PermissionOverrides.TryAdd(guildId, permOverrides);

            return result;
        }

        /// <summary>
        /// Removes the permission overrides for a given command.
        /// </summary>
        /// <param name="sid">The Discord server ID, or <see langword="null"/> if the override is global.</param>
        /// <param name="command">The qualified name of the command.</param>
        /// <returns><see langword="true"/> if the permission override was successfully removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemovePermissionOverrideAsync(ulong? sid, string command)
        {
            _dbCache.PermissionOverrides.TryGetValue(sid ?? default, out var permOverrides);

            var permOverride = permOverrides?.FirstOrDefault(x => x.Command.Equals(command, StringComparison.OrdinalIgnoreCase));

            if (permOverride is null)
                return false;

            // Remove from the cache
            permOverrides!.TryRemove(permOverride);

            if (permOverrides.Count is 0)
                _dbCache.PermissionOverrides.TryRemove(sid ?? default, out _);

            // Remove from the database
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            return await db.PermissionOverride.DeleteAsync(permOverride) is not 0;
        }

        /// <summary>
        /// Removes all permission overrides.
        /// </summary>
        /// <param name="sid">The Discord server ID, or <see langword="null"/> if the overrides are global.</param>
        /// <returns>The amount of overrides that got removed.</returns>
        public async Task<int> ClearPermissionOverridesAsync(ulong? sid)
        {
            if (!_dbCache.PermissionOverrides.TryRemove(sid ?? default, out var permOverrides))
                return 0;

            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            var result = await db.PermissionOverride.DeleteAsync(permOverrides);
            permOverrides.Clear();

            return result;
        }

        /// <summary>
        /// Gets all command permission overrides for the specified guild.
        /// </summary>
        /// <param name="sid">The Discord server ID, or <see langword="null"/> for all global overrides.</param>
        /// <returns>The command permission overrides.</returns>
        public IReadOnlyCollection<PermissionOverrideEntity> GetPermissionOverrides(ulong? sid)
        {
            _dbCache.PermissionOverrides.TryGetValue(sid ?? default, out var permOverrides);
            return permOverrides ?? new(1, 0);
        }
    }
}