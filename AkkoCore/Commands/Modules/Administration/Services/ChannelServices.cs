using AkkoCore.Commands.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="DiscordChannel"/> objects.
    /// </summary>
    public sealed class ChannelService : ICommandService
    {
        /// <summary>
        /// Sets the channel overrides for the mute role on all text channels visible to the bot.
        /// </summary>
        /// <param name="server">The Discord server.</param>
        /// <param name="muteRole">The mute role.</param>
        /// <param name="reason">The reason for the mute.</param>
        /// <param name="textOnly"><see langword="true"/> to only apply the overwrites to text channels, <see langword="false"/> to apply to all channels.</param>
        public async Task SetMuteOverwritesAsync(DiscordGuild server, DiscordRole muteRole, string reason, bool textOnly)
        {
            var denyPerms = (textOnly) ? RoleService.MuteTextPermsDeny : RoleService.MutePermsDeny;

            // Process the category channels first
            foreach (var channel in server.Channels.Values.Where(x => x.Users.Contains(server.CurrentMember) && x.Type is ChannelType.Category))
            {
                if (!channel.PermissionOverwrites.Any(x => x.Id == muteRole.Id && x.Denied == denyPerms))
                    await channel.AddOverwriteAsync(muteRole, Permissions.None, denyPerms, reason);
            }

            // Process everything else
            foreach (var channel in server.Channels.Values.Where(x => x.Users.Contains(server.CurrentMember) && x.Type is not ChannelType.Category))
            {
                if (!channel.PermissionOverwrites.Any(x => x.Id == muteRole.Id && x.Denied == denyPerms))
                    await channel.AddOverwriteAsync(muteRole, Permissions.None, denyPerms, reason);
            }
        }

        /// <summary>
        /// Sets the channel overrides for the muted user on all text channels visible to the bot.
        /// </summary>
        /// <param name="server">The Discord server.</param>
        /// <param name="user">The muted user.</param>
        /// <param name="reason">The reason for the mute.</param>
        /// <param name="textOnly"><see langword="true"/> to only apply the overwrites to text channels, <see langword="false"/> to apply to all channels.</param>
        public async Task SetMuteOverwritesAsync(DiscordGuild server, DiscordMember user, string reason, bool textOnly)
        {
            var denyPerms = (textOnly) ? RoleService.MuteTextPermsDeny : RoleService.MutePermsDeny;

            // Process the category channels first
            foreach (var channel in server.Channels.Values.Where(x => x.Users.Contains(server.CurrentMember) && x.Type is ChannelType.Category))
            {
                if (!channel.PermissionOverwrites.Any(x => x.Id == user.Id && x.Denied == denyPerms))
                    await channel.AddOverwriteAsync(user, Permissions.None, denyPerms, reason);
            }

            // Process everything else
            foreach (var channel in server.Channels.Values.Where(x => x.Users.Contains(server.CurrentMember) && x.Type is not ChannelType.Category))
            {
                if (!channel.PermissionOverwrites.Any(x => x.Id == user.Id && x.Denied == denyPerms) && (!textOnly || channel.Type is not ChannelType.Voice))
                    await channel.AddOverwriteAsync(user, Permissions.None, denyPerms, reason);
            }
        }

        /// <summary>
        /// Removes the channel overwrites that match <paramref name="predicate"/> from all Discord channels visible to the bot.
        /// </summary>
        /// <param name="server">The Discord server.</param>
        /// <param name="predicate">A method that defines what overwrites should be removed.</param>
        /// <param name="reason">The reason for the mute.</param>
        public async Task RemoveOverwritesAsync(DiscordGuild server, string reason, Func<DiscordOverwrite, bool> predicate)
        {
            var overwrites = server.Channels.Values
                .Where(x => x.Users.Contains(server.CurrentMember))
                .SelectMany(x => x.PermissionOverwrites.Where(predicate))
                .ToArray();

            for (var index = 0; index < overwrites.Length; index++)
                await overwrites[index].DeleteAsync(reason);
        }
    }
}