using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Extensions
{
    public static class DiscordEmojiExt
    {
        /// <summary>
        /// Converts an emoji to a guild emoji.
        /// </summary>
        /// <param name="emoji">This emoji.</param>
        /// <param name="server">The server the emoji is from.</param>
        /// <returns>The guild emoji, <see langword="null"/> if the emoji is not from the specified guild.</returns>
        public static async Task<DiscordGuildEmoji> ToGuildEmojiAsync(this DiscordEmoji emoji, DiscordGuild server)
        {
            if (server is null || !server.Emojis.ContainsKey(emoji.Id))
                return null;

            try { return await server.GetEmojiAsync(emoji.Id); }
            catch { return null; }
        }

        /// <summary>
        /// Converts a collection of emojis to a collection of guild emojis.
        /// </summary>
        /// <param name="emojis">This collection of emojis.</param>
        /// <param name="server">The server the emojis are from.</param>
        /// <returns>A collection of guild emojis, if they are found in the specified guild.</returns>
        public static async Task<IEnumerable<DiscordGuildEmoji>> ToGuildEmojisAsync(this IEnumerable<DiscordEmoji> emojis, DiscordGuild server)
        {
            if (server is null)
                return Enumerable.Empty<DiscordGuildEmoji>();

            try { return (await server.GetEmojisAsync()).Where(gEmoji => emojis.Any(emoji => emoji.Id == gEmoji.Id)); }
            catch { return Enumerable.Empty<DiscordGuildEmoji>(); }
        }
    }
}