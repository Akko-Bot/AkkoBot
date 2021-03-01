using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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

        [Required]
        [MaxLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string Locale
        {
            get => _locale;
            set => _locale = value?.MaxLength(10);
        }

        [Required]
        [MaxLength(15)]
        public string BotPrefix
        {
            get => _botPrefix;
            set => _botPrefix = value?.MaxLength(15);
        }

        [Required]
        [StringLength(6)]
        [Column(TypeName = "varchar(6)")]
        public string OkColor
        {
            get => _okColor;
            set => _okColor = value?.MaxLength(6).ToUpperInvariant();
        }

        [Required]
        [StringLength(6)]
        [Column(TypeName = "varchar(6)")]
        public string ErrorColor
        {
            get => _errorColor;
            set => _errorColor = value?.MaxLength(6).ToUpperInvariant();
        }

        public bool UseEmbed { get; set; } = true;

        public bool RespondToDms { get; set; } = true;

        public bool MentionPrefix { get; set; } = false;

        public bool EnableHelp { get; set; } = true;

        public bool RotateStatus { get; set; } = false;

        public bool CaseSensitiveCommands { get; set; } = true;

        public int MessageSizeCache { get; set; } = 200;

        public TimeSpan MinWarnExpire { get; set; } = TimeSpan.FromDays(30);

        [Required]
        public TimeSpan? InteractiveTimeout { get; set; } = TimeSpan.FromSeconds(30);

        // Implement .gcmd and .gmod?
        // Implement forward dms to owners?
        // Might be an issue for "message staff" type of features
    }
}
