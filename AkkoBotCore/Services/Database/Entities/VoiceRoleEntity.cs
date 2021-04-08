using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores a voice chat role.")]
    public class VoiceRoleEntity : DbEntity
    {
        public GuildConfigEntity GuildConfigRel { get; set; }

        public ulong GuildIdFk { get; set; }
        public ulong ChannelId { get; set; }
        public ulong RoleId { get; set; }
    }
}