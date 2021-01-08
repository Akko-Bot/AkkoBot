using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores settings related to individual Discord servers.")]
    public partial class GuildConfigEntity
    {
        [Key]
        public ulong GuildId { get; set; }

        [Required]
        [MaxLength(15)]
        public string Prefix { get; set; } = "!";

        public bool UseEmbed { get; set; } = true;

        [Required]
        [MaxLength(6)]
        [Column(TypeName = "varchar(6)")]
        public string OkColor { get; set; } = "007FFF";

        [Required]
        [MaxLength(6)]
        [Column(TypeName = "varchar(6)")]
        public string ErrorColor { get; set; } = "FB3D28";

        // Greet and Bye channels and messages
        // .asar - .iam/.iamnot roles
        // .fi - .sfi/.cfi Server invite filter
        // .fw - .sfq/.cfw Word filter
        // Muted users
        // .repeat and .remind
        // Warnings, warn expire and warn punishments
        // Xp notification
        // .rero messages and reactions
    }
}
