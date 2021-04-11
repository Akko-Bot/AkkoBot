using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Extensions
{
    public static class DiscordChannelExt
    {
        /// <summary>
        /// Safely sends a message to the specified channel.
        /// </summary>
        /// <param name="channel">This Discord channel.</param>
        /// <param name="message">The message to be sent.</param>
        /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
        public static async Task<DiscordMessage> SendMessageSafelyAsync(this DiscordChannel channel, DiscordMessageBuilder message)
            => await SendMessageSafelyAsync(channel, message.Content, message.Embed);

        /// <summary>
        /// Safely sends a message to the specified channel.
        /// </summary>
        /// <param name="channel">This Discord channel.</param>
        /// <param name="embed">The message's embed.</param>
        /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
        public static async Task<DiscordMessage> SendMessageSafelyAsync(this DiscordChannel channel, DiscordEmbed embed)
            => await SendMessageSafelyAsync(channel, null, embed);

        /// <summary>
        /// Safely sends a message to the specified channel.
        /// </summary>
        /// <param name="channel">This Discord channel.</param>
        /// <param name="content">The message's content.</param>
        /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
        public static async Task<DiscordMessage> SendMessageSafelyAsync(this DiscordChannel channel, string content)
            => await SendMessageSafelyAsync(channel, content, null);

        /// <summary>
        /// Safely sends a message to the specified channel.
        /// </summary>
        /// <param name="channel">This Discord channel.</param>
        /// <param name="content">The message's content.</param>
        /// <param name="embed">The message's embed.</param>
        /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
        public static async Task<DiscordMessage> SendMessageSafelyAsync(this DiscordChannel channel, string content, DiscordEmbed embed)
        {
            if (content is null && embed is null)
                return null;

            try { return await channel.SendMessageAsync(content, embed); }
            catch { return null; }
        }
    }
}