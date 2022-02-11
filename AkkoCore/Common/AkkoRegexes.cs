using System.Text.RegularExpressions;

namespace AkkoCore.Common;

/// <summary>
/// Groups regexes that are used across the entire project.
/// </summary>
public static class AkkoRegexes
{
    /// <summary>
    /// Regex to get the locale of the response files. Locale must be between "_" and ".yaml"
    /// </summary>
    /// <remarks>Example: "FileName_en-US.yaml" -> "_en-US" (en-US)</remarks>
    public static Regex ResponseFileLocale { get; } = new(
        @"_([\w-][^_]+(?=\.(?:yaml|yml)$))",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Regex to match any hyperlink valid on Discord.
    /// </summary>
    /// <remarks>Example: "Google is at https://www.google.com" -> "https://www.google.com"</remarks>
    public static Regex Url { get; } = new(
        @"https?:\/\/\S{2,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Regex to match any image link that gets automatically rendered by the Discord client.
    /// </summary>
    /// <remarks>Example: "My spicy nude https://sexy.com/spicy.png" -> "https://sexy.com/spicy.png" (png)</remarks>
    public static Regex ImageUrl { get; } = new(
        @"https?:\/\/[^.].+(png|jpg|jpeg|gif)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Regex to match any valid Discord server invite.
    /// </summary>
    /// <remarks>Example: "My super cool server https://discord.gg/l33tH4X0R" -> "discord.gg/l33tH4X0R" (l33tH4X0R)</remarks>
    public static Regex Invite { get; } = new(
        @"discord(?:\.gg|\.io|\.me|\.li|(?:app)?\.com\/invite)\/(\w+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Regex to get Discord emojis.
    /// </summary>
    /// <remarks>Example: "This ＜:emojiName:12345＞ is an emoji" -> "＜:emojiName:12345＞" (emojiName) (12345)</remarks>
    public static Regex Emoji { get; } = new(
        @"<a?:(\S+?):(\d+?)>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Regex to match Discord roles.
    /// </summary>
    /// <remarks>Example: "People in the ＜@＆12345＞ role" -> "＜@＆12345＞" (12345)</remarks>
    public static Regex Role { get; } = new(
        @"<@&(\d+?)>",
        RegexOptions.Compiled
    );
}
