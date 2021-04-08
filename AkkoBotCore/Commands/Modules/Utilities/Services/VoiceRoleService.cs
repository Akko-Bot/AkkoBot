using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.Entities;
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
        private readonly IServiceProvider _services;

        public VoiceRoleService(IServiceProvider services)
            => _services = services;

        /// <summary>
        /// Adds a voice role to the database.
        /// </summary>
        /// <param name="server">The Discord guild the role is from.</param>
        /// <param name="channel">The channel to be associated with a role.</param>
        /// <param name="role">The role to be assigned/removed on channel connect/disconnect.</param>
        /// <returns><see langword="true"/> if the role got added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddVoiceRoleAsync(DiscordGuild server, DiscordChannel channel, DiscordRole role)
        {
            if (channel.Type is not ChannelType.Voice)
                return false;

            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var guildSettings = await db.GuildConfig.GetGuildWithVoiceRolesAsync(server.Id);
            
            if (guildSettings.VoiceRolesRel.Any(x => x.ChannelId == channel.Id && x.RoleId == role.Id))
                return false;

            var newEntry = new VoiceRoleEntity()
            {
                GuildIdFk = server.Id,
                ChannelId = channel.Id,
                RoleId = role.Id
            };

            guildSettings.VoiceRolesRel.Add(newEntry);
            db.GuildConfig.Update(guildSettings);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Removes all voice roles that meet the criteria of the <paramref name="predicate"/>.
        /// </summary>
        /// <param name="server">The Discord guild the role is from.</param>
        /// <param name="predicate">A predicate that defines the roles elegible for removal.</param>
        /// <returns><see langword="true"/> if at least one voice role was removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveVoiceRoleAsync(DiscordGuild server, Predicate<VoiceRoleEntity> predicate)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var guildSettings = await db.GuildConfig.GetGuildWithVoiceRolesAsync(server.Id);
            guildSettings.VoiceRolesRel.RemoveAll(x => predicate(x));
            db.GuildConfig.Update(guildSettings);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Gets all voice roles from the specified Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild the roles are from.</param>
        /// <returns>A collection of voice roles.</returns>
        public async Task<List<VoiceRoleEntity>> GetVoiceRolesAsync(DiscordGuild server)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            var guildSettings = await db.GuildConfig.GetGuildWithVoiceRolesAsync(server.Id);
            var vcroles = guildSettings.VoiceRolesRel;

            for (var counter = 0; counter < vcroles.Count; counter++)
            {
                if (!server.Channels.TryGetValue(vcroles[counter].ChannelId, out _) || !server.Roles.TryGetValue(vcroles[counter].RoleId, out _))
                    vcroles.Remove(vcroles[counter]);
            }

            db.GuildConfig.Update(guildSettings);
            await db.SaveChangesAsync();

            return vcroles;
        }
    }
}
