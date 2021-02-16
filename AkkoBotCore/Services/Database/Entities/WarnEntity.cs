using System.ComponentModel.DataAnnotations;
using System;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    public enum WarnType { Notice, Warning }

    [Comment("Stores warnings issued to users on servers.")]
    public class WarnEntity : DbEntity
    {
        private readonly string _note;

        public GuildConfigEntity GuildConfigRel { get; set; }

        public ulong GuildIdFK { get; init; }
        public ulong UserId { get; init; }
        public ulong AuthorId { get; init; }
        public WarnType Type { get; init; }

        [MaxLength(2000)]
        public string WarningText
        {
            get => _note;
            init => _note = value.MaxLength(2000) ?? "-";
        }
    }
}
