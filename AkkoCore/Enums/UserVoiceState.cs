namespace AkkoCore.Enums;

/// <summary>
/// Represents the connection status of a Discord user when they interact with voice channels.
/// </summary>
public enum UserVoiceState
{
    /// <summary>
    /// Represents a user who has connected to a voice channel.
    /// </summary>
    Connected,

    /// <summary>
    /// Represents a user who has moved from voice channels.
    /// </summary>
    Moved,

    /// <summary>
    /// Represents a user who has disconnected from a voice channel.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Represents a user that temporarily lost connection to a voice channel
    /// or that received/lost a voice mute/deafen.
    /// </summary>
    Reconnected
}