namespace AkkoCore.Common
{
    /// <summary>
    /// Defines the format used for Discord markdown timestamps.
    /// </summary>
    public enum DiscordTimestamp
    {
        /// <summary>
        /// Sample: "July 3, 2021 1:13 AM"
        /// </summary>
        ShortDateAndTime = 'f',

        /// <summary>
        /// Sample: "Saturday, July 3, 2021 1:13 AM"
        /// </summary>
        LongDateAndTime = 'F',

        /// <summary>
        /// Sample: "07/03/2021"
        /// </summary>
        ShortDate = 'd',

        /// <summary>
        /// Sample: "July 3, 2021"
        /// </summary>
        LongDate = 'D',

        /// <summary>
        /// Sample: "1:13 AM"
        /// </summary>
        ShortTime = 't',

        /// <summary>
        /// Sample: "1:13:15 AM"
        /// </summary>
        LongTime = 'T',

        /// <summary>
        /// Sample: "3 days ago"
        /// </summary>
        RelativeTime = 'R'
    }
}