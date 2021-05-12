using AkkoBot.Common;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Represents the type of a poll.
    /// </summary>
    public enum PollType
    {
        /// <summary>
        /// Represents a "yes or no" poll.
        /// </summary>
        Simple,

        /// <summary>
        /// Represents a poll with up to 10 options that are voted on through reactions.
        /// </summary>
        Numeric,

        /// <summary>
        /// Represents a poll with unlimited options that are voted through Discord messages.
        /// </summary>
        Anonymous
    }

    /// <summary>
    /// Stores data related to a Discord guild poll.
    /// </summary>
    [Comment("Stores data related to a server poll.")]
    public class PollEntity : DbEntity
    {
        /// <summary>
        /// The settings of the Discord guild this poll is associated with.
        /// </summary>
        public GuildConfigEntity GuildConfigRel { get; init; }

        /// <summary>
        /// The ID of the Discord guild this poll is associated with.
        /// </summary>
        public ulong GuildIdFK { get; init; }

        /// <summary>
        /// The ID of the Discord channel this poll is associated with.
        /// </summary>
        public ulong ChannelId { get; init; }

        /// <summary>
        /// The ID of the Discord message used as reference for the poll.
        /// </summary>
        public ulong MessageId { get; init; }

        /// <summary>
        /// The type of this poll.
        /// </summary>
        public PollType Type { get; init; }

        /// <summary>
        /// The question of this poll.
        /// </summary>
        [Required]
        [MaxLength(AkkoConstants.MessageMaxLength)]
        public string Question { get; init; }

        /// <summary>
        /// The possible answers for this poll.
        /// </summary>
        public string[] Answers { get; init; }

        /// <summary>
        /// The votes that have been cast to this poll.
        /// </summary>
        public int[] Votes { get; init; }

        /// <summary>
        /// The ID of the Discord users that have voted on this poll.
        /// </summary>
        public List<long> Voters { get; init; } = new(); // Postgres doesn't support unsigned types for collections

        /// <summary>
        /// Gets the message this database entry represents.
        /// </summary>
        /// <param name="server">The Discord guild the poll is from.</param>
        /// <returns>The message that containst the poll, <see langword="null"/> if it's not found.</returns>
        public async Task<DiscordMessage> GetPollMessageAsync(DiscordGuild server)
        {
            if (server.Id != GuildIdFK)
                return null;

            server.Channels.TryGetValue(ChannelId, out var channel);

            try { return await channel?.GetMessageAsync(MessageId); }
            catch { return null; }
        }
    }
}