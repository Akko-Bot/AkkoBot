using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores data about users that got muted in a specific server.")]
    public class MutedUserEntity : DbEntity
    {
        public GuildConfigEntity GuildConfigRel { get; set; }

        public ulong GuildIdFK { get; init; }
        public ulong UserId { get; init; }
    }
}