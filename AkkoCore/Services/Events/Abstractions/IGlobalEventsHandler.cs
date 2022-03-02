using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Abstractions;

/// <summary>
/// Represents an object that handles events that are not specific to a Discord guild.
/// </summary>
public interface IGlobalEventsHandler
{
    /// <summary>
    /// Counts the amount of messages that have been sent since startup.
    /// </summary>
    uint MessageCount { get; }

    /// <summary>
    /// Increases the <see cref="MessageCount"/> counter when a message is received.
    /// </summary>
    Task CountMessageAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

    /// <summary>
    /// Stops the callback chain if the message comes from a blacklisted context.
    /// </summary>
    Task BlockBlacklistedAsync(DiscordClient client, MessageCreateEventArgs eventArgs);
}