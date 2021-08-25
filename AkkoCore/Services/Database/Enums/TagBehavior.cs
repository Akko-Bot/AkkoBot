using System;

namespace AkkoCore.Services.Database.Enums
{
    /// <summary>
    /// Defines the action to be taken when a tag is triggered.
    /// </summary>
    [Flags]
    public enum TagBehavior
    {
        /// <summary>
        /// No additional behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// Delete the message that triggered the tag.
        /// </summary>
        Delete = 1,

        /// <summary>
        /// Invoke the tag if the trigger is used anywhere in the message.
        /// </summary>
        Anywhere = 1 << 1,

        /// <summary>
        /// Send the tag in direct message.
        /// </summary>
        DirectMessage = 1 << 2,

        /// <summary>
        /// Sanitizes role pings.
        /// </summary>
        SanitizeRolePing = 1 << 3
    }
}
