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
    /// <summary>
    /// Stores settings related to the bot.
    /// </summary>
    [Comment("Stores settings related to the bot.")]
    public class BotConfigEntity : DbEntity, IMessageSettings
    {
        private string _locale = AkkoLocalizer.DefaultLanguage;
        private string _botPrefix = "!";
        private string _okColor = "007FFF";
        private string _errorColor = "FB3D28";

        /// <summary>
        /// Groups the qualified name of all commands that have been disabled.
        /// </summary>
        public List<string> DisabledCommands { get; init; } = new();

        /// <summary>
        /// The default bot locale.
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
        /// The default bot prefix.
        /// </summary>
        [Required]
        [MaxLength(15)]
        public string BotPrefix
        {
            get => _botPrefix;
            set => _botPrefix = value?.MaxLength(15);
        }

        /// <summary>
        /// The default color for embeds.
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
        /// The default color for error embeds.
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
        /// Defines whether embeds should be used in responses by default.
        /// </summary>
        public bool UseEmbed { get; set; } = true;

        /// <summary>
        /// Defines whether the bot responds to commands in direct message.
        /// </summary>
        public bool RespondToDms { get; set; } = true;

        /// <summary>
        /// Defines whether the bot responds to commands prefixed with a mention to the bot.
        /// </summary>
        public bool MentionPrefix { get; set; } = false;

        /// <summary>
        /// Defines whether the bot should respond to help commands.
        /// </summary>
        public bool EnableHelp { get; set; } = true;

        /// <summary>
        /// Defines whether the bot has statuses in rotation.
        /// </summary>
        public bool RotateStatus { get; set; } = false;

        /// <summary>
        /// Defines whether commands are case sensitive or not.
        /// </summary>
        public bool CaseSensitiveCommands { get; set; } = true;

        /// <summary>
        /// Defines the message cache size for every <see cref="DSharpPlus.DiscordClient"/>.
        /// </summary>
        public int MessageSizeCache { get; set; } = 200;

        /// <summary>
        /// Defines the minimum amount of time that guilds are allowed to set automatic expirations of warnings.
        /// </summary>
        public TimeSpan MinWarnExpire { get; set; } = TimeSpan.FromDays(30);

        /// <summary>
        /// Defines the maximum amount of time that an interactive command waits for user input.
        /// </summary>
        [Required]
        public TimeSpan? InteractiveTimeout { get; set; } = TimeSpan.FromSeconds(30);

        // Implement forward dms to owners?
        // Might be an issue for "message staff" type of features
    }
}