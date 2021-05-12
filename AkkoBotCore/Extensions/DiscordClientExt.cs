using DSharpPlus;
using DSharpPlus.Entities;
using System.Reflection;

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
    }
}