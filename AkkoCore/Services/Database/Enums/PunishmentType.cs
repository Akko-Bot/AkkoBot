namespace AkkoCore.Services.Database.Enums
{
    /// <summary>
    /// Represents the type of punishment to be applied to a user.
    /// </summary>
    public enum PunishmentType
    {
        /// <summary>
        /// Mutes the user.
        /// </summary>
        Mute,

        /// <summary>
        /// Kicks the user.
        /// </summary>
        Kick,

        /// <summary>
        /// Soft-bans the user.
        /// </summary>
        Softban,

        /// <summary>
        /// Bans the user.
        /// </summary>
        Ban,

        /// <summary>
        /// Adds a role to the user.
        /// </summary>
        AddRole,

        /// <summary>
        /// Removes a role from the user.
        /// </summary>
        RemoveRole
    }
}