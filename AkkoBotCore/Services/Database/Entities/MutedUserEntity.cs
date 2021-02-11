using System;
using AkkoBot.Services.Database.Abstractions;

namespace AkkoBot.Services.Database.Entities
{
    public class MutedUserEntity : DbEntity
    {
        public GuildConfigEntity GuildConfigRel { get; set; }

        public ulong GuildIdFK { get; init; }
        public ulong UserId { get; init; }
        public DateTimeOffset ElapseAt { get; set; }
    }
}