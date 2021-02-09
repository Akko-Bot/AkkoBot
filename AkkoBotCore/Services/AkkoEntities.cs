using DSharpPlus.Entities;

namespace AkkoBot.Services
{
    /// <summary>
    /// Groups objects to be used across the entire project.
    /// </summary>
    public static class AkkoEntities
    {
        /// <summary>
        /// Represents an emoji for successfull operations.
        /// </summary>
        public static DiscordEmoji SuccessEmoji { get; } = DiscordEmoji.FromUnicode("✅");

        /// <summary>
        /// Represents an emoji for failed operations.
        /// </summary>
        public static DiscordEmoji FailureEmoji { get; } = DiscordEmoji.FromUnicode("❌");

        /// <summary>
        /// Represents an emoji for warning the user that something went wrong.
        /// </summary>
        public static DiscordEmoji WarningEmoji { get; } = DiscordEmoji.FromUnicode("⚠️");
    }
}