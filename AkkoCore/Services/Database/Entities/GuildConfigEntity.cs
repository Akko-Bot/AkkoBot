using AkkoCore.Abstractions;
using AkkoCore.Common;
using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using AkkoCore.Services.Database.Abstractions;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AkkoCore.Services.Database.Entities
{
    /// <summary>
    /// Stores settings and data related to a Discord guild.
    /// </summary>
    [Comment("Stores settings and data related to a Discord server.")]
    public class GuildConfigEntity : DbEntity, IMessageSettings
    {
        private string _prefix = "!";
        private string _locale = AkkoConstants.DefaultLanguage;
        private string _okColor = "007FFF";
        private string _errorColor = "FB3D28";
        private string _banTemplate;

        /// <summary>
        /// The settings of the word filter and the words it is keeping track of.
        /// </summary>
        public FilteredWordsEntity FilteredWordsRel { get; init; }

        /// <summary>
        /// The gatekeeping settings of this Discord guild.
        /// </summary>
        public GatekeepEntity GatekeepRel { get; init; }

        /// <summary>
        /// The automatic slow mode settings of this Discord guild.
        /// </summary>
        public AutoSlowmodeEntity AutoSlowmodeRel { get; init; }

        /// <summary>
        /// The aliases associated with this Discord guild.
        /// </summary>
        public List<AliasEntity> AliasRel { get; init; }

        /// <summary>
        /// The timers associated with this Discord guild.
        /// </summary>
        public List<TimerEntity> TimerRel { get; init; }

        /// <summary>
        /// The list of content filters.
        /// </summary>
        public List<FilteredContentEntity> FilteredContentRel { get; init; }

        /// <summary>
        /// The list of muted users.
        /// </summary>
        public List<MutedUserEntity> MutedUserRel { get; init; }

        /// <summary>
        /// The list of warnings.
        /// </summary>
        public List<WarnEntity> WarnRel { get; init; }

        /// <summary>
        /// The list of warn punishments.
        /// </summary>
        public List<WarnPunishEntity> WarnPunishRel { get; init; }

        /// <summary>
        /// The list of user occurrences.
        /// </summary>
        public List<OccurrenceEntity> OccurrenceRel { get; init; }

        /// <summary>
        /// The list of voice roles.
        /// </summary>
        public List<VoiceRoleEntity> VoiceRolesRel { get; init; }

        /// <summary>
        /// The list of polls.
        /// </summary>
        public List<PollEntity> PollRel { get; init; }

        /// <summary>
        /// The list of repeaters.
        /// </summary>
        public List<RepeaterEntity> RepeaterRel { get; init; }

        /// <summary>
        /// The list of guild logs.
        /// </summary>
        public List<GuildLogEntity> GuildLogsRel { get; init; }

        /// <summary>
        /// The list of guild tags.
        /// </summary>
        public List<TagEntity> TagsRel { get; init; }

        /// <summary>
        /// The IDs of the roles that should be assigned to a Discord user when they join the guild.
        /// </summary>
        public List<long> JoinRoles { get; init; } = new();

        /// <summary>
        /// The context IDs where command messages should not be deleted, regardless of the value set in <see cref="DeleteCmdOnMessage"/>.
        /// </summary>
        public List<long> DelCmdBlacklist { get; init; } = new();

        /// <summary>
        /// The context IDs that guild logs should ignore.
        /// </summary>
        public List<long> GuildLogBlacklist { get; init; } = new();

        /// <summary>
        /// The ID of the Discord guild these settings are associated with.
        /// </summary>
        public ulong GuildId { get; init; }

        /// <summary>
        /// The prefix used in this guild.
        /// </summary>
        [Required]
        [MaxLength(15)]
        public string Prefix
        {
            get => _prefix;
            set => _prefix = value?.MaxLength(15);
        }

        /// <summary>
        /// The locale used in this guild.
        /// </summary>
        [Required]
        [MaxLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string Locale
        {
            get => _locale;
            set => _locale = value?.MaxLength(10);
        }

        /// <summary>
        /// The color to be used in response embeds.
        /// </summary>
        [Required]
        [StringLength(6)]
        [Column(TypeName = "varchar(6)")]
        public string OkColor
        {
            get => _okColor;
            set => _okColor = value?.MaxLength(6).ToUpperInvariant();
        }

        /// <summary>
        /// The color to be used in error embeds.
        /// </summary>
        [Required]
        [StringLength(6)]
        [Column(TypeName = "varchar(6)")]
        public string ErrorColor
        {
            get => _errorColor;
            set => _errorColor = value?.MaxLength(6).ToUpperInvariant();
        }

        /// <summary>
        /// Defines the template to be used on the notification message for permanent bans.
        /// </summary>
        [MaxLength(AkkoConstants.MaxMessageLength)]
        public string BanTemplate
        {
            get => _banTemplate;
            set => _banTemplate = value?.MaxLength(AkkoConstants.MaxMessageLength);
        }

        /// <summary>
        /// The time zone of this guild.
        /// </summary>
        [MaxLength(128)]
        public string Timezone { get; set; }

        /// <summary>
        /// Determines whether embeds should be used in responses or not.
        /// </summary>
        public bool UseEmbed { get; set; } = true;

        /// <summary>
        /// Determines whether role mentions should be sanitized by hierarchy (<see langword="true"/>)
        /// or by EveryoneServer permission (<see langword="false"/>).
        /// </summary>
        public bool PermissiveRoleMention { get; set; } = false;

        /// <summary>
        /// Determines whether command messages should be automatically deleted.
        /// </summary>
        public bool DeleteCmdOnMessage { get; set; } = false;

        /// <summary>
        /// Determines whether all global tags should be ignored in this guild.
        /// </summary>
        public bool IgnoreGlobalTags { get; set; } = false;

        /// <summary>
        /// Determines the minimum set of permissions required from a user for them to trigger a tag.
        /// </summary>
        public Permissions MinimumTagPermissions { get; set; } = Permissions.None;

        /// <summary>
        /// The ID of the mute role of this guild.
        /// </summary>
        public ulong? MuteRoleId { get; set; }

        /// <summary>
        /// The time for warning expirations.
        /// </summary>
        public TimeSpan WarnExpire { get; set; } = TimeSpan.FromDays(30 * 6);

        /// <summary>
        /// Defines the amount of time that an interactive command waits for user input.
        /// </summary>
        public TimeSpan? InteractiveTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Determines whether this guild has any sort of passive activity that needs to be handled by events.
        /// </summary>
        /// <remarks>
        /// This property is not mapped.
        /// It relies on this entity's navigation properties to determine whether this guild needs to handle passive activity.
        /// </remarks>
        [NotMapped]
        public bool HasPassiveActivity
            => GatekeepRel?.IsActive is true || FilteredWordsRel?.IsActive is true || PollRel.Any(x => x.Type is PollType.Anonymous)
            || FilteredContentRel?.Count is > 0 || VoiceRolesRel?.Count is > 0 || RepeaterRel?.Count is > 0 || GuildLogsRel?.Any(x => x.IsActive) is true;

        public GuildConfigEntity()
        {
        }

        public GuildConfigEntity(BotConfig config)
        {
            if (config is null)
                return;

            Prefix = config.BotPrefix;
            Locale = config.Locale;
            UseEmbed = config.UseEmbed;
            OkColor = config.OkColor;
            ErrorColor = config.ErrorColor;
        }

        public override IReadOnlyDictionary<string, string> GetSettings()
        {
            var result = base.GetSettings() as Dictionary<string, string>;

            result.Remove(nameof(GuildId).ToSnakeCase());
            result.Remove(nameof(BanTemplate).ToSnakeCase());

            return result;
        }
    }
}