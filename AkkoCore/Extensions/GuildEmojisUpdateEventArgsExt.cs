using AkkoCore.Enums;
using DSharpPlus.EventArgs;

namespace AkkoCore.Extensions;

public static class GuildEmojisUpdateEventArgsExt
{
    /// <summary>
    /// Gets the action that was performed on the emoji.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <returns>The type of action.</returns>
    public static EmojiActivity GetStatus(this GuildEmojisUpdateEventArgs eventArgs)
    {
        return (eventArgs.EmojisBefore.Count - eventArgs.EmojisAfter.Count) switch
        {
            0 => EmojiActivity.Updated,
            < 0 => EmojiActivity.Created,
            > 0 => EmojiActivity.Deleted
        };
    }
}
