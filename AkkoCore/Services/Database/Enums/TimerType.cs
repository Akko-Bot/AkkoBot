namespace AkkoCore.Services.Database.Enums;

/// <summary>
/// Represents the type of action this timer runs when it triggers.
/// </summary>
public enum TimerType
{
    /// <summary>
    /// Represents the timer for an autocommand.
    /// </summary>
    Command,

    /// <summary>
    /// Represents the timer for a reminder.
    /// </summary>
    Reminder,

    /// <summary>
    /// Represents the timer for a repeater.
    /// </summary>
    Repeater,

    /// <summary>
    /// Represents the timer for a scheduled unban.
    /// </summary>
    TimedBan,

    /// <summary>
    /// Represents the timer for a scheduled unmute.
    /// </summary>
    TimedMute,

    /// <summary>
    /// Represents the timer for adding a role to a Discord user.
    /// </summary>
    TimedRole,

    /// <summary>
    /// Represents the timer for removing a role from a Discord user.
    /// </summary>
    TimedUnrole,

    /// <summary>
    /// Represents the timer for removing old warnings from the database.
    /// </summary>
    TimedWarn
}