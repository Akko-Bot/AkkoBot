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
    /// Deletes scam links and optionally applies a punishment to the user.
    /// </summary>
    Task FilterPhishingLinksAsync(DiscordClient client, MessageCreateEventArgs eventArgs);
}
