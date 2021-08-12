using AkkoCore.Services.Timers.Abstractions;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events
{
    /// <summary>
    /// Represents an object that handles the behavior the bot should have for
    /// specific Discord events during the bot's startup.
    /// </summary>
    public interface IStartupEventHandler
    {
        /// <summary>
        /// Caches all guilds with passive activity available to this client.
        /// </summary>
        Task CacheActiveGuildsAsync(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs);

        /// <summary>
        /// Executes startup commands.
        /// </summary>
        Task ExecuteStartupCommandsAsync(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs);

        /// <summary>
        /// Initializes the bot's playing statuses.
        /// </summary>
        Task InitializePlayingStatuses(DiscordClient client, ReadyEventArgs eventArgs);

        /// <summary>
        /// Initializes the timers managed by the <see cref="ITimerManager"/>.
        /// </summary>
        Task InitializeTimersAsync(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs);

        /// <summary>
        /// Initializes additional state the bot may require.
        /// </summary>
        Task LoadInitialStateAsync(DiscordClient client, ReadyEventArgs eventArgs);

        /// <summary>
        /// Saves new guilds to the database on startup and caches them.
        /// </summary>
        Task SaveNewGuildsAsync(DiscordClient client, GuildDownloadCompletedEventArgs eventArgs);

        /// <summary>
        /// Unregisters disabled commands from the command handler of the current client,
        /// then sets up the cache for disabled commands if it hasn't been already.
        /// </summary>
        public Task UnregisterCommandsAsync(DiscordClient client, ReadyEventArgs eventArgs);
    }
}