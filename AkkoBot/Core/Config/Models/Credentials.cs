namespace AkkoBot.Core.Config.Models;

/// <summary>
/// A class that represents a credentials file.
/// </summary>
public sealed class Credentials
{
    /// <summary>
    /// Contains the IDs of the bot owners.
    /// </summary>
    public ConcurrentHashSet<ulong> OwnerIds { get; init; } = [ default ];

    /// <summary>
    /// The token used to connect to Discord.
    /// </summary>
    public string Token { get; init; } = "paste_your_token_here";
}