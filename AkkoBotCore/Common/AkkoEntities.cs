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
        /// Defines for how long the bot should wait between API calls when performing several of them in sequence.
        /// </summary>
        public static TimeSpan SafetyDelay { get; } = TimeSpan.FromSeconds(0.6);

        /// <summary>
        /// Defines the maximum age a message needs in order to be elegible for deletion.
        /// </summary>
        public static TimeSpan MaxMessageDeletionAge { get; } = TimeSpan.FromDays(14);

        public static string[] SupportedEmojiFormats { get; } = { "png", "gif" };
    }
}