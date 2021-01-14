using System.ComponentModel.DataAnnotations;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Determines the type of the blacklisted entity.
    /// </summary>
    public enum BlacklistType { User, Channel, Guild }

    [Comment("Stores users, channels, and servers blacklisted from the bot.")]
    public class BlacklistEntity : DbEntity
    {
        [Key]
        public ulong ContextId { get; set; }

        public BlacklistType Type { get; set; }

        [MaxLength(32)]
        public string Name { get; set; }
    }
}
