using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoCog.AntiPhishing.AntiPhishing.Abstractions;

/// <summary>
/// Handles events related to filtering phishing links.
/// </summary>
public interface IAntiPhishingHandler
{
    /// <summary>
    /// Deletes messages that contain scam links and optionally applies a punishment to the user.
    /// </summary>
    Task FilterPhishingMessagesAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

    /// <summary>
    /// Removes scam links from user nicknames and optionally applies a punishment to the user
    /// </summary>
    Task FilterPhishingNicknamesAsync(DiscordClient client, GuildMemberUpdateEventArgs eventArgs);

    /// <summary>
    /// Punishes a joining user if their username is a scam link.
    /// </summary>
    Task FilterPhishingUserJoinAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs);
}