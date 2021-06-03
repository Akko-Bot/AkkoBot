using DSharpPlus;
using DSharpPlus.Entities;
using System.Reflection;
using System.Threading.Tasks;

namespace AkkoBot.Extensions
{
    public static class DiscordClientExt
    {
        private static readonly PropertyInfo _messageCacheProp = typeof(DiscordClient).GetProperty("MessageCache", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Gets the message cache of this client.
        /// </summary>
        /// <param name="client">This Discord client.</param>
        /// <remarks>
        /// The cache has fixed size and indices that were not yet
        /// initialized will be set to <see langword="null"/>.
        /// </remarks>
        /// <returns>The cached Discord messages.</returns>
        public static RingBuffer<DiscordMessage> GetMessageCache(this DiscordClient client)
            => _messageCacheProp.GetValue(client) as RingBuffer<DiscordMessage>;

        /// <summary>
        /// Safely gets the the member with the specified ID.
        /// </summary>
        /// <param name="client">This Discord client.</param>
        /// <param name="uid">The Discord user ID.</param>
        /// <returns>The member with the specified ID, <see langword="null"/> if the user does not exist.</returns>
        public static async Task<DiscordUser> GetUserSafelyAsync(this DiscordClient client, ulong uid)
        {
            try { return await client.GetUserAsync(uid); }
            catch { return null; }
        }
    }
}