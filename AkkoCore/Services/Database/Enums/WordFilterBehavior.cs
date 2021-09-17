using System;

namespace AkkoCore.Services.Database.Enums
{
    /// <summary>
    /// Defines the additional behavior of a word filter.
    /// </summary>
    [Flags]
    public enum WordFilterBehavior
    {
        /// <summary>
        /// No additional behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// Automatically remove stickers.
        /// </summary>
        FilterSticker = 1 << 0,

        /// <summary>
        /// Automatically remove server invites.
        /// </summary>
        FilterInvite = 1 << 1,

        /// <summary>
        /// Automatically notify the offending user about the content they sent
        /// </summary>
        NotifyOnDelete = 1 << 2,

        /// <summary>
        /// Automatically punish the offending user.
        /// </summary>
        WarnOnDelete = 1 << 3
    }
}
