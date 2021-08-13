using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Extensions
{
    public static class DiscordMemberExt
    {
        /// <summary>
        /// Safely sends a direct message to the specified user.
        /// </summary>
        /// <param name="member">This Discord member.</param>
        /// <param name="message">The message to be sent.</param>
        /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
        public static async Task<DiscordMessage> SendMessageSafelyAsync(this DiscordMember member, DiscordMessageBuilder message)
            => await SendMessageSafelyAsync(member, message.Content, message.Embed);

        /// <summary>
        /// Safely sends a direct message to the specified user.
        /// </summary>
        /// <param name="member">This Discord member.</param>
        /// <param name="embed">The message's embed.</param>
        /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
        public static async Task<DiscordMessage> SendMessageSafelyAsync(this DiscordMember member, DiscordEmbed embed)
            => await SendMessageSafelyAsync(member, null, embed);

        /// <summary>
        /// Safely sends a direct message to the specified user.
        /// </summary>
        /// <param name="member">This Discord member.</param>
        /// <param name="content">The message's content.</param>
        /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
        public static async Task<DiscordMessage> SendMessageSafelyAsync(this DiscordMember member, string content)
            => await SendMessageSafelyAsync(member, content, null);

        /// <summary>
        /// Gets the time difference between the date the user joined the guild and the date their account was created.
        /// </summary>
        /// <param name="member">This Discord member.</param>
        /// <returns>The time interval between joining and creation.</returns>
        public static TimeSpan GetTimeDifference(this DiscordMember member)
            => member.JoinedAt.Subtract(member.CreationTimestamp);

        /// <summary>
        /// Safely sends a direct message to the specified user.
        /// </summary>
        /// <param name="member">This Discord member.</param>
        /// <param name="content">The message's content.</param>
        /// <param name="embed">The message's embed.</param>
        /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
        public static async Task<DiscordMessage> SendMessageSafelyAsync(this DiscordMember member, string content, DiscordEmbed embed)
        {
            if (content is null && embed is null)
                return null;

            try
            {
                return (embed is null)
                    ? await member.SendMessageAsync(content)
                    : await member.SendMessageAsync(content, embed);
            }
            catch
            {
                return null;
            }
        }
    }
}