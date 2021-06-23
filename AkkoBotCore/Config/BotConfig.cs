using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization;
using ConcurrentCollections;
using System;
using System.Collections.Generic;

namespace AkkoBot.Config
{
    /// <summary>
    /// Stores settings related to the bot.
    /// </summary>
    public class BotConfig : Settings, IMessageSettings
    {
        private string _locale = AkkoLocalizer.DefaultLanguage;
        private string _okColor = "007FFF";
        private string _errorColor = "FB3D28";
        private TimeSpan _bulkGatekeepingTime = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Groups the qualified name of all commands that have been disabled.
        /// </summary>
        public ConcurrentHashSet<string> DisabledCommands { get; init; } = new();

        /// <summary>
        /// The default bot locale.
        /// </summary>
        public string Locale
        {
            get => _locale;
            set => _locale = value?.MaxLength(10);
        }

        /// <summary>
        /// The default color for embeds.
        /// </summary>
        public string OkColor
        {
            get => _okColor;
            set => _okColor = value?.MaxLength(6).ToUpperInvariant();
        }

        /// <summary>
        /// The default color for error embeds.
        /// </summary>
        public string ErrorColor
        {
            get => _errorColor;
            set => _errorColor = value?.MaxLength(6).ToUpperInvariant();
        }

        /// <summary>
        /// Defines how long the bot waits on bulk greetings and farewells.
        /// </summary>
        public TimeSpan BulkGatekeepTime
        {
            get => _bulkGatekeepingTime;
            set => _bulkGatekeepingTime = (value <= TimeSpan.Zero) ? TimeSpan.FromSeconds(5) : value;
        }

        /// <summary>
        /// The default bot prefix.
        /// </summary>
        public string BotPrefix { get; set; } = "!";

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
        public TimeSpan? InteractiveTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public override IReadOnlyDictionary<string, string> GetSettings()
        {
            var result = base.GetSettings() as Dictionary<string, string>;
            result[nameof(DisabledCommands).ToSnakeCase()] = string.Join(", ", DisabledCommands);

            return result;
        }
    }
}