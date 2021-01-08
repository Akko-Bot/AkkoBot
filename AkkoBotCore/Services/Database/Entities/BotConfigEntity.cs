using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores settings related to the bot.")]
    public partial class BotConfigEntity
    {
        [Required]
        [MaxLength(15)]
        public string DefaultPrefix { get; set; } = "!";

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string LogFormat { get; set; } = "Default";

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
