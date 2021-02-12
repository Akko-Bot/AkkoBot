using AkkoBot.Command.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Command.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="DiscordChannel"/> objects.
    /// </summary>
    public class ChannelService : ICommandService
    {
        /// <summary>
        /// Sets the channel overrides for the mute role on all channels visible to the bot.
        /// </summary>
        /// <param name="server">The Discord server.</param>
        /// <param name="muteRole">The mute role.</param>
        public async Task SetMuteOverwritesAsync(DiscordGuild server, DiscordRole muteRole)
        {
            foreach (var channel in server.Channels.Values.Where(x => x.Users.Contains(server.CurrentMember)))
                await channel.AddOverwriteAsync(muteRole, Permissions.None, RoleService.MutePermsDeny);
        }

        /// <summary>
        /// Sets the channel overrides for the muted user on all channels visible to the bot.
        /// </summary>
        /// <param name="server">The Discord server.</param>
        /// <param name="user">The muted user.</param>
        public async Task SetMuteOverwritesAsync(DiscordGuild server, DiscordMember user)
        {
            foreach (var channel in server.Channels.Values.Where(x => x.Users.Contains(server.CurrentMember)))
                await channel.AddOverwriteAsync(user, Permissions.None, RoleService.MutePermsDeny);
        }

        /// <summary>
        /// Removes the channel overwrites that match <paramref name="selector"/> from all Discord channels visible to the bot.
        /// </summary>
        /// <param name="server">The Discord server.</param>
        /// <param name="selector">A method that defines what overwrites should be removed.</param>
        public async Task RemoveOverwritesAsync(DiscordGuild server, Func<DiscordOverwrite, bool> selector)
        {
            var overwrites = server.Channels.Values
                .Where(x => x.Users.Contains(server.CurrentMember))
                .SelectMany(x => x.PermissionOverwrites.Where(selector));

            foreach (var overwrite in overwrites)
                await overwrite.DeleteAsync();
        }
    }
}