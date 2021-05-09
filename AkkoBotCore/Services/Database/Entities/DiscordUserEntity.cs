using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores data and settings related to individual Discord users.")]
    public class DiscordUserEntity : DbEntity
    {
        public ulong UserId { get; set; }

        [Required]
        [MaxLength(32)]
        public string Username { get; set; }

        [Required]
        [StringLength(4)]
        [Column(TypeName = "varchar(4)")]
        public string Discriminator { get; set; }

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