using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;

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
        /// <exception cref="ArgumentException">Occurs when <paramref name="message"/> is <see langword="null"/>.</exception>
        DiscordWebhookBuilder GetMessageDeleteLog(DiscordMessage message);

        /// <summary>
        /// Generates a log message for a <see cref="MessageUpdateEventArgs"/> event.
        /// </summary>
        /// <param name="eventArgs">The event argument.</param>
        /// <returns>The guild log message.</returns>
        /// <exception cref="ArgumentException">Occurs when <see cref="MessageUpdateEventArgs.MessageBefore"/> is <see langword="null"/>.</exception>
        DiscordWebhookBuilder GetMessageUpdateLog(MessageUpdateEventArgs eventArgs);
    }
}