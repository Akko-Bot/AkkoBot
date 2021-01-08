using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    public enum BlacklistType { User, Channel, Guild }

    [Comment("Stores users, channels, and servers blacklisted from the bot.")]
    public class BlacklistEntity
    {
        [Key]
        public ulong TypeId { get; set; }

        public BlacklistType Type { get; set; }

        [Required]
        [MaxLength(32)]
        public string Name { get; set; }
    }
}
