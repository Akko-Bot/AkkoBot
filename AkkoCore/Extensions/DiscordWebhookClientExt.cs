using DSharpPlus;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoCore.Extensions
{
    public static class DiscordWebhookClientExt
    {
        /// <summary>
        /// Removes the specified webhook from this webhook client.
        /// </summary>
        /// <param name="client">The webhook client.</param>
        /// <param name="webhook">The webhook to be removed.</param>
        /// <returns><see langword="true"/> if the webhook was successfully removed, <see langword="false"/> otherwise.</returns>
        public static bool TryRemove(this DiscordWebhookClient client, DiscordWebhook webhook)
            => TryRemove(client, webhook.Id);

        /// <summary>
        /// Removes the webhook with the specified ID from this webhook client.
        /// </summary>
        /// <param name="client">The webhook client.</param>
        /// <param name="id">The ID of the webhook to be removed.</param>
        /// <returns><see langword="true"/> if the webhook was successfully removed, <see langword="false"/> otherwise.</returns>
        public static bool TryRemove(this DiscordWebhookClient client, ulong id)
        {
            if (client.GetRegisteredWebhook(id) is null)
                return false;

            client.RemoveWebhook(id);
            return true;
        }

        /// <summary>
        /// Adds the specified webhook to this webhook client.
        /// </summary>
        /// <param name="client">The webhook client.</param>
        /// <param name="webhook">The webhook to be added.</param>
        /// <returns><see langword="true"/> if the webhook was successfully added, <see langword="false"/> otherwise.</returns>
        public static bool TryAdd(this DiscordWebhookClient client, DiscordWebhook webhook)
        {
            if (client.GetRegisteredWebhook(webhook.Id) is not null)
                return false;

            client.AddWebhook(webhook);
            return true;
        }

        /// <summary>
        /// Adds the webhook with the specified ID and token.
        /// </summary>
        /// <param name="client">The webhook client.</param>
        /// <param name="id">The ID of the webhook to be added.</param>
        /// <param name="token">The token of the webhook to be added.</param>
        /// <returns><see langword="true"/> if the webhook was successfully added, <see langword="false"/> otherwise.</returns>
        public static async Task<bool> TryAddAsync(this DiscordWebhookClient client, ulong id, string token)
        {
            if (client.GetRegisteredWebhook(id) is not null)
                return false;

            await client.AddWebhookAsync(id, token).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Adds the webhook with the specified ID and the client that's retrieving it.
        /// </summary>
        /// <param name="client">The webhook client.</param>
        /// <param name="id">The ID of the webhook to be added.</param>
        /// <param name="baseClient">The client to retrieve the webhook.</param>
        /// <returns><see langword="true"/> if the webhook was successfully added, <see langword="false"/> otherwise.</returns>
        public static async Task<bool> TryAddAsync(this DiscordWebhookClient client, ulong id, BaseDiscordClient baseClient)
        {
            if (client.GetRegisteredWebhook(id) is not null)
                return false;

            await client.AddWebhookAsync(id, baseClient).ConfigureAwait(false);
            return true;
        }
    }
}