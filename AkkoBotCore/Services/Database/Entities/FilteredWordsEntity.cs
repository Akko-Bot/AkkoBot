using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores filtered words of a Discord server.")]
    public class FilteredWordsEntity : DbEntity
    {
        public GuildConfigEntity GuildConfigRel { get; set; }

        public ulong GuildIdFK { get; init; }

        public List<string> Words { get; init; } = new();

        public List<long> IgnoredIds { get; init; } = new(); // Postgres does not support unsigned types for collections :(

        [MaxLength(2000)]
        public string NotificationMessage { get; set; }

        public bool Enabled { get; set; } = true;

        public bool FilterStickers { get; set; } = false;

        public bool NotifyOnDelete { get; set; }

        public bool WarnOnDelete { get; set; }
    }
}