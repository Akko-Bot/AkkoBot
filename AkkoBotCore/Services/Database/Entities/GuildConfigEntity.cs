using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores settings related to individual Discord servers.")]
    public class GuildConfigEntity : DbEntity, IMessageSettings
    {
        private string _prefix = "!";
        private string _locale = AkkoLocalizer.DefaultLanguage;
        private string _okColor = "007FFF";
        private string _errorColor = "FB3D28";

        public FilteredWordsEntity FilteredWordsRel { get; set; }
        public List<FilteredContentEntity> FilteredContentRel { get; set; }
        public List<MutedUserEntity> MutedUserRel { get; set; }
        public List<WarnEntity> WarnRel { get; set; }
        public List<WarnPunishEntity> WarnPunishRel { get; set; }
        public List<OccurrenceEntity> OccurrenceRel { get; set; }
        public List<VoiceRoleEntity> VoiceRolesRel { get; set; }
        public List<PollEntity> PollRel { get; set; }

        public ulong GuildId { get; init; }

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

        public bool PermissiveRoleMention { get; set; } = false;

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

        [MaxLength(100)]
        public string Timezone { get; set; }

        public ulong? MuteRoleId { get; set; }

        public TimeSpan WarnExpire { get; set; } = TimeSpan.FromDays(30 * 6);

        public TimeSpan? InteractiveTimeout { get; set; }

        // Greet and Bye channels and messages
        // .asar - .iam/.iamnot roles
        // .repeat
        // Xp notification
        // .rero messages

        public GuildConfigEntity()
        {
        }

        public GuildConfigEntity(BotConfigEntity config)
        {
            Prefix = config.BotPrefix;
            Locale = config.Locale;
            UseEmbed = config.UseEmbed;
            OkColor = config.OkColor;
            ErrorColor = config.ErrorColor;
        }

        /// <summary>
        /// Adds the default server punishments to this instance.
        /// </summary>
        /// <remarks>Kick at 3 warnings, ban at 5 warnings.</remarks>
        /// <returns>This instance.</returns>
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

        public override IReadOnlyDictionary<string, string> GetSettings()
        {
            var result = base.GetSettings() as Dictionary<string, string>;
            result.Remove(nameof(GuildId).ToSnakeCase());

            return result;
        }     
    }
}