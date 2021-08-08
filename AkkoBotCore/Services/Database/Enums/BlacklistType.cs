namespace AkkoBot.Services.Database.Enums
{
    /// <summary>
    /// Determines the type of the blacklisted entity.
    /// </summary>
    public enum BlacklistType
    {
        /// <summary>
        /// Represents a Discord user.
        /// </summary>
        User,

        /// <summary>
        /// Represents a Discord channel.
        /// </summary>
        Channel,

        /// <summary>
        /// Represents a Discord guild.
        /// </summary>
        Server,

        /// <summary>
        /// Represents a blacklisted entity that was not specified.
        /// </summary>
        Unspecified
    }
}