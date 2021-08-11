using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Database.Queries;
using AkkoDatabase;
using AkkoDatabase.Entities;
using AkkoCore.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="VoiceRoleEntity"/> objects.
    /// </summary>
    public class VoiceRoleService : ICommandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;

        public VoiceRoleService(IServiceScopeFactory scopeFactory, IDbCache dbCache)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Adds a voice role to the database.
        /// </summary>
        /// <param name="server">The Discord guild the role is from.</param>
        /// <param name="channel">The channel to be associated with a role.</param>
        /// <param name="role">The role to be assigned/removed on channel connect/disconnect.</param>
        /// <remarks>Limited to 3 voice roles per channel.</remarks>
        /// <returns><see langword="true"/> if the role got added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddVoiceRoleAsync(DiscordGuild server, DiscordChannel channel, DiscordRole role)
        {
            if (channel.Type is not ChannelType.Voice
                || _dbCache.VoiceRoles.TryGetValue(server.Id, out var voiceRoles)
                || (voiceRoles?.Count(x => x.GuildIdFk == server.Id && x.ChannelId == channel.Id) >= 3
                && voiceRoles.Select(x => x.ChannelId).Contains(channel.Id)
                && voiceRoles.Select(x => x.RoleId).Contains(role.Id)))
                return false;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            // Add to the database
            var newEntry = new VoiceRoleEntity()
            {
                GuildIdFk = server.Id,
                ChannelId = channel.Id,
                RoleId = role.Id
            };

            db.Add(newEntry);
            var result = await db.SaveChangesAsync() is not 0;

            // Update the cache
            if (!_dbCache.VoiceRoles.ContainsKey(server.Id))
                _dbCache.VoiceRoles.TryAdd(server.Id, new());

            _dbCache.VoiceRoles[server.Id].Add(newEntry);

            return result;
        }

        /// <summary>
        /// Removes all voice roles that meet the criteria of the <paramref name="predicate"/>.
        /// </summary>
        /// <param name="server">The Discord guild the role is from.</param>
        /// <param name="predicate">A predicate that defines the roles elegible for removal.</param>
        /// <returns><see langword="true"/> if at least one voice role was removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveVoiceRoleAsync(DiscordGuild server, Func<VoiceRoleEntity, bool> predicate)
        {
            if (!_dbCache.VoiceRoles.TryGetValue(server.Id, out var voiceRoles))
                return false;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            // Remove from the cache
            var matches = voiceRoles
                .Where(predicate)
                .ToArray();

            foreach (var voiceRole in matches)
                voiceRoles.TryRemove(voiceRole);

            if (voiceRoles.Count is 0)
                _dbCache.VoiceRoles.TryRemove(server.Id, out _);

            // Remove from the database
            return matches.Length is not 0 && await db.VoiceRoles.DeleteAsync(matches) is not 0;
        }

        /// <summary>
        /// Gets all voice roles from the specified Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild the roles are from.</param>
        /// <returns>A collection of voice roles.</returns>
        public async Task<IReadOnlyCollection<VoiceRoleEntity>> GetVoiceRolesAsync(DiscordGuild server)
        {
            _dbCache.VoiceRoles.TryGetValue(server.Id, out var voiceRoles);

            await RemoveVoiceRoleAsync(
                server,
                x => !server.Channels.Values.Select(y => y.Id).ContainsOne(voiceRoles.Select(y => y.ChannelId))
                || !server.Roles.Values.Select(y => y.Id).ContainsOne(voiceRoles.Select(y => y.RoleId))
            );

            return voiceRoles ?? new();
        }
    }
}