using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores data and settings related to individual Discord users.")]
    public class DiscordUserEntity : DbEntity
    {
        [Key]
        public ulong UserId { get; set; }

        [Required]
        [MaxLength(32)]
        public string Username { get; set; }

        [Required]
        [StringLength(4)]
        [Column(TypeName = "varchar(4)")]
        public string Discriminator { get; set; }



        // Global xp, maybe?
        // Xp gain tick
        // Xp notification override?

        public override string ToString()
            => $"{Username}#{Discriminator}";
    }
}
