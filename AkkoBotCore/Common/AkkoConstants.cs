namespace AkkoBot.Common
{
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
        public const int MaxEmbedDescriptionLength = 2048;

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
        /// Defines how many lines a paginated embed should have.
        /// </summary>
        public const int LinesPerPage = 20;

        /// <summary>
        /// Represents a whitespace character that is not detected as such.
        /// </summary>
        public const string ValidWhitespace = "\u200B";
    }
}