namespace AkkoBot.Services.Database.Enums
{
    /// <summary>
    /// Represents the type of an automatic command.
    /// </summary>
    public enum AutoCommandType
    {
        /// <summary>
        /// Represents an autocommand that runs once at startup.
        /// </summary>
        Startup,

        /// <summary>
        /// Represents an autocommand that runs once at a specific time.
        /// </summary>
        Scheduled,

        /// <summary>
        /// Represents an autocommand that runs multiple times at specific intervals of time.
        /// </summary>
        Repeated
    }
}