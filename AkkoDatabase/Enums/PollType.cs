namespace AkkoDatabase.Enums
{
    /// <summary>
    /// Represents the type of a poll.
    /// </summary>
    public enum PollType
    {
        /// <summary>
        /// Represents a "yes or no" poll.
        /// </summary>
        Simple,

        /// <summary>
        /// Represents a poll with up to 10 options that are voted on through reactions.
        /// </summary>
        Numeric,

        /// <summary>
        /// Represents a poll with unlimited options that are voted through Discord messages.
        /// </summary>
        Anonymous
    }
}