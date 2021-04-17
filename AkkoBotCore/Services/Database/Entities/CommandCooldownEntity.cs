using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores commands whose execution is restricted by a cooldown.")]
    public class CommandCooldownEntity : DbEntity
    {
        [Required]
        [MaxLength(200)]
        public string Command { get; init; }
        public ulong? GuildId { get; init; }
        public TimeSpan Cooldown { get; set; }
    }
}
