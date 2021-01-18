using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores settings related to the bot.")]
    public class BotConfigEntity : DbEntity
    {
        private string _defaultPrefix = "!";
        private string _logFormat = "Default";

        [Required]
        [MaxLength(15)]
        public string DefaultPrefix
        {
            get => _defaultPrefix;
            set => _defaultPrefix = value.MaxLength(15);
        }

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string LogFormat
        {
            get => _logFormat;
            set => _logFormat = value.MaxLength(20);
        }

        [Column(TypeName = "varchar")]
        public string LogTimeFormat { get; set; }

        [Required]
        public bool RespondToDms { get; set; } = true;

        [Required]
        public bool CaseSensitiveCommands { get; set; } = true;

        [Required]
        public int MessageSizeCache { get; set; } = 200;

        //public HashSet<BlacklistItem> Blacklist { get; set; }

        //public HashSet<PlayingStatusItem> PlayingStatuses { get; set; }

        // Implement .gcmd and .gmod?
        // Implement forward dms to owners?
        // Might be an issue with "message staff" type of features
    }
}
