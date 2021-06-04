using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events.Abstractions
{
    /// <summary>
    /// Represents an object that handles guild events.
    /// </summary>
    public interface IGuildEventsHandler
    {
        /// <summary>
        /// Deletes a message that doesn't contain a certain type of content.
        /// </summary>
        Task FilterContentAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

        /// <summary>
        /// Deletes a user message if it contains a server invite.
        /// </summary>
        Task<bool> FilterInviteAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

        /// <summary>
        /// Deletes a user message if it contains a sticker.
        /// </summary>
        Task<bool> FilterStickerAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

        /// <summary>
        /// Deletes a user message if it contains a filtered word.
        /// </summary>
        Task<bool> FilterWordAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

        /// <summary>
        /// Registers votes for anonymous polls.
        /// </summary>
        Task PollVoteAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

        /// <summary>
        /// Mutes a user that has been previously muted.
        /// </summary>
        Task RemuteAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs);

        /// <summary>
        /// Sanitizes username on user join.
        /// </summary>
        Task SanitizeNameOnJoinAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs);

        /// <summary>
        /// Sanitizes user nickname on user update.
        /// </summary>
        Task SanitizeNameOnUpdateAsync(DiscordClient client, GuildMemberUpdateEventArgs eventArgs);
    }
}