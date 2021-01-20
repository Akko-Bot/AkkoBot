using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores settings related to the bot.")]
    public class BotConfigEntity : DbEntity, IMessageSettings
    {
        private string _locale = AkkoLocalizer.DefaultLanguage;
        private string _botPrefix = "!";
        private string _okColor = "007FFF";
        private string _errorColor = "FB3D28";
        private string _logFormat = "Default";

        [Key]
        public ulong BotId { get; init; }

        [Required]
        [MaxLength(6)]
        [Column(TypeName = "varchar(6)")]
        public string Locale
        {
            get => _locale;
            set => _locale = value?.MaxLength(6);
        }

        [Required]
        [MaxLength(15)]
        public string BotPrefix
        {
            get => _botPrefix;
            set => _botPrefix = value?.MaxLength(15);
        }

        [Required]
        [MaxLength(6)]
        [Column(TypeName = "varchar(6)")]
        public string OkColor
        {
            get => _okColor;
            set => _okColor = value?.MaxLength(6);
        }

        [Required]
        [MaxLength(6)]
        [Column(TypeName = "varchar(6)")]
        public string ErrorColor
        {
            get => _errorColor;
            set => _errorColor = value?.MaxLength(6);
        }

        [Required]
        public bool UseEmbed { get; set; } = true;

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string LogFormat
        {
            get => _logFormat;
            set => _logFormat = value?.MaxLength(20);
        }

        [Column(TypeName = "varchar")]
        public string LogTimeFormat { get; set; }

        [Required]
        public bool RespondToDms { get; set; } = true;

        [Required]
        public bool MentionPrefix { get; set; } = false;

        [Required]
        public bool EnableHelp { get; set; } = true;

        [Required]
        public bool CaseSensitiveCommands { get; set; } = true;

        [Required]
        public int MessageSizeCache { get; set; } = 200;

        public BotConfigEntity() { }

        public BotConfigEntity(ulong id)
            => BotId = id;

        //public HashSet<BlacklistItem> Blacklist { get; set; }

        //public HashSet<PlayingStatusItem> PlayingStatuses { get; set; }

        // Implement .gcmd and .gmod?
        // Implement forward dms to owners?
        // Might be an issue with "message staff" type of features
        public IReadOnlyDictionary<string, string> GetSettings()
        {
            var props = this.GetType().GetProperties();
            var result = new Dictionary<string, string>(props.Length);

            // Skip bot ID
            props.GetEnumerator().MoveNext();

            foreach (var prop in props)
            {
                result.TryAdd(
                    prop.Name.ToSnakeCase(),
                    prop.GetValue(this).ToString()
                );
            }

            return result;
        }
    }
}
