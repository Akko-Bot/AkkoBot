using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Stores data related to individual Discord users.
    /// </summary>
    [Comment("Stores data related to individual Discord users.")]
    public class DiscordUserEntity : DbEntity
    {
        /// <summary>
        /// The ID of the Discord user.
        /// </summary>
        public ulong UserId { get; init; }

        /// <summary>
        /// The username of the Discord user.
        /// </summary>
        [Required]
        [MaxLength(32)]
        public string Username { get; set; }

        /// <summary>
        /// The discriminator of the Discord user.
        /// </summary>
        [Required]
        [StringLength(4)]
        [Column(TypeName = "varchar(4)")]
        public string Discriminator { get; set; }

        /// <summary>
        /// The username and discriminator of the Discord user.
        /// </summary>
        /// <remarks>This property is not mapped.</remarks>
        [NotMapped]
        public string FullName
            => $"{Username}#{Discriminator}";

        public DiscordUserEntity()
        {
        }

        public DiscordUserEntity(DiscordUser user)
        {
            UserId = user.Id;
            Username = user.Username;
            Discriminator = user.Discriminator;
        }

        /* Overrides */

        public override string ToString()
            => $"{Username}#{Discriminator}";

        public static bool operator ==(DiscordUserEntity x, DiscordUserEntity y)
            => (x.UserId == y.UserId && x.Username == y.Username && x.Discriminator == y.Discriminator);

        public static bool operator !=(DiscordUserEntity x, DiscordUserEntity y)
            => !(x == y);

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj) || (obj is not null && obj is DiscordUserEntity dbUser && this == dbUser);

        public override int GetHashCode()
            => base.GetHashCode();
    }
}