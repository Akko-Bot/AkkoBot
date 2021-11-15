using AkkoCore.Services.Database.Enums;
using System;
using System.Linq;
using System.Text;

namespace AkkoCore.Extensions;

public static class TagBehaviorExt
{
    /// <summary>
    /// Gets the emoji string that represents the value of this tag behavior.
    /// </summary>
    /// <param name="behavior">This behavior.</param>
    /// <returns>The emoji string of this value.</returns>
    public static string ToEmojiString(this TagBehavior behavior)
    {
        var result = new StringBuilder();
        var allValues = Enum.GetValues<TagBehavior>()
            .Where(x => behavior.HasFlag(x));

        foreach (var value in allValues)
        {
            result = value switch
            {
                TagBehavior.Delete => result.Append("🗑️"),
                TagBehavior.Anywhere => result.Append("🗯️"),
                TagBehavior.DirectMessage => result.Append("📧"),
                TagBehavior.SanitizeRolePing => result.Append("🔕"),
                _ => result
            };
        }

        return result.ToString();
    }
}