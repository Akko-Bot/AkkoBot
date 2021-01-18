using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores settings related to individual Discord servers.")]
    public class GuildConfigEntity : DbEntity
    {
        private string _prefix = "!";
        private string _locale = AkkoLocalizer.DefaultLanguage;
        private string _okColor = "007FFF";
        private string _errorColor = "FB3D28";

        [Key]
        public ulong GuildId { get; set; }

        [Required]
        [MaxLength(15)]
        public string Prefix
        {
            get => _prefix;
            set => _prefix = value.MaxLength(15);
        }

        [Required]
        [MaxLength(6)]
        [Column(TypeName = "varchar(6)")]
        public string Locale
        {
            get => _locale;
            set => _locale = value.MaxLength(6);
        }

        [Required]
        public bool UseEmbed { get; set; } = true;

        [Required]
        [MaxLength(6)]
        [Column(TypeName = "varchar(6)")]
        public string OkColor
        {
            get => _okColor;
            set => _okColor = value.MaxLength(6);
        }

        [Required]
        [MaxLength(6)]
        [Column(TypeName = "varchar(6)")]
        public string ErrorColor
        {
            get => _errorColor;
            set => _errorColor = value.MaxLength(6);
        }

        // Greet and Bye channels and messages
        // .asar - .iam/.iamnot roles
        // .fi - .sfi/.cfi Server invite filter
        // .fw - .sfq/.cfw Word filter
        // Muted users
        // .repeat and .remind
        // Warnings, warn expire and warn punishments
        // Xp notification
        // .rero messages and reactions

        public GuildConfigEntity() { }
        
        public GuildConfigEntity(DiscordGuild guild = null)
            => GuildId = guild?.Id ?? default;
    }
}
