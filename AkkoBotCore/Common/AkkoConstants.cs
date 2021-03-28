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
        public const int MessageMaxLength = 2000;

        /// <summary>
        /// Represents the maximum string length allowed in a Discord embed property.
        /// </summary>
        public const int EmbedPropMaxLength = 2048;

        /// <summary>
        /// Represents the maximum string length allowed in a Discord embed field property.
        /// </summary>
        public const int EmbedFieldMaxLength = 1024;

        /// <summary>
        /// Represents the maximum string length allowed in a Discord embed title.
        /// </summary>
        public const int EmbedTitleMaxLength = 256;

        /// <summary>
        /// Represents the maximum username length allowed by Discord.
        /// </summary>
        public const int MaxUsernameLength = 32;

        /// <summary>
        /// Defines how many lines a paginated embed should have.
        /// </summary>
        public const int LinesPerPage = 20;
    }
}