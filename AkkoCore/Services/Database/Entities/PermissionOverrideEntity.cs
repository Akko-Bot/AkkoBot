using AkkoCore.Extensions;
using AkkoCore.Services.Database.Abstractions;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoCore.Services.Database.Entities
{
    /// <summary>
    /// Stores data related to permission overrides for commands.
    /// </summary>
    [Comment("Stores data related to permission overrides for commands.")]
    public class PermissionOverrideEntity : DbEntity
    {
        private string _command;

        /// <summary>
        /// The settings of the Discord guild this command permission override is associated with.
        /// </summary>
        public GuildConfigEntity GuildConfigRel { get; init; }
        
        /// <summary>
        /// The list of user IDs allowed to run this command.
        /// </summary>
        public List<long> AllowedUserIds { get; init; } = new();

        /// <summary>
        /// The list of channel IDs allowed to run this command.
        /// </summary>
        public List<long> AllowedChannelIds { get; init; } = new();

        /// <summary>
        /// The list of role IDs allowed to run this command.
        /// </summary>
        public List<long> AllowedRoleIds { get; init; } = new();

        /// <summary>
        /// The ID of the Discord guild this command permission override is associated with.
        /// </summary>
        /// <remarks>This property is <see langword="null"/> if the override is global.</remarks>
        public ulong? GuildIdFK { get; init; }

        /// <summary>
        /// The qualified of the command this permission override is mapped to.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Command
        {
            get => _command;
            init => _command = value?.MaxLength(200);
        }

        /// <summary>
        /// The permissions necessary to run this command.
        /// </summary>
        public Permissions Permissions { get; set; } = Permissions.None;

        /// <summary>
        /// Defines whether this override is active or not.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Defines whether this override contains any allowed ID.
        /// </summary>
        /// <remarks>This property is not mapped.</remarks>
        [NotMapped]
        public bool HasActiveIds
            => AllowedUserIds.Count is not 0 || AllowedRoleIds.Count is not 0 || AllowedChannelIds.Count is not 0;
    }
}
