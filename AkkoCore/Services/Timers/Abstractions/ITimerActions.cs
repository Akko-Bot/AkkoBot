using DSharpPlus;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoCore.Services.Timers.Abstractions
{
    /// <summary>
    /// Represents an object that encapsulates deferred actions to be performed at an arbitrary point in time.
    /// </summary>
    public interface ITimerActions
    {
        /// <summary>
        /// Adds a role to a Discord user.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="server">The Discord server.</param>
        /// <param name="userId">The ID of the user to be unmuted.</param>
        Task AddPunishRoleAsync(int entryId, DiscordGuild server, ulong userId);

        /// <summary>
        /// Executes a command in the context stored in the database.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="client">The Discord client that created the autocommand.</param>
        /// <param name="server">The Discord server.</param>
        Task ExecuteCommandAsync(int entryId, DiscordClient client, DiscordGuild server);

        /// <summary>
        /// Removes old warnings from the specified user.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="server">The Discord server.</param>
        /// <param name="userId">The ID of the user to be unmuted.</param>
        Task RemoveOldWarningAsync(int entryId, DiscordGuild server, ulong userId);

        /// <summary>
        /// Removes a role from a Discord user.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="server">The Discord server.</param>
        /// <param name="userId">The ID of the user to be unmuted.</param>
        Task RemovePunishRoleAsync(int entryId, DiscordGuild server, ulong userId);

        /// <summary>
        /// Sends a reminder to the channel specified in the database.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="client">The Discord client that created the reminder.</param>
        /// <param name="server">The Discord server.</param>
        Task SendReminderAsync(int entryId, DiscordClient client, DiscordGuild server);

        /// <summary>
        /// Sends a repeater message to the channel specified in the database.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="client">The Discord client that created the autocommand.</param>
        /// <param name="server">The Discord server.</param>
        Task SendRepeaterAsync(int entryId, DiscordClient client, DiscordGuild server);

        /// <summary>
        /// Unbans a user from a Discord server.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="server">The Discord server to unban from.</param>
        /// <param name="userId">The ID of the user to be unbanned.</param>
        Task UnbanAsync(int entryId, DiscordGuild server, ulong userId);

        /// <summary>
        /// Unmutes a user on a Discord server.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="server">The Discord server.</param>
        /// <param name="userId">The ID of the user to be unmuted.</param>
        Task UnmuteAsync(int entryId, DiscordGuild server, ulong userId);
    }
}