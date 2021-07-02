using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events.Abstractions
{
    /// <summary>
    /// Represents the punishments a user can receive when they are detected as an alt.
    /// </summary>
    public interface IAntiAltActions
    {
        /// <summary>
        /// Bans a user.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user to be banned.</param>
        Task BanAltAsync(CommandContext context, DiscordMember user);

        /// <summary>
        /// Kicks a user
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user to be kicked.</param>
        Task KickAltAsync(CommandContext context, DiscordMember user);

        /// <summary>
        /// Permanently mutes a user.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user to be muted.</param>
        /// <param name="muteRoleId">The ID of the mute role.</param>
        Task MuteAltAsync(CommandContext context, DiscordMember user, ulong muteRoleId);

        /// <summary>
        /// Adds a role to a user.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user to add the role to.</param>
        /// <param name="roleId">The ID of the role to be added.</param>
        Task RoleAltAsync(CommandContext context, DiscordMember user, ulong roleId);
    }
}