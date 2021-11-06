using AkkoCore.Enums;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Queries;
using AkkoCore.Services.Events.Abstractions;
using ConcurrentCollections;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events
{
    /// <summary>
    /// Handles role assignment and removal on voice channel connections and disconnections.
    /// </summary>
    internal sealed class VoiceRoleConnectionHandler : IVoiceRoleConnectionHandler
    {
        /// <summary>
        /// Caches the cancellation tokens for the most recent voice connections.
        /// </summary>
        /// <remarks>The first <see langword="ulong"/> is the ID of the Discord guild, the second is the ID of the Discord user.</remarks>
        private readonly ConcurrentDictionary<(ulong, ulong), CancellationTokenSource> _recentConnections = new();

        private readonly TimeSpan _waitTime = TimeSpan.FromSeconds(1.5);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;

        public VoiceRoleConnectionHandler(IServiceScopeFactory scopeFactory, IDbCache dbCache)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Assigns or revokes a role upon voice channel connection/disconnection
        /// </summary>
        public Task VoiceRoleAsync(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
        {
            // Check for role hierarchy, not just role perm
            if (eventArgs.Before == eventArgs.After
                || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasFlag(Permissions.ManageRoles))
                || !_dbCache.VoiceRoles.TryGetValue(eventArgs.Guild.Id, out var voiceRoles))
                return Task.CompletedTask;

            if (_recentConnections.TryRemove((eventArgs.Guild.Id, eventArgs.User.Id), out var tokenSource))
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
            }

            tokenSource = new CancellationTokenSource();

            _recentConnections.TryAdd((eventArgs.Guild.Id, eventArgs.User.Id), tokenSource);

            _ = AssignVoiceRoleAsync(voiceRoles, eventArgs, tokenSource.Token);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Waits for some time, then assigns the appropriate voice role to the user.
        /// </summary>
        /// <param name="voiceRoles">The cached voice roles.</param>
        /// <param name="eventArgs">The voice state event.</param>
        /// <param name="cToken">The cancellation token.</param>
        /// <remarks>If the cancellation token is cancelled within the waiting time, no action is taken.</remarks>
        /// <exception cref="TaskCanceledException">Occurs when the cancellation token is cancelled.</exception>
        private async Task AssignVoiceRoleAsync(ConcurrentHashSet<VoiceRoleEntity> voiceRoles, VoiceStateUpdateEventArgs eventArgs, CancellationToken cToken)
        {
            await Task.Delay(_waitTime, cToken).ConfigureAwait(false);

            var toRemove = eventArgs.GetVoiceState() switch
            {
                UserVoiceState.Connected => await AddRolesOnConnectAsync(voiceRoles, eventArgs),
                UserVoiceState.Disconnected => await RemoveRolesOnDisconnectAsync(voiceRoles, eventArgs),
                UserVoiceState.Moved => await UpdateRolesOnMoveAsync(voiceRoles, eventArgs),
                _ => Array.Empty<VoiceRoleEntity>()
            };

            // Remove the voice role if it has been deleted.
            if (toRemove.Count is not 0)
            {
                using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);
                await db.VoiceRoles.DeleteAsync(toRemove, CancellationToken.None);
            }
			
            foreach (var role in toRemove)
                voiceRoles.TryRemove(role);

            // Remove and dispose the cancellation token.
            if (_recentConnections.TryRemove((eventArgs.Guild.Id, eventArgs.User.Id), out var tokenSource))
                tokenSource.Dispose();
        }

        /// <summary>
        /// Adds roles on voice connection.
        /// </summary>
        /// <param name="voiceRoles">The roles to be added.</param>
        /// <param name="eventArgs">The event arguments.</param>
        /// <returns>A collection of roles that should be removed from the cache and database.</returns>
        private async Task<IReadOnlyList<VoiceRoleEntity>> AddRolesOnConnectAsync(ICollection<VoiceRoleEntity> voiceRoles, VoiceStateUpdateEventArgs eventArgs)
        {
            if (voiceRoles.Count is 0 || eventArgs.User is not DiscordMember user)
                return Array.Empty<VoiceRoleEntity>();

            var toRemove = new List<VoiceRoleEntity>(0);

            foreach (var voiceRole in voiceRoles.Where(x => x.ChannelId == eventArgs.Channel.Id))
            {
                if (eventArgs.Guild.Roles.TryGetValue(voiceRole.RoleId, out var role) && !user.Roles.Contains(role) && eventArgs.Guild.CurrentMember.Hierarchy > role.Position)
                    await user.GrantRoleAsync(role);
                else if (role is null)
                    toRemove.Add(voiceRole);
            }

            return toRemove;
        }

        /// <summary>
        /// Removes roles on voice disconnection.
        /// </summary>
        /// <param name="voiceRoles">The roles to be added.</param>
        /// <param name="eventArgs">The event arguments.</param>
        /// <returns>A collection of roles that should be removed from the cache and database.</returns>
        private async Task<IReadOnlyList<VoiceRoleEntity>> RemoveRolesOnDisconnectAsync(ICollection<VoiceRoleEntity> voiceRoles, VoiceStateUpdateEventArgs eventArgs)
        {
            if (voiceRoles.Count is 0 || eventArgs.User is not DiscordMember user)
                return Array.Empty<VoiceRoleEntity>();

            var toRemove = new List<VoiceRoleEntity>(0);

            foreach (var voiceRole in voiceRoles)
            {
                if (eventArgs.Guild.Roles.TryGetValue(voiceRole.RoleId, out var role) && user.Roles.Contains(role) && eventArgs.Guild.CurrentMember.Hierarchy > role.Position)
                    await user.RevokeRoleAsync(role);
                else if (role is null)
                    toRemove.Add(voiceRole);
            }

            return toRemove;
        }

        /// <summary>
        /// Updates roles on voice move.
        /// </summary>
        /// <param name="voiceRoles">The roles to be added.</param>
        /// <param name="eventArgs">The event arguments.</param>
        /// <returns>A collection of roles that should be removed from the cache and database.</returns>
        private async Task<IReadOnlyList<VoiceRoleEntity>> UpdateRolesOnMoveAsync(ICollection<VoiceRoleEntity> voiceRoles, VoiceStateUpdateEventArgs eventArgs)
        {
            if (voiceRoles.Count is 0 || eventArgs.User is not DiscordMember user)
                return Array.Empty<VoiceRoleEntity>();

            var toRemove = new List<VoiceRoleEntity>(0);

            foreach (var voiceRole in voiceRoles)
            {
                if (!eventArgs.Guild.Roles.TryGetValue(voiceRole.RoleId, out var role))
                    toRemove.Add(voiceRole);
                else if (voiceRole.ChannelId != user.VoiceState.Channel.Id && user.Roles.Contains(role) && eventArgs.Guild.CurrentMember.Hierarchy > role.Position)
                    await user.RevokeRoleAsync(role);
                else if (voiceRole.ChannelId == user.VoiceState.Channel.Id && !user.Roles.Contains(role) && eventArgs.Guild.CurrentMember.Hierarchy > role.Position)
                    await user.GrantRoleAsync(role);
            }

            return toRemove;
        }
    }
}