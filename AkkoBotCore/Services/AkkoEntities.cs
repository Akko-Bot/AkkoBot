using DSharpPlus.Entities;

namespace AkkoBot.Services
{
    /// <summary>
    /// Groups objects to be used across the entire project.
    /// </summary>
    public static class AkkoEntities
    {
        public static DiscordEmoji SuccessEmoji { get; } = DiscordEmoji.FromUnicode("✅");
        public static DiscordEmoji FailureEmoji { get; } = DiscordEmoji.FromUnicode("❌");
        public static DiscordEmoji WarningEmoji { get; } = DiscordEmoji.FromUnicode("⚠️");
    }
}