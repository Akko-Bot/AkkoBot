using AkkoCore.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoCore.Services.Database.Entities
{
    /// <summary>
    /// Stores a voice chat role.
    /// </summary>
    [Comment("Stores a voice chat role.")]
    public class VoiceRoleEntity : DbEntity
    {
        /// <summary>
        /// The settings of the Discord guild this voice role is associated with.
        /// </summary>
        public GuildConfigEntity? GuildConfigRel { get; init; }

        /// <summary>
        /// The ID of the Discord guild this voice role is associated with.
        /// </summary>
        public ulong GuildIdFk { get; init; }

        /// <summary>
        /// The ID of the Discord voice channel this voice role is associated with.
        /// </summary>
        public ulong ChannelId { get; init; }

        /// <summary>
        /// The ID of the Discord role this voice role is associated with.
        /// </summary>
        public ulong RoleId { get; init; }
    }
}