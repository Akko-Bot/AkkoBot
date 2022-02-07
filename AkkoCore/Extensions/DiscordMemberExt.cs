using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Extensions;

public static class DiscordMemberExt
{
    /// <summary>
    /// Times-out a member and restricts their ability to send messages, add reactions, speak in threads, and join voice channels.
    /// </summary>
    /// <param name="member">This Discord member.</param>
    /// <param name="time">For how long the user should be timed out.</param>
    /// <param name="reason">The reason for the punishment.</param>
    public static Task TimeoutAsync(this DiscordMember member, TimeSpan time, string? reason = default)
        => member.TimeoutAsync(DateTimeOffset.Now.Add(time), reason);

    /// <summary>
    /// Safely sends a direct message to the specified user.
    /// </summary>
    /// <param name="member">This Discord member.</param>
    /// <param name="message">The message to be sent.</param>
    /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
    public static async Task<DiscordMessage?> SendMessageSafelyAsync(this DiscordMember member, DiscordMessageBuilder message)
        => await SendMessageSafelyAsync(member, message.Content, message.Embed);

    /// <summary>
    /// Safely sends a direct message to the specified user.
    /// </summary>
    /// <param name="member">This Discord member.</param>
    /// <param name="embed">The message's embed.</param>
    /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
    public static async Task<DiscordMessage?> SendMessageSafelyAsync(this DiscordMember member, DiscordEmbed embed)
        => await SendMessageSafelyAsync(member, null, embed);

    /// <summary>
    /// Safely sends a direct message to the specified user.
    /// </summary>
    /// <param name="member">This Discord member.</param>
    /// <param name="content">The message's content.</param>
    /// <returns>The message that was sent, <see langword="null"/> if the message could not be sent.</returns>
    public static async Task<DiscordMessage?> SendMessageSafelyAsync(this DiscordMember member, string content)
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
    public static async Task<DiscordMessage?> SendMessageSafelyAsync(this DiscordMember member, string? content, DiscordEmbed? embed)
    {
        if (content is null && embed is null)
            return default;

        try
        {
            return (embed is null)
                ? await member.SendMessageAsync(content)
                : await member.SendMessageAsync(content, embed);
        }
        catch
        {
            return default;
        }
    }
}