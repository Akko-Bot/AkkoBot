using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Abstractions
{
    /// <summary>
    /// Represents an object that handles automated moderation features.
    /// </summary>
    public interface IGatekeepEventHandler : IDisposable
    {
        /// <summary>
        /// Applies a punishment to a user if they are deemed to be an alt.
        /// </summary>
        Task PunishAltAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs);

        /// <summary>
        /// Sanitizes the user's name when they join a guild.
        /// </summary>
        Task SanitizeNameOnJoinAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs);

        /// <summary>
        /// Sanitizes the user's name or nickname when they update themselves.
        /// </summary>
        Task SanitizeNameOnUpdateAsync(DiscordClient client, GuildMemberUpdateEventArgs eventArgs);

        /// <summary>
        /// Sends a farewell message to the user when they leave a guild.
        /// </summary>
        Task SendFarewellMessageAsync(DiscordClient client, GuildMemberRemoveEventArgs eventArgs);

        /// <summary>
        /// Sends a greet message to the user in direct message when they join a guild.
        /// </summary>
        Task SendGreetDmMessageAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs);

        /// <summary>
        /// Sends a greet message to the user when they join a guild.
        /// </summary>
        Task SendGreetMessageAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs);
    }
}