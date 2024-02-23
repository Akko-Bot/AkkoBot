namespace AkkoBot.Common;

/// <summary>
/// Groups constants that are used across the entire project.
/// </summary>
public static class AkkoConstants
{
    /// <summary>
    /// Represents the maximum message length allowed by Discord.
    /// </summary>
    public const int MaxMessageLength = 2000;

    /// <summary>
    /// Represents the maximum string length allowed in a Discord embed description.
    /// </summary>
    public const int MaxEmbedDescriptionLength = 4096;

    /// <summary>
    /// Represents the maximum string length allowed in a Discord embed field property.
    /// </summary>
    public const int MaxEmbedFieldLength = 1024;

    /// <summary>
    /// Represents the maximum string length allowed in a Discord embed title.
    /// </summary>
    public const int MaxEmbedTitleLength = 256;

    /// <summary>
    /// Represents the maximum username length allowed by Discord.
    /// </summary>
    public const int MaxUsernameLength = 32;

    /// <summary>
    /// Represents the maximum amount of embeds allowed in a message.
    /// </summary>
    public const int MaxEmbedAmount = 10;

    /// <summary>
    /// Defines how many lines a paginated embed should have.
    /// </summary>
    public const int LinesPerPage = 20;

    /// <summary>
    /// The language the response strings default to when the requested locale does not exist.
    /// </summary>
    public const string DefaultLanguage = "en-US";

    /// <summary>
    /// Represents a whitespace character that is not detected as such.
    /// </summary>
    public const string ValidWhitespace = "\u200B";

    /// <summary>
    /// Represents the terminator for any block of text that is too long to be sent.
    /// </summary>
    public const string EllipsisTerminator = "[…]";

    /// <summary>
    /// Represents the first part of a Discord guild invite link.
    /// </summary>
    public const string DiscordInviteLinkBase = "https://discord.gg/";

    /// <summary>
    /// Represents the URL to the project's repository.
    /// </summary>
    public const string RepositoryUrl = "https://github.com/Akko-Bot/AkkoBot";

    /// <summary>
    /// Represents the URL to the command list in the project's wiki.
    /// </summary>
    public const string CommandListUrl = "https://github.com/Akko-Bot/AkkoBot/wiki/Command-List";

    /// <summary>
    /// The name of the bot's author.
    /// </summary>
    public const string BotAuthor = "Kotz#7922";

    /// <summary>
    /// The current version of the bot.
    /// </summary>
    public const string BotVersion = "0.5.0-beta";
}