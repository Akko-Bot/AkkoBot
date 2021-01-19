using System.ComponentModel.DataAnnotations;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Determines the type of the blacklisted entity.
    /// </summary>
    public enum BlacklistType { User, Channel, Server, Unspecified }

    [Comment("Stores users, channels, and servers blacklisted from the bot.")]
    public class BlacklistEntity : DbEntity
    {
        private string _name;

        [Key]
        public ulong ContextId { get; set; }

        [Required]
        public BlacklistType Type { get; set; }

        [MaxLength(37)]
        public string Name
        {
            get => _name;
            set => _name = value?.MaxLength(37);
        }
    }
}
