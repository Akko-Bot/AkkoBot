using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Entities
{
    public enum PollType { Simple, Numeric, Anonymous }

    [Comment("Stores data related to guild polls.")]
    public class PollEntity : DbEntity
    {
        public GuildConfigEntity GuildConfigRel { get; set; }
        public ulong GuildIdFK { get; init; }
        public ulong ChannelId { get; init; }
        public ulong MessageId { get; init; }
        public PollType Type { get; init; }

        [Required]
        [MaxLength(2000)]
        public string Question { get; init; }

        public string[] Answers { get; set; }

        public int[] Votes { get; set; }

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
