using DSharpPlus.Entities;
using System;

namespace AkkoBot.Common
{
    /// <summary>
    /// Groups objects to be used across the entire project.
    /// </summary>
    public static class AkkoEntities
    {
        /// <summary>
        /// Represents an emoji for successful operations.
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

        /// <summary>
        /// Represents an emoji for warning the user that the command they tried to run is on a cooldown.
        /// </summary>
        public static DiscordEmoji CooldownEmoji { get; } = DiscordEmoji.FromUnicode("⏰");

        /// <summary>
        /// Represents a thumbs up emoji.
        /// </summary>
        public static DiscordEmoji ThumbsUpEmoji { get; } = DiscordEmoji.FromUnicode("👍");

        /// <summary>
        /// Represents a thumbs down emoji.
        /// </summary>
        public static DiscordEmoji ThumbsDownEmoji { get; } = DiscordEmoji.FromUnicode("👎");

        /// <summary>
        /// Represents all numeric emojis from 1 to 10.
        /// </summary>
        public static DiscordEmoji[] NumericEmojis { get; } = new DiscordEmoji[10]
        {
            DiscordEmoji.FromUnicode("1️⃣"),
            DiscordEmoji.FromUnicode("2️⃣"),
            DiscordEmoji.FromUnicode("3️⃣"),
            DiscordEmoji.FromUnicode("4️⃣"),
            DiscordEmoji.FromUnicode("5️⃣"),
            DiscordEmoji.FromUnicode("6️⃣"),
            DiscordEmoji.FromUnicode("7️⃣"),
            DiscordEmoji.FromUnicode("8️⃣"),
            DiscordEmoji.FromUnicode("9️⃣"),
            DiscordEmoji.FromUnicode("🔟")
        };

        /// <summary>
        /// Defines for how long the bot should wait between API calls when performing several of them in sequence.
        /// </summary>
        public static TimeSpan SafetyDelay { get; } = TimeSpan.FromSeconds(0.6);

        /// <summary>
        /// Defines the maximum age a message needs in order to be elegible for deletion.
        /// </summary>
        public static TimeSpan MaxMessageDeletionAge { get; } = TimeSpan.FromDays(14);

        /// <summary>
        /// Defines the image formats supported for emojis.
        /// </summary>
        /// <value>"png", "gif".</value>
        public static string[] SupportedEmojiFormats { get; } = { "png", "gif" };
    }
}