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
        private string _reason;

        [Key]
        public ulong ContextId { get; init; }

        [Required]
        public BlacklistType Type { get; init; }

        [MaxLength(37)]
        public string Name
        {
            get => _name;
            init => _name = value?.MaxLength(37);
        }

        [MaxLength(200)]
        public string Reason
        {
            get => _reason;
            init => _reason = value?.MaxLength(200);
        }
    }
}
