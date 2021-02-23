using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores settings related to individual Discord servers.")]
    public class GuildConfigEntity : DbEntity, IMessageSettings
    {
        private string _prefix = "!";
        private string _locale = AkkoLocalizer.DefaultLanguage;
        private string _okColor = "007FFF";
        private string _errorColor = "FB3D28";

        public List<MutedUserEntity> MutedUserRel { get; set; }
        public List<WarnEntity> WarnRel { get; set; }
        public List<WarnPunishEntity> WarnPunishRel { get; set; }
        public List<OccurrenceEntity> OccurrenceRel { get; set; }

        public ulong GuildId { get; set; }

        [Required]
        [MaxLength(15)]
        public string Prefix
        {
            get => _prefix;
            set => _prefix = value?.MaxLength(15);
        }

        [Required]
        [MaxLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string Locale
        {
            get => _locale;
            set => _locale = value?.MaxLength(10);
        }

        public bool UseEmbed { get; set; } = true;

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

        public ulong MuteRoleId { get; set; }

        public TimeSpan WarnExpire { get; set; } = TimeSpan.FromDays(30 * 6);

        public TimeSpan? InteractiveTimeout { get; set; }

        // Greet and Bye channels and messages
        // .asar - .iam/.iamnot roles
        // .fi - .sfi/.cfi Server invite filter
        // .fw - .sfq/.cfw Word filter
        // .repeat and .remind
        // Warnings, warn expire and warn punishments
        // Xp notification
        // .rero messages and reactions

        public GuildConfigEntity() { }

        public GuildConfigEntity(BotConfigEntity config)
        {
            Prefix = config.BotPrefix;
            Locale = config.Locale;
            UseEmbed = config.UseEmbed;
            OkColor = config.OkColor;
            ErrorColor = config.ErrorColor;
        }

        public GuildConfigEntity AddDefaultWarnPunishments()
        {
            var defaultPunishments = new WarnPunishEntity[]
            {
                new WarnPunishEntity()
                {
                    GuildIdFK = GuildId,
                    WarnAmount = 3,
                    Type = WarnPunishType.Kick
                },
                new WarnPunishEntity()
                {
                    GuildIdFK = GuildId,
                    WarnAmount = 5,
                    Type = WarnPunishType.Ban
                }
            };

            if (WarnPunishRel is null)
                WarnPunishRel = new(2);

            WarnPunishRel.AddRange(defaultPunishments);
            return this;
        }
    }
}