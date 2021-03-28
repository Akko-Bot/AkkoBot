using System.Linq;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AkkoBot.Extensions
{
    public static class DiscordEmojiExt
    {
        public static async Task<DiscordGuildEmoji> ToGuildEmojiAsync(this DiscordEmoji emoji, DiscordGuild server)
        {
            if (server is null || !server.Emojis.ContainsKey(emoji.Id))
                return null;

            try { return await server.GetEmojiAsync(emoji.Id); }
            catch { return null; }
        }

        public static async Task<IEnumerable<DiscordGuildEmoji>> ToGuildEmojisAsync(this IEnumerable<DiscordEmoji> emojis, DiscordGuild server)
        {
            if (server is null)
                return Enumerable.Empty<DiscordGuildEmoji>();

            try { return (await server.GetEmojisAsync()).Where(gEmoji => emojis.Any(emoji => emoji.Id == gEmoji.Id)); }
            catch { return Enumerable.Empty<DiscordGuildEmoji>(); }
        }
    }
}