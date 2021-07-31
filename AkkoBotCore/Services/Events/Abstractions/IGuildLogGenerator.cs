using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace AkkoBot.Services.Events.Abstractions
{
    /// <summary>
    /// Represents an object that generates guild log messages.
    /// </summary>
    public interface IGuildLogGenerator
    {
        /// <summary>
        /// Generates a log message for a <see cref="MessageDeleteEventArgs"/> event.
        /// </summary>
        /// <param name="message">The message that got deleted.</param>
        /// <returns>The guild log message.</returns>
        DiscordWebhookBuilder GetDeleteLog(DiscordMessage message);
    }
}