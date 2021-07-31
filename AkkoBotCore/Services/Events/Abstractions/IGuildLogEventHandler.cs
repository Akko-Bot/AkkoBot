using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events.Abstractions
{
    /// <summary>
    /// Represents an object that handles guild log events.
    /// </summary>
    public interface IGuildLogEventHandler
    {
        /// <summary>
        /// Caches the created message if the guild is logging message deletes.
        /// </summary>
        Task CacheMessageOnCreationAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

        /// <summary>
        /// Logs a deleted message.
        /// </summary>
        Task LogDeletedMessageAsync(DiscordClient client, MessageDeleteEventArgs eventArgs);
    }
}