using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace AkkoCore.Extensions;

public static class DiscordClientExt
{
    private static readonly Permissions _basePermissions = Permissions.AccessChannels | Permissions.SendMessages | Permissions.AddReactions | Permissions.SendMessagesInThreads;

    private static readonly PropertyInfo _messageCacheProp = typeof(DiscordClient).GetProperty("MessageCache", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException($"Could not find property \"MessageCache\" in the type \"{typeof(DiscordClient).Name}\".");

    /// <summary>
    /// Gets the URL to invite this bot to a Discord server.
    /// </summary>
    /// <param name="client">This Discord client.</param>
    /// <returns>The URL for inviting the bot.</returns>
    public static string GetBotInvite(this DiscordClient client)
        => $"https://discord.com/api/oauth2/authorize?client_id={client.CurrentUser.Id}&permissions={(long)_basePermissions}&scope=applications.commands%20bot";

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
        => (RingBuffer<DiscordMessage>)_messageCacheProp.GetValue(client)!;

    /// <summary>
    /// Safely gets the the member with the specified ID.
    /// </summary>
    /// <param name="client">This Discord client.</param>
    /// <param name="uid">The Discord user ID.</param>
    /// <returns>The member with the specified ID, <see langword="null"/> if the user does not exist.</returns>
    public static async Task<DiscordUser?> GetUserSafelyAsync(this DiscordClient client, ulong uid)
    {
        try { return await client.GetUserAsync(uid); }
        catch { return default; }
    }

    /// <summary>
    /// Safely gets the webhook with the specified ID.
    /// </summary>
    /// <param name="client">This Discord client.</param>
    /// <param name="id">The webhook ID.</param>
    /// <returns>The webhook with the specified ID, <see langword="null"/> if the webhook does not exist.</returns>
    public static async Task<DiscordWebhook?> GetWebhookSafelyAsync(this DiscordClient client, ulong id)
    {
        try { return await client.GetWebhookAsync(id); }
        catch { return default; }
    }
}