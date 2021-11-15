using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Abstractions;

/// <summary>
/// Represents an object that handles execution of global and guild tags.
/// </summary>
public interface ITagEventHandler
{
    /// <summary>
    /// Executes global react tags.
    /// </summary>
    Task ExecuteGlobalEmojiTagAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

    /// <summary>
    /// Executes global tags.
    /// </summary>
    Task ExecuteGlobalTagAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

    /// <summary>
    /// Executes guild react tags.
    /// </summary>
    Task ExecuteGuildEmojiTagAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

    /// <summary>
    /// Executes guild tags.
    /// </summary>
    Task ExecuteGuildTagAsync(DiscordClient client, MessageCreateEventArgs eventArgs);
}