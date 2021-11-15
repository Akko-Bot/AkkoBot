using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Extensions;

public static class DiscordChannelExt
{
    /// <summary>
    /// Safely sends a message to the specified channel.
    /// </summary>
    /// <param name="channel">This Discord channel.</param>
    /// <param name="message">The message to be sent.</param>
    /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
    public static async Task<DiscordMessage?> SendMessageSafelyAsync(this DiscordChannel channel, DiscordMessageBuilder message)
        => await SendMessageSafelyAsync(channel, message.Content, message.Embed);

    /// <summary>
    /// Safely sends a message to the specified channel.
    /// </summary>
    /// <param name="channel">This Discord channel.</param>
    /// <param name="embed">The message's embed.</param>
    /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
    public static async Task<DiscordMessage?> SendMessageSafelyAsync(this DiscordChannel channel, DiscordEmbed embed)
        => await SendMessageSafelyAsync(channel, default, embed);

    /// <summary>
    /// Safely sends a message to the specified channel.
    /// </summary>
    /// <param name="channel">This Discord channel.</param>
    /// <param name="content">The message's content.</param>
    /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
    public static async Task<DiscordMessage?> SendMessageSafelyAsync(this DiscordChannel channel, string content)
        => await SendMessageSafelyAsync(channel, content, default);

    /// <summary>
    /// Safely sends a message to the specified channel.
    /// </summary>
    /// <param name="channel">This Discord channel.</param>
    /// <param name="content">The message's content.</param>
    /// <param name="embed">The message's embed.</param>
    /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
    public static async Task<DiscordMessage?> SendMessageSafelyAsync(this DiscordChannel channel, string? content, DiscordEmbed? embed)
    {
        if (content is null && embed is null)
            return default;

        try { return await channel.SendMessageAsync(content, embed); }
        catch { return default; }
    }

    /// <summary>
    /// Gets the most recent message in this channel.
    /// </summary>
    /// <param name="channel">This Discord channel.</param>
    /// <param name="client">The Discord client that contains the cached messages.</param>
    /// <returns>The last message from this channel, <see langword="null"/> if the channel has no message.</returns>
    /// <exception cref="UnauthorizedException">Occurs when the client has no permission to see the content of this Discord channel.</exception>
    /// <exception cref="NotFoundException">Occurs when the client has no access to this Discord channel or when the channel has no messages.</exception>
    /// <exception cref="BadRequestException">Occurs when this Discord channel has been deleted or does not exist.</exception>
    /// <exception cref="ServerErrorException">Occurs when Discord is going through internal errors.</exception>
    public static async Task<DiscordMessage?> GetLatestMessageAsync(this DiscordChannel channel, DiscordClient client)
    {
        var messageCache = client.GetMessageCache();
        var message = messageCache
            .Where(x => x?.Channel.Id == channel.Id)
            .MaxBy(x => x.CreationTimestamp);

        if (message is null && (await channel.GetMessagesAsync(1)).TryGetValue(0, out message))
            messageCache.Add(message!);  // It's fine to add null values to the message cache

        return message;
    }

    /// <summary>
    /// Returns a collection of messages from the last message in this channel.
    /// </summary>
    /// <param name="channel">This Discord channel.</param>
    /// <param name="client">The Discord client that contains the cached messages.</param>
    /// <param name="amount">The amount of messages to fetch.</param>
    /// <remarks>
    /// This method gets messages from the cache and only calls the API if they are enough to satisfy <paramref name="amount"/>.
    /// Messages that get deleted on Discord persist on the cache until they are overwritten by newer messages.
    /// </remarks>
    /// <returns>A collection of Discord messages, sorted from most recent to oldest.</returns>
    /// <exception cref="UnauthorizedException">Occurs when the client has no permission to see the content of this Discord channel.</exception>
    /// <exception cref="NotFoundException">Occurs when the client has no access to this Discord channel or when the channel has no messages.</exception>
    /// <exception cref="BadRequestException">Occurs when this Discord channel has been deleted or does not exist.</exception>
    /// <exception cref="ServerErrorException">Occurs when Discord is going through internal errors.</exception>
    public static async Task<IEnumerable<DiscordMessage>> GetMessagesAsync(this DiscordChannel channel, DiscordClient client, int amount)
    {
        var messages = client.GetMessageCache()
            .Where(x => x?.Channel.Id == channel.Id)
            .OrderByDescending(x => x.Id)
            .Take(amount);

        var messagesCount = messages.Count();

        if (messagesCount == amount)
            return messages;
        else if (messagesCount == 0)
            return await channel.GetMessagesAsync(amount);
        else
        {
            return messages.Concat(
                (await channel.GetMessagesBeforeAsync(messages.Last().Id, amount - messagesCount))
                    .OrderByDescending(x => x.Id)
            );
        }
    }
}